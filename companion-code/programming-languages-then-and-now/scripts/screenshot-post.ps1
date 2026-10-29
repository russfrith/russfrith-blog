<#
Screenshot helper for this post, built during Phase 1e/1f's rendering work.
Uses the Edge already installed on this machine (headless, remote debugging via
the Chrome DevTools Protocol) -- no new software installed. Captures a single
element (by CSS selector, or by a text needle inside a container selector) as a
PNG, cropped locally rather than via CDP's `clip` (which was found, during this
work, to return a blank image once the page has scrolled -- see the comment
below). Run this before publishing to spot-check rendering changes.

Usage (from companion-code/programming-languages-then-and-now):
  ./scripts/screenshot-post.ps1 -Url "http://127.0.0.1:1313/posts/programming-languages-then-and-now/" -ContainsText "WARNING: DATA RACE" -OutFile "race.png" -Dark
  ./scripts/screenshot-post.ps1 -Url "..." -Selector "aside" -OutFile "aside.png" -Width 390

Requires a Hugo dev server already running (`hugo server -D` from the repo
root). Launches its own dedicated headless Edge instance with remote debugging
on -Port (default 9222) if one isn't already listening there -- it does not
touch any Edge window you already have open.

Output goes to .screenshots/ by default (gitignored at the repo root -- see
.gitignore), never committed.
#>
param(
    [Parameter(Mandatory)] [string]$Url,
    [string]$Selector,
    [string]$ContainsText,
    [string]$ContainsTextSelector = ".highlight",
    [Parameter(Mandatory)] [string]$OutFile,
    [int]$Width = 1280,
    [switch]$Dark,
    [int]$Pad = 16,
    [int]$MaxHeight = 1600,
    [int]$Port = 9222,
    [string]$OutDir
)

$ErrorActionPreference = "Stop"

if (-not $OutDir) {
    $repoRoot = Resolve-Path (Join-Path $PSScriptRoot "../../..")
    $OutDir = Join-Path $repoRoot ".screenshots"
}
New-Item -ItemType Directory -Force -Path $OutDir | Out-Null
$OutFile = Join-Path $OutDir $OutFile

# Launch a dedicated headless Edge with remote debugging if nothing is listening
# on $Port yet -- its own profile, so it never touches a real Edge window.
try {
    Invoke-RestMethod -Uri "http://127.0.0.1:$Port/json/version" -TimeoutSec 2 | Out-Null
} catch {
    $edgeCandidates = @(
        "C:\Program Files (x86)\Microsoft\Edge\Application\msedge.exe"
        "C:\Program Files\Microsoft\Edge\Application\msedge.exe"
    )
    $edge = $edgeCandidates | Where-Object { Test-Path $_ } | Select-Object -First 1
    if (-not $edge) { throw "Microsoft Edge not found at the usual install paths; adjust `$edgeCandidates in this script." }
    $profileDir = Join-Path $OutDir "edge-profile"
    New-Item -ItemType Directory -Force -Path $profileDir | Out-Null
    Start-Process -FilePath $edge -ArgumentList "--headless", "--disable-gpu", "--remote-debugging-port=$Port", "--user-data-dir=$profileDir", "--no-first-run", "about:blank" -WindowStyle Hidden
    Start-Sleep -Seconds 2
}

Add-Type -AssemblyName System.Net.WebSockets.Client -ErrorAction SilentlyContinue
Add-Type -AssemblyName System.Drawing

function New-CdpTarget {
    Invoke-RestMethod -Uri "http://127.0.0.1:$Port/json/new?about:blank" -Method PUT
}

function Connect-Cdp($wsUrl) {
    $ws = New-Object System.Net.WebSockets.ClientWebSocket
    $cts = New-Object System.Threading.CancellationTokenSource
    $ws.ConnectAsync([Uri]$wsUrl, $cts.Token).Wait()
    return $ws
}

function Send-Cdp($ws, $method, $params, [ref]$idCounter) {
    $id = $idCounter.Value
    $idCounter.Value++
    $msg = @{ id = $id; method = $method; params = $params } | ConvertTo-Json -Depth 10 -Compress
    $bytes = [System.Text.Encoding]::UTF8.GetBytes($msg)
    $seg = New-Object System.ArraySegment[byte] -ArgumentList @(,$bytes)
    $ws.SendAsync($seg, [System.Net.WebSockets.WebSocketMessageType]::Text, $true, [System.Threading.CancellationToken]::None).Wait()
    return $id
}

function Receive-CdpUntil($ws, $predicate, $timeoutSec = 15) {
    $chunkSize = 1MB
    $buffer = New-Object byte[] $chunkSize
    $seg = New-Object System.ArraySegment[byte] -ArgumentList @(,$buffer)
    $sw = [System.Diagnostics.Stopwatch]::StartNew()
    while ($sw.Elapsed.TotalSeconds -lt $timeoutSec) {
        $ms = New-Object System.IO.MemoryStream
        do {
            $resultTask = $ws.ReceiveAsync($seg, [System.Threading.CancellationToken]::None)
            if (-not $resultTask.Wait(2000)) { $ms = $null; break }
            $result = $resultTask.Result
            $ms.Write($buffer, 0, $result.Count)
        } while ($result -and -not $result.EndOfMessage)
        if (-not $ms) { continue }
        $text = [System.Text.Encoding]::UTF8.GetString($ms.ToArray())
        $obj = $text | ConvertFrom-Json
        if (& $predicate $obj) { return $obj }
    }
    throw "CDP: timed out waiting for condition"
}

# --- main ---
$target = New-CdpTarget
$ws = Connect-Cdp $target.webSocketDebuggerUrl
$idc = 0

Send-Cdp $ws "Page.enable" @{} ([ref]$idc) | Out-Null
Receive-CdpUntil $ws { param($o) $o.id -eq 0 } | Out-Null

Send-Cdp $ws "Emulation.setEmulatedMedia" @{ features = @(@{ name = "prefers-color-scheme"; value = $(if ($Dark) { "dark" } else { "light" }) }) } ([ref]$idc) | Out-Null
Receive-CdpUntil $ws { param($o) $true } | Out-Null

Send-Cdp $ws "Emulation.setDeviceMetricsOverride" @{ width = $Width; height = $MaxHeight; deviceScaleFactor = 2; mobile = ($Width -lt 500) } ([ref]$idc) | Out-Null
Receive-CdpUntil $ws { param($o) $true } | Out-Null

Send-Cdp $ws "Page.navigate" @{ url = $Url } ([ref]$idc) | Out-Null
Receive-CdpUntil $ws { param($o) $o.method -eq "Page.loadEventFired" } 30 | Out-Null
Start-Sleep -Milliseconds 500

# Selectors starting with # use getElementById (safe for ids starting with digits,
# which are invalid in querySelector's CSS id syntax without escaping -- this
# post's scenario headings are "#1-absent-or-null-value" etc).
if ($ContainsText) {
    $textJson = $ContainsText | ConvertTo-Json
    $containerSelJson = $ContainsTextSelector | ConvertTo-Json
    $lookup = "(function(){var nodes=document.querySelectorAll(" + $containerSelJson + "); var needle=" + $textJson + "; for (var i=0;i<nodes.length;i++){ if(nodes[i].textContent.indexOf(needle)!==-1) return nodes[i]; } return null;})()"
} elseif ($Selector.StartsWith("#")) {
    $idJson = ($Selector.Substring(1)) | ConvertTo-Json
    $lookup = "document.getElementById(" + $idJson + ")"
} else {
    $selJson = $Selector | ConvertTo-Json
    $lookup = "document.querySelector(" + $selJson + ")"
}

# Page.captureScreenshot's `clip` parameter was found, during this work, to return
# a BLANK image once the page has scrolled (confirmed via multiple isolated repros:
# an unclipped viewport screenshot at the same scroll position shows the real
# content fine; adding `clip` -- with or without captureBeyondViewport, at any y --
# comes back blank). Workaround: screenshot the full viewport and crop locally with
# System.Drawing instead of relying on CDP's clip.
$scrollExpr = "(function(){var el=" + $lookup + "; if(!el) return false; el.scrollIntoView({block:'center'}); return true;})()"
$scrollId = Send-Cdp $ws "Runtime.evaluate" @{ expression = $scrollExpr; returnByValue = $true } ([ref]$idc)
Receive-CdpUntil $ws { param($o) $o.id -eq $scrollId } | Out-Null
Start-Sleep -Milliseconds 400

$expr = "(function(){var el=" + $lookup + "; if(!el) return null; var r=el.getBoundingClientRect(); return JSON.stringify({x:r.x, y:r.y, w:r.width, h:r.height, dpr:window.devicePixelRatio});})()"
$evalId = Send-Cdp $ws "Runtime.evaluate" @{ expression = $expr; returnByValue = $true } ([ref]$idc)
$evalResp = Receive-CdpUntil $ws { param($o) $o.id -eq $evalId }
$boxJson = $evalResp.result.result.value
if (-not $boxJson) {
    $ws.CloseAsync([System.Net.WebSockets.WebSocketCloseStatus]::NormalClosure, "done", [System.Threading.CancellationToken]::None).Wait()
    throw "Selector/text not found: $(if ($ContainsText) { $ContainsText } else { $Selector })"
}
$box = $boxJson | ConvertFrom-Json
$dpr = $box.dpr

$shotId = Send-Cdp $ws "Page.captureScreenshot" @{ format = "png" } ([ref]$idc)
$shotResp = Receive-CdpUntil $ws { param($o) $o.id -eq $shotId } 20
$fullBytes = [Convert]::FromBase64String($shotResp.result.data)

$ms = New-Object System.IO.MemoryStream(,$fullBytes)
$fullImg = [System.Drawing.Image]::FromStream($ms)

$cropXcss = [math]::Max(0, $box.x - $Pad)
$cropYcss = [math]::Max(0, $box.y - $Pad)
$cropWcss = $box.w + (2 * $Pad)
$cropHcss = $box.h + (2 * $Pad)

$cropX = [int]([math]::Round($cropXcss * $dpr))
$cropY = [int]([math]::Round($cropYcss * $dpr))
$cropW = [int]([math]::Round($cropWcss * $dpr))
$cropH = [int]([math]::Round($cropHcss * $dpr))
if ($cropX + $cropW -gt $fullImg.Width) { $cropW = $fullImg.Width - $cropX }
if ($cropY + $cropH -gt $fullImg.Height) { $cropH = $fullImg.Height - $cropY }

$cropRect = New-Object System.Drawing.Rectangle($cropX, $cropY, $cropW, $cropH)
$cropped = New-Object System.Drawing.Bitmap($cropW, $cropH)
$g = [System.Drawing.Graphics]::FromImage($cropped)
$g.DrawImage($fullImg, (New-Object System.Drawing.Rectangle(0, 0, $cropW, $cropH)), $cropRect, [System.Drawing.GraphicsUnit]::Pixel)
$cropped.Save($OutFile, [System.Drawing.Imaging.ImageFormat]::Png)
$g.Dispose(); $cropped.Dispose(); $fullImg.Dispose(); $ms.Dispose()
Write-Host "Saved: $OutFile (${cropW}x${cropH})"

$ws.CloseAsync([System.Net.WebSockets.WebSocketCloseStatus]::NormalClosure, "done", [System.Threading.CancellationToken]::None).Wait()
Invoke-RestMethod -Uri "http://127.0.0.1:$Port/json/close/$($target.id)" -Method PUT | Out-Null

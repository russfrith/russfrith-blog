# Builds the site and checks the "Programming Languages, Then and Now" post for:
#   1. every {{< snippet file="..." >}} path matching git's exact casing
#      (Windows' filesystem is case-insensitive; Linux CI and most readers' clones
#      are not -- a casing mismatch builds fine here and breaks there).
#   2. the build actually resolving every snippet (no missing-file/region error).
#   3. no literal "{{<" shortcode syntax leaking into the rendered HTML (would
#      mean a shortcode call wasn't recognized/processed).
#   4. no literal triple-backtick fences in the rendered HTML (would mean a
#      shortcode emitted raw markdown instead of calling `highlight`).
#   5. no "Raw HTML omitted" Goldmark warning scoped to this post (would mean
#      a snippet's content got treated as unsafe raw HTML instead of being
#      highlighted properly).
#   6. verbatim guard for transcript blocks (layouts/partials/transcript.html):
#      for every captured output.txt rendered through the colored transcript
#      renderer, stripping the HTML tags and unescaping entities reproduces the
#      source file's bytes exactly -- and every block that should have routed
#      through the transcript renderer (lang="text", or a file named output.txt
#      or ending in _output.txt) actually did, rather than silently falling
#      back to Chroma's plain (uncolored) text lexer.
#   7. provenance manifest (results/provenance.json, written by
#      run-scenarios.ps1 -Update): every embedded output.txt/*_output.txt's
#      sha256 must match its manifest entry, and every one must HAVE a manifest
#      entry -- a mismatch means the committed file was hand-edited (or just
#      not regenerated) since the manifest was last written; a missing entry
#      means that file isn't covered by -Update at all yet.
#
# Usage (from repo root):
#   ./companion-code/programming-languages-then-and-now/scripts/check-post.ps1
#
# Exits non-zero and prints every problem found.

$ErrorActionPreference = "Stop"
$repoRoot = Resolve-Path (Join-Path $PSScriptRoot "../../..")
Push-Location $repoRoot

$postDir = "content/posts/programming-languages-then-and-now"
$postFile = "$postDir/index.md"
$companionDir = "companion-code/programming-languages-then-and-now"

$problems = @()

# --- 1 & 2: casing audit (source-level, doesn't need a build) ---
$tracked = git ls-files "$companionDir/*" | ForEach-Object { $_.Substring("$companionDir/".Length) }
$refs = (Select-String -Path $postFile -Pattern 'snippet file="([^"]+)"' -AllMatches).Matches |
    ForEach-Object { $_.Groups[1].Value } | Sort-Object -Unique

foreach ($r in $refs) {
    $exact = $tracked -ccontains $r
    if (-not $exact) {
        $caseInsensitiveMatch = $tracked | Where-Object { $_ -ieq $r }
        if ($caseInsensitiveMatch) {
            $problems += "CASE MISMATCH: post references '$r', git has '$caseInsensitiveMatch'"
        } else {
            $problems += "MISSING: post references '$r', not found in git at all (any case)"
        }
    }
}

# --- 3, 4, 5: build and inspect rendered HTML ---
$buildDir = Join-Path $env:TEMP "check-post-build-$(Get-Date -Format 'yyyyMMddHHmmss')"
$buildOutput = hugo -D -F --destination $buildDir 2>&1
$buildExit = $LASTEXITCODE

if ($buildExit -ne 0) {
    $problems += "BUILD FAILED (missing snippet/region, or another Hugo error):"
    $problems += ($buildOutput | Where-Object { $_ -match "ERROR" })
} else {
    $renderedFile = Join-Path $buildDir "posts/programming-languages-then-and-now/index.html"
    if (-not (Test-Path $renderedFile)) {
        $problems += "BUILD SUCCEEDED but rendered file not found: $renderedFile"
    } else {
        $html = Get-Content -Raw $renderedFile

        if ($html -match '\{\{<') {
            $problems += 'Literal "{{<" found in rendered HTML -- a shortcode call was not processed.'
        }
        if ($html -match '```') {
            $problems += 'Literal triple-backtick fence found in rendered HTML -- a shortcode emitted raw markdown instead of calling highlight.'
        }
    }

    # Hugo's "Raw HTML omitted" warning is printed to stderr/stdout during build,
    # scoped by the file path it names -- check specifically for our post.
    $rawHtmlWarnings = $buildOutput | Where-Object { $_ -match "Raw HTML omitted" -and $_ -match [regex]::Escape($postFile.Replace('/', '\')) -or ($_ -match "Raw HTML omitted" -and $_ -match "programming-languages-then-and-now[\\/]index\.md") }
    if ($rawHtmlWarnings) {
        $problems += "Raw HTML omitted warning scoped to this post's index.md:"
        $problems += $rawHtmlWarnings
    }

    # --- 6: transcript verbatim guard ---
    # Every snippet reference in document order, with its lang (explicit or extension-inferred,
    # matching layouts/shortcodes/snippet.html's own default-map logic) and whether it should
    # have routed through the transcript renderer.
    $extMap = @{ ".py"="python"; ".go"="go"; ".rs"="rust"; ".ts"="typescript"; ".kt"="kotlin"; ".cs"="csharp"; ".csproj"="xml"; ".c"="c"; ".h"="c"; ".sh"="bash"; ".ps1"="powershell"; ".yml"="yaml"; ".yaml"="yaml"; ".txt"="text"; ".md"="text" }
    $shortcodeCalls = (Select-String -Path $postFile -Pattern '\{\{<\s*snippet(out)?\s+file="([^"]+)"(?:\s+region="[^"]*")?(?:\s+lang="([^"]+)")?' -AllMatches).Matches
    $transcriptFiles = @()
    foreach ($m in $shortcodeCalls) {
        $f = $m.Groups[2].Value
        $langOverride = $m.Groups[3].Value
        $base = Split-Path $f -Leaf
        $ext = [IO.Path]::GetExtension($f)
        $lang = if ($langOverride) { $langOverride } elseif ($extMap.ContainsKey($ext)) { $extMap[$ext] } else { "text" }
        $isTranscript = ($lang -eq "text") -or ($base -eq "output.txt") -or ($base.EndsWith("_output.txt"))
        if ($isTranscript) { $transcriptFiles += $f }
    }

    if ($buildExit -eq 0 -and (Test-Path $renderedFile)) {
        $html = Get-Content -Raw $renderedFile
        $blockPattern = '<div class="highlight transcript"><pre[^>]*><code class="language-text" data-lang="text">(.*?)</code></pre></div>'
        $transcriptBlocks = [regex]::Matches($html, $blockPattern, [Text.RegularExpressions.RegexOptions]::Singleline)

        # Any lang="text" block NOT wrapped in class="highlight transcript" means a text-lang
        # snippet silently fell back to Chroma's plain (uncolored) lexer instead of routing
        # through the transcript renderer.
        $plainTextBlocks = [regex]::Matches($html, '<div class="highlight"><pre[^>]*><code class="language-text" data-lang="text">', [Text.RegularExpressions.RegexOptions]::Singleline)
        if ($plainTextBlocks.Count -gt 0) {
            $problems += "$($plainTextBlocks.Count) lang=`"text`" block(s) rendered via Chroma's plain text lexer instead of the transcript renderer (missing the 'transcript' class)."
        }

        if ($transcriptBlocks.Count -ne $transcriptFiles.Count) {
            $problems += "Transcript block count mismatch: post has $($transcriptFiles.Count) transcript-eligible snippet refs, rendered HTML has $($transcriptBlocks.Count) transcript blocks."
        } else {
            for ($i = 0; $i -lt $transcriptBlocks.Count; $i++) {
                $srcPath = Join-Path $companionDir $transcriptFiles[$i]
                if (-not (Test-Path $srcPath)) { continue }  # already reported by the casing/missing check above
                $sourceText = Get-Content -Raw $srcPath
                $innerHtml = $transcriptBlocks[$i].Groups[1].Value
                $stripped = [regex]::Replace($innerHtml, '<[^>]+>', '')
                $decoded = [System.Net.WebUtility]::HtmlDecode($stripped)
                # normalize line endings -- the source may be read with either, HTML has none
                $decodedNorm = $decoded -replace "`r`n", "`n"
                $sourceNorm = $sourceText -replace "`r`n", "`n"
                if ($decodedNorm -ne $sourceNorm) {
                    $problems += "VERBATIM MISMATCH: transcript block for '$($transcriptFiles[$i])' does not exactly match its source file after stripping tags and decoding entities."
                }
            }
        }
    }
}

# --- 7: provenance manifest check ---
$provenanceFile = "$companionDir/results/provenance.json"
$embeddedOutputs = (Select-String -Path $postFile -Pattern 'file="([^"]+(?:output\.txt|_output\.txt))"' -AllMatches).Matches |
    ForEach-Object { $_.Groups[1].Value } | Sort-Object -Unique

if (-not (Test-Path $provenanceFile)) {
    $problems += "Provenance manifest not found: $provenanceFile (run scripts/run-scenarios.ps1 -Update)"
} else {
    $manifest = Get-Content -Raw $provenanceFile | ConvertFrom-Json
    $manifestByFile = @{}
    foreach ($entry in $manifest) { $manifestByFile[$entry.file] = $entry }

    foreach ($f in $embeddedOutputs) {
        $fullPath = "$companionDir/$f"
        if (-not (Test-Path $fullPath)) { continue }  # already reported by the casing/missing check above
        if (-not $manifestByFile.ContainsKey($f)) {
            $problems += "PROVENANCE MISSING: '$f' is embedded in the post but has no entry in results/provenance.json"
            continue
        }
        # Hash LF-normalized bytes, matching how run-scenarios.ps1 -Update computes
        # this (and how git itself stores/restores these files) -- a plain
        # Get-FileHash would mismatch after a git checkout CRLF-normalizes a file
        # that was originally written with CRLF line endings.
        $rawForHash = Get-Content -Path $fullPath -Raw
        $normalizedForHash = $rawForHash -replace "`r`n", "`n"
        $bytesForHash = [System.Text.Encoding]::UTF8.GetBytes($normalizedForHash)
        $shaForHash = [System.Security.Cryptography.SHA256]::Create()
        $actualHash = [System.BitConverter]::ToString($shaForHash.ComputeHash($bytesForHash)).Replace("-", "").ToLower()
        $shaForHash.Dispose()
        if ($actualHash -ne $manifestByFile[$f].sha256) {
            $problems += "PROVENANCE MISMATCH: '$f' sha256 does not match results/provenance.json -- committed file differs from what was last captured"
        }
    }
}

Remove-Item -Recurse -Force $buildDir -ErrorAction SilentlyContinue
Pop-Location

if ($problems.Count -gt 0) {
    Write-Host "check-post.ps1 FOUND PROBLEMS:" -ForegroundColor Red
    $problems | ForEach-Object { Write-Host "  - $_" -ForegroundColor Red }
    exit 1
} else {
    Write-Host "check-post.ps1: all snippet paths match git's exact casing, build succeeds, no literal shortcode/fence/raw-HTML leakage, every transcript block matches its source file verbatim, every embedded output file matches the provenance manifest." -ForegroundColor Green
    exit 0
}

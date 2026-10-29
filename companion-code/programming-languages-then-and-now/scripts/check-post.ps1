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
#   8. productivity numbers (results/productivity.json, written by
#      scripts/measure-productivity.ps1): every line-of-code count, dependency
#      count, build-time median/range, and image size in that file must appear
#      somewhere in the post's "Productivity" section (H4) -- a number present
#      in the JSON but missing from the post means the post's tables are stale
#      relative to the last real measurement.
#   9. prose claims (scripts/claims.psd1): comparative/derived numbers stated
#      in the post's own sentences (race frequencies, the Go-vs-Node CPU-heavy
#      percentage, C# Debug/Release timings, iteration counts, etc.) must
#      match the real source file they're derived from, within a stated
#      tolerance -- these are numbers no other check here covers, because
#      they're prose, not a table cell or an embedded transcript.
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

# --- 8: productivity numbers check ---
$productivityFile = "$companionDir/results/productivity.json"
if (-not (Test-Path $productivityFile)) {
    $problems += "Productivity data not found: $productivityFile (run scripts/measure-productivity.ps1)"
} else {
    $prod = Get-Content -Raw $productivityFile | ConvertFrom-Json
    # Scan the RENDERED HTML, not the markdown source -- I1 moved the productivity
    # tables to a shortcode that derives them from this same JSON at build time, so
    # the numbers no longer appear in the markdown source at all, only in the output.
    $postText = if ($buildExit -eq 0 -and (Test-Path $renderedFile)) { Get-Content -Raw $renderedFile } else { "" }

    # Pull every numeric value out of productivity.json (medians, range bounds,
    # loc counts, dependency counts, image MB) as a flat list, formatted the
    # same way Format-Num below formats post-side numbers, so a value present
    # in the JSON but written differently in the post (e.g. a stale "19.5" vs
    # the JSON's current "18.6") is still caught as a real mismatch.
    function Format-Num($n) {
        # Match how the post renders these: integers bare, decimals to 2 places
        # with trailing zeros trimmed (matches both "1.26" and "1.0" -> "1").
        $r = [math]::Round([double]$n, 2)
        return ($r.ToString("0.##", [System.Globalization.CultureInfo]::InvariantCulture))
    }

    $expectedNumbers = [System.Collections.Generic.HashSet[string]]::new()
    foreach ($lang in $prod.buildTimes.PSObject.Properties) {
        $bt = $lang.Value
        [void]$expectedNumbers.Add((Format-Num $bt.cleanMedian))
        [void]$expectedNumbers.Add((Format-Num $bt.incrementalMedian))
        foreach ($part in ($bt.cleanRange -split '-')) { [void]$expectedNumbers.Add((Format-Num $part)) }
        foreach ($part in ($bt.incrementalRange -split '-')) { [void]$expectedNumbers.Add((Format-Num $part)) }
    }
    foreach ($lang in $prod.linesOfCode.PSObject.Properties) {
        [void]$expectedNumbers.Add((Format-Num $lang.Value))
    }
    foreach ($lang in $prod.dependencies.PSObject.Properties) {
        [void]$expectedNumbers.Add((Format-Num $lang.Value.count))
    }
    foreach ($lang in $prod.imageSizes.PSObject.Properties) {
        if ($lang.Value.mb) { [void]$expectedNumbers.Add((Format-Num $lang.Value.mb)) }
        if ($lang.Value.variants) {
            foreach ($v in $lang.Value.variants.PSObject.Properties) {
                [void]$expectedNumbers.Add((Format-Num $v.Value.mb))
            }
        }
    }

    # Numbers actually present anywhere in the post (as a safety net against
    # false positives from e.g. "55" appearing as part of an unrelated number,
    # we require a word-boundary-ish match: not preceded/followed by a digit).
    $postNumbers = [System.Collections.Generic.HashSet[string]]::new()
    foreach ($m in [regex]::Matches($postText, '(?<![\d.])\d+\.?\d*(?![\d])')) {
        [void]$postNumbers.Add((Format-Num $m.Value))
    }

    $missingFromPost = $expectedNumbers | Where-Object { -not $postNumbers.Contains($_) }
    if ($missingFromPost) {
        $problems += "PRODUCTIVITY STALE: these values from results/productivity.json don't appear anywhere in the post -- $($missingFromPost -join ', ')"
    }
}

# --- 9: claims.psd1 -- prose numbers checked against their real source ---
$claimsFile = "$companionDir/scripts/claims.psd1"
if (-not (Test-Path $claimsFile)) {
    $problems += "Claims file not found: $claimsFile"
} else {
    $claims = (Import-PowerShellDataFile -Path $claimsFile).Claims
    $postTextForClaims = if ($buildExit -eq 0 -and (Test-Path $renderedFile)) { Get-Content -Raw $renderedFile } else { "" }

    function ClaimNum($s) { return [double]($s -replace '[,_]', '') }

    foreach ($claim in $claims) {
        $postMatch = [regex]::Match($postTextForClaims, $claim.PostPattern)
        if (-not $postMatch.Success) {
            $problems += "CLAIM '$($claim.Name)': post pattern not found -- '$($claim.PostPattern)'"
            continue
        }

        switch ($claim.Kind) {
            "simple" {
                $claimed = ClaimNum $postMatch.Groups[1].Value
                $srcPath = Join-Path $companionDir $claim.SourceFile
                $srcMatch = [regex]::Match((Get-Content -Raw $srcPath), $claim.SourcePattern)
                if (-not $srcMatch.Success) { $problems += "CLAIM '$($claim.Name)': source pattern not found in $($claim.SourceFile)"; continue }
                $actual = ClaimNum $srcMatch.Groups[1].Value
                if ([math]::Abs($claimed - $actual) -gt $claim.Tolerance) {
                    $problems += "CLAIM MISMATCH '$($claim.Name)': post says $claimed, $($claim.SourceFile) says $actual (tolerance $($claim.Tolerance))"
                }
            }
            "percentdiff" {
                $claimed = ClaimNum $postMatch.Groups[1].Value
                $a = ClaimNum ([regex]::Match((Get-Content -Raw (Join-Path $companionDir $claim.SourceFileA)), $claim.SourcePatternA).Groups[1].Value)
                $b = ClaimNum ([regex]::Match((Get-Content -Raw (Join-Path $companionDir $claim.SourceFileB)), $claim.SourcePatternB).Groups[1].Value)
                $actual = [math]::Abs($a - $b) / $a * 100
                if ([math]::Abs($claimed - $actual) -gt $claim.Tolerance) {
                    $problems += "CLAIM MISMATCH '$($claim.Name)': post says ${claimed}%, actual is $([math]::Round($actual,2))% (tolerance $($claim.Tolerance))"
                }
            }
            "avg2" {
                $claimed = ClaimNum $postMatch.Groups[1].Value
                $srcText = Get-Content -Raw (Join-Path $companionDir $claim.SourceFile)
                $matches = [regex]::Matches($srcText, $claim.SourcePattern)
                if ($matches.Count -lt 2) { $problems += "CLAIM '$($claim.Name)': fewer than 2 source matches in $($claim.SourceFile)"; continue }
                $actual = (([double]$matches[0].Groups[1].Value) + ([double]$matches[1].Groups[1].Value)) / 2
                if ([math]::Abs($claimed - $actual) -gt $claim.Tolerance) {
                    $problems += "CLAIM MISMATCH '$($claim.Name)': post says $claimed, avg of first 2 in $($claim.SourceFile) is $actual (tolerance $($claim.Tolerance))"
                }
            }
            "range2" {
                $claimedMin = ClaimNum $postMatch.Groups[1].Value
                $claimedMax = ClaimNum $postMatch.Groups[2].Value
                $srcText = Get-Content -Raw (Join-Path $companionDir $claim.SourceFile)
                $matches = [regex]::Matches($srcText, $claim.SourcePattern)
                if ($matches.Count -lt 2) { $problems += "CLAIM '$($claim.Name)': fewer than 2 source matches in $($claim.SourceFile)"; continue }
                $last2 = @($matches)[-2..-1] | ForEach-Object { [double]$_.Groups[1].Value }
                $actualMin = ($last2 | Measure-Object -Minimum).Minimum
                $actualMax = ($last2 | Measure-Object -Maximum).Maximum
                if ([math]::Abs($claimedMin - $actualMin) -gt $claim.Tolerance -or [math]::Abs($claimedMax - $actualMax) -gt $claim.Tolerance) {
                    $problems += "CLAIM MISMATCH '$($claim.Name)': post says $claimedMin-$claimedMax, last 2 in $($claim.SourceFile) are $actualMin-$actualMax (tolerance $($claim.Tolerance))"
                }
            }
            "ticks-zero" {
                foreach ($f in $claim.SourceFiles) {
                    $srcMatch = [regex]::Match((Get-Content -Raw (Join-Path $companionDir $f)), $claim.SourcePattern)
                    if (-not $srcMatch.Success) { $problems += "CLAIM '$($claim.Name)': source pattern not found in $f"; continue }
                    $actual = ClaimNum $srcMatch.Groups[1].Value
                    if ($actual -ne 0) {
                        $problems += "CLAIM MISMATCH '$($claim.Name)': post says 0 heartbeat ticks, $f shows $actual"
                    }
                }
            }
            default {
                $problems += "CLAIM '$($claim.Name)': unknown Kind '$($claim.Kind)'"
            }
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
    Write-Host "check-post.ps1: all snippet paths match git's exact casing, build succeeds, no literal shortcode/fence/raw-HTML leakage, every transcript block matches its source file verbatim, every embedded output file matches the provenance manifest, productivity numbers match results/productivity.json, all claims.psd1 prose claims match their source." -ForegroundColor Green
    exit 0
}

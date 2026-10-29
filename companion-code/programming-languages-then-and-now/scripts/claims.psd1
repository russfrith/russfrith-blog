# I4: every prose number in the post that isn't already covered by the
# provenance/productivity checks gets a claim here. check-post.ps1 extracts
# the claimed value from the rendered post and the actual value from the
# named source file(s), and fails if they disagree beyond Tolerance.
#
# Kind = "simple":      one number in the post vs. one number in the source.
# Kind = "percentdiff": a claimed percentage vs. the real |a-b|/a*100 between
#                       two source files' first matched number.
# Kind = "avg2":        a claimed single number vs. the average of the first
#                       two numbers SourcePattern matches in SourceFile.
# Kind = "range2":      a claimed [min,max] vs. the min/max of the LAST two
#                       numbers SourcePattern matches in SourceFile.
# Kind = "ticks-zero":  the post asserts "0" for every file listed; each
#                       file's SourcePattern match must literally be 0.
#
# All numbers are compared after stripping commas/underscores.

# Import-PowerShellDataFile requires a top-level hashtable (restricted
# language mode), not a bare array -- the actual claim list is under .Claims.
@{
Claims = @(
    @{
        Name = "race-c"
        Kind = "simple"
        PostPattern = 'C, Kotlin, and Go \(below\) lost an update in every one of (\d+) runs'
        SourceFile = "c/scenarios/02-data-race/output.txt"
        SourcePattern = 'lost an update in (\d+) of \d+ runs'
        Tolerance = 0
    }
    @{
        Name = "race-kotlin"
        Kind = "simple"
        PostPattern = 'C, Kotlin, and Go \(below\) lost an update in every one of (\d+) runs'
        SourceFile = "kotlin/scenarios/02_data_race/output.txt"
        SourcePattern = 'lost an update in (\d+) of \d+ runs'
        Tolerance = 0
    }
    @{
        Name = "race-go"
        Kind = "simple"
        PostPattern = 'C, Kotlin, and Go \(below\) lost an update in every one of (\d+) runs'
        SourceFile = "go/scenarios/02_data_race/output.txt"
        SourcePattern = 'lost an update in (\d+) of \d+ runs'
        Tolerance = 0
    }
    @{
        Name = "race-typescript"
        Kind = "simple"
        PostPattern = 'TypeScript lost one in (\d+) of 5 runs'
        SourceFile = "typescript/scenarios/02_data_race/output.txt"
        SourcePattern = 'lost an update in (\d+) of \d+ runs'
        Tolerance = 0
    }
    @{
        Name = "race-csharp"
        Kind = "simple"
        PostPattern = 'C# in (\d+) of 5'
        SourceFile = "csharp/scenarios/02_data_race/output.txt"
        SourcePattern = 'lost an update in (\d+) of \d+ runs'
        Tolerance = 0
    }
    @{
        Name = "race-python"
        Kind = "simple"
        PostPattern = 'both lost an update in every one of (\d+) runs'
        SourceFile = "python/scenarios/02_data_race/output.txt"
        SourcePattern = 'lost an update in (\d+) of \d+ runs'
        Tolerance = 0
    }
    @{
        Name = "race-rust"
        Kind = "simple"
        PostPattern = 'both lost an update in every one of (\d+) runs'
        SourceFile = "rust/scenarios/02_data_race/output.txt"
        SourcePattern = 'lost an update in (\d+) of \d+ runs'
        Tolerance = 0
    }
    @{
        Name = "python-iterations"
        Kind = "simple"
        PostPattern = 'reduced the loop to (\d+) iterations per thread'
        SourceFile = "python/scenarios/02_data_race/broken.py"
        SourcePattern = 'ITERATIONS = (\d+)'
        Tolerance = 0
    }
    @{
        Name = "rust-iterations"
        Kind = "simple"
        PostPattern = 'Rust kept the full ([\d,]+)'
        SourceFile = "rust/scenarios/02_data_race/broken.rs"
        SourcePattern = 'ITERATIONS: i64 = ([\d_]+)'
        Tolerance = 0
    }
    @{
        Name = "go-node-cpu-pct"
        Kind = "percentdiff"
        PostPattern = 'within about (\d+)% of each other'
        SourceFileA = "go/scenarios/07_cpu_heavy_blocking/output.txt"
        SourcePatternA = 'finished after ([\d.]+)ms'
        SourceFileB = "typescript/scenarios/07_cpu_heavy_blocking/output.txt"
        SourcePatternB = 'finished after ([\d.]+)ms'
        Tolerance = 2
    }
    @{
        Name = "csharp-debug-ms"
        Kind = "avg2"
        PostPattern = 'took roughly (\d+)ms'
        SourceFile = "csharp/scenarios/07_cpu_heavy_blocking/output.txt"
        SourcePattern = 'finished after (\d+)ms'
        Tolerance = 150
    }
    @{
        Name = "csharp-release-ms-range"
        Kind = "range2"
        PostPattern = '~(\d+)-(\d+)ms'
        SourceFile = "csharp/scenarios/07_cpu_heavy_blocking/output.txt"
        SourcePattern = 'finished after (\d+)ms'
        Tolerance = 10
    }
    @{
        Name = "cpu-heavy-zero-ticks"
        Kind = "ticks-zero"
        PostPattern = '\(0 heartbeat ticks\)'
        SourceFiles = @(
            "typescript/scenarios/07_cpu_heavy_blocking/output.txt"
            "python/scenarios/07_cpu_heavy_blocking/output.txt"
        )
        SourcePattern = 'ticks seen: (\d+)'
        Tolerance = 0
    }
)
}

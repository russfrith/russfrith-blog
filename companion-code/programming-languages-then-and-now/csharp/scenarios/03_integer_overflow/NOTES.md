Confirms the prediction exactly: C#'s default is unchecked (silent, defined
wraparound, like Go/Kotlin). Both an explicit `checked()` expression and the
project-level `CheckForOverflowUnderflow` MSBuild property throw
OverflowException for the identical plain `x + 1` source line -- the project
setting changes the behavior of code that looks completely ordinary, with no
`checked` keyword anywhere.
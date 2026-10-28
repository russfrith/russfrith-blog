Confirms the footgun, with one correction to the comment in broken/Program.cs:
the try/catch wrapped around the call to DoWorkAsync() does NOT catch the
exception -- exactly as predicted -- but the result is not silent: the exception
is rethrown on the thread pool and crashes the whole process with an unhandled
exception. The surprising part is specifically that a try/catch that looks like
it should catch this (it's wrapped directly around the call) does nothing at
all, not that the failure is quiet. `async Task` + `await` lets the exception
propagate through the try/catch normally.
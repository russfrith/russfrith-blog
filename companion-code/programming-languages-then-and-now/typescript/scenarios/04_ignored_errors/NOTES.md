Mismatch with prediction: "done" DOES print before the failure surfaces (confirming
the ignored error doesn't block execution), but current Node's default behavior for
an unhandled rejection is to crash the process afterward (since Node 15, unhandled
rejections are fatal by default) rather than just log a warning and keep going
indefinitely. The error is still effectively "ignored" at the point it happens --
the crash comes later, asynchronously, disconnected from the line that caused it.

Type checker: TypeScript 5.9.3. tsc does not flag the dropped `await` in broken.ts
as an error (or even a warning) under these strict settings.

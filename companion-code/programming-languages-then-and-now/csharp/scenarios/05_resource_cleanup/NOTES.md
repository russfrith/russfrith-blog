Same pattern as every other GC'd language here: broken.cs's FileStream is still
open when the program's last statement runs -- the finalizer will eventually
release the OS handle, but that timing is not deterministic. `using` disposes it
deterministically when the declaring scope ends.
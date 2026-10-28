the printed sys.version / sys._is_gil_enabled() above -- corrected after an audit)
Note: broken.py's file descriptor is still open when the script's last line runs --
CPython's refcounting GC will close it shortly after, but that timing is a CPython
implementation detail (PyPy's GC is not refcounting), not a language guarantee.
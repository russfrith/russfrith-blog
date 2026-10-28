the printed sys.version / sys._is_gil_enabled() above -- corrected after an audit)
Note: broken.py exits 0 and prints "done" -- the bare except swallowed the error so
completely that nothing at all signals a problem happened.
the printed sys.version / sys._is_gil_enabled() above -- corrected after an audit)
No broken/fixed pair: Python ints are arbitrary-precision, so there is nothing to
overflow and nothing to fix.
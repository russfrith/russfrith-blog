"""Python's signature footgun: a mutable default argument is created ONCE, at
function definition time, and shared across every call that doesn't pass its own."""
import sys
print(f"[python {sys.version.split()[0]}, gil_enabled={sys._is_gil_enabled()}]", file=sys.stderr)


def append_item(item, bucket=[]):
    bucket.append(item)
    return bucket


print(append_item("a"))
print(append_item("b"))  # "a" is still there -- bucket was never empty

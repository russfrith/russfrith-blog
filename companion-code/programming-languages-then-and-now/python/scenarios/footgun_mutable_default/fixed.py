"""Fixed: default to None, create a fresh list inside the function body."""
import sys
print(f"[python {sys.version.split()[0]}, gil_enabled={sys._is_gil_enabled()}]", file=sys.stderr)


def append_item(item, bucket=None):
    if bucket is None:
        bucket = []
    bucket.append(item)
    return bucket


print(append_item("a"))
print(append_item("b"))

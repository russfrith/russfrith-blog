"""Fixed: default to None, create a fresh list inside the function body."""


def append_item(item, bucket=None):
    if bucket is None:
        bucket = []
    bucket.append(item)
    return bucket


print(append_item("a"))
print(append_item("b"))

"""Scenario 1: absent value. Direct dict indexing raises KeyError when a field is
missing -- at least it's a loud, specific exception, not a silent wrong value."""
record = {"id": 1, "label": "widget"}  # "count" is absent
print(record["count"])

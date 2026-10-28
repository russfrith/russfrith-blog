"""Scenario 1 fixed: .get() with an explicit default, or check for the sentinel."""
record = {"id": 1, "label": "widget"}
count = record.get("count")
if count is None:
    print("count absent")
else:
    print(f"count = {count}")

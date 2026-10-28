"""Scenario 4 fixed: catch the specific exception and handle it explicitly."""
try:
    with open("/no/such/path.txt") as f:
        data = f.read()
except FileNotFoundError as e:
    print(f"file not found: {e}")
else:
    print(f"read {len(data)} bytes")

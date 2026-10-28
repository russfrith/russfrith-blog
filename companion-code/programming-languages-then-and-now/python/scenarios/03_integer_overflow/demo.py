"""Scenario 3: integer overflow -- a non-event in Python. ints are arbitrary
precision, so there is nothing to overflow and therefore no broken/fixed pair here.
This is captured as a fact, not a win: it trades overflow bugs for the cost of
boxed, slower integer arithmetic versus a fixed-width machine int."""
import sys

x = sys.maxsize
y = x + 1
print(f"sys.maxsize + 1 = {y}")

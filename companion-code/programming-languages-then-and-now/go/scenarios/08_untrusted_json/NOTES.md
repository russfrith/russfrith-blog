Confirms the prediction: the missing field silently zero-fills with NO error
(err=<nil>), while the wrong-typed field DOES produce a decode error. Go's
encoding/json is strict about type mismatches but silent about absence -- a split
that's easy to miss if you only test one of the two.
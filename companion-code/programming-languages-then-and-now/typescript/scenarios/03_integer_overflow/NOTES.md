Note: "+1" and "+2" print the SAME value -- this is silent precision loss, not
wraparound. No broken/fixed pair: ordinary `number` has no fix for itself; BigInt is
a different, exact type you must opt into deliberately.

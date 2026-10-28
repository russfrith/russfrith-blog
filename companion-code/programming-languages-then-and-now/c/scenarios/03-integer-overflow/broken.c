// Scenario 3: signed integer overflow. This is undefined behavior in C, not just a
// wraparound -- a compiler is free to assume it never happens and optimize accordingly.
#include <limits.h>
#include <stdio.h>

int main(void) {
    int x = INT_MAX;
    int y = x + 1; // UB: signed overflow
    printf("INT_MAX + 1 = %d\n", y);
    return 0;
}

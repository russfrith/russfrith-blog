// Scenario 3 fixed: __builtin_add_overflow (GCC/Clang extension) detects the overflow
// before it happens instead of relying on undefined behavior.
#include <limits.h>
#include <stdio.h>

int main(void) {
    int x = INT_MAX;
    int result;
    if (__builtin_add_overflow(x, 1, &result)) {
        printf("overflow detected: INT_MAX + 1 would overflow\n");
    } else {
        printf("INT_MAX + 1 = %d\n", result);
    }
    return 0;
}

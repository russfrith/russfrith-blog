// Scenario 1: absent/null value. C has no concept of "field absent" baked into the
// language -- the baseline here is a NULL pointer that nothing checks before use.
#include <stdio.h>
#include <string.h>

struct Record {
    const char *label; // may legitimately be NULL -- "absent"
};

int main(void) {
    struct Record r = { .label = NULL };
    // Nothing stops this from compiling or running up to the crash.
    printf("label length: %zu\n", strlen(r.label));
    return 0;
}

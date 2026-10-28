// Scenario 1 fixed: an explicit NULL check before use. C gives you no help enforcing
// this -- it is a convention you must apply by hand at every call site.
#include <stdio.h>
#include <string.h>

struct Record {
    const char *label;
};

int main(void) {
    struct Record r = { .label = NULL };
    if (r.label == NULL) {
        printf("label absent\n");
    } else {
        printf("label length: %zu\n", strlen(r.label));
    }
    return 0;
}

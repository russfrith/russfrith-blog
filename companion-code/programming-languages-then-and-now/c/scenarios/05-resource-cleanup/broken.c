// Scenario 5: resource cleanup and leaks. Allocated memory is never freed. In a
// long-running process this grows without bound; here it's a single allocation so a
// leak detector can point at the exact leaked block.
#include <stdlib.h>
#include <string.h>

int main(void) {
    char *buf = malloc(128);
    strcpy(buf, "never freed");
    // no free(buf) before returning
    return 0;
}

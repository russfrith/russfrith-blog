// Scenario 5 fixed: free the allocation. C has no destructors or borrow checker --
// this discipline is entirely manual and entirely the programmer's responsibility.
#include <stdlib.h>
#include <string.h>

int main(void) {
    char *buf = malloc(128);
    strcpy(buf, "never freed");
    free(buf);
    return 0;
}

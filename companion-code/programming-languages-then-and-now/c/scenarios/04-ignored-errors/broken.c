// Scenario 4: ignored errors. fopen's NULL return (file not found, permissions, etc.)
// is never checked, so the program dereferences a NULL FILE* on the next line.
#include <stdio.h>

int main(void) {
    FILE *f = fopen("/no/such/path.txt", "r");
    char buf[16];
    fread(buf, 1, sizeof(buf), f); // f may be NULL here; return value never checked either
    printf("read some bytes\n");
    return 0;
}

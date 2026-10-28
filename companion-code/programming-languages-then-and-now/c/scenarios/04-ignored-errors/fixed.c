// Scenario 4 fixed: check fopen's return value before using it.
#include <stdio.h>

int main(void) {
    FILE *f = fopen("/no/such/path.txt", "r");
    if (f == NULL) {
        perror("fopen");
        return 1;
    }
    char buf[16];
    size_t n = fread(buf, 1, sizeof(buf), f);
    printf("read %zu bytes\n", n);
    fclose(f);
    return 0;
}

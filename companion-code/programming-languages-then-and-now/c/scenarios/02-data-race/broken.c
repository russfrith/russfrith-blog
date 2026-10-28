// Scenario 2: data race. Two threads increment a shared counter with no
// synchronization. The final value is nondeterministic and usually wrong.
#include <pthread.h>
#include <stdio.h>

static long counter = 0;
#define ITERATIONS 200000

void *increment(void *arg) {
    (void)arg;
    for (int i = 0; i < ITERATIONS; i++) {
        counter++; // read-modify-write with no lock: a race
    }
    return NULL;
}

int main(void) {
    pthread_t t1, t2;
    pthread_create(&t1, NULL, increment, NULL);
    pthread_create(&t2, NULL, increment, NULL);
    pthread_join(t1, NULL);
    pthread_join(t2, NULL);
    printf("counter = %ld (expected %d)\n", counter, 2 * ITERATIONS);
    return 0;
}

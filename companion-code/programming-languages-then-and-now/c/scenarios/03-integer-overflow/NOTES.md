Note: signed overflow is undefined behavior in C, not merely "wraps to -2147483648."
GCC happened to produce two's-complement wraparound at -O0 with -Wall and no further
optimization; this is not a guarantee and the compiler is free to do something else,
including removing code, at higher optimization levels.
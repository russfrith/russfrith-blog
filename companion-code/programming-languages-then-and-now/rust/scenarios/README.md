# Rust scenarios

Most scenarios are single `.rs` files compiled directly with `rustc`:

```
docker run --rm -v "${PWD}/01_absent_null:/src" -w /src rust:1.96.1-slim-bookworm bash -c "rustc -O broken.rs -o broken && ./broken"
docker run --rm -v "${PWD}/01_absent_null:/src" -w /src rust:1.96.1-slim-bookworm bash -c "rustc -O fixed.rs -o fixed && ./fixed"
```

Scenario 2 also has two standalone compile-rejection files (`compile_reject_plain_mut.rs`, `compile_reject_rc.rs`) that are EXPECTED to fail to compile -- see `compile_reject_output.txt`.

Scenarios 7 and 8 are full Cargo projects (they need `tokio`/`serde`/`ureq`), with multiple binaries under `src/bin/`:

```
docker run --rm -v "${PWD}/07_cpu_heavy_blocking:/src" -w /src rust:1.96.1-slim-bookworm cargo build --release
docker run --rm -v "${PWD}/07_cpu_heavy_blocking:/src" -w /src rust:1.96.1-slim-bookworm ./target/release/current_thread_broken
```

Scenario 8 needs the mock server reachable:

```
docker compose -f ../../docker-compose.yml up -d mockserver
docker run --rm --network programming-languages-then-and-now_wordfreq -v "${PWD}/08_untrusted_json:/src" -w /src rust:1.96.1-slim-bookworm cargo build --release
docker run --rm --network programming-languages-then-and-now_wordfreq -v "${PWD}/08_untrusted_json:/src" -w /src rust:1.96.1-slim-bookworm ./target/release/strict
```

See each scenario's own `output.txt` for the exact command(s) used.

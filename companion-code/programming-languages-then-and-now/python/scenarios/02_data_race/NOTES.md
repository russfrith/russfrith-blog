the printed sys.version / sys._is_gil_enabled() above -- corrected after an audit; no free-threaded
run was performed, see VERSIONS.md for why)
Mismatch with prediction: a plain `counter += 1` loop (200,000 iterations, no
artificial delay) did NOT reproduce a lost update in this container, even with
sys.setswitchinterval lowered -- see PREDICTIONS.md. Reducing to 200 iterations
per thread and inserting an explicit time.sleep(0) between the read and the write
(simulating the gap a real read-modify-write over I/O would have) made the race
reliable and is what's captured above. The exact wrong total (200 this run) is
nondeterministic and varies run to run.
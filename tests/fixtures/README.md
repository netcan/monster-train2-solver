# Native regression fixtures

These fixed inputs reproduce the independently modeled battle and calibration
checks. Run `pwsh -NoProfile -File scripts/Check-Models.ps1` from the repository
root. The script reads the curated fixture list in this directory.

Native JSON captures are kept unchanged; `.json.gz` files contain their original
bytes compressed with gzip. `SIMULATION.md` records coverage, capture versions
and raw JSON hashes. Moving a capture does not change its contents.

`results/` is ignored and holds local logs, catalogs and benchmark outputs.
Isolated native probes use `.probe-runs/`, which is also ignored. Successful
captures become fixed inputs here after native and independent comparisons pass.

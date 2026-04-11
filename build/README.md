# Build Folder

`build/` is reserved for repository-level build helpers and future build
customizations.

Current state:

- `build.cmd` is the Windows bootstrap entry point for the baseline validation
  flow
- `build.sh` is the Unix-like shell bootstrap entry point for the same baseline
  flow
- the main validation logic still lives in `ops/validate-platform-readiness.ps1`
  while the repo remains early and narrowly scoped

This folder exists now so Strata can standardize its top-level layout before
broader build and packaging work lands.

# Verification

Run `pwsh -File tools/Verify.ps1` from a clean committed checkout using .NET SDK 10.0.303 and PowerShell 7. The script requires its reviewed `docs/PublicApi.txt` baseline. It produces a unique `artifacts/verification-<id>` directory and records the most recent successful directory in `artifacts/latest-verification.txt`.

The following gates must pass:

1. Locked restore, Release build with warnings treated as errors, and whitespace/style verification for library, tests, example and audit tool.
2. All xUnit tests, with machine-readable TRX counts; no skipped or silently absent tests count as success.
3. Exact public API inventory comparison and an executable assertion-based example.
4. `.nupkg` and `.snupkg` archive inventories, metadata, no runtime dependencies, original license/icon hashes, README bytes and XML documentation.
5. Portable PDB/assembly identity, immutable-commit Source Link mapping and checksums for every source file.
6. Installation of the exact package into a temporary consumer with no project references, an empty global package cache and explicit source mapping. The consumer checks quoting, multiline Unicode, empty fields, dialects, syntax positions and input bounds.

Tests include RFC-style fixtures, strict error positions, EOF distinctions, decoded limits, no read-ahead through later records, UTF-8 byte boundaries, disposal ownership and partial-output exceptions. Two hundred fixed-seed single-line cases are checked against the platform CSV parser, and 1,000 fixed-seed records cover lossless multiline roundtrips across both newline dialects. These are bounded functional checks, not performance or interoperability claims for every CSV dialect.

The Windows and Ubuntu CI jobs execute the same verification independently and retain their unique verified packages. Local verification does not itself establish Linux CI success, NuGet publication, package indexing, public symbols, or the gallery rendering of the icon. Those gates are handled in [RELEASE.md](RELEASE.md).

The embedded icon is extracted during package audit and must match the unchanged master hash. The shared master has been visually inspected at 32 and 128 pixels on light and dark backgrounds; the final gallery still needs its publication check.

# Provenance

BenScr.DelimitedText is a new independent implementation. It incorporates no source from private projects or third-party CSV libraries. The GitHub repository and NuGet package ID were absent when checked on 2026-11-02.

CSV quoting and record structure were reviewed against [RFC 4180, section 2](https://www.rfc-editor.org/rfc/rfc4180#section-2). The documented LF, delimiter and Unicode behavior are intentional extensions. The implementation does not claim every interpretation of CSV is interchangeable.

The tests include independently specified input/output fixtures and a comparison with the .NET platform's `Microsoft.VisualBasic.FileIO.TextFieldParser` on a compatible single-line subset. Separate tests verify exact multiline content, strict errors, character and byte boundaries, bounded reading, ownership and output failure behavior. Fixed seeds make generated cases repeatable.

Build and packaging infrastructure follows the existing BenScr library structure. There are no runtime package dependencies. The test-only xunit.v3 version is pinned to 4.0.2 with its complete dependency lock file.

The unchanged supplied icon is `branding/BenScr_Icon_Master.png`: PNG RGBA, 1254 × 1254, 795819 bytes, SHA-256 `a5dcabceb766e13c1840a95130ee98a458e12e3713fd9b07f3ceae75a1f5b993`. The same bytes are embedded as `icon.png`; no resize or graphic modification is performed.

`LICENSE.txt` preserves the existing Ben Schneider custom attribution license bytes, SHA-256 `f6600e285e17c48188a65745d7595807e1755c72201e8eef201a9174fca18573`. The package audit checks both asset hashes. This license is not represented as MIT.

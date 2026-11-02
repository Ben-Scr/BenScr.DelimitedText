# Changelog

## 1.0.0 — 2026-11-02

- Introduces bounded, synchronous `CsvReader` and `CsvWriter` wrappers over .NET text streams.
- Supports explicit delimiters and CRLF/LF record separators, multiline fields, doubled quotes and lossless Unicode text.
- Reports strict syntax and input-limit failures with physical line and UTF-16 column positions.
- Provides immutable options, explicit disposal ownership, independently owned result arrays and prevalidated output records.
- Ships XML documentation, Portable PDB symbols, Source Link and independent package-consumer verification for .NET 10.

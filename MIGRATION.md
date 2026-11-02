# Migration

Version 1.0.0 is the first release of this implementation. It does not replace an earlier public API or import an existing CSV library. There are no legacy namespaces, compatibility shims or mandatory BenScr dependencies.

When adapting an application from another CSV reader, select its actual delimiter and record separator explicitly. This reader rejects stray quotes and whitespace after closing quotes. It preserves spaces, blank records, multiline content, control characters and formula-like text; it performs no object mapping or type conversion. Applications decide whether the first record is a header and whether subsequent field counts must match.

Review input limits for your dataset. Counts are decoded UTF-16 units, not encoded bytes. An empty stream has zero records, while a blank line or a quoted empty field has one record containing one empty string. Failure faults a reader after consuming input; do not continue from the wrapper after a parse or I/O error.

The writer requires at least one non-null field, validates before output, always quotes empty fields and emits a final configured record separator. Output and flush failures may leave a partial record. Encoding, BOM behavior, file replacement and application-specific spreadsheet policies remain under the caller's control.

The supported target is .NET 10. No .NET Standard, .NET Framework, Unity, native interop, trimming or Native AOT compatibility guarantee is made by this release.

# BenScr.DelimitedText

Strict, synchronous CSV reading and writing for .NET 10. Read one bounded record at a time from a `TextReader`, or write validated records to a `TextWriter`. No runtime package dependencies.

Created by Ben Schneider — https://github.com/Ben-Scr

```shell
dotnet add package BenScr.DelimitedText --version 1.0.0
```

```csharp
using System.IO;
using BenScr.DelimitedText;

using var input = new StringReader("name,note\r\nAda,\"first\r\nsecond\"\r\n");
using var csv = new CsvReader(input, leaveOpen: true);

string[]? header = csv.ReadRecord();
string[]? row;
while ((row = csv.ReadRecord()) is not null)
{
    // Each array is independently owned and may be retained or changed.
    System.Console.WriteLine(row[1]);
}

using var output = new StringWriter();
using (var writer = new CsvWriter(output, leaveOpen: true))
{
    writer.WriteRecord(new[] { "Ada", "first\r\nsecond", "" });
    writer.Flush();
}
```

The first record is a header only when your application treats it as one. The library does not map objects, interpret formulas, convert numbers, trim whitespace, skip blank records, or require equal column counts between records.

## Format contract

The default uses comma delimiters, doubled quotes inside quoted fields, and CRLF record separators, following the quoting and record structure described in [RFC 4180 section 2](https://www.rfc-editor.org/rfc/rfc4180#section-2). A final separator is optional on input and always emitted on output. Fields containing delimiters, quotes or line breaks are quoted; the writer also quotes every empty field.

`CsvOptions` selects a different one-character delimiter and/or `CsvNewline.Lf`. Readers require the exact selected separator outside quoted fields; mixed line endings and lone CR record separators are rejected. Quoted fields preserve any CR, LF or CRLF content unchanged. Quotes in unquoted fields, whitespace after a closing quote, and unclosed quoted fields are errors.

This is an explicit text dialect: Unicode and other UTF-16 code units, including controls, are preserved rather than restricted to RFC 4180's ASCII `TEXTDATA`. A delimiter must not be a quote, a surrogate, or a control character other than tab. No delimiter detection, comments, escaping with backslashes, or BOM stripping is performed. Encoding and BOM handling belong to your `TextReader`/`TextWriter`.

| Input | Records |
| --- | --- |
| Empty text | Zero |
| `""` | One field containing an empty string |
| CRLF | One field containing an empty string |
| `a,` | Two fields: `a` and an empty string |
| `a` followed by CRLF | One record; no extra empty record |
| Two CRLF separators | Two records, each with one empty field |

Formula-like values such as `=1+1` are ordinary text and survive unchanged. CSV quoting is not spreadsheet formula neutralization. Apply any spreadsheet-specific data policy explicitly in your application before writing.

## Bounds and errors

Settings are sealed, immutable and safe to share. Every limit is positive and inclusive:

| Setting | Default | Meaning |
| --- | ---: | --- |
| `MaxFieldLength` | 1,048,576 | Decoded UTF-16 code units in one field |
| `MaxRecordLength` | 4,194,304 | Sum of decoded field lengths, excluding separators and quote syntax |
| `MaxColumns` | 16,384 | Fields per record, including empty fields |

These are character and field bounds, not byte or total-file-size limits. A supplementary Unicode character occupies two UTF-16 units. Quoted CRLF content occupies two units; a doubled quote decodes to one. All counts reset at each record.

Malformed input and exceeded input limits throw `CsvFormatException`. `Line` and `Column` are one-based; each LF advances the physical line, including inside quotes, and columns count UTF-16 units. An EOF error reports the next position. CR occupies one column until a following LF. A failed read faults that wrapper; subsequent reads throw `InvalidOperationException`. The input may already have been consumed through the offending character.

The writer snapshots field references and validates the entire record before touching its destination. Null records, null fields, zero fields and exceeded bounds are rejected without writing and leave the wrapper usable. Limits produce `ArgumentException`. An output or flush exception propagates unchanged, may leave partial output, and faults the writer. There is no rollback or automatic retry.

## Lifetime and streaming

Readers and writers are forward-only, synchronous, not thread-safe and not reentrant. `ReadRecord()` avoids implicit enumeration or repeated-enumeration semantics; it returns null at EOF and never recycles returned arrays. It calls `TextReader.Read()` only through the requested record separator. The underlying text reader may maintain its own buffer. Memory use is bounded by the configured per-record limits, not the file size.

By default, disposing a wrapper disposes its supplied reader or writer. `leaveOpen: true` keeps the underlying object open. Leaving a writer open also leaves flushing to your explicit `Flush()` call or the underlying writer's owner. `WriteRecord()` never flushes automatically. Do not otherwise use the underlying object until the wrapper is disposed. Disposal is idempotent; operations afterward throw `ObjectDisposedException`.

## Build and verification

Install .NET SDK 10.0.303 and PowerShell 7, then run from a clean committed checkout:

```powershell
pwsh -File tools/Verify.ps1
```

Verification covers formatting, tests, the executable example, a reviewed public API baseline, package inventory, license/icon hashes, XML documentation, matching Portable PDBs, Source Link checksums, and an isolated consumer with an empty package cache. CI runs the same workflow on Windows and Ubuntu. See [VERIFICATION.md](VERIFICATION.md), [RELEASE.md](RELEASE.md), and [provenance](docs/PROVENANCE.md).

Licensed under the custom [Ben Schneider attribution license](LICENSE.txt). The embedded package icon is the unchanged `BenScr_Icon_Master.png`.

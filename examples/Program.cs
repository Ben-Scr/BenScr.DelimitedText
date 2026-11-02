using System;
using System.IO;
using System.Linq;
using BenScr.DelimitedText;

using var text = new StringWriter();
using (var writer = new CsvWriter(text, leaveOpen: true))
{
    writer.WriteRecord(new[] { "name", "note", "optional" });
    writer.WriteRecord(new[] { "東京😀", "one\r\ntwo, \"quoted\"", "" });
}
Check(text.ToString() == "name,note,optional\r\n東京😀,\"one\r\ntwo, \"\"quoted\"\"\",\"\"\r\n", "Exact wire format");
using (var reader = new CsvReader(new StringReader(text.ToString())))
{
    Check(reader.ReadRecord()!.SequenceEqual(new[] { "name", "note", "optional" }), "Header");
    Check(reader.ReadRecord()!.SequenceEqual(new[] { "東京😀", "one\r\ntwo, \"quoted\"", "" }), "Lossless fields");
    Check(reader.ReadRecord() is null, "No extra record");
}
using (var reader = new CsvReader(new StringReader("a;\"b;c\";\n"), new CsvOptions(';', CsvNewline.Lf)))
{
    Check(reader.ReadRecord()!.SequenceEqual(new[] { "a", "b;c", "" }), "Explicit dialect");
}
using (var reader = new CsvReader(new StringReader("")))
{
    Check(reader.ReadRecord() is null, "Empty input");
}
ExpectFormatFailure("\"x\"z", new CsvOptions(), 1, 4);
ExpectFormatFailure("abcd", new CsvOptions(maxFieldLength: 3), 1, 4);
Console.WriteLine("CSV quoting, Unicode, multiline fields, dialects, positions and bounds verified.");

static void ExpectFormatFailure(string input, CsvOptions options, long line, long column)
{
    using var reader = new CsvReader(new StringReader(input), options);
    try
    {
        reader.ReadRecord();
        throw new InvalidOperationException("Expected a CSV format error.");
    }
    catch (CsvFormatException error)
    {
        Check(error.Line == line && error.Column == column, "Error position");
    }
}

static void Check(bool condition, string scenario)
{
    if (!condition)
    {
        throw new InvalidOperationException("Failed: " + scenario);
    }
}

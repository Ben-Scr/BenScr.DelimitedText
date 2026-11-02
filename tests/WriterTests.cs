using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using BenScr.DelimitedText;
using Microsoft.VisualBasic.FileIO;
using Xunit;

namespace BenScr.DelimitedText.Tests;

public sealed class WriterTests
{
    [Fact]
    public void WritesExactQuotingWithoutChangingValues()
    {
        using var output = new StringWriter();
        using var writer = new CsvWriter(output, leaveOpen: true);
        writer.WriteRecord(new[] { " a ", "x,y", "a\"b", "line\r\nline", "", "=1+1", "東京😀", "\0" });
        Assert.Equal(" a ,\"x,y\",\"a\"\"b\",\"line\r\nline\",\"\",=1+1,東京😀,\0\r\n", output.ToString());
    }

    [Fact]
    public void OneEmptyFieldHasAnExplicitRepresentation()
    {
        using var output = new StringWriter();
        using var writer = new CsvWriter(output);
        writer.WriteRecord(new[] { "" });
        Assert.Equal("\"\"\r\n", output.ToString());
    }

    [Fact]
    public void WriterUsesConfiguredNewlineInsteadOfTextWriterNewline()
    {
        using var output = new StringWriter { NewLine = "ignored" };
        using var writer = new CsvWriter(output, new CsvOptions(';', CsvNewline.Lf));
        writer.WriteRecord(new[] { "a;b", "x,y", "" });
        Assert.Equal("\"a;b\";x,y;\"\"\n", output.ToString());
    }

    [Fact]
    public void ValidationPrecedesAllOutputAndDoesNotFaultWriter()
    {
        using var output = new StringWriter();
        using var writer = new CsvWriter(output, new CsvOptions(maxFieldLength: 2, maxRecordLength: 3, maxColumns: 2));
        Assert.Throws<ArgumentNullException>(() => writer.WriteRecord(null!));
        Assert.Throws<ArgumentException>(() => writer.WriteRecord(Array.Empty<string>()));
        Assert.Throws<ArgumentException>(() => writer.WriteRecord(new[] { "x", null! }));
        Assert.Throws<ArgumentException>(() => writer.WriteRecord(new[] { "", "", "" }));
        Assert.Throws<ArgumentException>(() => writer.WriteRecord(new[] { "abc" }));
        Assert.Throws<ArgumentException>(() => writer.WriteRecord(new[] { "ab", "cd" }));
        Assert.Equal("", output.ToString());
        writer.WriteRecord(new[] { "ab", "c" });
        writer.WriteRecord(new[] { "ab", "c" });
        Assert.Equal("ab,c\r\nab,c\r\n", output.ToString());
    }

    [Fact]
    public void CollectionAccessFailureWritesNothingAndLeavesWriterUsable()
    {
        using var output = new StringWriter();
        using var writer = new CsvWriter(output);
        Assert.Throws<InvalidOperationException>(() => writer.WriteRecord(new FailingList()));
        Assert.Equal("", output.ToString());
        writer.WriteRecord(new[] { "ok" });
        Assert.Equal("ok\r\n", output.ToString());
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void WriterOwnershipAndFlushAreExplicit(bool leaveOpen)
    {
        using var output = new ObservedWriter();
        var writer = new CsvWriter(output, leaveOpen: leaveOpen);
        writer.WriteRecord(new[] { "a" });
        Assert.Equal(0, output.FlushCount);
        writer.Flush();
        Assert.Equal(1, output.FlushCount);
        writer.Dispose();
        writer.Dispose();
        Assert.Equal(!leaveOpen, output.Disposed);
        Assert.Equal(1, output.FlushCount);
        Assert.Throws<ObjectDisposedException>(() => writer.WriteRecord(new[] { "a" }));
        Assert.Throws<ObjectDisposedException>(() => writer.Flush());
    }

    [Fact]
    public void PartialWriteFailureIsPropagatedAndFaultsWriter()
    {
        using var output = new ObservedWriter { FailAfter = 4 };
        using var writer = new CsvWriter(output);
        Assert.Same(output.Failure, Assert.Throws<IOException>(() => writer.WriteRecord(new[] { "a,b", "c" })));
        Assert.Equal("\"a,b", output.ToString());
        Assert.Throws<InvalidOperationException>(() => writer.WriteRecord(new[] { "ok" }));
        Assert.Throws<InvalidOperationException>(() => writer.Flush());
    }

    [Fact]
    public void FlushFailureFaultsWriter()
    {
        using var output = new ObservedWriter { FailFlush = true };
        using var writer = new CsvWriter(output);
        Assert.Same(output.Failure, Assert.Throws<IOException>(() => writer.Flush()));
        Assert.Throws<InvalidOperationException>(() => writer.WriteRecord(new[] { "ok" }));
    }

    [Fact]
    public void FixedSeedRecordsAgreeWithIndependentPlatformCsvParser()
    {
        var random = new Random(716_303);
        const string Alphabet = "abc XYZ,\"09é東京";
        for (int iteration = 0; iteration < 200; iteration++)
        {
            string[] expected = Enumerable.Range(0, random.Next(1, 10)).Select(_ =>
                "v" + new string(Enumerable.Range(0, random.Next(30)).Select(_ => Alphabet[random.Next(Alphabet.Length)]).ToArray())).ToArray();
            using var text = new StringWriter();
            using (var writer = new CsvWriter(text, leaveOpen: true))
            {
                writer.WriteRecord(expected);
            }
            using var reference = new TextFieldParser(new StringReader(text.ToString()))
            {
                TextFieldType = FieldType.Delimited,
                HasFieldsEnclosedInQuotes = true,
                TrimWhiteSpace = false
            };
            reference.SetDelimiters(",");
            Assert.Equal(expected, reference.ReadFields());
            using var csv = new CsvReader(new StringReader(text.ToString()));
            Assert.Equal(expected, csv.ReadRecord());
        }
    }

    [Fact]
    public void FixedSeedMultilineUnicodeRecordsRoundTripAcrossBothDialects()
    {
        var random = new Random(4180);
        string[] tokens = { "a", ",", ";", "\t", "\r", "\n", "\"", " ", "東京", "😀", "é", "\0", "=1+1", "\uFEFF" };
        foreach (CsvNewline newline in new[] { CsvNewline.CrLf, CsvNewline.Lf })
        {
            var options = new CsvOptions(';', newline, maxFieldLength: 1000, maxRecordLength: 10000);
            var records = new List<string[]>();
            using var output = new StringWriter();
            using (var writer = new CsvWriter(output, options, true))
            {
                for (int iteration = 0; iteration < 500; iteration++)
                {
                    string[] fields = Enumerable.Range(0, random.Next(1, 15)).Select(_ =>
                        string.Concat(Enumerable.Range(0, random.Next(30)).Select(_ => tokens[random.Next(tokens.Length)]))).ToArray();
                    records.Add(fields);
                    writer.WriteRecord(fields);
                }
            }
            using var reader = new CsvReader(new StringReader(output.ToString()), options);
            foreach (string[] expected in records)
            {
                Assert.Equal(expected, reader.ReadRecord());
            }
            Assert.Null(reader.ReadRecord());
        }
    }

    private sealed class FailingList : IReadOnlyList<string>
    {
        public int Count => 2;
        public string this[int index] => index == 0 ? "first" : throw new InvalidOperationException();
        public IEnumerator<string> GetEnumerator() => throw new NotSupportedException();
        System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator() => GetEnumerator();
    }

    private sealed class ObservedWriter : StringWriter
    {
        public bool Disposed { get; private set; }
        public int FlushCount { get; private set; }
        public int FailAfter { get; init; } = int.MaxValue;
        public bool FailFlush { get; init; }
        public IOException Failure { get; } = new IOException("Output failed.");

        public override void Write(char value)
        {
            if (GetStringBuilder().Length == FailAfter)
            {
                throw Failure;
            }
            base.Write(value);
        }

        public override void Flush()
        {
            if (FailFlush)
            {
                throw Failure;
            }
            FlushCount++;
        }

        protected override void Dispose(bool disposing)
        {
            Disposed = true;
            base.Dispose(disposing);
        }
    }
}

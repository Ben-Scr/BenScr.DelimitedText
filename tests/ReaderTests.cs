using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using BenScr.DelimitedText;
using Xunit;

namespace BenScr.DelimitedText.Tests;

public sealed class ReaderTests
{
    public static IEnumerable<object[]> Records()
    {
        yield return new object[] { "a,b,c", new[] { "a", "b", "c" } };
        yield return new object[] { "\"a\",\"b\",\"c\"\r\n", new[] { "a", "b", "c" } };
        yield return new object[] { "a,\"b,c\",d", new[] { "a", "b,c", "d" } };
        yield return new object[] { "\"a\"\"b\",c", new[] { "a\"b", "c" } };
        yield return new object[] { "\"a\r\nb\",c", new[] { "a\r\nb", "c" } };
        yield return new object[] { "\"a\nb\rc\",d", new[] { "a\nb\rc", "d" } };
        yield return new object[] { " a , b ", new[] { " a ", " b " } };
        yield return new object[] { ",", new[] { "", "" } };
        yield return new object[] { "a,", new[] { "a", "" } };
        yield return new object[] { "\"\"", new[] { "" } };
        yield return new object[] { "\r\n", new[] { "" } };
        yield return new object[] { "\"\"\"\"", new[] { "\"" } };
        yield return new object[] { "Grüße,東京,😀", new[] { "Grüße", "東京", "😀" } };
        yield return new object[] { "=1+1,\0,\uFEFFname", new[] { "=1+1", "\0", "\uFEFFname" } };
    }

    [Theory]
    [MemberData(nameof(Records))]
    public void ParsesKnownRecords(string text, string[] expected)
    {
        using var csv = new CsvReader(new StringReader(text));
        Assert.Equal(expected, csv.ReadRecord());
        Assert.Null(csv.ReadRecord());
        Assert.Null(csv.ReadRecord());
    }

    [Fact]
    public void EmptyInputHasNoRecordButTwoBlankLinesHaveTwoRecords()
    {
        using var empty = new CsvReader(new StringReader(""));
        Assert.Null(empty.ReadRecord());
        using var blank = new CsvReader(new StringReader("\r\n\r\n"));
        Assert.Equal(new[] { "" }, blank.ReadRecord());
        Assert.Equal(new[] { "" }, blank.ReadRecord());
        Assert.Null(blank.ReadRecord());
    }

    [Fact]
    public void ArraysAreIndependentAndColumnCountsMayDiffer()
    {
        using var csv = new CsvReader(new StringReader("name,value\r\none\r\ntwo,2,"));
        string[] header = csv.ReadRecord()!;
        Assert.Equal(new[] { "one" }, csv.ReadRecord());
        header[0] = "changed";
        Assert.Equal(new[] { "two", "2", "" }, csv.ReadRecord());
    }

    [Theory]
    [InlineData('；')]
    [InlineData(';')]
    [InlineData('\t')]
    public void ExplicitDelimiterAndLfAreRespected(char delimiter)
    {
        using var csv = new CsvReader(new StringReader($"a{delimiter}\"x{delimiter}y\"\n"), new CsvOptions(delimiter, CsvNewline.Lf));
        Assert.Equal(new[] { "a", $"x{delimiter}y" }, csv.ReadRecord());
        Assert.Null(csv.ReadRecord());
    }

    [Theory]
    [InlineData("a\"b", 1, 2)]
    [InlineData(" \"a\"", 1, 2)]
    [InlineData("\"a\" x", 1, 4)]
    [InlineData("\"abc", 1, 5)]
    [InlineData("\"a\"\"", 1, 5)]
    [InlineData("a\n", 1, 2)]
    [InlineData("a\r", 1, 3)]
    [InlineData("a\rx", 1, 3)]
    [InlineData("\"a\nb\"x", 2, 3)]
    [InlineData("\"😀\"x", 1, 5)]
    public void RejectsInvalidSyntaxAtExactPositionAndFaults(string text, long line, long column)
    {
        using var csv = new CsvReader(new StringReader(text));
        CsvFormatException error = Assert.Throws<CsvFormatException>(() => csv.ReadRecord());
        Assert.Equal(line, error.Line);
        Assert.Equal(column, error.Column);
        Assert.Throws<InvalidOperationException>(() => csv.ReadRecord());
    }

    [Fact]
    public void TracksPositionsAcrossRecordsAndCrLfInQuotedFields()
    {
        using var csv = new CsvReader(new StringReader("head\r\n\"a\r\nb\"x"));
        Assert.Equal(new[] { "head" }, csv.ReadRecord());
        CsvFormatException error = Assert.Throws<CsvFormatException>(() => csv.ReadRecord());
        Assert.Equal(3, error.Line);
        Assert.Equal(3, error.Column);
    }

    [Fact]
    public void LfModeRejectsCrLfOutsideQuotes()
    {
        using var csv = new CsvReader(new StringReader("a\r\n"), new CsvOptions(newline: CsvNewline.Lf));
        Assert.Equal(2, Assert.Throws<CsvFormatException>(() => csv.ReadRecord()).Column);
    }

    [Fact]
    public void LengthLimitsCountDecodedCodeUnitsAndResetPerRecord()
    {
        var options = new CsvOptions(maxFieldLength: 2, maxRecordLength: 3, maxColumns: 2);
        using var csv = new CsvReader(new StringReader("\"\"\"x\",y\r\nab,c"), options);
        Assert.Equal(new[] { "\"x", "y" }, csv.ReadRecord());
        Assert.Equal(new[] { "ab", "c" }, csv.ReadRecord());
        using var field = new CsvReader(new StringReader("abc"), options);
        Assert.Equal(3, Assert.Throws<CsvFormatException>(() => field.ReadRecord()).Column);
        using var record = new CsvReader(new StringReader("ab,cd"), options);
        Assert.Equal(5, Assert.Throws<CsvFormatException>(() => record.ReadRecord()).Column);
        using var columns = new CsvReader(new StringReader(",,"), options);
        Assert.Equal(2, Assert.Throws<CsvFormatException>(() => columns.ReadRecord()).Column);
    }

    [Fact]
    public void BoundStopsInputWithoutScanningTheRest()
    {
        using var input = new ObservedReader("\"" + new string('x', 100_000) + "\"");
        using var csv = new CsvReader(input, new CsvOptions(maxFieldLength: 3));
        Assert.Throws<CsvFormatException>(() => csv.ReadRecord());
        Assert.Equal(5, input.ReadCount);
    }

    [Fact]
    public void ReadsOnlyTheRequestedRecordAndDoesNotReadAfterEof()
    {
        using var input = new ObservedReader("a,b\r\nc,d");
        using var csv = new CsvReader(input, leaveOpen: true);
        Assert.Equal(0, input.ReadCount);
        Assert.Equal(new[] { "a", "b" }, csv.ReadRecord());
        Assert.Equal(5, input.ReadCount);
        Assert.Equal(new[] { "c", "d" }, csv.ReadRecord());
        Assert.Equal(9, input.ReadCount);
        Assert.Null(csv.ReadRecord());
        Assert.Equal(9, input.ReadCount);
    }

    [Theory]
    [InlineData(1)]
    [InlineData(2)]
    [InlineData(3)]
    [InlineData(7)]
    public void HandlesUtf8ByteAndQuoteBoundaries(int chunkSize)
    {
        byte[] bytes = Encoding.UTF8.GetBytes("\"Grüße😀\r\n東京\",\"a\"\"b\",\r\nend");
        using var stream = new ChunkedStream(bytes, chunkSize);
        using var text = new StreamReader(stream, new UTF8Encoding(false, true), false, 128);
        using var csv = new CsvReader(text);
        Assert.Equal(new[] { "Grüße😀\r\n東京", "a\"b", "" }, csv.ReadRecord());
        Assert.Equal(new[] { "end" }, csv.ReadRecord());
        Assert.Null(csv.ReadRecord());
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void HonorsInputOwnership(bool leaveOpen)
    {
        using var input = new ObservedReader("a");
        var csv = new CsvReader(input, leaveOpen: leaveOpen);
        csv.Dispose();
        csv.Dispose();
        Assert.Equal(!leaveOpen, input.Disposed);
        Assert.Throws<ObjectDisposedException>(() => csv.ReadRecord());
    }

    [Fact]
    public void InputExceptionIsNotWrappedAndFaultsReader()
    {
        using var input = new ObservedReader("a") { FailAt = 0 };
        using var csv = new CsvReader(input);
        Assert.Same(input.Failure, Assert.Throws<IOException>(() => csv.ReadRecord()));
        Assert.Throws<InvalidOperationException>(() => csv.ReadRecord());
    }

    private sealed class ObservedReader(string text) : TextReader
    {
        public int ReadCount { get; private set; }
        public bool Disposed { get; private set; }
        public int FailAt { get; init; } = -1;
        public IOException Failure { get; } = new IOException("Read failed.");

        public override int Read()
        {
            if (ReadCount == FailAt)
            {
                throw Failure;
            }
            int index = ReadCount++;
            return index < text.Length ? text[index] : -1;
        }

        protected override void Dispose(bool disposing)
        {
            Disposed = true;
            base.Dispose(disposing);
        }
    }

    private sealed class ChunkedStream(byte[] bytes, int chunkSize) : MemoryStream(bytes)
    {
        public override int Read(byte[] buffer, int offset, int count) => base.Read(buffer, offset, Math.Min(count, chunkSize));
        public override int Read(Span<byte> buffer) => base.Read(buffer[..Math.Min(buffer.Length, chunkSize)]);
    }
}

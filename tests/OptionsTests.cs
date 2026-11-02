using System;
using BenScr.DelimitedText;
using Xunit;

namespace BenScr.DelimitedText.Tests;

public sealed class OptionsTests
{
    [Fact]
    public void DefaultOptionsAreBounded()
    {
        var options = new CsvOptions();
        Assert.Equal(',', options.Delimiter);
        Assert.Equal(CsvNewline.CrLf, options.Newline);
        Assert.Equal(1_048_576, options.MaxFieldLength);
        Assert.Equal(4_194_304, options.MaxRecordLength);
        Assert.Equal(16_384, options.MaxColumns);
    }

    [Theory]
    [InlineData('"')]
    [InlineData('\r')]
    [InlineData('\n')]
    [InlineData('\0')]
    [InlineData('\u007f')]
    [InlineData('\ud800')]
    public void InvalidDelimiterIsRejected(char delimiter)
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => new CsvOptions(delimiter));
    }

    [Fact]
    public void InvalidLimitsAndNewlineAreRejected()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => new CsvOptions(newline: (CsvNewline)99));
        Assert.Throws<ArgumentOutOfRangeException>(() => new CsvOptions(maxFieldLength: 0));
        Assert.Throws<ArgumentOutOfRangeException>(() => new CsvOptions(maxRecordLength: -1));
        Assert.Throws<ArgumentOutOfRangeException>(() => new CsvOptions(maxColumns: 0));
    }

    [Fact]
    public void NullSourcesAndInvalidErrorPositionsAreRejected()
    {
        Assert.Throws<ArgumentNullException>(() => new CsvReader(null!));
        Assert.Throws<ArgumentNullException>(() => new CsvWriter(null!));
        Assert.Throws<ArgumentNullException>(() => new CsvFormatException(null!, 1, 1));
        Assert.Throws<ArgumentOutOfRangeException>(() => new CsvFormatException("error", 0, 1));
        Assert.Throws<ArgumentOutOfRangeException>(() => new CsvFormatException("error", 1, 0));
    }
}

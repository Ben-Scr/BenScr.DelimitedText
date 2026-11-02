using System;

namespace BenScr.DelimitedText;

/// <summary>A CSV syntax error or resource-limit violation with a one-based position.</summary>
public sealed class CsvFormatException : FormatException
{
    /// <summary>Creates an error at a position in the text.</summary>
    /// <param name="message">An explanation of the failure.</param>
    /// <param name="line">One-based physical line. Each LF advances a line, including in quoted fields.</param>
    /// <param name="column">One-based UTF-16 column. CR counts as one code unit until a following LF.</param>
    /// <exception cref="ArgumentNullException"><paramref name="message"/> is null.</exception>
    /// <exception cref="ArgumentOutOfRangeException">A position is not positive.</exception>
    public CsvFormatException(string message, long line, long column)
        : base($"{message} (line {line}, column {column}).")
    {
        ArgumentNullException.ThrowIfNull(message);
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(line);
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(column);
        Line = line;
        Column = column;
    }

    /// <summary>Gets the one-based physical line of the invalid character, or the next position at EOF.</summary>
    public long Line { get; }

    /// <summary>Gets the one-based UTF-16 column of the invalid character, or the next position at EOF.</summary>
    public long Column { get; }
}

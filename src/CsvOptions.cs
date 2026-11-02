using System;

namespace BenScr.DelimitedText;

/// <summary>Immutable CSV dialect and inclusive resource limits, safe to share between instances.</summary>
public sealed class CsvOptions
{
    /// <summary>Creates settings for comma-separated records terminated by CRLF unless overridden.</summary>
    /// <param name="delimiter">One non-control, non-surrogate character other than a double quote. Tab is also allowed.</param>
    /// <param name="newline">The exact separator outside quoted fields; quoted line endings are always preserved.</param>
    /// <param name="maxFieldLength">Maximum decoded UTF-16 code units per field, inclusive; must be positive.</param>
    /// <param name="maxRecordLength">Maximum sum of decoded field lengths per record, inclusive; must be positive. Separators and quoting syntax are excluded.</param>
    /// <param name="maxColumns">Maximum fields per record, inclusive; must be positive.</param>
    /// <exception cref="ArgumentOutOfRangeException">A delimiter, enum value or limit is invalid.</exception>
    public CsvOptions(char delimiter = ',', CsvNewline newline = CsvNewline.CrLf,
        int maxFieldLength = 1_048_576, int maxRecordLength = 4_194_304, int maxColumns = 16_384)
    {
        if (delimiter == '"' || char.IsSurrogate(delimiter) || (char.IsControl(delimiter) && delimiter != '\t'))
        {
            throw new ArgumentOutOfRangeException(nameof(delimiter));
        }
        if (newline != CsvNewline.CrLf && newline != CsvNewline.Lf)
        {
            throw new ArgumentOutOfRangeException(nameof(newline));
        }
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(maxFieldLength);
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(maxRecordLength);
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(maxColumns);
        Delimiter = delimiter;
        Newline = newline;
        MaxFieldLength = maxFieldLength;
        MaxRecordLength = maxRecordLength;
        MaxColumns = maxColumns;
    }

    /// <summary>Gets the field delimiter.</summary>
    public char Delimiter { get; }

    /// <summary>Gets the exact record separator.</summary>
    public CsvNewline Newline { get; }

    /// <summary>Gets the inclusive decoded UTF-16 field-length limit.</summary>
    public int MaxFieldLength { get; }

    /// <summary>Gets the inclusive sum of decoded UTF-16 field lengths in a record.</summary>
    public int MaxRecordLength { get; }

    /// <summary>Gets the inclusive number of fields allowed per record.</summary>
    public int MaxColumns { get; }
}

namespace BenScr.DelimitedText;

/// <summary>The exact record separator accepted by a reader and emitted by a writer.</summary>
public enum CsvNewline
{
    /// <summary>A carriage return followed by a line feed, as described in RFC 4180.</summary>
    CrLf,

    /// <summary>A line feed, for files using the common LF dialect.</summary>
    Lf
}

using System;
using System.Collections.Generic;
using System.IO;

namespace BenScr.DelimitedText;

/// <summary>A synchronous CSV writer that validates and snapshots each record before writing it.</summary>
/// <remarks>Not thread-safe or reentrant. Do not otherwise use the supplied writer until this wrapper is disposed.
/// Output exceptions may leave a partial record and fault the instance. CSV quoting does not neutralize spreadsheet formulas.</remarks>
public sealed class CsvWriter : IDisposable
{
    private readonly TextWriter writer;
    private readonly CsvOptions options;
    private readonly bool leaveOpen;
    private bool disposed;
    private bool faulted;

    /// <summary>Wraps a text writer without writing to it.</summary>
    /// <param name="writer">The destination, whose encoding and BOM handling belong to its caller.</param>
    /// <param name="options">Immutable settings, or null for defaults.</param>
    /// <param name="leaveOpen">Whether disposal leaves the destination open. If true, disposal does not flush it.</param>
    /// <exception cref="ArgumentNullException"><paramref name="writer"/> is null.</exception>
    public CsvWriter(TextWriter writer, CsvOptions? options = null, bool leaveOpen = false)
    {
        ArgumentNullException.ThrowIfNull(writer);
        this.writer = writer;
        this.options = options ?? new CsvOptions();
        this.leaveOpen = leaveOpen;
    }

    /// <summary>Writes a record followed by the configured separator. Empty fields are always quoted.</summary>
    /// <param name="fields">One or more non-null strings. The collection must not change during this call.</param>
    /// <exception cref="ArgumentNullException"><paramref name="fields"/> is null.</exception>
    /// <exception cref="ArgumentException">A field is null, the record is empty, or a configured limit is exceeded.</exception>
    /// <exception cref="ObjectDisposedException">The instance is disposed.</exception>
    /// <exception cref="InvalidOperationException">An earlier output failure faulted the writer.</exception>
    /// <remarks>Invalid input writes nothing and leaves the writer usable. Each field reference is copied before any output;
    /// strings are immutable. Output exceptions propagate unchanged and may leave partial output.
    /// This method does not flush. Headers are written as ordinary records. It never trims or changes field values.</remarks>
    public void WriteRecord(IReadOnlyList<string> fields)
    {
        EnsureUsable();
        ArgumentNullException.ThrowIfNull(fields);
        int count = fields.Count;
        if (count < 1 || count > options.MaxColumns)
        {
            throw new ArgumentException("Record must contain between one and MaxColumns fields.", nameof(fields));
        }
        var snapshot = new string[count];
        int recordLength = 0;
        for (int index = 0; index < count; index++)
        {
            string value = fields[index] ?? throw new ArgumentException("Fields must not be null.", nameof(fields));
            if (value.Length > options.MaxFieldLength || value.Length > options.MaxRecordLength - recordLength)
            {
                throw new ArgumentException("Field or record length limit exceeded.", nameof(fields));
            }
            snapshot[index] = value;
            recordLength += value.Length;
        }
        try
        {
            for (int index = 0; index < snapshot.Length; index++)
            {
                if (index != 0)
                {
                    writer.Write(options.Delimiter);
                }
                WriteField(snapshot[index]);
            }
            writer.Write(options.Newline == CsvNewline.CrLf ? "\r\n" : "\n");
        }
        catch
        {
            faulted = true;
            throw;
        }
    }

    /// <summary>Flushes the destination. Flush failures propagate unchanged and fault this instance.</summary>
    /// <exception cref="ObjectDisposedException">The instance is disposed.</exception>
    /// <exception cref="InvalidOperationException">An earlier output failure faulted the writer.</exception>
    public void Flush()
    {
        EnsureUsable();
        try
        {
            writer.Flush();
        }
        catch
        {
            faulted = true;
            throw;
        }
    }

    /// <summary>Disposes this wrapper and, unless leaveOpen was true, its destination. Repeated disposal is harmless.</summary>
    /// <remarks>Owned destination disposal may flush and throw. This wrapper stays disposed if that happens.</remarks>
    public void Dispose()
    {
        if (disposed)
        {
            return;
        }
        disposed = true;
        if (!leaveOpen)
        {
            writer.Dispose();
        }
    }

    private void WriteField(string field)
    {
        bool quoted = field.Length == 0;
        foreach (char character in field)
        {
            quoted |= character == options.Delimiter || character == '"' || character == '\r' || character == '\n';
        }
        if (!quoted)
        {
            writer.Write(field);
            return;
        }
        writer.Write('"');
        foreach (char character in field)
        {
            if (character == '"')
            {
                writer.Write('"');
            }
            writer.Write(character);
        }
        writer.Write('"');
    }

    private void EnsureUsable()
    {
        ObjectDisposedException.ThrowIf(disposed, this);
        if (faulted)
        {
            throw new InvalidOperationException("The writer is faulted and cannot resume.");
        }
    }
}

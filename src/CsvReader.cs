using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace BenScr.DelimitedText;

/// <summary>A synchronous, forward-only CSV reader that retains at most one bounded record.</summary>
/// <remarks>Not thread-safe or reentrant. No read-ahead beyond the current record separator is performed.
/// Do not otherwise use the supplied reader until this instance is disposed. Syntax and input failures
/// fault the instance; dispose it rather than attempting to resume. Headers are ordinary records.</remarks>
public sealed class CsvReader : IDisposable
{
    private readonly TextReader reader;
    private readonly CsvOptions options;
    private readonly bool leaveOpen;
    private long line = 1;
    private long column = 1;
    private bool disposed;
    private bool faulted;
    private bool ended;

    /// <summary>Wraps a text reader without reading from it.</summary>
    /// <param name="reader">The text source, whose encoding and BOM handling are managed by its caller.</param>
    /// <param name="options">Immutable settings, or null for defaults.</param>
    /// <param name="leaveOpen">Whether disposal leaves the supplied reader open; default false transfers disposal ownership.</param>
    /// <exception cref="ArgumentNullException"><paramref name="reader"/> is null.</exception>
    public CsvReader(TextReader reader, CsvOptions? options = null, bool leaveOpen = false)
    {
        ArgumentNullException.ThrowIfNull(reader);
        this.reader = reader;
        this.options = options ?? new CsvOptions();
        this.leaveOpen = leaveOpen;
    }

    /// <summary>Reads the next record into an independently owned array, or returns null at EOF.</summary>
    /// <returns>One or more fields. Empty input has no records; a blank line or a quoted empty field has one empty field.
    /// A final separator does not create an extra record. The final record need not have a separator.</returns>
    /// <exception cref="CsvFormatException">Invalid quoting, an incorrect separator, or a resource limit was encountered.</exception>
    /// <exception cref="ObjectDisposedException">The instance is disposed.</exception>
    /// <exception cref="InvalidOperationException">A previous input or parsing failure faulted the instance.</exception>
    /// <remarks>Underlying read exceptions propagate unchanged and fault this instance. Returned arrays are never reused.
    /// Spaces, Unicode, control characters, embedded line endings and formula-like strings are preserved.</remarks>
    public string[]? ReadRecord()
    {
        ObjectDisposedException.ThrowIf(disposed, this);
        if (faulted)
        {
            throw new InvalidOperationException("The reader is faulted and cannot resume.");
        }
        if (ended)
        {
            return null;
        }
        try
        {
            return ReadCore();
        }
        catch
        {
            faulted = true;
            throw;
        }
    }

    /// <summary>Disposes this wrapper and, unless leaveOpen was true, its input. Repeated disposal is harmless.</summary>
    /// <remarks>Marks this wrapper disposed even if disposal of the input throws.</remarks>
    public void Dispose()
    {
        if (disposed)
        {
            return;
        }
        disposed = true;
        if (!leaveOpen)
        {
            reader.Dispose();
        }
    }

    private string[]? ReadCore()
    {
        var fields = new List<string>();
        var field = new StringBuilder();
        int recordLength = 0;
        State state = State.Start;
        bool anyInput = false;
        while (true)
        {
            int value = ReadCharacter(out long characterLine, out long characterColumn);
            if (value < 0)
            {
                if (state == State.Quoted)
                {
                    throw new CsvFormatException("Unterminated quoted field", characterLine, characterColumn);
                }
                ended = true;
                if (!anyInput)
                {
                    return null;
                }
                fields.Add(field.ToString());
                return fields.ToArray();
            }
            anyInput = true;
            char character = (char)value;
            if (state == State.Quoted)
            {
                if (character == '"')
                {
                    state = State.AfterQuote;
                }
                else
                {
                    Append(character, characterLine, characterColumn);
                }
                continue;
            }
            if (character == '"')
            {
                if (state == State.Start)
                {
                    state = State.Quoted;
                }
                else if (state == State.AfterQuote)
                {
                    Append('"', characterLine, characterColumn);
                    state = State.Quoted;
                }
                else
                {
                    throw new CsvFormatException("Quote inside an unquoted field", characterLine, characterColumn);
                }
                continue;
            }
            if (character == options.Delimiter)
            {
                if (fields.Count == options.MaxColumns - 1)
                {
                    throw new CsvFormatException("Column limit exceeded", characterLine, characterColumn);
                }
                fields.Add(field.ToString());
                field.Clear();
                state = State.Start;
                continue;
            }
            if (character == '\r' || character == '\n')
            {
                if (options.Newline == CsvNewline.CrLf)
                {
                    if (character != '\r')
                    {
                        throw new CsvFormatException("Expected CRLF record separator", characterLine, characterColumn);
                    }
                    if (ReadCharacter(out characterLine, out characterColumn) != '\n')
                    {
                        throw new CsvFormatException("Expected LF after CR", characterLine, characterColumn);
                    }
                }
                else if (character != '\n')
                {
                    throw new CsvFormatException("Expected LF record separator", characterLine, characterColumn);
                }
                fields.Add(field.ToString());
                return fields.ToArray();
            }
            if (state == State.AfterQuote)
            {
                throw new CsvFormatException("Unexpected character after a closing quote", characterLine, characterColumn);
            }
            Append(character, characterLine, characterColumn);
            state = State.Unquoted;
        }

        void Append(char character, long characterLine, long characterColumn)
        {
            if (field.Length == options.MaxFieldLength)
            {
                throw new CsvFormatException("Field length limit exceeded", characterLine, characterColumn);
            }
            if (recordLength == options.MaxRecordLength)
            {
                throw new CsvFormatException("Record length limit exceeded", characterLine, characterColumn);
            }
            field.Append(character);
            recordLength++;
        }
    }

    private int ReadCharacter(out long characterLine, out long characterColumn)
    {
        characterLine = line;
        characterColumn = column;
        int character = reader.Read();
        if (character == '\n')
        {
            line++;
            column = 1;
        }
        else if (character >= 0)
        {
            column++;
        }
        return character;
    }

    private enum State
    {
        Start,
        Unquoted,
        Quoted,
        AfterQuote
    }
}

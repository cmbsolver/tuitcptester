namespace tuitcptester.Models;

/// <summary>
/// Represents a single data transaction.
/// </summary>
public class Transaction
{
    /// <summary>
    /// Gets or sets the data to be sent.
    /// </summary>
    public string Data { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the encoding used for the data.
    /// </summary>
    public TransactionEncoding Encoding { get; set; } = TransactionEncoding.Ascii;

    /// <summary>
    /// Gets or sets whether to append a carriage return (\r) to the data.
    /// </summary>
    public bool AppendReturn { get; set; }

    /// <summary>
    /// Gets or sets whether to append a newline (\n) to the data.
    /// </summary>
    public bool AppendNewline { get; set; }
}

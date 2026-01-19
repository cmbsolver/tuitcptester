namespace tuitcptester.Models;

/// <summary>
/// Specifies the encoding used for transactions.
/// </summary>
public enum TransactionEncoding
{
    /// <summary>
    /// Plain text ASCII encoding.
    /// </summary>
    Ascii,
    /// <summary>
    /// Hexadecimal string representation.
    /// </summary>
    Hex,
    /// <summary>
    /// Binary data (typically represented as Base64 in config).
    /// </summary>
    Binary
}

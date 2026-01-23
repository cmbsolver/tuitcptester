using System.Text;

namespace tuitcptester.Logic;

/// <summary>
/// Utility class for formatting and converting data.
/// </summary>
public static class DataUtils
{
    /// <summary>
    /// Converts a byte array to a hexadecimal string.
    /// </summary>
    /// <param name="data">The byte array to convert.</param>
    /// <returns>A hexadecimal string representation of the data.</returns>
    public static string ToHexString(byte[] data)
    {
        return string.Join(" ", data.Select(b => b.ToString("x2")));
    }

    /// <summary>
    /// Converts a portion of a byte array to a hexadecimal string.
    /// </summary>
    /// <param name="data">The byte array to convert.</param>
    /// <param name="offset">The starting offset in the array.</param>
    /// <param name="count">The number of bytes to convert.</param>
    /// <returns>A hexadecimal string representation of the data.</returns>
    public static string ToHexString(byte[] data, int offset, int count)
    {
        return string.Join(" ", data.Skip(offset).Take(count).Select(b => b.ToString("x2")));
    }

    /// <summary>
    /// Converts a hexadecimal string to a byte array.
    /// </summary>
    /// <param name="hex">The hex string to convert.</param>
    /// <returns>A byte array representing the hex data.</returns>
    public static byte[] HexToBytes(string hex)
    {
        hex = hex.Replace("-", "").Replace(" ", "").Replace("\n", "").Replace("\r", "");
        if (hex.Length % 2 != 0) throw new ArgumentException("Hex string must have an even length.");

        var bytes = new byte[hex.Length / 2];
        for (var i = 0; i < hex.Length; i += 2)
            bytes[i / 2] = Convert.ToByte(hex.Substring(i, 2), 16);
        return bytes;
    }
}

using System.Net.Sockets;
using System.Text;

namespace tuitcptester.Logic;

/// <summary>
/// Provides logic for generating and sending custom packets (hex/raw) to a target.
/// </summary>
public class PacketGenerator
{
    /// <summary>
    /// Sends a custom packet multiple times with a specified delay.
    /// </summary>
    /// <param name="host">Target host.</param>
    /// <param name="port">Target port.</param>
    /// <param name="hexData">The data to send in hexadecimal format.</param>
    /// <param name="iterations">Number of times to send the packet.</param>
    /// <param name="delayMs">Delay between iterations in milliseconds.</param>
    /// <param name="onLog">Callback for logging progress.</param>
    /// <param name="cancellationToken">Token to cancel the operation.</param>
    public static async Task RunAsync(string host, int port, string hexData, int iterations, int delayMs, Action<string>? onLog = null, CancellationToken cancellationToken = default)
    {
        byte[] data;
        try
        {
            data = HexToBytes(hexData);
        }
        catch (Exception ex)
        {
            onLog?.Invoke($"[PacketGen] Invalid hex data: {ex.Message}");
            return;
        }

        using var client = new TcpClient();
        try
        {
            onLog?.Invoke($"[PacketGen] Connecting to {host}:{port}...");
            await client.ConnectAsync(host, port, cancellationToken);
            using var stream = client.GetStream();
            onLog?.Invoke($"[PacketGen] Connected. Sending {iterations} packets...");

            for (int i = 0; i < iterations; i++)
            {
                if (cancellationToken.IsCancellationRequested) break;

                await stream.WriteAsync(data, 0, data.Length, cancellationToken);
                onLog?.Invoke($"[PacketGen] Sent packet {i + 1}/{iterations} ({data.Length} bytes)");

                if (delayMs > 0 && i < iterations - 1)
                {
                    await Task.Delay(delayMs, cancellationToken);
                }
            }
            onLog?.Invoke("[PacketGen] Done.");
        }
        catch (Exception ex)
        {
            onLog?.Invoke($"[PacketGen] Error: {ex.Message}");
        }
    }

    /// <summary>
    /// Converts a hexadecimal string to a byte array.
    /// </summary>
    private static byte[] HexToBytes(string hex)
    {
        hex = hex.Replace("-", "").Replace(" ", "").Replace("\n", "").Replace("\r", "");
        if (hex.Length % 2 != 0) throw new ArgumentException("Hex string must have an even length.");
        
        var bytes = new byte[hex.Length / 2];
        for (var i = 0; i < hex.Length; i += 2)
            bytes[i / 2] = Convert.ToByte(hex.Substring(i, 2), 16);
        return bytes;
    }
}

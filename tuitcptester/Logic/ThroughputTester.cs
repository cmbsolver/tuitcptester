using System.Diagnostics;
using System.Net.Sockets;

namespace tuitcptester.Logic;

/// <summary>
/// Provides logic for testing TCP throughput and connection stress.
/// </summary>
public class ThroughputTester
{
    /// <summary>
    /// Represents the results of a throughput test.
    /// </summary>
    /// <param name="TotalBytesSent">Total bytes sent during the test.</param>
    /// <param name="Duration">Actual duration of the test.</param>
    /// <param name="BytesPerSecond">Average throughput in bytes per second.</param>
    /// <param name="Success">Whether the test completed without critical errors.</param>
    public record ThroughputResult(long TotalBytesSent, TimeSpan Duration, double BytesPerSecond, bool Success);

    /// <summary>
    /// Runs a throughput test by sending data as fast as possible to a target for a specified duration.
    /// </summary>
    /// <param name="host">Target host.</param>
    /// <param name="port">Target port.</param>
    /// <param name="durationSeconds">How long to run the test.</param>
    /// <param name="onProgress">Callback for progress updates (bytes sent so far).</param>
    /// <param name="cancellationToken">Token to cancel the test.</param>
    /// <returns>A <see cref="ThroughputResult"/> object.</returns>
    public static async Task<ThroughputResult> RunTestAsync(string host, int port, int durationSeconds, Action<long>? onProgress = null, CancellationToken cancellationToken = default)
    {
        using var client = new TcpClient();
        try
        {
            await client.ConnectAsync(host, port, cancellationToken);
            using var stream = client.GetStream();

            byte[] buffer = new byte[65536]; // 64KB buffer
            new Random().NextBytes(buffer);

            long totalBytes = 0;
            var sw = Stopwatch.StartNew();
            var endTime = DateTime.Now.AddSeconds(durationSeconds);

            while (DateTime.Now < endTime && !cancellationToken.IsCancellationRequested)
            {
                await stream.WriteAsync(buffer, 0, buffer.Length, cancellationToken);
                totalBytes += buffer.Length;
                onProgress?.Invoke(totalBytes);
            }

            sw.Stop();
            return new ThroughputResult(totalBytes, sw.Elapsed, totalBytes / sw.Elapsed.TotalSeconds, true);
        }
        catch (Exception)
        {
            return new ThroughputResult(0, TimeSpan.Zero, 0, false);
        }
    }
}

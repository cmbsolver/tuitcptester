using System.Net.Sockets;
using System.Text;
using tuitcptester.Models;

namespace tuitcptester.Logic;

/// <summary>
/// Provides a base implementation for TCP connections, handling status changes and data sending.
/// </summary>
public abstract class TcpConnectionBase : ITcpConnection
{
    /// <inheritdoc/>
    public event Action<string>? OnLog;
    /// <inheritdoc/>
    public event Action<ConnectionStatus>? OnStatusChanged;
    /// <inheritdoc/>
    public event Action<string>? OnError;

    private ConnectionStatus _status = ConnectionStatus.Disconnected;
    /// <inheritdoc/>
    public ConnectionStatus Status
    {
        get => _status;
        protected set
        {
            if (_status != value)
            {
                _status = value;
                OnStatusChanged?.Invoke(_status);
            }
        }
    }

    /// <summary>
    /// Invokes the <see cref="OnLog"/> event with the specified message.
    /// </summary>
    /// <param name="message">The message to log.</param>
    protected void Log(string message) => OnLog?.Invoke(message);

    /// <summary>
    /// Invokes the <see cref="OnError"/> event with the specified message.
    /// </summary>
    /// <param name="message">The error message.</param>
    protected void Error(string message) => OnError?.Invoke(message);

    /// <inheritdoc/>
    public abstract void Start();
    /// <inheritdoc/>
    public abstract void Stop();
    /// <inheritdoc/>
    public abstract void Send(Transaction tx);
    /// <inheritdoc/>
    public abstract void Dispose();

    /// <summary>
    /// Sends a transaction over the specified network stream.
    /// </summary>
    /// <param name="tx">The transaction to send.</param>
    /// <param name="stream">The network stream to write to.</param>
    protected void SendInternal(Transaction tx, NetworkStream stream)
    {
        try
        {
            var dataToSend = tx.Data;
            if (tx.AppendReturn) dataToSend += "\r";
            if (tx.AppendNewline) dataToSend += "\n";

            var data = tx.Encoding switch
            {
                TransactionEncoding.Ascii => Encoding.ASCII.GetBytes(dataToSend),
                TransactionEncoding.Hex => DataUtils.HexToBytes(dataToSend),
                TransactionEncoding.Binary => Convert.FromBase64String(dataToSend),
                _ => Encoding.ASCII.GetBytes(dataToSend)
            };

            stream.Write(data, 0, data.Length);
            string hexDump = DataUtils.ToHexDump(data, 0, data.Length);
            Log($"Sent ({tx.Encoding}) {data.Length} bytes:\n{hexDump}");
        }
        catch (Exception ex)
        {
            Log($"Send error: {ex.Message}");
        }
    }

    /// <summary>
    /// Handles incoming data from a network stream in a loop.
    /// </summary>
    /// <param name="stream">The network stream to read from.</param>
    /// <param name="token">A cancellation token to stop the loop.</param>
    /// <param name="onDataReceived">A callback invoked when data is received.</param>
    protected void HandleIncomingData(NetworkStream stream, CancellationToken token, Action<byte[], int> onDataReceived)
    {
        byte[] buffer = new byte[4096];
        while (!token.IsCancellationRequested)
        {
            try
            {
                if (stream.DataAvailable)
                {
                    int bytesRead = stream.Read(buffer, 0, buffer.Length);
                    if (bytesRead == 0) break;

                    onDataReceived(buffer, bytesRead);
                }
                else
                {
                    Thread.Sleep(50);
                }
            }
            catch (Exception ex) when (!token.IsCancellationRequested)
            {
                Log($"Read error: {ex.Message}");
                break;
            }
        }
    }
}

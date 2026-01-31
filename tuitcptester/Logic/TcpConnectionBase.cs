using System.Net.Sockets;
using System.Text;
using tuitcptester.Models;

namespace tuitcptester.Logic;

public abstract class TcpConnectionBase : ITcpConnection
{
    public event Action<string>? OnLog;
    public event Action<ConnectionStatus>? OnStatusChanged;
    public event Action<string>? OnError;

    private ConnectionStatus _status = ConnectionStatus.Disconnected;
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

    protected void Log(string message) => OnLog?.Invoke(message);
    protected void Error(string message) => OnError?.Invoke(message);

    public abstract void Start();
    public abstract void Stop();
    public abstract void Send(Transaction tx);
    public abstract void Dispose();

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

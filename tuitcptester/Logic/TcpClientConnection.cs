using System.Net.Sockets;
using tuitcptester.Models;

namespace tuitcptester.Logic;

public class TcpClientConnection : TcpConnectionBase
{
    private readonly string _host;
    private readonly int _port;
    private TcpClient? _client;
    private CancellationTokenSource? _cts;
    private readonly Action<byte[], int> _onDataReceived;

    public TcpClientConnection(string host, int port, Action<byte[], int> onDataReceived)
    {
        _host = host;
        _port = port;
        _onDataReceived = onDataReceived;
    }

    public override void Start()
    {
        _cts = new CancellationTokenSource();
        try
        {
            _client = new TcpClient();
            _client.Connect(_host, _port);
            Status = ConnectionStatus.Connected;
            Log("Connected.");

            Task.Run(() => HandleIncomingData(_client.GetStream(), _cts.Token, _onDataReceived), _cts.Token);
        }
        catch (Exception ex)
        {
            Status = ConnectionStatus.Error;
            Error(ex.Message);
            Log($"Failed to connect: {ex.Message}");
            throw;
        }
    }

    public override void Stop()
    {
        _cts?.Cancel();
        _client?.Close();
        Status = ConnectionStatus.Disconnected;
        Log("Disconnected.");
    }

    public override void Send(Transaction tx)
    {
        if (_client is { Connected: true })
        {
            SendInternal(tx, _client.GetStream());
        }
        else
        {
            Log("Cannot send: Not connected.");
        }
    }

    public override void Dispose()
    {
        Stop();
        _cts?.Dispose();
        _client?.Dispose();
    }
}

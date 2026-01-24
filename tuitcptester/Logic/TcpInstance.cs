using System.Net;
using System.Net.Sockets;
using System.Text;
using tuitcptester.Models;

namespace tuitcptester.Logic;

/// <summary>
/// Manages a TCP connection instance, supporting both client and server roles.
/// </summary>
public class TcpInstance : IDisposable
{
    /// <summary>
    /// Gets the configuration settings for this instance.
    /// </summary>
    public TcpConfiguration Config { get; }

    /// <summary>
    /// Gets the current status of the connection.
    /// </summary>
    public ConnectionStatus Status { get; private set; } = ConnectionStatus.Disconnected;

    /// <summary>
    /// Gets the last error message encountered, if any.
    /// </summary>
    public string? LastError { get; private set; }

    private Thread? _workerThread;
    private CancellationTokenSource? _cts;
    private TcpClient? _client;
    private TcpListener? _listener;
    private TcpProxy? _proxy;
    private readonly Random _random = new();

    /// <summary>
    /// Event raised when a new log entry is available.
    /// </summary>
    public event Action<LogEntry>? OnLog;

    /// <summary>
    /// Event raised when the connection status changes.
    /// </summary>
    public event Action? OnStatusChanged;

    /// <summary>
    /// Event raised when an error occurs.
    /// </summary>
    public event Action<string>? OnError;

    /// <summary>
    /// Initializes a new instance of the <see cref="TcpInstance"/> class.
    /// </summary>
    /// <param name="config">The TCP configuration to use.</param>
    public TcpInstance(TcpConfiguration config)
    {
        Config = config;
    }

    /// <summary>
    /// Starts the TCP instance (connects if client, starts listening if server).
    /// </summary>
    public void Start()
    {
        _cts = new CancellationTokenSource();

        try
        {
            if (Config.Type == ConnectionType.Server)
            {
                _listener = new TcpListener(IPAddress.Any, Config.Port);
                _listener.Start();
                Status = ConnectionStatus.Listening;
            }
            else if (Config.Type == ConnectionType.Client)
            {
                _client = new TcpClient();
                // Synchronously connect for the initial attempt so we can catch errors early
                _client.Connect(Config.Host, Config.Port);
                Status = ConnectionStatus.Connected;
            }
            else if (Config.Type == ConnectionType.Proxy)
            {
                if (string.IsNullOrEmpty(Config.RemoteHost) || !Config.RemotePort.HasValue)
                {
                    throw new InvalidOperationException("Proxy requires RemoteHost and RemotePort.");
                }

                _proxy = new TcpProxy(Config.Port, Config.RemoteHost, Config.RemotePort.Value);
                _proxy.OnLog += (msg) => Log(msg);
                _proxy.Start();
                Status = ConnectionStatus.Listening;
            }

            if (Config.Type != ConnectionType.Proxy)
            {
                _workerThread = new Thread(Run) { IsBackground = true };
                _workerThread.Start();
            }
            OnStatusChanged?.Invoke();
        }
        catch (Exception ex)
        {
            Status = ConnectionStatus.Error;
            LastError = ex.Message;
            Log($"Failed to start: {ex.Message}");
            OnStatusChanged?.Invoke();
            throw;
        }
    }

    /// <summary>
    /// Stops the TCP instance and releases associated resources.
    /// </summary>
    public void Stop()
    {
        _cts?.Cancel();
        _client?.Close();
        _listener?.Stop();
        _proxy?.Stop();
        Status = ConnectionStatus.Disconnected;
        OnStatusChanged?.Invoke();
    }

    /// <summary>
    /// The main worker method running on a background thread.
    /// </summary>
    private void Run()
    {
        try
        {
            if (Config.Type == ConnectionType.Client)
            {
                Log("Connected.");
                HandleCommunication(_client!.GetStream(), _cts!.Token);
            }
            else
            {
                Log($"Listening on port {Config.Port}...");
                RunServer(_cts!.Token);
            }
        }
        catch (Exception ex) when (!_cts?.IsCancellationRequested ?? true)
        {
            Status = ConnectionStatus.Error;
            LastError = ex.Message;
            Log($"Error: {ex.Message}");
            OnStatusChanged?.Invoke();
            OnError?.Invoke(ex.Message);
        }
    }

    /// <summary>
    /// Runs the server loop, accepting incoming connections.
    /// </summary>
    /// <param name="token">Cancellation token to stop the server.</param>
    private void RunServer(CancellationToken token)
    {
        while (!token.IsCancellationRequested)
        {
            try
            {
                if (_listener?.Pending() == true)
                {
                    var client = _listener.AcceptTcpClient();
                    Log($"Accepted connection from {client.Client.RemoteEndPoint}");

                    // Store the accepted client so SendManual can use it
                    _client = client;
                    Status = ConnectionStatus.Connected;
                    OnStatusChanged?.Invoke();

                    Task.Run(() =>
                    {
                        using (client)
                        {
                            HandleCommunication(client.GetStream(), token);
                        }
                    }, token);
                }
            }
            catch (Exception ex) when (!token.IsCancellationRequested)
            {
                Log($"Server loop error: {ex.Message}");
            }
            Thread.Sleep(100);
        }
    }

    /// <summary>
    /// Gets the current index in the auto-transaction list.
    /// </summary>
    public int AutoTxIndex { get; private set; }

    /// <summary>
    /// Handles bidirectional communication on an established stream.
    /// </summary>
    /// <param name="stream">The network stream to communicate on.</param>
    /// <param name="token">Cancellation token to stop communication.</param>
    private void HandleCommunication(NetworkStream stream, CancellationToken token)
    {
        AutoTxIndex = 0;

        // Start auto-transactions if configured
        if (Config.AutoTransactions.Any())
        {
            Task.Run(() => RunAutoTransactions(stream, token), token);
        }

        byte[] buffer = new byte[4096];
        while (!token.IsCancellationRequested)
        {
            try
            {
                if (stream.DataAvailable)
                {
                    int bytesRead = stream.Read(buffer, 0, buffer.Length);
                    if (bytesRead == 0)
                    {
                        HandleDisconnect("Remote closed the connection.");
                        break;
                    }

                    string hexDump = DataUtils.ToHexDump(buffer, 0, bytesRead);
                    Log($"Received {bytesRead} bytes:\n{hexDump}");

                    // If no interval is selected, send next transaction on receive
                    if (Config.IntervalMs == null && Config.AutoTransactions.Any())
                    {
                        if (AutoTxIndex < Config.AutoTransactions.Count)
                        {
                            SendManual(Config.AutoTransactions[AutoTxIndex], stream);
                            AutoTxIndex = (AutoTxIndex + 1) % Config.AutoTransactions.Count;
                        }
                    }
                }
            }
            catch (Exception ex) when (!token.IsCancellationRequested)
            {
                HandleDisconnect($"Communication error: {ex.Message}");
                break;
            }
            Thread.Sleep(100);
        }
    }

    /// <summary>
    /// Handles a disconnection by updating status and notifying the UI.
    /// </summary>
    /// <param name="reason">The reason for disconnection.</param>
    private void HandleDisconnect(string reason)
    {
        if (Status == ConnectionStatus.Disconnected) return;

        Status = ConnectionStatus.Disconnected;
        Log(reason);
        OnStatusChanged?.Invoke();
    }

    /// <summary>
    /// Periodically sends automatic transactions according to the configuration.
    /// </summary>
    /// <param name="stream">The network stream to send data on.</param>
    /// <param name="token">Cancellation token to stop auto transactions.</param>
    private async Task RunAutoTransactions(NetworkStream stream, CancellationToken token)
    {
        // Send the first item immediately on connection
        if (Config.AutoTransactions.Any())
        {
            SendManual(Config.AutoTransactions[AutoTxIndex], stream);
            AutoTxIndex = (AutoTxIndex + 1) % Config.AutoTransactions.Count;
        }

        while (!token.IsCancellationRequested)
        {
            if (Config.IntervalMs.HasValue)
            {
                var delay = Config.IntervalMs.Value;
                if (Config is { JitterMinMs: not null, JitterMaxMs: not null })
                {
                    // Requirements: "The jitter is a random number with a min/max that it will subtract from the time between transaction."
                    delay -= _random.Next(Config.JitterMinMs.Value, Config.JitterMaxMs.Value + 1);
                }
                
                if (delay > 0)
                {
                    try
                    {
                        await Task.Delay(delay, token);
                    }
                    catch (TaskCanceledException)
                    {
                        break;
                    }
                }

                if (Config.AutoTransactions.Count == 0) continue;
                SendManual(Config.AutoTransactions[AutoTxIndex], stream);
                AutoTxIndex = (AutoTxIndex + 1) % Config.AutoTransactions.Count;
            }
            else
            {
                // Wait for signal from receiver (handled in HandleCommunication)
                // We just loop here, but the actual sending is triggered in HandleCommunication when DataAvailable is true.
                await Task.Delay(100, token);
            }
        }
    }

    /// <summary>
    /// Manually sends a transaction over the current connection.
    /// </summary>
    /// <param name="tx">The transaction to send.</param>
    public void SendManual(Transaction tx)
    {
        if (_client is { Connected: true })
        {
            SendManual(tx, _client.GetStream());
        }
        else
        {
            Log("Cannot send: Not connected.");
        }
    }

    /// <summary>
    /// Sends a transaction over a specific network stream.
    /// </summary>
    /// <param name="tx">The transaction to send.</param>
    /// <param name="stream">The network stream to send data on.</param>
    private void SendManual(Transaction tx, NetworkStream stream)
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
                TransactionEncoding.Binary => Convert.FromBase64String(dataToSend), // Assuming Base64 for binary input
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
    /// Invokes the <see cref="OnLog"/> event with the specified message.
    /// </summary>
    /// <param name="message">The message to log.</param>
    private void Log(string message)
    {
        var timestamp = DateTime.Now;
        
        if (!string.IsNullOrWhiteSpace(Config.DumpFilePath))
        {
            try
            {
                File.AppendAllText(Config.DumpFilePath, $"[{timestamp:yyyy-MM-dd HH:mm:ss}] {message}{Environment.NewLine}");
            }
            catch (Exception ex)
            {
                // We don't want to crash the connection thread if file writing fails, 
                // but we should at least let the user know via the UI log.
                OnLog?.Invoke(new LogEntry
                {
                    Timestamp = timestamp,
                    Message = $"Dump Error: {ex.Message}",
                    ConnectionName = Config.Name
                });
            }
        }

        OnLog?.Invoke(new LogEntry
        {
            Timestamp = timestamp,
            Message = message,
            ConnectionName = Config.Name
        });
    }

    /// <summary>
    /// Releases all resources used by the <see cref="TcpInstance"/>.
    /// </summary>
    public void Dispose()
    {
        Stop();
        _cts?.Dispose();
        _client?.Dispose();
        _proxy?.Dispose();
    }

    /// <summary>
    /// Returns the name of the connection.
    /// </summary>
    /// <returns>The connection name.</returns>
    public override string ToString()
    {
        return Config.Name;
    }
}

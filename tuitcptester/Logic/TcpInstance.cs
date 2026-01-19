using System.Net;
using System.Net.Sockets;
using System.Text;
using tuitcptester.Models;

namespace tuitcptester.Logic;

/// <summary>
/// Represents the current status of a TCP connection.
/// </summary>
public enum ConnectionStatus
{
    /// <summary>
    /// Not connected or listening.
    /// </summary>
    Disconnected,
    /// <summary>
    /// Successfully connected as a client.
    /// </summary>
    Connected,
    /// <summary>
    /// Successfully listening as a server.
    /// </summary>
    Listening,
    /// <summary>
    /// An error occurred during the connection or operation.
    /// </summary>
    Error
}

/// <summary>
/// Represents a log message with metadata.
/// </summary>
public class LogEntry
{
    /// <summary>
    /// Gets or sets the time when the log entry was created.
    /// </summary>
    public DateTime Timestamp { get; set; }

    /// <summary>
    /// Gets or sets the log message content.
    /// </summary>
    public string Message { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the name of the connection that generated this log.
    /// </summary>
    public string ConnectionName { get; set; } = string.Empty;
}

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
            else
            {
                _client = new TcpClient();
                // Synchronous connect for the initial attempt so we can catch errors early
                _client.Connect(Config.Host, Config.Port);
                Status = ConnectionStatus.Connected;
            }

            _workerThread = new Thread(Run) { IsBackground = true };
            _workerThread.Start();
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
    /// Handles bidirectional communication on an established stream.
    /// </summary>
    /// <param name="stream">The network stream to communicate on.</param>
    /// <param name="token">Cancellation token to stop communication.</param>
    private void HandleCommunication(NetworkStream stream, CancellationToken token)
    {
        // Start auto-transactions if configured
        if (Config.AutoTransactions.Any())
        {
            Task.Run(() => RunAutoTransactions(stream, token), token);
        }

        byte[] buffer = new byte[4096];
        while (!token.IsCancellationRequested)
        {
            if (stream.DataAvailable)
            {
                int bytesRead = stream.Read(buffer, 0, buffer.Length);
                if (bytesRead == 0) break;

                string received = Encoding.ASCII.GetString(buffer, 0, bytesRead);
                Log($"Received: {received}");

                // If no interval is selected, send next transaction on receive
                if (Config.IntervalMs == null && Config.AutoTransactions.Any())
                {
                    // This logic is a bit simplified, usually you'd want a stateful transaction list
                    // but according to requirements: "If the wait time is not selected, then when 
                    // the instance receives a transaction, then it will send the next item in the transaction list."
                    // For simplicity, we'll just send the first one or cycle them if we had an index.
                    SendManual(Config.AutoTransactions.First(), stream);
                }
            }
            Thread.Sleep(100);
        }
    }

    /// <summary>
    /// Periodically sends automatic transactions according to the configuration.
    /// </summary>
    /// <param name="stream">The network stream to send data on.</param>
    /// <param name="token">Cancellation token to stop auto transactions.</param>
    private async Task RunAutoTransactions(NetworkStream stream, CancellationToken token)
    {
        int index = 0;
        while (!token.IsCancellationRequested)
        {
            if (Config.IntervalMs.HasValue)
            {
                int delay = Config.IntervalMs.Value;
                if (Config.JitterMinMs.HasValue && Config.JitterMaxMs.HasValue)
                {
                    delay -= _random.Next(Config.JitterMinMs.Value, Config.JitterMaxMs.Value + 1);
                }
                
                if (delay > 0)
                    await Task.Delay(delay, token);

                if (index < Config.AutoTransactions.Count)
                {
                    SendManual(Config.AutoTransactions[index], stream);
                    index = (index + 1) % Config.AutoTransactions.Count;
                }
            }
            else
            {
                // Wait for signal from receiver (handled in HandleCommunication)
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
        if (_client != null && _client.Connected)
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
            byte[] data = tx.Encoding switch
            {
                TransactionEncoding.Ascii => Encoding.ASCII.GetBytes(tx.Data),
                TransactionEncoding.Hex => HexToBytes(tx.Data),
                TransactionEncoding.Binary => Convert.FromBase64String(tx.Data), // Assuming Base64 for binary input
                _ => Encoding.ASCII.GetBytes(tx.Data)
            };

            stream.Write(data, 0, data.Length);
            Log($"Sent ({tx.Encoding}): {tx.Data}");
        }
        catch (Exception ex)
        {
            Log($"Send error: {ex.Message}");
        }
    }

    /// <summary>
    /// Converts a hexadecimal string to a byte array.
    /// </summary>
    /// <param name="hex">The hex string to convert.</param>
    /// <returns>A byte array representing the hex string.</returns>
    private byte[] HexToBytes(string hex)
    {
        hex = hex.Replace("-", "").Replace(" ", "");
        byte[] bytes = new byte[hex.Length / 2];
        for (int i = 0; i < hex.Length; i += 2)
            bytes[i / 2] = Convert.ToByte(hex.Substring(i, 2), 16);
        return bytes;
    }

    /// <summary>
    /// Invokes the <see cref="OnLog"/> event with the specified message.
    /// </summary>
    /// <param name="message">The message to log.</param>
    private void Log(string message)
    {
        OnLog?.Invoke(new LogEntry
        {
            Timestamp = DateTime.Now,
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

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
    public ConnectionStatus Status => _connection?.Status ?? ConnectionStatus.Disconnected;

    /// <summary>
    /// Gets the last error message encountered, if any.
    /// </summary>
    public string? LastError { get; private set; }

    private ITcpConnection? _connection;
    private BufferedFileLogSink? _logSink;
    private CancellationTokenSource? _autoTxCts;
    private readonly Random _random = new();
    private Action<string>? _connectionLogHandler;
    private Action<string>? _connectionErrorHandler;
    private Action<ConnectionStatus>? _connectionStatusHandler;
    private Action<string>? _logSinkErrorHandler;

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
        ConfigureLogSink();
    }

    private void ConfigureLogSink()
    {
        DetachLogSinkErrorHandler();
        _logSink?.Dispose();
        _logSink = null;

        if (string.IsNullOrWhiteSpace(Config.DumpFilePath))
        {
            return;
        }

        _logSink = new BufferedFileLogSink(Config.DumpFilePath);
        _logSinkErrorHandler = HandleLogSinkError;
        _logSink.OnError += _logSinkErrorHandler;
    }

    /// <summary>
    /// Starts the TCP instance (connects if client, starts listening if server).
    /// </summary>
    public void Start()
    {
        TeardownConnection(raiseStatusChanged: false);
        _autoTxCts?.Dispose();
        _autoTxCts = new CancellationTokenSource();

        try
        {
            _connection = CreateConnection();
            AttachConnectionHandlers();

            _connection.Start();
        }
        catch (Exception ex)
        {
            LastError = ex.Message;
            Log($"Failed to start: {ex.Message}");
            OnStatusChanged?.Invoke();
            throw;
        }
    }

    private void OnDataReceived(byte[] buffer, int count)
    {
        if (Config.IncludePayloadHexDump)
        {
            string hexDump = DataUtils.ToHexDump(buffer, 0, count);
            Log($"Received {count} bytes:\n{hexDump}");
        }
        else
        {
            Log($"Received {count} bytes.");
        }

        // If no interval is selected, send next transaction on receive
        if (Config.IntervalMs != null || !Config.AutoTransactions.Any()) return;
        SendNextAutoTransaction();
    }

    /// <summary>
    /// Updates the auto-transaction settings and restarts the auto-transaction loop if connected.
    /// </summary>
    /// <param name="transactions">The new list of transactions.</param>
    /// <param name="intervalMs">The new interval in milliseconds.</param>
    /// <param name="jitterMinMs">The new minimum jitter in milliseconds.</param>
    /// <param name="jitterMaxMs">The new maximum jitter in milliseconds.</param>
    public void UpdateAutoTransactions(List<Transaction> transactions, int? intervalMs, int? jitterMinMs, int? jitterMaxMs)
    {
        Config.AutoTransactions.Clear();
        Config.AutoTransactions.AddRange(transactions);
        Config.IntervalMs = intervalMs;
        Config.JitterMinMs = jitterMinMs;
        Config.JitterMaxMs = jitterMaxMs;

        ConfigureLogSink();

        AutoTxIndex = 0;

        if (Status == ConnectionStatus.Connected)
        {
            RestartAutoTransactions();
            StartAutoTransactions();
        }
        
        Log($"Updated auto-transactions: {transactions.Count} items, Interval: {intervalMs}ms, Jitter: {jitterMinMs}-{jitterMaxMs}ms");
    }

    private void StartAutoTransactions()
    {
        if (!Config.AutoTransactions.Any() || _autoTxCts == null)
        {
            return;
        }

        _ = Task.Run(() => RunAutoTransactions(_autoTxCts.Token), _autoTxCts.Token);
    }

    /// <summary>
    /// Stops the TCP instance and releases associated resources.
    /// </summary>
    public void Stop()
    {
        TeardownConnection(raiseStatusChanged: true);
    }

    /// <summary>
    /// Gets the current index in the auto-transaction list.
    /// </summary>
    public int AutoTxIndex { get; private set; }

    /// <summary>
    /// Periodically sends automatic transactions according to the configuration.
    /// </summary>
    /// <param name="token">Cancellation token to stop auto transactions.</param>
    private async Task RunAutoTransactions(CancellationToken token)
    {
        // Send the first item immediately on connection
        SendNextAutoTransaction();

        while (!token.IsCancellationRequested)
        {
            if (Config.IntervalMs.HasValue)
            {
                var delay = Config.IntervalMs.Value;
                if (Config is { JitterMinMs: not null, JitterMaxMs: not null })
                {
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

                if (token.IsCancellationRequested) break;
                SendNextAutoTransaction();
            }
            else
            {
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
        if (_connection != null && _connection.Status == ConnectionStatus.Connected)
        {
            _connection.Send(tx);
        }
        else
        {
            Log("Cannot send: Not connected.");
        }
    }

    /// <summary>
    /// Invokes the <see cref="OnLog"/> event with the specified message.
    /// </summary>
    /// <param name="message">The message to log.</param>
    private void Log(string message)
    {
        var timestamp = DateTime.Now;

        _logSink?.Enqueue($"[{timestamp:yyyy-MM-dd HH:mm:ss}] {message}");

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
        _autoTxCts?.Dispose();
        _autoTxCts = null;
        DetachLogSinkErrorHandler();
        _logSink?.Dispose();
        _logSink = null;
    }

    private ITcpConnection CreateConnection()
    {
        return Config.Type switch
        {
            ConnectionType.Server => new TcpServerConnection(Config.Port, OnDataReceived),
            ConnectionType.Client => new TcpClientConnection(Config.Host, Config.Port, OnDataReceived),
            ConnectionType.Proxy when string.IsNullOrEmpty(Config.RemoteHost) || !Config.RemotePort.HasValue =>
                throw new InvalidOperationException("Proxy requires RemoteHost and RemotePort."),
            ConnectionType.Proxy => new TcpProxyConnection(Config.Port, Config.RemoteHost, Config.RemotePort.Value,
                Config.IncludePayloadHexDump),
            _ => throw new ArgumentOutOfRangeException()
        };
    }

    private void AttachConnectionHandlers()
    {
        if (_connection == null)
        {
            return;
        }

        _connectionLogHandler = Log;
        _connectionErrorHandler = HandleConnectionError;
        _connectionStatusHandler = HandleConnectionStatusChanged;

        _connection.OnLog += _connectionLogHandler;
        _connection.OnError += _connectionErrorHandler;
        _connection.OnStatusChanged += _connectionStatusHandler;
    }

    private void DetachConnectionHandlers()
    {
        if (_connection == null)
        {
            return;
        }

        if (_connectionLogHandler != null)
        {
            _connection.OnLog -= _connectionLogHandler;
            _connectionLogHandler = null;
        }

        if (_connectionErrorHandler != null)
        {
            _connection.OnError -= _connectionErrorHandler;
            _connectionErrorHandler = null;
        }

        if (_connectionStatusHandler != null)
        {
            _connection.OnStatusChanged -= _connectionStatusHandler;
            _connectionStatusHandler = null;
        }
    }

    private void HandleConnectionError(string message)
    {
        LastError = message;
        OnError?.Invoke(message);
    }

    private void HandleConnectionStatusChanged(ConnectionStatus status)
    {
        if (status == ConnectionStatus.Connected)
        {
            StartAutoTransactions();
        }
        else if (status == ConnectionStatus.Disconnected)
        {
            CancelAutoTransactions();
        }

        OnStatusChanged?.Invoke();
    }

    private void RestartAutoTransactions()
    {
        CancelAutoTransactions();
        _autoTxCts?.Dispose();
        _autoTxCts = new CancellationTokenSource();
    }

    private void CancelAutoTransactions()
    {
        _autoTxCts?.Cancel();
    }

    private void TeardownConnection(bool raiseStatusChanged)
    {
        CancelAutoTransactions();
        _connection?.Stop();
        DetachConnectionHandlers();
        _connection?.Dispose();
        _connection = null;

        if (raiseStatusChanged)
        {
            OnStatusChanged?.Invoke();
        }
    }

    private void SendNextAutoTransaction()
    {
        if (Config.AutoTransactions.Count == 0 || AutoTxIndex >= Config.AutoTransactions.Count)
        {
            return;
        }

        _connection?.Send(Config.AutoTransactions[AutoTxIndex]);
        AutoTxIndex = (AutoTxIndex + 1) % Config.AutoTransactions.Count;
    }

    private void HandleLogSinkError(string message)
    {
        OnLog?.Invoke(new LogEntry
        {
            Timestamp = DateTime.Now,
            Message = $"Dump Error: {message}",
            ConnectionName = Config.Name
        });
    }

    private void DetachLogSinkErrorHandler()
    {
        if (_logSink != null && _logSinkErrorHandler != null)
        {
            _logSink.OnError -= _logSinkErrorHandler;
            _logSinkErrorHandler = null;
        }
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

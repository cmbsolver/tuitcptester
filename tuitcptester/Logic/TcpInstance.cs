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
    public ConnectionStatus Status => _connection?.Status ?? ConnectionStatus.Disconnected;

    /// <summary>
    /// Gets the last error message encountered, if any.
    /// </summary>
    public string? LastError { get; private set; }

    private ITcpConnection? _connection;
    private CancellationTokenSource? _autoTxCts;
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
        _autoTxCts = new CancellationTokenSource();

        try
        {
            if (Config.Type == ConnectionType.Server)
            {
                _connection = new TcpServerConnection(Config.Port, OnDataReceived);
            }
            else if (Config.Type == ConnectionType.Client)
            {
                _connection = new TcpClientConnection(Config.Host, Config.Port, OnDataReceived);
            }
            else if (Config.Type == ConnectionType.Proxy)
            {
                if (string.IsNullOrEmpty(Config.RemoteHost) || !Config.RemotePort.HasValue)
                {
                    throw new InvalidOperationException("Proxy requires RemoteHost and RemotePort.");
                }

                _connection = new TcpProxyConnection(Config.Port, Config.RemoteHost, Config.RemotePort.Value);
            }

            if (_connection != null)
            {
                _connection.OnLog += (msg) => Log(msg);
                _connection.OnError += (msg) =>
                {
                    LastError = msg;
                    OnError?.Invoke(msg);
                };
                _connection.OnStatusChanged += (status) =>
                {
                    if (status == ConnectionStatus.Connected)
                    {
                        StartAutoTransactions();
                    }
                    else if (status == ConnectionStatus.Disconnected)
                    {
                        _autoTxCts?.Cancel();
                    }
                    OnStatusChanged?.Invoke();
                };

                _connection.Start();
            }
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
        string hexDump = DataUtils.ToHexDump(buffer, 0, count);
        Log($"Received {count} bytes:\n{hexDump}");

        // If no interval is selected, send next transaction on receive
        if (Config.IntervalMs == null && Config.AutoTransactions.Any())
        {
            if (AutoTxIndex < Config.AutoTransactions.Count)
            {
                _connection?.Send(Config.AutoTransactions[AutoTxIndex]);
                AutoTxIndex = (AutoTxIndex + 1) % Config.AutoTransactions.Count;
            }
        }
    }

    private void StartAutoTransactions()
    {
        if (Config.AutoTransactions.Any())
        {
            _ = Task.Run(() => RunAutoTransactions(_autoTxCts!.Token), _autoTxCts!.Token);
        }
    }

    /// <summary>
    /// Stops the TCP instance and releases associated resources.
    /// </summary>
    public void Stop()
    {
        _autoTxCts?.Cancel();
        _connection?.Stop();
        OnStatusChanged?.Invoke();
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
        if (Config.AutoTransactions.Any())
        {
            _connection?.Send(Config.AutoTransactions[AutoTxIndex]);
            AutoTxIndex = (AutoTxIndex + 1) % Config.AutoTransactions.Count;
        }

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
                if (Config.AutoTransactions.Count == 0) continue;
                
                _connection?.Send(Config.AutoTransactions[AutoTxIndex]);
                AutoTxIndex = (AutoTxIndex + 1) % Config.AutoTransactions.Count;
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

        if (!string.IsNullOrWhiteSpace(Config.DumpFilePath))
        {
            try
            {
                File.AppendAllText(Config.DumpFilePath, $"[{timestamp:yyyy-MM-dd HH:mm:ss}] {message}{Environment.NewLine}");
            }
            catch (Exception ex)
            {
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
        _autoTxCts?.Dispose();
        _connection?.Dispose();
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

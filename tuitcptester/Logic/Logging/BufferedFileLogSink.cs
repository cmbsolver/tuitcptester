using System.Threading.Channels;

namespace tuitcptester.Logic;

/// <summary>
/// Writes log lines to a file using a background-buffered worker.
/// </summary>
public sealed class BufferedFileLogSink : IDisposable
{
    private readonly string _filePath;
    private readonly Channel<string> _channel;
    private readonly CancellationTokenSource _cts;
    private readonly Task _workerTask;
    private bool _disposed;

    /// <summary>
    /// Initializes a new instance of the <see cref="BufferedFileLogSink"/> class.
    /// </summary>
    /// <param name="filePath">Target file path.</param>
    public BufferedFileLogSink(string filePath)
    {
        _filePath = filePath;
        _cts = new CancellationTokenSource();
        _channel = Channel.CreateUnbounded<string>(new UnboundedChannelOptions
        {
            SingleReader = true,
            SingleWriter = false
        });

        _workerTask = Task.Run(() => ProcessQueueAsync(_cts.Token));
    }

    /// <summary>
    /// Event raised when sink write processing encounters an error.
    /// </summary>
    public event Action<string>? OnError;

    /// <summary>
    /// Enqueues a formatted log line for asynchronous file writing.
    /// </summary>
    /// <param name="line">The log line to persist.</param>
    public void Enqueue(string line)
    {
        if (_disposed) return;
        _channel.Writer.TryWrite(line);
    }

    private async Task ProcessQueueAsync(CancellationToken token)
    {
        var batch = new List<string>(64);

        try
        {
            while (await _channel.Reader.WaitToReadAsync(token))
            {
                while (_channel.Reader.TryRead(out var line))
                {
                    batch.Add(line);
                }

                if (batch.Count == 0)
                {
                    continue;
                }

                try
                {
                    await File.AppendAllLinesAsync(_filePath, batch, token);
                }
                catch (Exception ex)
                {
                    OnError?.Invoke(ex.Message);
                }
                finally
                {
                    batch.Clear();
                }
            }
        }
        catch (OperationCanceledException)
        {
            // Normal shutdown path
        }

        while (_channel.Reader.TryRead(out var pendingLine))
        {
            batch.Add(pendingLine);
        }

        if (batch.Count > 0)
        {
            try
            {
                await File.AppendAllLinesAsync(_filePath, batch, token);
            }
            catch (Exception ex)
            {
                OnError?.Invoke(ex.Message);
            }
        }
    }

    /// <summary>
    /// Disposes the sink and flushes pending lines.
    /// </summary>
    public void Dispose()
    {
        if (_disposed) return;
        _disposed = true;

        _channel.Writer.TryComplete();
        _cts.Cancel();

        try
        {
            _workerTask.GetAwaiter().GetResult();
        }
        catch
        {
            // Errors are reported via OnError.
        }

        _cts.Dispose();
    }
}
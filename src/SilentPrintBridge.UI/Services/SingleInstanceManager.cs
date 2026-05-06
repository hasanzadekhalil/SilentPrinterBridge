using System.IO.Pipes;
using System.Threading;

namespace SilentPrintBridge.UI.Services;

public sealed class SingleInstanceManager : IDisposable
{
    private readonly string _pipeName;
    private readonly Mutex _mutex;
    private CancellationTokenSource? _cancellationTokenSource;
    private Task? _listenerTask;

    public bool IsPrimaryInstance { get; }

    public event Action? ActivationRequested;

    public SingleInstanceManager(string mutexName, string pipeName)
    {
        _pipeName = pipeName;
        _mutex = new Mutex(initiallyOwned: true, mutexName, out var createdNew);
        IsPrimaryInstance = createdNew;
    }

    public void StartListening()
    {
        if (!IsPrimaryInstance || _listenerTask != null)
        {
            return;
        }

        _cancellationTokenSource = new CancellationTokenSource();
        _listenerTask = Task.Run(() => ListenLoopAsync(_cancellationTokenSource.Token));
    }

    public async Task<bool> SignalPrimaryInstanceAsync()
    {
        try
        {
            using var client = new NamedPipeClientStream(
                serverName: ".",
                pipeName: _pipeName,
                direction: PipeDirection.Out);

            await client.ConnectAsync(1500);
            await client.WriteAsync(new byte[] { 1 });
            await client.FlushAsync();
            return true;
        }
        catch
        {
            return false;
        }
    }

    private async Task ListenLoopAsync(CancellationToken cancellationToken)
    {
        while (!cancellationToken.IsCancellationRequested)
        {
            try
            {
                using var server = new NamedPipeServerStream(
                    _pipeName,
                    PipeDirection.In,
                    1,
                    PipeTransmissionMode.Byte,
                    PipeOptions.Asynchronous);

                await server.WaitForConnectionAsync(cancellationToken);

                var buffer = new byte[1];
                await server.ReadAsync(buffer, cancellationToken);
                ActivationRequested?.Invoke();
            }
            catch (OperationCanceledException)
            {
                break;
            }
            catch
            {
            }
        }
    }

    public void Dispose()
    {
        _cancellationTokenSource?.Cancel();
        try
        {
            _listenerTask?.Wait(1000);
        }
        catch
        {
        }

        _cancellationTokenSource?.Dispose();
        if (IsPrimaryInstance)
        {
            _mutex.ReleaseMutex();
        }
        _mutex.Dispose();
    }
}

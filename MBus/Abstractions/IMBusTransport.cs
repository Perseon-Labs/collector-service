using System.Net.Sockets;

namespace CollectorService.MBus.Abstractions;

public interface IMBusTransport : IAsyncDisposable
{
    Task ConnectAsync(string host, int port, CancellationToken cancellationToken);
    Task SendAsync(ReadOnlyMemory<byte> buffer, CancellationToken cancellationToken);
    Task<int> ReceiveAsync(Memory<byte> buffer, CancellationToken cancellationToken);
}

public sealed class TcpMBusTransport : IMBusTransport
{
    private TcpClient? _client;

    public async Task ConnectAsync(string host, int port, CancellationToken cancellationToken)
    {
        _client = new TcpClient();
        using var reg = cancellationToken.Register(() => _client?.Close());
        await _client.ConnectAsync(host, port, cancellationToken);
    }

    public async Task SendAsync(ReadOnlyMemory<byte> buffer, CancellationToken cancellationToken)
    {
        if (_client?.Connected != true) throw new InvalidOperationException("Not connected");
        await _client.GetStream().WriteAsync(buffer, cancellationToken);
    }

    public async Task<int> ReceiveAsync(Memory<byte> buffer, CancellationToken cancellationToken)
    {
        if (_client?.Connected != true) throw new InvalidOperationException("Not connected");
        return await _client.GetStream().ReadAsync(buffer, cancellationToken);
    }

    public ValueTask DisposeAsync()
    {
        try { _client?.Dispose(); } catch { }
        return ValueTask.CompletedTask;
    }
}

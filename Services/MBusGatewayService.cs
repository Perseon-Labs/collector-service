using CollectorService.Contracts;
using CollectorService.MBus.Configuration;
using CollectorService.MBus.Abstractions;
using CollectorService.MBus.Parsing;
using CollectorService.MBus.Protocol;
using CollectorService.MBus.Util;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace CollectorService.Services;

public class MBusGatewayService : IMBusGatewayService
{
    private readonly IOptions<MBusOptions> _options;
    private readonly ILogger<MBusGatewayService> _logger;
    private readonly MBusFrameParser _parser = new();

    public MBusGatewayService(IOptions<MBusOptions> options, ILogger<MBusGatewayService> logger)
    {
        _options = options;
        _logger = logger;
    }

    public async Task<MeterReadingResult> ReadMeterAsync(string deviceId, CancellationToken cancellationToken = default)
    {
        var device = _options.Value.Devices.FirstOrDefault(d => d.DeviceId == deviceId);
        if (device is null)
        {
            return new MeterReadingResult { DeviceId = deviceId, TimestampUtc = DateTime.UtcNow, Success = false, ErrorMessage = "Unknown device" };
        }

        for (int attempt = 0; attempt <= _options.Value.RetryCount; attempt++)
        {
            try
            {
                await using IMBusTransport transport = new TcpMBusTransport();
                using var cts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
                cts.CancelAfter(_options.Value.TimeoutMs);
                await transport.ConnectAsync(device.Host, device.Port, cts.Token);

                // Build a simple REQ_UD2 long frame: 10 5B A0 5B 16 (short frame) but we expect long response
                // For now send short frame request for class 2 data.
                Span<byte> request = stackalloc byte[5];
                request[0] = MBusConstants.StartShort; // 0x10
                request[1] = MBusConstants.REQ_UD2;    // Control field
                request[2] = device.Address;           // Address
                request[3] = (byte)(request[1] + request[2]); // Checksum simple sum for short frame
                request[4] = MBusConstants.Stop;       // 0x16
                await transport.SendAsync(request.ToArray(), cts.Token);

                // Receive response (simplified single read)
                byte[] buffer = new byte[MBusConstants.MaxFrameLength];
                int read = await transport.ReceiveAsync(buffer, cts.Token);
                if (read <= 0)
                {
                    throw new IOException("No data received");
                }
                var span = new ReadOnlySpan<byte>(buffer, 0, read);
                var parsed = _parser.Parse(span);
                if (!parsed.Success)
                {
                    return new MeterReadingResult
                    {
                        DeviceId = deviceId,
                        TimestampUtc = DateTime.UtcNow,
                        Success = false,
                        ErrorMessage = parsed.Error,
                        RawFrameHex = parsed.RawHex
                    };
                }
                return new MeterReadingResult
                {
                    DeviceId = deviceId,
                    TimestampUtc = DateTime.UtcNow,
                    Success = true,
                    Records = parsed.Records,
                    RawFrameHex = parsed.RawHex
                };
            }
            catch (Exception ex) when (attempt < _options.Value.RetryCount)
            {
                _logger.LogWarning(ex, "Attempt {Attempt} failed for device {DeviceId}", attempt + 1, deviceId);
                await Task.Delay(100, cancellationToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to read device {DeviceId}", deviceId);
                return new MeterReadingResult
                {
                    DeviceId = deviceId,
                    TimestampUtc = DateTime.UtcNow,
                    Success = false,
                    ErrorMessage = ex.Message
                };
            }
        }

        return new MeterReadingResult
        {
            DeviceId = deviceId,
            TimestampUtc = DateTime.UtcNow,
            Success = false,
            ErrorMessage = "Exhausted retries"
        };
    }

    public async Task<IEnumerable<MeterReadingResult>> ReadAllMetersAsync(CancellationToken cancellationToken = default)
    {
        var tasks = _options.Value.Devices.Select(d => ReadMeterAsync(d.DeviceId, cancellationToken));
        return await Task.WhenAll(tasks);
    }
}

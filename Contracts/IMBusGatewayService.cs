using System.Collections.Generic;
using System.Threading.Tasks;

namespace CollectorService.Contracts
{
    /// <summary>
    /// Gateway abstraction for reading M-Bus meters.
    /// </summary>
    public interface IMBusGatewayService
    {
        Task<MeterReadingResult> ReadMeterAsync(string deviceId, CancellationToken cancellationToken = default);
        Task<IEnumerable<MeterReadingResult>> ReadAllMetersAsync(CancellationToken cancellationToken = default);
    }

    /// <summary>
    /// High level reading result that can contain multiple data records from one physical meter.
    /// </summary>
    public sealed class MeterReadingResult
    {
        public required string DeviceId { get; init; }
        public DateTime TimestampUtc { get; init; }
        public bool Success { get; init; }
        public string? ErrorMessage { get; init; }
        public string? RawFrameHex { get; init; }
        public IReadOnlyList<MeterRecord> Records { get; init; } = Array.Empty<MeterRecord>();
    }

    public sealed class MeterRecord
    {
        public required MBusDataType Type { get; init; }
        public double? Value { get; init; }
        public string? Unit { get; init; }
        public int? Scaling { get; init; }
        public string? Code { get; init; }
    }

    /// <summary>
    /// Simplified canonical data types recognized by the parser.
    /// </summary>
    public enum MBusDataType
    {
        Unknown = 0,
        Energy,
        Volume,
        Power,
        Flow,
        Temperature,
        Pressure,
        Time,
        DateTime,
        Voltage,
        Current
    }
}

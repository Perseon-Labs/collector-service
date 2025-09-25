namespace CollectorService.MBus.Configuration;

public class MBusOptions
{
    public int TimeoutMs { get; set; } = 5000;
    public int RetryCount { get; set; } = 2;
    public List<MBusDeviceOptions> Devices { get; set; } = new();
}

public class MBusDeviceOptions
{
    public required string DeviceId { get; set; }
    public byte Address { get; set; } // Primary address 0-250
    public required string Host { get; set; }
    public int Port { get; set; } = 10001;
}

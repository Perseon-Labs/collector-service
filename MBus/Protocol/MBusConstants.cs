namespace CollectorService.MBus.Protocol;

internal static class MBusConstants
{
    public const byte StartShort = 0x10; // Short frame start
    public const byte StartLong = 0x68;  // Long/variable frame start
    public const byte Stop = 0x16;

    // Control field function codes (subset)
    public const byte SND_NKE = 0x40;   // Initialization
    public const byte REQ_UD2 = 0x5B;   // Request for class 2 data
    public const byte SND_UD = 0x53;    // Send user data

    public const int MaxFrameLength = 300; // Basic safeguard
}

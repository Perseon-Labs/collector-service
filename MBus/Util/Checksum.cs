using System.Runtime.CompilerServices;

namespace CollectorService.MBus.Util;

internal static class Checksum
{
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static byte Compute(ReadOnlySpan<byte> data)
    {
        int sum = 0;
        for (int i = 0; i < data.Length; i++) sum += data[i];
        return (byte)(sum & 0xFF);
    }
}

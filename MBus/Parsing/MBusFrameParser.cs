using System.Text;
using CollectorService.MBus.Protocol;
using CollectorService.MBus.Util;
using CollectorService.Contracts;

namespace CollectorService.MBus.Parsing;

internal sealed class MBusFrameParser
{
    public ParseResult Parse(ReadOnlySpan<byte> buffer)
    {
        // Long variable data frame validation: 68 L L 68 C A CI ... CS 16
        if (buffer.Length < 9) return ParseResult.Fail("Frame too short");
        if (buffer[0] != MBusConstants.StartLong || buffer[3] != MBusConstants.StartLong)
            return ParseResult.Fail("Not a long frame");
        var len1 = buffer[1];
        var len2 = buffer[2];
        if (len1 != len2) return ParseResult.Fail("Length mismatch");
        int expectedTotal = len1 + 6; // 68 L L 68 ... CS 16
        if (buffer.Length < expectedTotal) return ParseResult.Fail("Incomplete frame");
        var slice = buffer.Slice(0, expectedTotal);
        if (slice[^1] != MBusConstants.Stop) return ParseResult.Fail("Missing stop byte");
        var checksum = slice[^2];
        var calc = Checksum.Compute(slice.Slice(4, slice.Length - 6));
        if (checksum != calc) return ParseResult.Fail("Checksum mismatch");

        byte control = slice[4]; // currently unused
        byte address = slice[5]; // currently unused
        byte ci = slice[6];      // CI (0x72 / 0x73 common for variable data)

        var payload = slice.Slice(7, slice.Length - 9); // After CI up to before checksum
    var reader = new PayloadReader(payload);
        var records = new List<MeterRecord>();

        while (reader.Remaining > 0)
        {
            var rec = TryReadRecord(ref reader);
            if (rec == null)
            {
                // Could not parse further; capture remainder as raw and break
                if (reader.Remaining > 0)
                {
                    var remaining = reader.Snapshot();
                    records.Add(new MeterRecord
                    {
                        Type = MBusDataType.Unknown,
                        Code = "RAW:" + Convert.ToHexString(remaining.ToArray())
                    });
                }
                break;
            }
            records.Add(rec);
        }

        // Fallback: if no records but payload exists, store payload raw
        if (records.Count == 0 && payload.Length > 0)
        {
            records.Add(new MeterRecord { Type = MBusDataType.Unknown, Code = Convert.ToHexString(payload.ToArray()) });
        }

        return ParseResult.Ok(records, Convert.ToHexString(slice.ToArray()));
    }

    private sealed class PayloadReader
    {
        private readonly byte[] _buffer;
        private int _offset;
        public PayloadReader(ReadOnlySpan<byte> span)
        {
            _buffer = span.ToArray();
            _offset = 0;
        }
        public int Remaining => _buffer.Length - _offset;
        public byte ReadByte() { var b = _buffer[_offset]; _offset++; return b; }
        public ReadOnlySpan<byte> ReadBytes(int count) { var s = new ReadOnlySpan<byte>(_buffer, _offset, count); _offset += count; return s; }
        public ReadOnlySpan<byte> Snapshot() => new ReadOnlySpan<byte>(_buffer, _offset, Remaining);
    }

    private MeterRecord? TryReadRecord(ref PayloadReader reader)
    {
        if (reader.Remaining == 0) return null;

        byte dif = reader.ReadByte();
        if (dif == 0x2F) // No data (fill) DIF per spec just skip
        {
            return new MeterRecord { Type = MBusDataType.Unknown, Code = "DIF:2F" };
        }

        bool hasMoreDif = (dif & 0x80) != 0; // Extension bit (not iterated yet for simplicity)
        int dataFieldCode = dif & 0x0F;
        int storageNr = (dif & 0x40) != 0 ? 1 : 0; // Simplified

        // Determine length in bytes from data field code (subset)
        int length = dataFieldCode switch
        {
            0x00 => 0,   // no data
            0x01 => 1,   // 8-bit int
            0x02 => 2,   // 16-bit int
            0x03 => 3,   // 24-bit int
            0x04 => 4,   // 32-bit int / float
            0x05 => 4,   // 32-bit int (alt)
            0x06 => 6,   // 48-bit int
            0x07 => 8,   // 64-bit int / double
            0x09 => 1,   // 2-digit BCD
            0x0A => 2,   // 4-digit BCD
            0x0B => 3,   // 6-digit BCD
            0x0C => 4,   // 8-digit BCD
            0x0D => 6,   // 12-digit BCD
            0x0E => 8,   // 16-digit BCD
            _ => -1
        };

        // Read VIF
        if (reader.Remaining == 0) return null;
        byte vif = reader.ReadByte();
        bool vifExt = (vif & 0x80) != 0; // extension bit (ignored for now)

        // Additional VIF extensions skipped for MVP

        ReadOnlySpan<byte> valueBytes = length > 0 && length <= reader.Remaining ? reader.ReadBytes(length) : ReadOnlySpan<byte>.Empty;

        // Decode unit & scaling from VIF (subset) and value from bytes
        var (type, unit, scaling) = DecodeVif(vif);
        double? value = DecodeValue(dataFieldCode, valueBytes, scaling);

        string code = $"DIF:{dif:X2}|VIF:{vif:X2}";
        return new MeterRecord
        {
            Type = type,
            Unit = unit,
            Scaling = scaling,
            Value = value,
            Code = code
        };
    }

    private static (MBusDataType Type, string? Unit, int? Scaling) DecodeVif(byte vif)
    {
        // Basic mapping referencing EN 13757-3 patterns (subset)
        // Energy (E0..EF) pattern 0x08..0x0F etc in real spec; here simplified heuristic segments
        // We'll implement a few common encodings using upper nibbles.
        byte baseCode = (byte)(vif & 0x7F); // remove extension bit
        return baseCode switch
        {
            0x06 => (MBusDataType.Energy, "Wh", 0),
            0x07 => (MBusDataType.Energy, "Wh", 1),
            0x08 => (MBusDataType.Energy, "Wh", 2),
            0x09 => (MBusDataType.Energy, "Wh", 3),
            0x10 => (MBusDataType.Volume, "m3", 0),
            0x11 => (MBusDataType.Volume, "m3", 1),
            0x12 => (MBusDataType.Volume, "m3", 2),
            0x13 => (MBusDataType.Volume, "m3", 3),
            0x28 => (MBusDataType.Power, "W", 0),
            0x29 => (MBusDataType.Power, "W", 1),
            0x2A => (MBusDataType.Power, "W", 2),
            0x2B => (MBusDataType.Power, "W", 3),
            0x3C => (MBusDataType.Flow, "m3/h", 0),
            0x3D => (MBusDataType.Flow, "m3/h", 1),
            0x3E => (MBusDataType.Flow, "m3/h", 2),
            0x3F => (MBusDataType.Flow, "m3/h", 3),
            0x5E => (MBusDataType.Temperature, "°C", 0),
            0x5F => (MBusDataType.Temperature, "°C", -1),
            _ => (MBusDataType.Unknown, null, null)
        };
    }

    private static double? DecodeValue(int dataFieldCode, ReadOnlySpan<byte> bytes, int? scaling)
    {
        if (bytes.IsEmpty) return null;
        try
        {
            double raw = dataFieldCode switch
            {
                0x01 => (sbyte)bytes[0],
                0x02 => BitConverter.ToInt16(bytes),
                0x03 => DecodeInt24(bytes),
                0x04 or 0x05 => BitConverter.ToInt32(PadTo(bytes,4)),
                0x06 => DecodeInt48(bytes),
                0x07 => BitConverter.ToInt64(PadTo(bytes,8)),
                0x09 or 0x0A or 0x0B or 0x0C or 0x0D or 0x0E => DecodeBcd(bytes),
                _ => double.NaN
            };
            if (double.IsNaN(raw)) return null;
            if (scaling.HasValue)
            {
                return raw * Math.Pow(10, scaling.Value);
            }
            return raw;
        }
        catch
        {
            return null;
        }
    }

    private static double DecodeBcd(ReadOnlySpan<byte> bytes)
    {
        var sb = new StringBuilder(bytes.Length * 2);
        for (int i = bytes.Length - 1; i >= 0; i--) // Reverse as per little-endian BCD convention
        {
            byte b = bytes[i];
            sb.Append((b >> 4) & 0xF);
            sb.Append(b & 0xF);
        }
        if (double.TryParse(sb.ToString().TrimStart('0'), out var val)) return val;
        return 0d;
    }

    private static long DecodeInt24(ReadOnlySpan<byte> bytes)
    {
        int value = bytes[0] | (bytes[1] << 8) | (bytes[2] << 16);
        if ((value & 0x800000) != 0) value |= unchecked((int)0xFF000000); // sign extend
        return value;
    }

    private static long DecodeInt48(ReadOnlySpan<byte> bytes)
    {
        long value = 0;
        for (int i = 0; i < 6; i++) value |= (long)bytes[i] << (8 * i);
        if ((value & 0x800000000000) != 0) value |= unchecked((long)0xFFFF000000000000); // sign extend for 48-bit
        return value;
    }

    private static byte[] PadTo(ReadOnlySpan<byte> bytes, int size)
    {
        var arr = new byte[size];
        bytes.CopyTo(arr);
        return arr;
    }

    public sealed class ParseResult
    {
        public bool Success { get; private set; }
        public string? Error { get; private set; }
        public IReadOnlyList<MeterRecord> Records { get; private set; } = Array.Empty<MeterRecord>();
        public string? RawHex { get; private set; }
        public static ParseResult Ok(IReadOnlyList<MeterRecord> records, string rawHex) => new() { Success = true, Records = records, RawHex = rawHex };
        public static ParseResult Fail(string error) => new() { Success = false, Error = error };
    }
}

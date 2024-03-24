using Emedia_1_wpf.Extensions;

namespace Emedia_1_wpf.Services.Chunks;

public class pHYsChunk : PngChunk
{
    public uint PixelsPerUnitX { get; }
    public uint PixelsPerUnitY { get; }
    public UnitSpecifier UnitSpecifier { get; }

    public override bool RemoveWhenAnonymizing => true;

    public pHYsChunk(uint length, byte[] data, string type, uint crc, bool crcValid) :
        base(length, data, type, crc, crcValid)
    {
        var span = data.AsSpan();
        
        PixelsPerUnitX = span[..4].GetUint();
        PixelsPerUnitY = span[4..8].GetUint();
        UnitSpecifier = (UnitSpecifier) span[8];
    }

    public override string FormatData() => $"Type: {Type}, Pixels Per Unit: ({PixelsPerUnitX}, {PixelsPerUnitY}), Unit Specifier: {UnitSpecifier}";

    protected override void EnsureValid()
    {
        if (Data.Length != 9)
        {
            throw new ArgumentException("Invalid pHYs chunk data length.");
        }
    }
}

public enum UnitSpecifier : byte
{
    Unknown = 0,
    Meter = 1
}
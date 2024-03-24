using Emedia_1_wpf.Extensions;

namespace Emedia_1_wpf.Services.Chunks;

public class oFFsChunk : PngChunk
{
    public UnitType Unit { get; }
    public int OffsetX { get; }
    public int OffsetY { get; }

    public oFFsChunk(uint length, byte[] data, string type, uint crc, bool crcValid) :
        base(length, data, type, crc, crcValid)
    {   
        Unit = (UnitType) data[8];

        var span = data.AsSpan();

        OffsetX = (int) span[..4].GetUint();
        OffsetY = (int) span[4..8].GetUint();
    }
    
    public override string FormatData() => $"Type: {Type}, Offset: X={OffsetX} Y={OffsetY}, Unit: {Unit}";

    protected override void EnsureValid()
    {
        if (Data.Length != 9)
        {
            throw new ArgumentException("oFFs chunk data must be exactly 9 bytes long.");
        }

        if (!Enum.IsDefined(typeof(UnitType), Unit))
        {
            throw new ArgumentException("Invalid bit data value.");
        }
    }
}

public enum UnitType : byte
{
    Pixels = 0,
    Micrometers = 1,
    Unknown = 2
}
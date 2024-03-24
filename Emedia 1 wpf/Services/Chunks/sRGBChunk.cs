namespace Emedia_1_wpf.Services.Chunks;

public class sRGBChunk : PngChunk
{
    public RenderingIntent RenderingIntent { get; }

    public sRGBChunk(uint length, byte[] data, string type, uint crc, bool crcValid) :
        base(length, data, type, crc, crcValid)
    {
        RenderingIntent = (RenderingIntent)data[0];
    }

    public override string FormatData() => $"Type: {Type}, Rendering Intent: {RenderingIntent}";

    protected override void EnsureValid()
    {
        if (!Enum.IsDefined(typeof(RenderingIntent), RenderingIntent))
        {
            throw new ArgumentException("Invalid rendering intent.");
        }
        
        if (Data.Length != 1)
        {
            throw new ArgumentException("sRGB chunk data must be exactly 1 byte long.");
        }
    }
}
public enum RenderingIntent : byte
{
    Perceptual = 0,
    RelativeColorimetric = 1,
    Saturation = 2,
    AbsoluteColorimetric = 3
}
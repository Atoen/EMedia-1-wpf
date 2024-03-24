using Emedia_1_wpf.Extensions;

namespace Emedia_1_wpf.Services.Chunks;

public class IHDRChunk : PngChunk
{
    public uint Width { get; }
    public uint Height { get; }
    public BitDepth BitDepth { get; }
    public ColorType ColorType { get; }
    public CompressionMethod CompressionMethod { get; }
    public FilterMethod FilterMethod { get; }
    public InterlaceMethod InterlaceMethod { get; }
    
    public override bool RemoveWhenAnonymizing => false;

    public IHDRChunk(uint length, byte[] data, string type, uint crc, bool crcValid) :
        base(length, data, type, crc, crcValid)
    {
        var span = data.AsSpan();

        Width = span[..4].GetUint();
        Height = span[4..8].GetUint();
        
        BitDepth = (BitDepth) span[8];
        ColorType = (ColorType) span[9];
        CompressionMethod = (CompressionMethod) span[10];
        FilterMethod = (FilterMethod) span[11];
        InterlaceMethod = (InterlaceMethod) span[12];
    }

    public override string FormatData() =>
    $"Type: {Type}, Width: {Width}, Height: {Height}, Bit Depth: {BitDepth}, Color Type: {ColorType}, Compression Method: {CompressionMethod}, Filter Method: {FilterMethod}, Interlace Method: {InterlaceMethod}";

    protected override void EnsureValid()
    {
        if (Data.Length != 13)
        {
            throw new ArgumentException("Invalid IHDR chunk data length.");
        }
        
        if (!Enum.IsDefined(typeof(BitDepth), BitDepth))
            throw new ArgumentException("Invalid bit depth value.");

        if (!Enum.IsDefined(typeof(ColorType), ColorType))
            throw new ArgumentException("Invalid color type value.");

        if (!Enum.IsDefined(typeof(CompressionMethod), CompressionMethod))
            throw new ArgumentException("Invalid compression method value.");

        if (!Enum.IsDefined(typeof(FilterMethod), FilterMethod))
            throw new ArgumentException("Invalid filter method value.");

        if (!Enum.IsDefined(typeof(InterlaceMethod), InterlaceMethod))
            throw new ArgumentException("Invalid interlace method value.");
        
        ValidateBitDepth();
    }

    private void ValidateBitDepth()
    {
        switch (ColorType)
        {
            case ColorType.Grayscale:
                if (BitDepth != BitDepth.Bit1 &&
                    BitDepth != BitDepth.Bit2 &&
                    BitDepth != BitDepth.Bit4 &&
                    BitDepth != BitDepth.Bit8 &&
                    BitDepth != BitDepth.Bit16)
                {
                    throw new ArgumentException("Invalid bit depth for grayscale color type.");
                }
                break;
            case ColorType.RGB:
                if (BitDepth != BitDepth.Bit8 && BitDepth != BitDepth.Bit16)
                {
                    throw new ArgumentException("Invalid bit depth for true color color type.");
                }
                break;
            case ColorType.PaletteIndex:
                if (BitDepth != BitDepth.Bit1 &&
                    BitDepth != BitDepth.Bit2 &&
                    BitDepth != BitDepth.Bit4 &&
                    BitDepth != BitDepth.Bit8)
                {
                    throw new ArgumentException("Invalid bit depth for indexed color type.");
                }
                break;
            case ColorType.GrayscaleWithAlpha:
                if (BitDepth != BitDepth.Bit8 && BitDepth != BitDepth.Bit16)
                {
                    throw new ArgumentException("Invalid bit depth for grayscale with alpha color type.");
                }
                break;
            case ColorType.RGBA:
                if (BitDepth != BitDepth.Bit8 && BitDepth != BitDepth.Bit16)
                {
                    throw new ArgumentException("Invalid bit depth for true color with alpha color type.");
                }
                break;
            default:
                throw new ArgumentException("Invalid color type.");
        }
    }
}

public enum BitDepth : byte
{
    Bit1 = 1,
    Bit2 = 2,
    Bit4 = 4,
    Bit8 = 8,
    Bit16 = 16
}

public enum ColorType : byte
{
    Grayscale = 0,
    RGB = 2,
    PaletteIndex = 3,
    GrayscaleWithAlpha = 4,
    RGBA = 6
}

public enum CompressionMethod : byte
{
    Deflate = 0
}

public enum FilterMethod : byte
{
    Standard = 0
}

public enum InterlaceMethod : byte
{
    None = 0,
    Adam7 = 1
}
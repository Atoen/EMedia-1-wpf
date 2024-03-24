using System.IO;
using System.IO.Compression;
using System.Text;
using Emedia_1_wpf.Extensions;

namespace Emedia_1_wpf.Services.Chunks;

public class PngChunk
{
    public uint Length { get; }
    public byte[] Data { get; }
    public string Type { get; }
    public uint Crc { get; }
    public bool CrcValid { get; }

    public virtual bool RemoveWhenAnonymizing => true;

    public virtual string FormatData() => $"Type: {Type}, Length: {Length}, CRC: {CrcValid}";

    protected virtual void EnsureValid() { }
    
    public static async Task<PngChunk> CreateAsync(Stream stream)
    {
        var length = (await stream.ReadBytesAsync(4)).GetUint();
        var typeBytes = await stream.ReadBytesAsync(4);
        
        var data = await stream.ReadBytesAsync(length);
        var crc = (await stream.ReadBytesAsync(4)).GetUint();

        var chunk = CreateTyped(length, typeBytes, data, crc);
        chunk.EnsureValid();

        return chunk;
    }

    private static PngChunk CreateTyped(uint length, byte[] typeBytes, byte[] data, uint crc)
    {
        var typeName = Encoding.ASCII.GetString(typeBytes);
        if (!Enum.TryParse<PngChunkType>(typeName, out var type))
        {
            Console.WriteLine($"Unknown chunk type: {typeName}");
        }

        var expectedCrc = Crc32.Get([..typeBytes, ..data]);
        var crcValid = expectedCrc == crc;
        if (!crcValid)
        {
            Console.WriteLine($"Crc value is invalid. Expected: {expectedCrc}, actual: {crc}");
        }

        return type switch
        {
            PngChunkType.IHDR => new IHDRChunk(length, data, typeName, crc, crcValid),
            PngChunkType.tEXt => new tEXtChunk(length, data, typeName, crc, crcValid),
            PngChunkType.IDAT => new IDATChunk(length, data, typeName, crc, crcValid),
            PngChunkType.IEND => new IENDChunk(length, data, typeName, crc, crcValid),
            PngChunkType.gAMA => new gAMAChunk(length, data, typeName, crc, crcValid),
            PngChunkType.sRGB => new sRGBChunk(length, data, typeName, crc, crcValid),
            PngChunkType.PLTE => new PLTEChunk(length, data, typeName, crc, crcValid),
            PngChunkType.cHRM => new cHRMChunk(length, data, typeName, crc, crcValid),
            PngChunkType.tRNS => new tRNSChunk(length, data, typeName, crc, crcValid),
            PngChunkType.tIME => new tIMEChunk(length, data, typeName, crc, crcValid),
            PngChunkType.zTXt => new zTXtChunk(length, data, typeName, crc, crcValid),
            PngChunkType.pHYs => new pHYsChunk(length, data, typeName, crc, crcValid),
            PngChunkType.bKGD => new bKGDChunk(length, data, typeName, crc, crcValid),
            PngChunkType.hIST => new hISTChunk(length, data, typeName, crc, crcValid),
            PngChunkType.iTXt => new iTXtChunk(length, data, typeName, crc, crcValid),
            PngChunkType.oFFs => new oFFsChunk(length, data, typeName, crc, crcValid),
            PngChunkType.sBIT => new sBITChunk(length, data, typeName, crc, crcValid),
            PngChunkType.sPLT => new sPLTChunk(length, data, typeName, crc, crcValid),
            PngChunkType.sTER => new sTERChunk(length, data, typeName, crc, crcValid),
            _ => new PngChunk(length, data, typeName, crc, crcValid)
        };
    }

    public async Task AppendToStreamAsync(Stream stream)
    {
        await stream.WriteUIntAsync(Length);
        await stream.WriteAsciiStringAsync(Type);
        await stream.WriteAsync(Data);
        await stream.WriteUIntAsync(Crc);
    }

    public static byte[] Decompress(byte[] data)
    {
        using var decompressedStream = new MemoryStream();
        using var compressStream = new MemoryStream(data);
        using var deflateStream = new DeflateStream(compressStream, CompressionMode.Decompress);
        
        deflateStream.CopyTo(decompressedStream);
        return decompressedStream.ToArray();
    }
    
    public static async Task<byte[]> DecompressAsync(byte[] data)
    {
        using var decompressedStream = new MemoryStream();
        using var compressStream = new MemoryStream(data);
        await using var deflateStream = new DeflateStream(compressStream, CompressionMode.Decompress);
        
        await deflateStream.CopyToAsync(decompressedStream);
        return decompressedStream.ToArray();
    }

    public static string DecompressString(byte[] data, Encoding encoding)
    {
        return encoding.GetString(Decompress(data));
    }
    
    public static async Task<string> DecompressStringAsync(byte[] data, Encoding encoding)
    {
        return encoding.GetString(await DecompressAsync(data));
    }

    protected PngChunk(uint length, byte[] data, string type, uint crc, bool crcValid)
    {
        Length = length;
        Data = data;
        Type = type;
        Crc = crc;
        CrcValid = crcValid;
    }
}

public enum PngChunkType
{
    IHDR, // Image header
    PLTE, // Palette
    IDAT, // Image data
    IEND, // Image end
    tRNS, // Transparency
    cHRM, // Chromaticity
    sRGB, // Standard RGB color space
    gAMA, // Gamma
    tEXt, // Textual data
    zTXt, // Compressed textual data
    iTXt, // International textual data
    bKGD, // Background color
    hIST, // Image histogram
    pHYs, // Physical pixel dimensions
    sBIT, // Significant bits
    sPLT, // Suggested palette
    tIME, // Image last-modification time
    oFFs, // Image offset
    sTER  // Stereo image indicator
}
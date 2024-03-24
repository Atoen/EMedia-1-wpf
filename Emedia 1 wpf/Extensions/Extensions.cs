using System.Buffers.Binary;
using System.IO;
using System.Text;
using Emedia_1_wpf.Services.Chunks;

namespace Emedia_1_wpf.Extensions;

public static class Extensions
{
    public static uint GetUint(this Span<byte> span)
    {
        return BinaryPrimitives.ReadUInt32BigEndian(span);
    }

    public static double GetFixedPoint(this Span<byte> span)
    {
        var num = BinaryPrimitives.ReadUInt32BigEndian(span);
        return num / 100000.0;
    }
    
    public static uint GetUint(this byte[] array)
    {
        return BinaryPrimitives.ReadUInt32BigEndian(array);
    }

    public static byte[] ReadBytes(this Stream stream, uint length)
    {
        var buffer = new byte[length];
        var bytesRead = stream.Read(buffer);
        if (bytesRead != length)
        {
            throw new InvalidOperationException("Failed to read requested amount");
        }

        return buffer;
    }
    
    public static async Task<byte[]> ReadBytesAsync(this Stream stream, uint length)
    {
        var buffer = new byte[length];
        var bytesRead = await stream.ReadAsync(buffer);
        if (bytesRead != length)
        {
            throw new InvalidOperationException("Failed to read requested amount");
        }

        return buffer;
    }

    public static void WriteUInt(this Stream stream, uint num)
    {
        Span<byte> span = stackalloc byte[4];
        BinaryPrimitives.WriteUInt32BigEndian(span, num);
        stream.Write(span);
    }
    
    public static void WriteAsciiString(this Stream stream, string text)
    {
        var bytes = Encoding.ASCII.GetBytes(text);
        stream.Write(bytes);
    }

    public static int GetByteWidth(this ColorType colorType) => colorType switch
    {
        ColorType.Grayscale => 1,
        ColorType.RGB => 3,
        ColorType.PaletteIndex => 1,
        ColorType.GrayscaleWithAlpha => 2,
        ColorType.RGBA => 4,
        _ => throw new ArgumentOutOfRangeException(nameof(colorType), colorType, null)
    };
}
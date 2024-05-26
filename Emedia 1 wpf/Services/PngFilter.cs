using Emedia_1_wpf.Extensions;
using Emedia_1_wpf.Services.Chunks;

namespace Emedia_1_wpf.Services;

public class PngFilter
{
    private readonly int _rowByteCount;
    
    private byte[] _lastRow;
    
    public int PixelWidth { get; }

    public PngFilter(int imageWidth, ColorType colorType, BitDepth bitDepth)
    {
        PixelWidth = (int) Math.Ceiling(colorType.GetByteWidth() * (int) bitDepth / 8.0);
        _rowByteCount = imageWidth * PixelWidth;
        _lastRow = new byte[_rowByteCount];
    }

    public byte[] Decode(byte[] rawData)
    {
        var filterType = (FilterType) rawData[0];
        var encodedData = new ArraySegment<byte>(rawData, 1, rawData.Length - 1);

        return filterType switch
        {
            FilterType.None => encodedData.ToArray(),
            FilterType.Sub => DecodeSub(encodedData),
            FilterType.Up => DecodeUp(encodedData),
            FilterType.Average => DecodeAverage(encodedData),
            FilterType.Paeth => DecodePaeth(encodedData),
            _ => throw new ArgumentOutOfRangeException(nameof(filterType))
        };
    }

    private static byte ArrayGet(byte[] array, int index) => index < 0 ? (byte) 0 : array[index];

    private byte[] DecodeSub(ArraySegment<byte> encodedData)
    {
        var decoded = new byte[encodedData.Count];
        for (var i = 0; i < encodedData.Count; i++)
        {
            decoded[i] = i < _rowByteCount
                ? (byte) (encodedData[i] + ArrayGet(decoded, i - PixelWidth))
                : encodedData[i];
        }

        _lastRow = decoded;
        return decoded;
    }
    
    private byte[] DecodeUp(ArraySegment<byte> encodedData)
    {
        var decoded = new byte[encodedData.Count];
        for (var i = 0; i < encodedData.Count; i++)
        {
            decoded[i] = i < _rowByteCount
                ? (byte) (encodedData[i] + _lastRow[i])
                : encodedData[i];
        }
        
        _lastRow = decoded;
        return decoded;
    }
    
    private byte[] DecodeAverage(ArraySegment<byte> encodedData)
    {
        var decoded = new byte[encodedData.Count];
        for (var i = 0; i < encodedData.Count; i++)
        {
            if (i < _rowByteCount)
            {
                var a =  ArrayGet(decoded, i - PixelWidth);
                var b = _lastRow[i];
                decoded[i] = (byte) (encodedData[i] + (a + b) / 2);
            }
            else
            {
                decoded[i] = encodedData[i];
            }
        }
        
        _lastRow = decoded;
        return decoded;
    }

    private byte[] DecodePaeth(ArraySegment<byte> encodedData)
    {
        var decodedRow = new byte[encodedData.Count];
        for (var i = 0; i < encodedData.Count; i++)
        {
            if (i < _rowByteCount)
            {
                var a =  ArrayGet(decodedRow, i - PixelWidth);
                var b = _lastRow[i];
                var c = ArrayGet(_lastRow, i - PixelWidth);

                var p = a + b - c;
                var pa = Math.Abs(p - a);
                var pb = Math.Abs(p - b);
                var pc = Math.Abs(p - c);

                if (pa <= pb && pa <= pc)
                {
                    decodedRow[i] = (byte) (encodedData[i] + a);
                }
                else if (pb <= pc)
                {
                    decodedRow[i] = (byte) (encodedData[i] + b);
                }
                else
                {
                    decodedRow[i] = (byte) (encodedData[i] + c);
                }
            }
            else
            {
                decodedRow[i] = encodedData[i];
            }
        }

        _lastRow = decodedRow;
        return decodedRow;
    }
    
    public byte[] EncodeNone(byte[] rawData)
    {
        var encodedData = new byte[rawData.Length + 1];
        encodedData[0] = (byte) FilterType.None;
        Array.Copy(rawData, 0, encodedData, 1, rawData.Length);
        return encodedData;
    }
}

public enum FilterType : byte
{
    None,
    Sub,
    Up,
    Average,
    Paeth
}
namespace Emedia_1_wpf.Services.Chunks;

public class hISTChunk : PngChunk
{
    public ushort[] Histogram { get; }

    public override bool RemoveWhenAnonymizing => true;

    public hISTChunk(uint length, byte[] data, string type, uint crc, bool crcValid) :
        base(length, data, type, crc, crcValid)
    {
        Histogram = new ushort[length / 2]; 
        
        for (var i = 0; i < Histogram.Length; i++)
        {
            Histogram[i] = (ushort)((data[i * 2] << 8) | data[i * 2 + 1]);
        }
    }

    public override string FormatData()
    {
        return $"Type: {Type}, Histogram:";

        // for (var i = 0; i < Histogram.Length; i++)
        // {
        //     Console.WriteLine($"Color {i}: {Histogram[i]} occurances");
        // }
    }
    
    protected override void EnsureValid()
    {
        if (Data.Length != 2)
        {
            throw new ArgumentException("hIST chunk data must be exactly 2 bytes long.");
        }
    }
}
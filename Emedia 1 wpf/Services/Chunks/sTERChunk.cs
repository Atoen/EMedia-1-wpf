namespace Emedia_1_wpf.Services.Chunks;

public class sTERChunk : PngChunk
{
    public StereoIndicator Indicator { get;}
    
    public override bool RemoveWhenAnonymizing => true;
    
    public sTERChunk(uint length, byte[] data, string type, uint crc, bool crcValid) :
        base(length, data, type, crc, crcValid)
    {
        Indicator = (StereoIndicator) data[0];
    }
    
    public override string FormatData() => $"Type: {Type}, Mode: {Indicator}";

    protected override void EnsureValid()
    {
        if (Data.Length != 1)
        {
            throw new ArgumentException("sTER chunk data must be exactly 1 byte long.");
        }

        if (!Enum.IsDefined(typeof(StereoIndicator), Indicator))
        {
            throw new ArgumentException("Invalid bit StereoIndicator value.");
        }
    }
}

public enum StereoIndicator
{
    CrossFuseLayout = 0,
    DivergingFuseLayout = 1
}
using System.IO;
using Emedia_1_wpf.Services.Chunks;

namespace Emedia_1_wpf.Services;

public class PngService
{
    public static readonly byte[] PngSignature = [0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A];
    public static int SignatureLength => PngSignature.Length;
    
    public async Task<bool> VerifyPngAsync(string filePath)
    {
        try
        {
            await using var stream = File.Open(filePath, FileMode.Open);
            return await CheckSignatureAsync(stream);
        }
        catch
        {
            return false;
        }
    }

    public async Task<List<PngChunk>> ReadDataAsync(string filePath, Action<string>? callback)
    {
        var chunks = new List<PngChunk>();
        await using var stream = File.Open(filePath, FileMode.Open);
        stream.Seek(SignatureLength, SeekOrigin.Begin);
        
        var chunk = default(PngChunk);
        do
        {
            try
            {
                chunk = await PngChunk.CreateAsync(stream);
                callback?.Invoke(chunk.FormatData());
                
                chunks.Add(chunk);
            }
            catch (Exception e)
            {
                callback?.Invoke($"Error while parsing chunk: {e.Message}");
            }
            
        } while (chunk is not IENDChunk);

        return chunks;
    }
    
    private async Task<bool> CheckSignatureAsync(Stream stream)
    {
        var buffer = new byte[SignatureLength];
        var bytesRead = await stream.ReadAsync(buffer);

        return buffer.Take(bytesRead)
            .SequenceEqual(PngSignature);
    }
}
using System.IO;
using System.Text;
using Emedia_1_wpf.Models;
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

    public async Task<List<PngChunk>> ReadDataAsync(string filePath, Action<Log>? callback)
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
                callback?.Invoke(new Log(LogType.Info, chunk.FormatData()));
                
                chunks.Add(chunk);
            }
            catch (Exception e)
            {
                callback?.Invoke(new Log(LogType.Error, $"Error while parsing chunk: {e.Message}"));
            }
        } while (chunk is not IENDChunk);

        return chunks;
    }

    public async Task AnonymizeImageAsync(List<PngChunk> chunks, Stream destinationStream)
    {
        var chunksToSave = chunks.Where(x => !x.RemoveWhenAnonymizing)
            .ToArray();
        
        var data = chunksToSave.Where(x => x is IDATChunk)
            .SelectMany(x => x.Data)
            .ToArray();
        
        var crc = Crc32.Get([.."IDAT"u8, ..data]);
        var dataChunk = new IDATChunk((uint) data.Length, data, "IDAT", crc, true);

        if (chunksToSave is not [IHDRChunk header, .., IENDChunk end])
        {
            throw new InvalidOperationException();
        }
        
        await destinationStream.WriteAsync(PngSignature);
        await header.AppendToStreamAsync(destinationStream);
        if (chunksToSave.SingleOrDefault(x => x is PLTEChunk) is { } palette)
        {
            await palette.AppendToStreamAsync(destinationStream);
        }
        
        await dataChunk.AppendToStreamAsync(destinationStream);
        await end.AppendToStreamAsync(destinationStream);
    }
    
    private async Task<bool> CheckSignatureAsync(Stream stream)
    {
        var buffer = new byte[SignatureLength];
        var bytesRead = await stream.ReadAsync(buffer);

        return buffer.Take(bytesRead)
            .SequenceEqual(PngSignature);
    }
}
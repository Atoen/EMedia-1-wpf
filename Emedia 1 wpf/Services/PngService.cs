using System.IO;
using System.Text;
using Emedia_1_wpf.Models;
using Emedia_1_wpf.Services.Chunks;

namespace Emedia_1_wpf.Services;

public class PngService
{
    public static readonly byte[] PngSignature = [0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A];
    public static readonly byte[] DeflateSignature = [120, 94];
    public static int SignatureLength => PngSignature.Length;
    
    private readonly CryptoService _cryptoService = new();
    
    public async Task<List<PngChunk>> DecryptAsync(string filePath)
    {
        var chunks = await ReadDataAsync(filePath);
        var data = chunks.Where(x => x is IDATChunk)
            .SelectMany(x => x.Data)
            .Skip(2)
            .ToArray();
        
        var decompressed = await PngChunk.DecompressAsync(data);
        var decrypted = _cryptoService.Decrypt(decompressed);
        var compressed = await PngChunk.CompressAsync(decrypted);
        
        var otherChunks = chunks.Where(x => x is not IDATChunk)
            .ToList();
        
        otherChunks.Add(IDATChunk.FromBytes(compressed));
        (otherChunks[^1], otherChunks[^2]) = (otherChunks[^2], otherChunks[^1]);

        return otherChunks;
    }

    public async Task EncryptAsync(List<PngChunk> chunks, Stream destinationStream)
    {
        var data = chunks.Where(x => x is IDATChunk)
            .SelectMany(x => x.Data)
            .Skip(2)
            .ToArray();

        var decompressed = await PngChunk.DecompressAsync(data);
        var encrypted = _cryptoService.Encrypt(decompressed);
        var compressed = await PngChunk.CompressAsync(encrypted);
        
        var encryptedChunk = IDATChunk.FromBytes(compressed);

        var indexOfData = chunks.FindIndex(x => x is IDATChunk);
        var otherChunks = chunks.Where(x => x is not IDATChunk)
            .ToArray();

        await destinationStream.WriteAsync(PngSignature);
        for (var i = 0; i < otherChunks.Length; i++)
        {
            var chunk = otherChunks[i];
            if (i == indexOfData)
            {
                await encryptedChunk.AppendToStreamAsync(destinationStream);
            }

            await chunk.AppendToStreamAsync(destinationStream);
        }
    }
    
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

    public async Task<List<PngChunk>> ReadDataAsync(string filePath, Action<Log>? callback = null)
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

        var dataChunk = IDATChunk.FromBytes(data);
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
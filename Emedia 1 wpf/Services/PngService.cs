using System.IO;
using Emedia_1_wpf.Models;
using Emedia_1_wpf.Services.Chunks;

namespace Emedia_1_wpf.Services;

public class PngService
{
    public static readonly byte[] PngSignature = [0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A];
    public static readonly byte[] DeflateSignature = [0x78, 0x5E];
    public static int SignatureLength => PngSignature.Length;
    
    private readonly CryptoService _cryptoService = new();
    
    public async Task DecryptAsync(List<PngChunk> chunks, string filePath, bool useLibrary, IProgress<double>? 
            progress = null)
    {
        await using var stream = File.Create(filePath);
        var data = chunks.Where(x => x is IDATChunk)
            .SelectMany(x => x.Data)
            .Skip(0)
            .ToArray();
        
        var decompressed = await PngChunk.DecompressAsync(data);
        var decrypted = await _cryptoService.DecryptAsync(decompressed, useLibrary, progress);
        var compressed = await PngChunk.CompressAsync(decrypted);
        
        var otherChunks = chunks.Where(x => x is not IDATChunk)
            .ToList();
        
        otherChunks.Add(IDATChunk.FromBytes(compressed));
        (otherChunks[^1], otherChunks[^2]) = (otherChunks[^2], otherChunks[^1]);

        await stream.WriteAsync(PngSignature);
        foreach (var chunk in otherChunks)
        {
            await chunk.AppendToStreamAsync(stream);
        }
    }

    public async Task EncryptAsync(List<PngChunk> chunks, string filePath, bool useLibrary, IProgress<double>? 
        progress = null)
    {
        await using var stream = File.Create(filePath);
        var data = chunks.Where(x => x is IDATChunk)
            .SelectMany(x => x.Data)
            .Skip(2)
            .ToArray();

        var decompressed = await PngChunk.DecompressAsync(data);
        var encrypted = await _cryptoService.EncryptAsync(decompressed, useLibrary, progress);
        var compressed = await PngChunk.CompressAsync(encrypted);
        
        var encryptedChunk = IDATChunk.FromBytes(compressed);

        var indexOfData = chunks.FindIndex(x => x is IDATChunk);
        var otherChunks = chunks.Where(x => x is not IDATChunk)
            .ToArray();

        await stream.WriteAsync(PngSignature);
        for (var i = 0; i < otherChunks.Length; i++)
        {
            var chunk = otherChunks[i];
            if (i == indexOfData)
            {
                await encryptedChunk.AppendToStreamAsync(stream);
            }

            await chunk.AppendToStreamAsync(stream);
        }
    }
    
    public async ValueTask<bool> VerifyPngAsync(string filePath)
    {
        try
        {
            await using var stream = File.Open(filePath, FileMode.Open);
            return await CheckSignatureAsync(stream);
        }
        catch (Exception e)
        {
            Console.WriteLine(e);
            return false;
        }
    }

    public async Task<List<PngChunk>> ReadDataAsync(string filePath, Action<Log>? callback = null)
    {
        await using var stream = File.Open(filePath, FileMode.Open);
        stream.Seek(SignatureLength, SeekOrigin.Begin);
        
        var chunks = new List<PngChunk>();
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

    public async Task AnonymizeImageAsync(List<PngChunk> chunks, string filepath)
    {
        await using var stream = File.Create(filepath);
        
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
        
        await stream.WriteAsync(PngSignature);
        await header.AppendToStreamAsync(stream);
        if (chunksToSave.SingleOrDefault(x => x is PLTEChunk) is { } palette)
        {
            await palette.AppendToStreamAsync(stream);
        }
        
        await dataChunk.AppendToStreamAsync(stream);
        await end.AppendToStreamAsync(stream);
    }
    
    private async ValueTask<bool> CheckSignatureAsync(Stream stream)
    {
        var buffer = new byte[SignatureLength];
        var bytesRead = await stream.ReadAsync(buffer);

        return buffer.Take(bytesRead)
            .SequenceEqual(PngSignature);
    }
}
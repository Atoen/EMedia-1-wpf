using System.IO;
using System.Numerics;
using System.Security.Cryptography;
using Emedia_1_wpf.Services.RSA;

namespace Emedia_1_wpf.Services;

public class CryptoService
{
    private readonly RSAParameters _privateKey;
    private readonly RSAParameters _publicKey;

    public const int KeySize = 2048;
    public const int EncryptChunkSize = KeySize / 8 - 42;
    public const int DecryptChunkSize = KeySize / 8;

    private readonly CustomRSA _customRSA;
    
    public CryptoService()
    {
        var privateKey64 = File.ReadAllText("private.pem");
        
        var rsa = new RSACryptoServiceProvider(KeySize);
        rsa.ImportFromPem(privateKey64);

        _privateKey = rsa.ExportParameters(true);
        _publicKey = rsa.ExportParameters(false);
        // _customRSA = new CustomRSA();
    }

    private const double UpdateThreshold = 0.005;

    public async Task<IEnumerable<byte>> EncryptAsync(
        IEnumerable<byte> data,
        CryptographyMode cryptographyMode,
        IProgress<double>? progress = null)
    {
        return await Task.Run(() => EncryptLibrary(data, progress));

        // return await Task.Run(() => useLibrary
        //     ? EncryptLibrary(data, progress)
        //     : EncryptECB(data).ToArray());
    }
    
    public async Task<byte[]> DecryptAsync(
        IEnumerable<byte> encryptedData,
        CryptographyMode cryptographyMode,
        IProgress<double>? progress = null)
    {
        return [];
        
        // return await Task.Run(() => useLibrary
        //     ? DecryptLibrary(encryptedData, progress)
        //     : Decrypt(encryptedData, progress));
    }

    private byte[] EncryptLibrary(IEnumerable<byte> data, IProgress<double>? progress = null)
    {
        using var csp = new RSACryptoServiceProvider();
        csp.ImportParameters(_publicKey);

        var chunks = data.Chunk(EncryptChunkSize).ToArray();
        var step = 1.0 / chunks.Length;
        var lastReportedProgress = 0.0;
        
        return chunks
            .Select((x, i) =>
            {
                var encrypted = csp.Encrypt(x, RSAEncryptionPadding.OaepSHA1);
                
                var totalProgress = (i + 1) * step;
                if (totalProgress - lastReportedProgress >= UpdateThreshold)
                {
                    lastReportedProgress = totalProgress;
                    progress?.Report(totalProgress);
                }
                
                return encrypted;
            })
            .SelectMany(x => x)
            .ToArray();
    }

    private byte[] DecryptLibrary(byte[] encryptedData, IProgress<double>? progress = null)
    {
        using var csp = new RSACryptoServiceProvider();
        csp.ImportParameters(_privateKey);
        
        var chunks = encryptedData.Chunk(DecryptChunkSize)
            .ToArray();
        
        var step = 1.0 / chunks.Length;
        var lastReportedProgress = 0.0;

        return chunks
            .Where(x => x.Length == DecryptChunkSize)
            .Select((x, i) =>
            {
                // var data = x.Length == DecryptChunkSize 
                //     ? x
                //     : [..Enumerable.Repeat<byte>(0, DecryptChunkSize - x.Length), ..x];

                var decrypted = csp.Decrypt(x, RSAEncryptionPadding.OaepSHA1);

                var totalProgress = (i + 1) * step;
                if (totalProgress - lastReportedProgress >= UpdateThreshold)
                {
                    lastReportedProgress = totalProgress;
                    progress?.Report(totalProgress);
                }

                return decrypted;
            })
            .SelectMany(x1 => x1)
            .ToArray();
    }
    
    private byte[] Encrypt(byte[] data, IProgress<double>? progress = null)
    {
        var n = new BigInteger(_publicKey.Modulus!);
        var e = new BigInteger(_publicKey.Exponent!);
        
        using var csp = new RSACryptoServiceProvider();
        csp.ImportParameters(_publicKey);
        
        var chunks = data.Chunk(EncryptChunkSize).ToArray();
        var step = 1.0 / chunks.Length;
        var lastReportedProgress = 0.0;
        
        return chunks
            .Select((x, i) =>
            {
                // var chunk = x.Length < EncryptChunkSize
                //     ? x.Concat(new byte[EncryptChunkSize - x.Length]).ToArray()
                //     : x;

                // Perform RSA encryption manually
                var m = new BigInteger(x, isBigEndian: true); // Convert chunk to BigInteger
                var c = BigInteger.ModPow(m, e, n); // c = m^e % n

                var encryptedChunk = c.ToByteArray(isUnsigned: false, isBigEndian: true); // Convert back to byte array and reverse

                // Ensure the encrypted chunk is the correct length (key size in bytes)
                if (encryptedChunk.Length < x.Length)
                {
                    var paddedChunk = new byte[x.Length];
                    Buffer.BlockCopy(encryptedChunk, 0, paddedChunk, x.Length - encryptedChunk.Length, encryptedChunk
                        .Length);
                    encryptedChunk = paddedChunk;
                }

                var totalProgress = (i + 1) * step;
                if (totalProgress - lastReportedProgress >= UpdateThreshold)
                {
                    lastReportedProgress = totalProgress;
                    progress?.Report(totalProgress);
                }

                return encryptedChunk;
            })
            .SelectMany(x => x)
            .ToArray();
    }
    
    private byte[] Decrypt(byte[] encryptedData, IProgress<double>? progress = null)
    {
        var n = new BigInteger(_privateKey.Modulus!);
        var d = new BigInteger(_privateKey.D!);
        
        var chunks = encryptedData.Chunk(256)
            .Where(x => x.Length == 256)
            .ToArray();
        
        var step = 1.0 / chunks.Length;
        var lastReportedProgress = 0.0;
        
        return chunks
            .Select((x, i) =>
            {
                var c = new BigInteger(x);
                var decrypted = BigInteger.ModPow(c, d, n);
                
                var totalProgress = (i + 1) * step;
                if (totalProgress - lastReportedProgress >= UpdateThreshold)
                {
                    lastReportedProgress = totalProgress;
                    progress?.Report(totalProgress);
                }
                
                return decrypted.ToByteArray();
            })
            .SelectMany(x => x)
            .ToArray();
    }

    private byte[] EncryptECB(byte[] data, IProgress<double>? progress = null)
    {
        var n = new BigInteger(_publicKey.Modulus!);
        var e = new BigInteger(_publicKey.Exponent!);
        
        var chunks = data.Chunk(EncryptChunkSize).ToArray();
        var step = 1.0 / chunks.Length;
        var lastReportedProgress = 0.0;

        return chunks.Select((x, i) =>
        {
            var m = new BigInteger(x, isBigEndian: true);
            var result = BigInteger.ModPow(m, e, n);

            var totalProgress = (i + 1) * step;
            if (totalProgress - lastReportedProgress >= UpdateThreshold)
            {
                lastReportedProgress = totalProgress;
                progress?.Report(totalProgress);
            }

            return result.ToByteArray();
        })
        .SelectMany(x => x)
        .ToArray();
    }
    
    // private readonly RSAParameters _publicKey;
    private readonly int _keySize;
    private BigInteger _cbcVector;
    
    public byte[] EncryptionCbc(byte[] dataToEncrypt, IProgress<double>? progress = null)
    {
        Console.WriteLine("Encrypting...");
        var encryptedData = new List<byte>();
        var step = _keySize / 8 - 1; // Step size in bytes

        // Generate a random CBC vector
        using (var rng = new RNGCryptoServiceProvider())
        {
            var vectorBytes = new byte[_keySize / 8];
            rng.GetBytes(vectorBytes);
            _cbcVector = new BigInteger(vectorBytes.Concat(new byte[] { 0 }).ToArray());
        }

        BigInteger prevXor = _cbcVector;
        
        var progressStep = dataToEncrypt.Length / step;
        var lastReportedProgress = 0.0;

        for (var i = 0; i < dataToEncrypt.Length; i += step)
        {
            var rawBytes = dataToEncrypt.Skip(i).Take(step).ToArray();
            var inputLength = rawBytes.Length;
            var intFromBytes = new BigInteger(rawBytes.Concat(new byte[] { 0 }).ToArray());

            if (intFromBytes >= new BigInteger(_publicKey.Modulus))
                throw new ArgumentException("M is bigger than n");

            var prevXorBytes = prevXor.ToByteArray().Reverse().ToArray();
            if (prevXorBytes.Length > inputLength)
            {
                prevXorBytes = prevXorBytes.Take(inputLength).ToArray();
            }

            var xoredInt = intFromBytes ^ new BigInteger(prevXorBytes.Concat(new byte[] { 0 }).ToArray());
            var encryptedInt = BigInteger.ModPow(xoredInt, new BigInteger(_publicKey.Exponent), new BigInteger(_publicKey.Modulus));

            prevXor = encryptedInt;

            var encryptedBytes = encryptedInt.ToByteArray().Reverse().ToArray();
            var encryptedLength = encryptedBytes.Length;

            for (var j = 0; j < inputLength; j++)
            {
                if (j < inputLength - 1)
                {
                    encryptedData.Add(encryptedBytes[j]);
                }
                else
                {
                    encryptedData.Add(encryptedBytes.Skip(j).FirstOrDefault());
                }
            }
            
            var totalProgress = (i + 1) * step;
            if (totalProgress - lastReportedProgress >= UpdateThreshold)
            {
                lastReportedProgress = totalProgress;
                progress?.Report(totalProgress);
            }
        }

        return encryptedData.ToArray();
    }
}
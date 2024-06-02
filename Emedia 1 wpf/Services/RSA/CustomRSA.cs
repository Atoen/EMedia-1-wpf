using System.Numerics;
using Emedia_1_wpf.Extensions;

namespace Emedia_1_wpf.Services.RSA;

public class CustomRSA
{
    private readonly BigInteger _modulus;
    private readonly BigInteger _exponent;
    private readonly BigInteger _privateKey;

    private BigInteger _cbcVector;

    public const int KeySize = 2048;

    public CustomRSA(BigInteger m)
    {
        var keys = MathUtils.GenerateKeys(m, KeySize);

        _modulus = keys.modulus;
        _exponent = keys.exponent;
        _privateKey = keys.privateKey;
    }
    
    public byte[] EncryptECB(IEnumerable<byte> data, IProgress<double>? progress = null)
    {
        const int step = KeySize / 8 - 1;
        var chunks = data.Chunk(step).ToArray();
        var progressStep = 1.0 / chunks.Length;
        
        return chunks
            .Select(progressStep, progress, x =>
            {
                var bigInt = new BigInteger(x, isUnsigned: true, isBigEndian: true);
                if (bigInt >= _modulus)
                {
                    throw new ArgumentException("M is bigger than n");
                }
                
                var encryptedBigInt = BigInteger.ModPow(bigInt, _exponent, _modulus);
                var encryptedBytes = encryptedBigInt.ToByteArray(isUnsigned: true, isBigEndian: true);
                
                if (encryptedBytes.Length < step)
                {
                    var paddedBytes = new byte[step];
                    Array.Copy(x, 0, paddedBytes, 0, encryptedBytes.Length);
                    encryptedBytes = paddedBytes;
                }

                return encryptedBytes.Take(step);
            })
            .SelectMany(x => x)
            .ToArray();
    }
    
    public byte[] DecryptECB(IEnumerable<byte> encryptedData, IProgress<double>? progress = null)
    {
        const int step = KeySize / 8 - 1;
        var chunks = encryptedData.Chunk(step).ToArray();
        var progressStep = 1.0 / chunks.Length;
        var progressValue = 0.0;
        var lastReportedValue = 0.0;
        
        return chunks
            .AsParallel()
            .AsOrdered()
            .Select(x =>
            {
                var bigInt = new BigInteger(x, isUnsigned: true, isBigEndian: true);
                var decryptedBigInt = BigInteger.ModPow(bigInt, _privateKey, _modulus);
                var decryptedBytes = decryptedBigInt.ToByteArray(isUnsigned: true, isBigEndian: true);
                
                if (decryptedBytes.Length < step)
                {
                    var paddedBytes = new byte[step];
                    Array.Copy(x, 0, paddedBytes, 0, decryptedBytes.Length);
                    decryptedBytes = paddedBytes;
                }
                
                if (progress is not null)
                {
                    lock (progress)
                    {
                        var currentProgress = progressValue += progressStep;
                        if (currentProgress - lastReportedValue >= 0.005)
                        {
                            lastReportedValue = currentProgress;
                            progress.Report(progressValue);
                        }
                    }
                }
                
                return decryptedBytes.Take(step);
            })
            .SelectMany(x => x)
            .ToArray();
    }
    
    public byte[] EncryptCBC(IEnumerable<byte> data, IProgress<double>? progress = null)
    {
        var step = KeySize / 8 - 11;
        var chunks = data.Chunk(step).ToArray();
        var progressStep = 1.0 / chunks.Length;
        
        _cbcVector = MathUtils.RandomIntegerBelow(_modulus);
        var previousBlock = _cbcVector;

        return chunks.Select(progressStep, progress, x =>
            {
                var bigInt = new BigInteger(x, isUnsigned: true, isBigEndian: true);
                if (bigInt >= _modulus)
                {
                    throw new ArgumentException("M is bigger than n");
                }

                bigInt ^= previousBlock;

                var encryptedBigInt = BigInteger.ModPow(bigInt, _exponent, _modulus);
                previousBlock = encryptedBigInt;

                var encryptedBytes = encryptedBigInt.ToByteArray(isUnsigned: true, isBigEndian: true);

                if (encryptedBytes.Length < step)
                {
                    var paddedBytes = new byte[step];
                    Array.Copy(encryptedBytes, 0, paddedBytes, step - encryptedBytes.Length, encryptedBytes.Length);
                    encryptedBytes = paddedBytes;
                }

                return encryptedBytes.Take(step);
            })
            .SelectMany(x => x)
            .ToArray();
    }
    
    public byte[] DecryptCBC(IEnumerable<byte> encryptedData, IProgress<double>? progress = null)
    {
        var step = KeySize / 8 - 0;
        var chunks = encryptedData.Chunk(step).ToArray();
        var progressStep = 1.0 / chunks.Length;

        var previousBlock = _cbcVector;

        return chunks.Select(progressStep, progress, x =>
            {
                var bigInt = new BigInteger(x, isUnsigned: true, isBigEndian: true);
                var decryptedBigInt = BigInteger.ModPow(bigInt, _privateKey, _modulus);

                decryptedBigInt ^= previousBlock;
                previousBlock = bigInt;

                var decryptedBytes = decryptedBigInt.ToByteArray(isUnsigned: true, isBigEndian: true);

                if (decryptedBytes.Length < step)
                {
                    var paddedBytes = new byte[step];
                    Array.Copy(decryptedBytes, 0, paddedBytes, step - decryptedBytes.Length, decryptedBytes.Length);
                    decryptedBytes = paddedBytes;
                }

                return decryptedBytes.Take(step);
            })
            .SelectMany(x => x)
            .ToArray();
    }
}
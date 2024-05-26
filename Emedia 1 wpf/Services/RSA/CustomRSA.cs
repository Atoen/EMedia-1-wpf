using System.IO;
using System.Numerics;
using System.Security.Cryptography;

namespace Emedia_1_wpf.Services.RSA;

public class CustomRSA
{
    private readonly BigInteger[] _publicKey = new BigInteger[2];
    private BigInteger _privateKey;

    public const int KeySize = 2048;
    private const double UpdateThreshold = 0.005;

    public CustomRSA(BigInteger m)
    {
        // GenerateKeys(m);
        
        var privateKey64 = File.ReadAllText("private.pem");
        
        var rsa = new RSACryptoServiceProvider(KeySize);
        rsa.ImportFromPem(privateKey64);

        var parameters = rsa.ExportParameters(true);
        _publicKey[0] = new BigInteger(parameters.Modulus!);
        _publicKey[1] = new BigInteger(parameters.Exponent!);
        _privateKey = new BigInteger(parameters.D!);
    }

    private void GenerateKeys(BigInteger m)
    {
        var n = BigInteger.Zero;
        var p = BigInteger.Zero;
        var q = BigInteger.Zero;

        while (m > n)
        {
            (p, q) = MathUtils.GeneratePQ(KeySize);
            n = p * q;
        }

        _publicKey[0] = n;
        var phi = (p - 1) * (q - 1);

        BigInteger e;
        for (e = 2; e < phi; e++)
        {
            if (MathUtils.GreatestCommonDivisor(e, phi) == 1)
            {
                break;
            }
        }

        _publicKey[1] = e;

        var d = MathUtils.ModInverse(e, phi);
        _privateKey = d;
    }

    public IEnumerable<byte> EncryptECB(IEnumerable<byte> data, IProgress<double>? progress = null)
    {
        var step = KeySize / 8 - 1;
        var chunks = data.Chunk(step).ToArray();
        var progressStep = 1.0 / chunks.Length;
        var lastReportedProgress = 0.0;

        return chunks.Select((x, i) =>
            {
                var bigInt = new BigInteger(x, isBigEndian: true);
                if (bigInt >= _publicKey[0])
                {
                    throw new ArgumentException("M is bigger than n");
                }

                var encryptedBigInt = BigInteger.ModPow(bigInt, _publicKey[1], _publicKey[0]);
                var encryptedBytes = encryptedBigInt.ToByteArray(isBigEndian: true);

                if (encryptedBytes.Length < step)
                {
                    var paddedBytes = new byte[step];
                    Array.Copy(encryptedBytes, 0, paddedBytes, step - encryptedBytes.Length, encryptedBytes.Length);
                    encryptedBytes = paddedBytes;
                }

                var totalProgress = (i + 1) * progressStep;
                if (totalProgress - lastReportedProgress >= UpdateThreshold)
                {
                    lastReportedProgress = totalProgress;
                    progress?.Report(totalProgress);
                }

                return encryptedBytes.Take(step);
            })
            .SelectMany(x => x);
    }
    
    public IEnumerable<byte> EncryptCBC(IEnumerable<byte> data, IProgress<double>? progress = null)
    {
        var step = KeySize / 8 - 1;
        var chunks = data.Chunk(step).ToArray();
        var progressStep = 1.0 / chunks.Length;
        var lastReportedProgress = 0.0;
        
        var cbcVector = MathUtils.RandomIntegerBelow(_publicKey[0]);
        var previousBlock = cbcVector;

        return chunks.Select((x, i) =>
            {
                var bigInt = new BigInteger(x, isBigEndian: true);
                if (bigInt >= _publicKey[0])
                {
                    throw new ArgumentException("M is bigger than n");
                }

                // XOR with the previous block
                bigInt ^= previousBlock;

                var encryptedBigInt = BigInteger.ModPow(bigInt, _publicKey[1], _publicKey[0]);
                previousBlock = encryptedBigInt;

                var encryptedBytes = encryptedBigInt.ToByteArray(isBigEndian: true);

                if (encryptedBytes.Length < step)
                {
                    var paddedBytes = new byte[step];
                    Array.Copy(encryptedBytes, 0, paddedBytes, step - encryptedBytes.Length, encryptedBytes.Length);
                    encryptedBytes = paddedBytes;
                }

                var totalProgress = (i + 1) * progressStep;
                if (totalProgress - lastReportedProgress >= UpdateThreshold)
                {
                    lastReportedProgress = totalProgress;
                    progress?.Report(totalProgress);
                }

                return encryptedBytes.Take(step);
            })
            .SelectMany(x => x);
    }
}
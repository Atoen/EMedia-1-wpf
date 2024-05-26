using System.Numerics;

namespace Emedia_1_wpf.Services.RSA
{
    public static class MathUtils
    {
        public static BigInteger GreatestCommonDivisor(BigInteger a, BigInteger b) => BigInteger.GreatestCommonDivisor(a, b);

        public static (BigInteger g, BigInteger x, BigInteger y) ExtendedGCD(BigInteger a, BigInteger b)
        {
            if (a == 0)
            {
                return (b, 0, 1);
            }

            var (g, y, x) = ExtendedGCD(b % a, a);
            return (g, x - (b / a) * y, y);
        }

        public static BigInteger ModInverse(BigInteger a, BigInteger m)
        {
            var (g, x, y) = ExtendedGCD(a, m);
            if (g != 1)
            {
                throw new Exception("Modular inverse does not exist");
            }

            return (x % m + m) % m; // Ensure positive result
        }

        public static bool CheckPrimeNumber(BigInteger number)
        {
            if (number <= 1)
            {
                return false;
            }

            for (var i = new BigInteger(2); i * i <= number; i++)
            {
                if (number % i == 0)
                {
                    return false;
                }
            }

            return true;
        }

        public static bool IsPrime(BigInteger p, int n)
        {
            if (p < 2)
            {
                return false;
            }

            var d = p - 1;
            var s = 0;

            while (d % 2 == 0)
            {
                s++;
                d /= 2;
            }
            
            for (var i = 0; i < n; i++)
            {
                var a = RandomIntegerBelow(p - 1);
                var x = BigInteger.ModPow(a, d, p);
                if (x == 1 || x == p - 1)
                {
                    continue;
                }

                for (var j = 1; j < s; j++)
                {
                    x = BigInteger.ModPow(x, 2, p);
                    if (x == 1)
                    {
                        return false;
                    }

                    if (x == p - 1)
                    {
                        break;
                    }
                }

                if (x != p - 1)
                {
                    return false;
                }
            }

            return true;
        }
        
        public static BigInteger RandomIntegerBelow(BigInteger n)
        {
            var bytes = n.ToByteArray();
            BigInteger r;

            do {
                Random.Shared.NextBytes(bytes);
                bytes [^1] &= 0x7F;
                r = new BigInteger (bytes);
            } while (r >= n);

            return r;
        }

        public static BigInteger GeneratePrimeNumber(int bitSize)
        {
            Console.WriteLine("Generating prime number...");
            var tmpBytes = new byte[bitSize / 8];
            
            while (true)
            {
                Random.Shared.NextBytes(tmpBytes);
                tmpBytes[^1] &= 0x7F;
                var tmp = new BigInteger(tmpBytes);
                if (IsPrime(tmp, 10))
                {
                    return tmp;
                }
            }
        }

        public static (BigInteger p, BigInteger q) GeneratePQ(int keySize)
        {
            var p = GeneratePrimeNumber(keySize / 2);
            BigInteger q;
            do
            {
                q = GeneratePrimeNumber(keySize / 2);
            } while (p == q);

            return (p, q);
        }
    }
}

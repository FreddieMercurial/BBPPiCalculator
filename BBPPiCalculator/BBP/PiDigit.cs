using System.Text;

namespace BBP
{
    /// <summary>       
    /// Used to generate the [n]th digit of Pi in base16 (hex). This algorithm creates
    /// a hex 'slice' that is part of the hexidecimal stream if you were to represent 
    /// Pi as a hexidecimal number.
    /// 
    /// The formulaic code was borrowed from: 
    /// http://latkin.org/blog/2012/11/03/the-bailey-borwein-plouffe-algorithm-in-c-and-f/
    /// 
    /// And the original formula by the author above was borrowed from Bailey (of BBP): 
    /// http://www.experimentalmath.info/bbp-codes/piqpr8.c
    /// 
    /// It conforms to this mathematical formula: 
    /// http://upload.wikimedia.org/math/d/5/6/d56287522ec15b6d47c9402f24a9ba89.png
    /// 
    /// A description of the algorithm can be found in this very informative whitepaper (Bailey):
    /// http://crd-legacy.lbl.gov/~dhbailey/dhbpapers/bbp-alg.pdf
    /// 
    /// Modifications:
    ///     - Converted the functional logic to a standardized class structure.
    ///     - Added more in depth commentary
    ///     - Added parallelization for certain tasks
    ///     
    /// Verified the hexidecimal output by cross referencing this whitepaper:
    /// http://crd-legacy.lbl.gov/~dhbailey/dhbpapers/bbp-alg.pdf
    /// 
    /// Pi in Hexidecimal: 
    /// http://calccrypto.wikidot.com/math:pi-hex
    /// 
    /// Hex positions for debugging
    /// 0:          243F6A8885A308D313198A2
    /// 1,000,000:  6C65E52CB459350050E4BB1
    /// 
    /// </summary>

    public class PiDigit : IBBPCalculator
    {
        #region Vars
        private const int NumHexDigits = 16;
        private const double Epsilon = 1e-17;
        private const int NumTwoPowers = 100;
        private static readonly double[] TwoPowers = {
            1d,
            2d,
            4d,
            8d,
            16d,
            32d,
            64d,
            128d,
            256d,
            512d,
            1024d,
            2048d,
            4096d,
            8192d,
            16384d,
            32768d,
            65536d,
            131072d,
            262144d,
            524288d,
            1048576d,
            2097152d,
            4194304d,
            8388608d,
            16777216d,
            33554432d,
            67108864d,
            134217728d,
            268435456d,
            536870912d,
            1073741824d,
            2147483648d,
            4294967296d,
            8589934592d,
            17179869184d,
            34359738368d,
            68719476736d,
            137438953472d,
            274877906944d,
            549755813888d,
            1099511627776d,
            2199023255552d,
            4398046511104d,
            8796093022208d,
            17592186044416d,
            35184372088832d,
            70368744177664d,
            140737488355328d,
            281474976710656d,
            562949953421312d,
            1125899906842624d,
            2251799813685248d,
            4503599627370496d,
            9007199254740992d,
            18014398509481984d,
            36028797018963968d,
            72057594037927936d,
            144115188075855872d,
            288230376151711744d,
            576460752303423488d,
            1152921504606846976d,
            2305843009213693952d,
            4611686018427387904d,
            9223372036854775808d,
            18446744073709551616d,
            36893488147419103232d,
            73786976294838206464d,
            147573952589676412928d,
            295147905179352825856d,
            590295810358705651712d,
            1180591620717411303424d,
            2361183241434822606848d,
            4722366482869645213696d,
            9444732965739290427392d,
            18889465931478580854784d,
            37778931862957161709568d,
            75557863725914323419136d,
            151115727451828646838272d,
            302231454903657293676544d,
            604462909807314587353088d,
            1208925819614629174706176d,
            2417851639229258349412352d,
            4835703278458516698824704d,
            9671406556917033397649408d,
            19342813113834066795298816d,
            38685626227668133590597632d,
            77371252455336267181195264d,
            154742504910672534362390528d,
            309485009821345068724781056d,
            618970019642690137449562112d,
            1237940039285380274899124224d,
            2475880078570760549798248448d,
            4951760157141521099596496896d,
            9903520314283042199192993792d,
            19807040628566084398385987584d,
            39614081257132168796771975168d,
            79228162514264337593543950336d,
            158456325028528675187087900672d,
            316912650057057350374175801344d,
            633825300114114700748351602688d,
            1267650600228229401496703205376d
        };
        private long nOffset = 0;
        #endregion

        /// <summary>
        /// Constructor
        /// </summary>
        public PiDigit(long nOffset = 0)
        {
            if (nOffset < 0)
            {
                throw new ArgumentException(nameof(nOffset));
            }
            this.nOffset = nOffset;
        }

        public async IAsyncEnumerable<char> PiBytesAsync(long n = -1, int count = 10)
        {
            if (n == -1)
            {
                n = this.nOffset;
            } else
            {
                this.nOffset = n;
            }
            long offset = 0;
            int remaining = count;

            while (remaining > 0)
            {
                var result = Calculate(n + offset);
                offset += result.HexDigits.Length;
                for (int i = 0; i < result.HexDigits.Length; i++)
                {
                    this.nOffset = n + offset + i + 1;
                    yield return result.HexDigits[i];
                    if (--remaining == 0)
                    {
                        yield break;
                    }
                }
            }
        }

        public IEnumerable<char> PiBytes(long n = -1, int count = 10)
        {
            if (n == -1)
            {
                n = this.nOffset;
            } else
            {
                this.nOffset = n;
            }

            long offset = 0;
            int remaining = count;

            while (remaining > 0)
            {
                var result = PiDigit.Calculate(n + offset);
                offset += result.HexDigits.Length;
                for (int i = 0; i < result.HexDigits.Length; i++)
                {
                    this.nOffset = n + offset + i + 1;
                    yield return result.HexDigits[i];
                    if (--remaining == 0)
                    {
                        yield break;
                    }
                }
            }
        }

        /// <summary>
        /// Calculates the [n]th hexidecimal digit of Pi.
        /// </summary>
        /// <param name="n">The digit of Pi which you wish to solve for.</param>
        /// <returns>Returns ten hexidecimal values of Pi from the offset (n).</returns>
        public static BBPResult Calculate(long n)
        {
            if (n < 0)
            {
                throw new ArgumentException(nameof(n));
            }

            double pid, s1, s2, s3, s4;     // summations           
            string hexDigits;               // the hexidecimal digits

            // calc the summations
            s1 = Series(1, n);
            s2 = Series(4, n);
            s3 = Series(5, n);
            s4 = Series(6, n);

            pid = 4d * s1 - 2d * s2 - s3 - s4;          // transform the summations
            pid = pid - (long)pid + 1d;                  // create the fraction
            hexDigits = HexString(pid, NumHexDigits);   // convert the fraction to the hex digit slice      

            return new BBPResult() { Digit = n, HexDigits = hexDigits.Substring(0, 10) };
        }

        /// <summary>
        /// Calculates the [n]th hexidecimal digit of Pi.
        /// </summary>
        /// <param name="n">The digit of Pi which you wish to solve for.</param>
        /// <returns>Returns ten hexidecimal values of Pi from the offset (n).</returns>
        public BBPResult Calc(long n)
        {
            this.nOffset = n + 1;
            return PiDigit.Calculate(n: n);
        }

        public BBPResult Next() =>
            this.Calc(this.nOffset);

        /// <summary>
        /// Converts the fraction to a hexidecimal string. Multiply by 16
        /// and take the whole number on each round and then convert it to 
        /// it's hex equivalent.
        /// </summary>
        /// <param name="x">The fraction of the summations.</param>
        /// <param name="numDigits">Number of digits to render to hex.</param>
        /// <returns>Returns a hex string of length numDigits</returns>
        private static string HexString(double x, int numDigits)
        {
            string hexChars = "0123456789ABCDEF";
            StringBuilder sb = new StringBuilder(numDigits);
            double y = Math.Abs(x);
            for (int i = 0; i < numDigits; i++)
            {
                y = 16d * (y - Math.Floor(y));
                sb.Append(hexChars[(int)y]);
            }
            return sb.ToString();
        }

        /// <summary>
        /// This routine evaluates the series  sum_k 16^(n-k)/(8*k+m)
        /// using the modular exponentiation technique.
        /// </summary>
        /// <param name="m"></param>
        /// <param name="n"></param>
        /// <returns></returns>
        private static double Series(long m, long n)
        {
            double denom, pow, sum = 0d, term;

            // Sum the series up to n.              
            for (long k = 0; k < n; k++)
            {
                denom = 8 * k + m;
                pow = n - k;
                term = ModPow16(pow, denom);
                sum = sum + term / denom;
                sum = sum - (long)sum;
            }

            //  Compute a few terms where k &gt;= n.
            for (long k = n; k <= n + 100; k++)
            {
                denom = 8 * k + m;
                term = Math.Pow(16d, (double)(n - k)) / denom;
                if (term < Epsilon)
                {
                    break;
                }
                sum = sum + term;
                sum = sum - (long)sum;
            }

            return sum;
        }

        /// <summary>
        /// Internal helper method.
        /// </summary>
        /// <param name="p"></param>
        /// <param name="m"></param>
        /// <returns></returns>
        private static double ModPow16(double p, double m)
        {
            long i;
            double pow1, pow2, result;
            if (m == 1d)
            {
                return 0d;
            }

            // Find the greatest power of two less than or equal to p.
            for (i = 0; i < NumTwoPowers; i++)
            {
                if (TwoPowers[i] > p)
                {
                    break;
                }
            }
            pow2 = TwoPowers[i - 1];
            pow1 = p;
            result = 1d;

            // Perform binary exponentiation algorithm modulo m.                 
            for (long j = 1; j <= i; j++)
            {
                if (pow1 >= pow2)
                {
                    result = 16d * result;
                    result = result - (long)(result / m) * m;
                    pow1 = pow1 - pow2;
                }
                pow2 = 0.5 * pow2;
                if (pow2 >= 1d)
                {
                    result = result * result;
                    result = result - (long)(result / m) * m;
                }
            }

            return result;
        }
    }
}

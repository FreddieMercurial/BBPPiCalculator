// ReSharper disable MemberCanBePrivate.Global

namespace BBP;

/// <summary>
///     Used to generate the [n]th digit of Pi in base16 (hex). This algorithm creates
///     a hex 'slice' that is part of the hexadecimal stream if you were to represent
///     Pi as a hexadecimal number.
///     The formulaic code was borrowed from:
///     http://latkin.org/blog/2012/11/03/the-bailey-borwein-plouffe-algorithm-in-c-and-f/
///     And the original formula by the author above was borrowed from Bailey (of BBP):
///     http://www.experimentalmath.info/bbp-codes/piqpr8.c
///     It conforms to this mathematical formula:
///     http://upload.wikimedia.org/math/d/5/6/d56287522ec15b6d47c9402f24a9ba89.png
///     A description of the algorithm can be found in this very informative whitepaper (Bailey):
///     http://crd-legacy.lbl.gov/~dhbailey/dhbpapers/bbp-alg.pdf
///     Modifications:
///     - Converted the functional logic to a standardized class structure.
///     - Added more in depth commentary
///     - Added parallelization for certain tasks
///     Verified the hexadecimal output by cross referencing this whitepaper:
///     http://crd-legacy.lbl.gov/~dhbailey/dhbpapers/bbp-alg.pdf
///     Pi in Hexidecimal:
///     http://calccrypto.wikidot.com/math:pi-hex
///     Hex positions for debugging
///     0:          243F6A8885A308D313198A2
///     1,000,000:  6C65E52CB459350050E4BB1
/// </summary>
public static class BBPCalculator
{
    public static async IAsyncEnumerable<char> PiCharsAsync(long n, int count)
    {
        if (n < 0)
        {
            throw new ArgumentOutOfRangeException(paramName: nameof(n),
                message: "n must be greater than or equal to 0");
        }

        long offset = 0;
        var remaining = count;

        while (remaining > 0)
        {
            var piResult = await CalculateAsync(n: n + offset)
                .ConfigureAwait(continueOnCapturedContext: false);
            offset += piResult.HexDigits.Length;
            foreach (var t in piResult.HexDigits)
            {
                yield return t;
                if (--remaining == 0)
                {
                    yield break;
                }
            }
        }
    }

    public static IEnumerable<char> PiChars(long n, int count)
    {
        if (n < 0)
        {
            throw new ArgumentOutOfRangeException(paramName: nameof(n),
                message: "n must be greater than or equal to 0");
        }

        long offset = 0;
        var remaining = count;

        while (remaining > 0)
        {
            var piResult = Calculate(n: n + offset);
            offset += piResult.HexDigits.Length;
            foreach (var t in piResult.HexDigits)
            {
                yield return t;
                if (--remaining == 0)
                {
                    yield break;
                }
            }
        }
    }

    public static async IAsyncEnumerable<byte> PiBytesAsync(long n, int count)
    {
        long offset = 0;
        var remaining = count;

        while (remaining > 0)
        {
            var piResult = await CalculateAsync(n: n + offset)
                .ConfigureAwait(continueOnCapturedContext: false);
            offset += piResult.HexDigits.Length;
            for (var i = 0; i < piResult.HexDigits.Length; i += 2)
            {
                var hexSubstring = piResult.HexDigits.Substring(
                    startIndex: i,
                    length: 2);
                var byteResult = Convert.ToByte(
                    value: hexSubstring,
                    fromBase: 16);
                yield return byteResult;
                if (--remaining == 0)
                {
                    yield break;
                }
            }
        }
    }

    public static IEnumerable<byte> PiBytes(long n, int count)
    {
        long offset = 0;
        var remaining = count;

        while (remaining > 0)
        {
            var piResult = Calculate(n: n + offset);
            offset += piResult.HexDigits.Length;
            for (var i = 0; i < piResult.HexDigits.Length; i += 2)
            {
                var hexSubstring = piResult.HexDigits.Substring(
                    startIndex: i,
                    length: 2);
                var byteResult = Convert.ToByte(
                    value: hexSubstring,
                    fromBase: 16);
                yield return byteResult;
                if (--remaining == 0)
                {
                    yield break;
                }
            }
        }
    }


    public static BBPResult Calculate(long n)
    {
        if (n < 0)
        {
            throw new ArgumentException(message: null,
                paramName: nameof(n));
        }

        // calc the summations
        var s1 = Series(m: 1,
            n: n);
        var s2 = Series(m: 4,
            n: n);
        var s3 = Series(m: 5,
            n: n);
        var s4 = Series(m: 6,
            n: n);

        var pid = (4d * s1) - (2d * s2) - s3 - s4;
        pid = pid - (long)pid + 1d; // create the fraction
        var hexDigits = HexString(x: pid,
            numDigits: NumHexDigits);

        return new BBPResult(Digit: n,
            HexDigits: hexDigits[..NativeChunkSizeInChars]);
    }

    /// <summary>
    ///     Calculates the [n]th hexadecimal digit of Pi.
    /// </summary>
    /// <param name="n">The digit of Pi which you wish to solve for.</param>
    /// <returns>Returns ten hexadecimal values of Pi from the offset (n).</returns>
    public static async Task<BBPResult> CalculateAsync(long n)
    {
        return await Task
            // ReSharper disable once HeapView.DelegateAllocation
            .Run(function: () => Calculate(n: n))
            .ConfigureAwait(continueOnCapturedContext: false);
    }

    /// <summary>
    ///     Converts the fraction to a hexadecimal string. Multiply by 16
    ///     and take the whole number on each round and then convert it to
    ///     it's hex equivalent.
    /// </summary>
    /// <param name="x">The fraction of the summations.</param>
    /// <param name="numDigits">Number of digits to render to hex.</param>
    /// <returns>Returns a hex string of length numDigits</returns>
    private static string HexString(double x, int numDigits)
    {
        var hexChars = "0123456789ABCDEF";
        var piChars = new char[numDigits];
        var y = Math.Abs(value: x);
        for (var i = 0; i < numDigits; i++)
        {
            y = 16d * (y - Math.Floor(d: y));
            piChars[i] = hexChars[index: (int)y];
        }

        return new string(value: piChars);
    }

    /// <summary>
    ///     This routine evaluates the series  sum_k 16^(n-k)/(8*k+m)
    ///     using the modular exponentiation technique.
    /// </summary>
    /// <param name="m"></param>
    /// <param name="n"></param>
    /// <returns></returns>
    private static double Series(long m, long n)
    {
        double denominator;
        var sum = 0d;
        double term;

        // Sum the series up to n.              
        for (long k = 0; k < n; k++)
        {
            denominator = (8 * k) + m;
            double power = n - k;
            term = ModPow16(p: power,
                m: denominator);
            sum += term / denominator;
            sum -= (long)sum;
        }

        //  Compute a few terms where k &gt;= n.
        for (var k = n; k <= n + 100; k++)
        {
            denominator = (8 * k) + m;
            term = Math.Pow(x: 16d,
                y: n - k) / denominator;
            if (term < Epsilon)
            {
                break;
            }

            sum += term;
            sum -= (long)sum;
        }

        return sum;
    }

    /// <summary>
    ///     Internal helper method.
    /// </summary>
    /// <param name="p"></param>
    /// <param name="m"></param>
    /// <returns></returns>
    private static double ModPow16(double p, double m)
    {
        long i;
        const double tolerance = 0.00001d;

        if (Math.Abs(value: m - 1d) < tolerance)
        {
            return 0d;
        }

        // Find the greatest power of two less than or equal to p.
        for (i = 0; i < _powersOfTwo.Length; i++)
        {
            if (_powersOfTwo[i] > p)
            {
                break;
            }
        }

        var pow2 = _powersOfTwo[i - 1];
        var pow1 = p;
        var result = 1d;

        // Perform binary exponentiation algorithm modulo m.                 
        for (long j = 1; j <= i; j++)
        {
            if (pow1 >= pow2)
            {
                result = 16d * result;
                result -= (long)(result / m) * m;
                pow1 -= pow2;
            }

            pow2 = 0.5 * pow2;
            if (!(pow2 >= 1d))
            {
                continue;
            }

            result *= result;
            result -= (long)(result / m) * m;
        }

        return result;
    }

    #region Vars

    public const int NumHexDigits = 16;
    public const int NativeChunkSizeInChars = 10; // out of 16
    private const double Epsilon = 1e-17;

    // ReSharper disable once HeapView.ObjectAllocation.Evident
    private static readonly double[] _powersOfTwo =
    {
        1d, 2d, 4d, 8d, 16d, 32d, 64d, 128d, 256d, 512d, 1024d, 2048d, 4096d, 8192d, 16384d, 32768d, 65536d, 131072d, 262144d, 524288d,
        1048576d, 2097152d, 4194304d, 8388608d, 16777216d, 33554432d, 67108864d, 134217728d, 268435456d, 536870912d, 1073741824d,
        2147483648d, 4294967296d, 8589934592d, 17179869184d, 34359738368d, 68719476736d, 137438953472d, 274877906944d, 549755813888d,
        1099511627776d, 2199023255552d, 4398046511104d, 8796093022208d, 17592186044416d, 35184372088832d, 70368744177664d,
        140737488355328d, 281474976710656d, 562949953421312d, 1125899906842624d, 2251799813685248d, 4503599627370496d,
        9007199254740992d, 18014398509481984d, 36028797018963968d, 72057594037927936d, 144115188075855872d, 288230376151711744d,
        576460752303423488d, 1152921504606846976d, 2305843009213693952d, 4611686018427387904d, 9223372036854775808d,
        18446744073709551616d, 36893488147419103232d, 73786976294838206464d, 147573952589676412928d, 295147905179352825856d,
        590295810358705651712d, 1180591620717411303424d, 2361183241434822606848d, 4722366482869645213696d, 9444732965739290427392d,
        18889465931478580854784d, 37778931862957161709568d, 75557863725914323419136d, 151115727451828646838272d,
        302231454903657293676544d, 604462909807314587353088d, 1208925819614629174706176d, 2417851639229258349412352d,
        4835703278458516698824704d, 9671406556917033397649408d, 19342813113834066795298816d, 38685626227668133590597632d,
        77371252455336267181195264d, 154742504910672534362390528d, 309485009821345068724781056d, 618970019642690137449562112d,
        1237940039285380274899124224d, 2475880078570760549798248448d, 4951760157141521099596496896d, 9903520314283042199192993792d,
        19807040628566084398385987584d, 39614081257132168796771975168d, 79228162514264337593543950336d, 158456325028528675187087900672d,
        316912650057057350374175801344d, 633825300114114700748351602688d, 1267650600228229401496703205376d,
    };

    #endregion
}

namespace BBP
{
    public static class PiBlockMiner
    {
        private static Queue<char> MinedChars;
        private static PiDigit PiDigit = new PiDigit();

        public async static void MineNext(int count = 10)
        {
            var bytesAsync = PiDigit.PiBytesAsync(
                n: -1,
                count: count);
            await foreach (var c in bytesAsync)
            {
                MinedChars.Append(c);
            }
        }
    }
}

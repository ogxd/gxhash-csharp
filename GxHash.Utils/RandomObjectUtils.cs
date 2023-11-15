namespace GxHash.Utils;

public static class RandomObjectUtils
{
    private static readonly Random _Random = new();

    public static unsafe T CreateRandom<T>()
        where T : unmanaged
    {
        int size = sizeof(T);

        // Create object on the stack
        T val = default;

        // Get span to internal data
        byte* p = (byte*)&val;
        var bytes = new Span<byte>(p, size);

        // Fill with random bytes
        _Random.NextBytes(bytes);

        return val;
    }

    public static byte[] CreateRandomBytes(int minSize, int maxSize)
    {
        byte[] bytes = new byte[_Random.Next(minSize, maxSize)];
        _Random.NextBytes(bytes);
        return bytes;
    }

    public static string CreateRandomString(int minSize = 4, int maxSize = 100, int seed = 0, string charSet = " abcdefghijklmnopqrstuvwxyzABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789")
    {
        Random random = new Random(seed);
        return new string(Enumerable
            .Repeat(charSet, random.Next(minSize, maxSize + 1))
            .Select(s => s[random.Next(s.Length)])
            .ToArray());
    }
}
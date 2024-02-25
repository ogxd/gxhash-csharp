namespace GxHash.Utils;

public static class QualificationUtils
{
    /// <summary>
    /// Returns distribution ratio
    /// Best case is 0 (perfectly evenly distributed)
    /// Worst case is 1 (the worst distribution)
    /// </summary>
    /// <typeparam name="T"></typeparam>
    /// <param name="input"></param>
    /// <returns></returns>
    public static unsafe double BitsDistribution<T>(this IEnumerable<T> input)
        where T : unmanaged
    {
        int size = sizeof(T);
        int count = 0;
        int[] bitBuckets = new int[size * 8]; // Not sponsored...

        foreach (T val in input)
        {
            count++;
            byte* p = (byte*)&val;
            for (int b = 0; b < size; b++)
            {
                for (int k = 0; k < 8; k++)
                {
                    bitBuckets[8 * b + k] += (p[b] >> k) & 1;
                }
            }
        }

        double[] bitBucketsFloating = bitBuckets.Select(x => 1d * x).ToArray();
        double std = StandardDeviation(bitBucketsFloating);
        double worstStd = 0.5d * count; // For normalizing std from 0 to 1
        
        return std / worstStd;
    }
    
    public delegate TResult HashFunction<out TResult>(ReadOnlySpan<byte> data)
        where TResult : unmanaged;

    /// <summary>
    /// Compute the avalanche effect of an hashing function.
    /// "The strict avalanche criterion (SAC) is a formalization of the avalanche effect.
    /// It is satisfied if, whenever a single input bit is complemented, each of the output bits changes with a 50% probability"
    /// https://en.wikipedia.org/wiki/Avalanche_effect
    /// Best case is 0 (means 50% probability for each bit to be changed on 1 bit changed)
    /// Worst case is 1
    /// </summary>
    /// <typeparam name="TResult"></typeparam>
    /// <param name="hashFunction"></param>
    /// <param name="inputSizeBytes"></param>
    /// <param name="iterations"></param>
    /// <returns></returns>
    public static unsafe double Avalanche<TResult>(this HashFunction<TResult> hashFunction, ReadOnlySpan<byte> input, int inputSizeBytes)
        where TResult : unmanaged
    {
        unchecked
        {
            int sizeR = sizeof(TResult);

            Span<byte> bytesBitChanged = stackalloc byte[inputSizeBytes];

            int iterations = input.Length / inputSizeBytes;

            double[] results = new double[iterations];

            for (int i = 0; i < iterations; i++)
            {
                var slice = input.Slice(i + inputSizeBytes, inputSizeBytes);

                UnsafeUtils.FlipRandomBit(slice, bytesBitChanged);

                TResult v1 = hashFunction(slice);
                TResult v2 = hashFunction(bytesBitChanged);

                // Analyze the difference in output hash
                byte* pv1 = (byte*)&v1;
                byte* pv2 = (byte*)&v2;
                int diffs = 0;
                for (int b = 0; b < sizeR; b++)
                {
                    int delta = pv1[b] ^ pv2[b];
                    for (int k = 0; k < 8; k++)
                    {
                        if ((delta & (1 << k)) != 0)
                        {
                            diffs++;
                        }
                    }
                }

                results[i] = 1d * diffs / (sizeR * 8);
            }

            return Math.Abs(1d - 2 * results.Average());
        }
    }

    /// <summary>
    /// Returns a collision ratio. 
    /// Best case is 0 (no collisions)
    /// Worst case is 1 (everything is colliding).
    /// </summary>
    /// <param name="input"></param>
    /// <returns></returns>
    public static double Uniqueness<T>(this IEnumerable<T> input)
    {
        int collisions = 0;
        int count = 0;
        HashSet<T> set = new HashSet<T>();
        foreach (T val in input)
        {
            count++;
            if (!set.Add(val))
            {
                collisions++;
            }
        }
        return 1d * collisions / (count - 1);
    }

    /// <summary>
    /// Returns the standard deviation of a given serie.
    /// If average is not given, it will be computed from the input.
    /// </summary>
    /// <param name="input"></param>
    /// <param name="average"></param>
    /// <returns></returns>
    public static double StandardDeviation(this IEnumerable<double> input, double? average = null)
    {
        double avg = average ?? input.Average();
        double sum = input.Sum(i => Math.Pow(i - avg, 2));
        return Math.Sqrt(sum / input.Count());
    }
}
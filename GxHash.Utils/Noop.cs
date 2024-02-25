using System.Runtime.CompilerServices;

namespace GxHash.Utils;

public static class No
{
    /// <summary>
    /// Prevents compiler optimizations such as code hoisting, with minimal overhead
    /// </summary>
    /// <param name="input"></param>
    /// <typeparam name="T"></typeparam>
    [MethodImpl(MethodImplOptions.NoInlining)]
    public static void NoOp<T>(this T input)
    {
        // Noop
    }
    
    /// <summary>
    /// Prevents compiler optimizations such as code hoisting, with minimal overhead
    /// </summary>
    /// <param name="input"></param>
    /// <typeparam name="T"></typeparam>
    /// <returns></returns>
    [MethodImpl(MethodImplOptions.NoInlining)]
    public static T NoOpReturn<T>(this T input)
    {
        return input;
    }
}
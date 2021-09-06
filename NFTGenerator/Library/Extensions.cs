using System;
using System.Collections.Generic;

namespace NFTGenerator
{
    internal static class Extensions
    {
        public static void PrintAll<T>(this IEnumerable<T> set)
        {
            foreach (T elem in set)
            {
                Console.WriteLine(elem.ToString());
            }
        }
    }
}
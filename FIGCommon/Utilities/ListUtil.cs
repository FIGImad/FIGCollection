using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.EntityFrameworkCore.Metadata.Internal;

namespace FIGCommon.Utilities
{
    public class ListUtil
    {

        // GetTopNRecords Key Features:
        // -------------------------------------------------------
        // * Efficiently retrieves top N records from a list
        // * Supports both sorted and unsorted lists
        // * Handles null and empty cases gracefully
        // * Prevents buffer overflows and ensures memory efficiency
        // * Optimized for IList<T> to avoid unnecessary allocations
        //
        // Example usage:
        // -------------------------------------------------------
        //
        //var numbers = new List<int> { 5, 3, 1, 4, 2 };
        //
        //// Unsorted top 3
        //var top3 = GetTopNRecords(numbers, 3); // [5, 3, 1]
        //
        //// Sorted top 3 (ascending)
        //var top3Sorted = GetTopNRecords(numbers, 3, Comparer<int>.Default); // [1, 2, 3]
        //
        //// Sorted top 3 (descending)
        //var top3Desc = GetTopNRecords(numbers, 3,
        //    Comparer<int>.Create((a, b) => b.CompareTo(a))); // [5, 4, 3]

        public static List<T> GetTopNRecords<T>(IList<T> source, int n, IComparer<T>? comparer = null)
        {
            if (source == null)
                throw new ArgumentNullException(nameof(source));

            if (n < 0)
                throw new ArgumentOutOfRangeException(nameof(n), "n must be non-negative");

            // Handle empty case or n=0
            if (n == 0 || source.Count == 0)
                return new List<T>();

            // No sorting needed - use optimized path
            if (comparer == null)
            {
                int count = Math.Min(n, source.Count);
                var result = new List<T>(count);

                for (int i = 0; i < count; i++)
                {
                    result.Add(source[i]);
                }
                return result;
            }

            // Sorting required - optimized for IList
            int elementsToTake = Math.Min(n, source.Count);
            var sorted = new List<T>(source.Count);
            sorted.AddRange(source);
            sorted.Sort(comparer);

            // Take top N from the sorted list
            var result2 = new List<T>(elementsToTake);
            for (int i = 0; i < elementsToTake; i++)
            {
                result2.Add(sorted[i]);
            }

            return result2;
        }
    }
}

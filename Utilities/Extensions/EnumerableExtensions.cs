using System.Collections.Generic;
using System.Linq;

namespace N17Solutions.Microphobia.Utilities.Extensions
{
    public static class EnumerableExtensions
    {
        /// <summary>
        /// Detects whether the Enumerable target is null or empty.
        /// </summary>
        /// <param name="target">The Enumerable to check.</param>
        /// <typeparam name="T">The Type of the Enumerable content.</typeparam>
        /// <returns>Boolean denoting if Null or Empty</returns>
        public static bool IsNullOrEmpty<T>(this IEnumerable<T> target)
        {
            return target == null || !target.Any();
        }

        /// <summary>
        /// Takes a collection and breaks it up into smaller chunks.
        /// </summary>
        /// <param name="collection">The collection to chunk.</param>
        /// <param name="chunkSize">The size to chunk into.</param>
        /// <typeparam name="T">The Type of the Enumerable content.</typeparam>
        /// <returns>A collection of collections.</returns>
        public static IEnumerable<IEnumerable<T>> Chunk<T>(this IEnumerable<T> collection, int chunkSize)
        {
            var result = collection.Select((value, index) => new {Index = index, Value = value})
                .GroupBy(x => x.Index / chunkSize)
                .Select(g => g.Select(x => x.Value));

            return result;
        }
    }
}
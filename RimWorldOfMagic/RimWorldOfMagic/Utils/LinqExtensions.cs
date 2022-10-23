using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TorannMagic.Utils
{
    public static class LinqExtensions
    {
        public static IEnumerable<TSource> DistinctBy<TSource, TKey>(
            this IEnumerable<TSource> source,
            Func<TSource, TKey> keySelector
        )
        // Efficiently get the first elements that are distinct by a property or group (Like injury.Part)
        {
            HashSet<TKey> seenKeys = new HashSet<TKey>();
            foreach (TSource element in source)
            {
                if (seenKeys.Contains(keySelector(element))) continue;

                seenKeys.Add(keySelector(element));
                yield return element;
            }
        }

        public static IEnumerable<TSource> DistinctBy<TSource, TKey>(
            this IEnumerable<TSource> source,
            Func<TSource, TKey> keySelector,
            int elementsPerGroup
        )
        // Efficiently get the first N elements that are distinct by a property or group (like injury.Part)
        {
            Dictionary<TKey, int> keyCounter = new Dictionary<TKey, int>();
            foreach (TSource element in source)
            {
                keyCounter.TryGetValue(keySelector(element), out int timesElementSeen);
                if (timesElementSeen >= elementsPerGroup) continue;

                keyCounter[keySelector(element)] = timesElementSeen + 1;
                yield return element;
            }
        }
    }
}

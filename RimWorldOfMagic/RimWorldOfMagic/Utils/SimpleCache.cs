using System;
using System.Collections.Generic;
using System.Linq;
using Verse;
using UnityEngine;

namespace TorannMagic.Utils
{
    public class SimpleCache<TKey, TValue>
    {
        private class CacheValue<T>
        {
            public readonly T Value;
            public readonly DateTime Timeout;
            public CacheValue(T value, double secondsToTimeout)
            {
                Value = value;
                Timeout = DateTime.UtcNow.AddSeconds(secondsToTimeout);
            }
        }

        private readonly Dictionary<TKey, CacheValue<TValue>> cache;
        private DateTime nextFlush;
        private readonly double minutesUntilFlush;

        public SimpleCache(double minutesUntilFlush)
        {
            cache = new Dictionary<TKey, CacheValue<TValue>>();
            nextFlush = DateTime.UtcNow.AddMinutes(minutesUntilFlush);
            this.minutesUntilFlush = minutesUntilFlush;
        }

        public TValue GetOrCreate(TKey key, Func<TValue> valueGetter, double secondsToTimeout)
        {
            CacheValue<TValue> cacheValue = cache.TryGetValue(key);
            if (cacheValue != null && cacheValue.Timeout > DateTime.UtcNow)
            {
                return cacheValue.Value;
            }

            if (DateTime.UtcNow > nextFlush)
            {
                DateTime now = DateTime.UtcNow;
                var itemsToRemove = cache.Where(kv => now > kv.Value.Timeout).ToArray();
                foreach (var item in itemsToRemove)
                {
                    cache.Remove(item.Key);
                }

                nextFlush = DateTime.UtcNow.AddMinutes(minutesUntilFlush);
            }
            cacheValue = new CacheValue<TValue>(valueGetter(), secondsToTimeout);
            cache[key] = cacheValue;
            return cacheValue.Value;
        }
    }
}

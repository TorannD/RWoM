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
            if (cache.ContainsKey(key) && cache[key].Timeout > DateTime.UtcNow)
            {
                return cache[key].Value;
            }

            if (DateTime.UtcNow > nextFlush)
            {
                DateTime now = DateTime.UtcNow;
                var itemsToRemove = cache.Where(kv => now > kv.Value.Timeout).ToList();
                foreach (var item in itemsToRemove)
                {
                    cache.Remove(item.Key);
                }

                nextFlush = DateTime.UtcNow.AddMinutes(minutesUntilFlush);
            }
            cache[key] = new CacheValue<TValue>(valueGetter(), secondsToTimeout);
            return cache[key].Value;
        }
    }
}

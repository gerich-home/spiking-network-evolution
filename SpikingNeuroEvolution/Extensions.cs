using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;

namespace SpikingNeuroEvolution
{
    static class Extensions
    {
        public static void Each<T>(this IEnumerable<T> enumerable, Action<T, int> action)
        {
            var index = 0;
            foreach (var item in enumerable)
            {
                action(item, index++);
            }
        }

        public static void Each<T>(this IEnumerable<T> enumerable, Action<T> action)
        {
            foreach (var item in enumerable)
            {
                action(item);
            }
        }

        public static IImmutableDictionary<TKey, TMappedValue> MapBy<TKey, TMappedValue>(this IEnumerable<TMappedValue> enumerable, Func<TMappedValue, TKey> mapKey) =>
            enumerable.ToImmutableDictionary(value => mapKey(value), value => value);

        public static IImmutableDictionary<TKey, TMappedValue> ToDictionary<TKey, TMappedValue>(this IEnumerable<TKey> enumerable, Func<TKey, TMappedValue> map) =>
            enumerable.ToImmutableDictionary(value => value, value => map(value));

        public static IImmutableDictionary<TKey, TMappedValue> MapValues<TKey, TValue, TMappedValue>(this IImmutableDictionary<TKey, TValue> dictionary, Func<TValue, TMappedValue> map) =>
            dictionary.ToImmutableDictionary(pair => pair.Key, pair => map(pair.Value));

        public static IEnumerable<T> RandomlyOrdered<T>(this IEnumerable<T> values, Random rnd)
        {
            return values
                .Select(value => new { value, order = rnd.Next() })
                .OrderBy(p => p.order)
                .Select(p => p.value);
        }

        public static IEnumerable<T> RandomlyRepeated<T>(this IEnumerable<T> values, Random rnd)
        {
            var list = values.ToImmutableList();

            while (true)
            {
                yield return list[rnd.Next(list.Count)];
            }
        }
    }
}

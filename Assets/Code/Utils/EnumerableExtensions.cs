using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public static class EnumerableExtensions
{
    public static IEnumerable<T> Cycle<T>(this IEnumerable<T> source)
    {
        if (source == null || !source.Any())
            yield break;

        while (true)
            foreach (var item in source)
                yield return item;
    }
}

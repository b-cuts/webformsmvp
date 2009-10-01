﻿namespace System.Collections.Generic
{
    internal static class CollectionExtensions
    {
        internal static void AddRange<T>(this IList<T> target, IEnumerable<T> collection)
        {
            foreach (var item in collection)
                target.Add(item);
        }
    }
}
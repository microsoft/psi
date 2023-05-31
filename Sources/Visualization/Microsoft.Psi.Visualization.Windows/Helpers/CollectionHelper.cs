// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.

namespace Microsoft.Psi.Visualization.Helpers
{
    using System;
    using System.Collections.Generic;

    /// <summary>
    /// Helper methods for collections.
    /// </summary>
    internal static class CollectionHelper
    {
        /// <summary>
        /// Inserts an item into a list in sorted order based on the supplied comparison method.
        /// </summary>
        /// <typeparam name="T">The type of the list items.</typeparam>
        /// <param name="list">The list in which to insert the item.</param>
        /// <param name="item">The item to insert.</param>
        /// <param name="comparison">An optional comparison method for determining where to insert the item.
        /// If null, the default comparer for the item type is used.</param>
        internal static void InsertSorted<T>(this IList<T> list, T item, Comparison<T> comparison = null)
        {
            if (list.Count == 0)
            {
                list.Add(item);
            }
            else
            {
                comparison ??= Comparer<T>.Default.Compare;

                int index = 0;
                while (index < list.Count && comparison(list[index], item) < 1)
                {
                    index++;
                }

                list.Insert(index, item);
            }
        }
    }
}

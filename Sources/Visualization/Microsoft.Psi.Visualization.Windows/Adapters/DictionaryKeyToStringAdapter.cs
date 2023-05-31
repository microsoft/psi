// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.

namespace Microsoft.Psi.Visualization.Adapters
{
    using System.Collections.Generic;
    using Microsoft.Psi.Visualization.Data;

    /// <summary>
    /// Implements a stream adapter from a generic dictionary to dictionary with string keys.
    /// </summary>
    /// <typeparam name="TKey">The type of the generic dictionary key.</typeparam>
    /// <typeparam name="TValue">The type of the generic dictionary values.</typeparam>
    /// <remarks>This adapter is used for series numerical plots, to support dictionaries with any key types.</remarks>
    public class DictionaryKeyToStringAdapter<TKey, TValue> : StreamAdapter<Dictionary<TKey, TValue>, Dictionary<string, TValue>>
    {
        /// <inheritdoc/>
        public override Dictionary<string, TValue> GetAdaptedValue(Dictionary<TKey, TValue> source, Envelope envelope)
        {
            if (source == null)
            {
                return null;
            }

            // As we convert the keys to string, make sure if we have string duplicates once we convert from
            // the original TKey space to string, we deduplicate them by adding _2, _3, etc.
            var result = new Dictionary<string, TValue>();
            var keyCount = new Dictionary<string, int>();

            foreach (var key in source.Keys)
            {
                var keyString = key.ToString();
                if (!keyCount.ContainsKey(keyString))
                {
                    keyCount.Add(keyString, 1);
                    result.Add(keyString, source[key]);
                }
                else
                {
                    keyCount[keyString]++;
                    result.Add($"{keyString}_{keyCount[keyString]}", source[key]);
                }
            }

            return result;
        }
    }
}

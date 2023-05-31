// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.

namespace Microsoft.Psi.Visualization.Adapters
{
    using System.Collections.Generic;
    using Microsoft.Psi.Visualization.Data;

    /// <summary>
    /// Implements a stream adapter from a generic dictionary to the value of the specified key string.
    /// </summary>
    /// <typeparam name="TKey">The type of the generic dictionary key.</typeparam>
    /// <typeparam name="TValue">The type of the generic dictionary values.</typeparam>
    /// <typeparam name="TDestination">The type of the destination data.</typeparam>
    public class DictionaryKeyToValueAdapter<TKey, TValue, TDestination> : StreamAdapter<Dictionary<TKey, TValue>, TDestination>
    {
        private readonly string key;

        /// <summary>
        /// Initializes a new instance of the <see cref="DictionaryKeyToValueAdapter{TKey, TValue, TDestination}"/> class.
        /// </summary>
        /// <param name="key">The key for the adapted value.</param>
        public DictionaryKeyToValueAdapter(string key)
            : base()
        {
            this.key = key;
        }

        /// <inheritdoc/>
        public override TDestination GetAdaptedValue(Dictionary<TKey, TValue> source, Envelope envelope)
        {
            if (source is not null)
            {
                foreach (var item in source)
                {
                    if (item.Key.ToString() == this.key)
                    {
                        // TDestination may be TValue or Nullable<TValue>. Cast to object, then to TDestination to handle either case.
                        return (TDestination)(object)item.Value;
                    }
                }
            }

            return default;
        }
    }
}

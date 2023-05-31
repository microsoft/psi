// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.

#pragma warning disable SA1649 // File name must match first type name

namespace Microsoft.Psi.Arrays
{
    using System.Collections.Generic;

    /// <summary>
    /// Common interface for multi-dimensional indexers.
    /// The interface contract is needed by NdArray, but might not be optimal from a user standpoint.
    /// </summary>
    public interface IIndexer
    {
        /// <summary>
        /// Takes a rectangular slice of the possible values of this indexer.
        /// </summary>
        /// <param name="ranges">The set of restrictions to apply to each dimension.</param>
        /// <returns>A rectangular slice of the current index space.</returns>
        Indexer Slice(params Range[] ranges);
    }

    /// <summary>
    /// Base class for multi-dimensional indexers.
    /// </summary>
    public abstract class Indexer
    {
        private int count;

        /// <summary>
        /// Initializes a new instance of the <see cref="Indexer"/> class.
        /// </summary>
        /// <param name="count">The count of distinct possible index values.</param>
        public Indexer(int count)
        {
            this.count = count;
        }

        /// <summary>
        /// Gets the count of distinct possible index values.
        /// </summary>
        public int Count => this.count;

        /// <summary>
        /// Gets the absolute index values.
        /// </summary>
        public abstract IEnumerable<int> Values { get; }

        /// <summary>
        /// Gets the absolute index values, expressed as contiguous ranges.
        /// </summary>
        public abstract IEnumerable<Range> Ranges { get; }
    }
}

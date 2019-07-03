// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.

namespace Microsoft.Psi.Arrays
{
    using System;
    using System.Collections.Generic;
    using System.Linq;

    /// <summary>
    /// Base class for a single-dimensional index definition.
    /// </summary>
    public abstract class IndexDefinition
    {
        private readonly int count;
        private readonly int elementStride;

        /// <summary>
        /// Initializes a new instance of the <see cref="IndexDefinition"/> class.
        /// </summary>
        /// <param name="count">The count of possible values for an index.</param>.
        /// <param name="elementStride">The spacing between consecutive index values.</param>
        public IndexDefinition(int count, int elementStride)
        {
            this.count = count;
            this.elementStride = elementStride;
        }

        /// <summary>
        /// Gets the count of possible values of the index.
        /// </summary>
        public int Count => this.count;

        /// <summary>
        /// Gets the spacing between consecutive index values. This value is needed when converting an index value to an absolute index value.
        /// </summary>
        public int ElementStride => this.elementStride;

        /// <summary>
        /// Gets the set of possible values an index of this type can take.
        /// These values need to be multiplied by <see cref="ElementStride"/> when computing absolute values.
        /// </summary>
        public abstract IEnumerable<int> Values { get; }

        /// <summary>
        /// Gets the set of possible values an index can take, expressed as ranges.
        /// These values need to be multiplied by <see cref="ElementStride"/> when computing absolute ranges.
        /// </summary>
        public abstract IEnumerable<Range> Ranges { get; }

        /// <summary>
        /// Gets the domain-relative value of the specified index value.
        /// Example: if the index definition consists of a set of values {128, 256, 1024}, then index[1] == 256.
        /// Note: the returned value needs to be multiplied by <see cref="ElementStride"/> to obtain an absolute value.
        /// </summary>
        /// <param name="index">The index value to use.</param>
        /// <returns>The domain-relative value.</returns>
        public abstract int this[int index] { get; }

        /// <summary>
        /// Takes a subset of the current index definition, expressed as a relative range within the [0, Count-1] range.
        /// </summary>
        /// <param name="subRange">The range of relative index values to take. Must be a subset of [0, Count-1].</param>
        /// <returns>An index definition for the specified range.</returns>
        public abstract IndexDefinition Slice(Range subRange);

        /// <summary>
        /// Takes a subset of the current index definition, expressed as a discrete set of relative values in [0, Count-1] range.
        /// </summary>
        /// <param name="values">The set of relative index values to take. The values must be in the [0, Count-1] range.</param>
        /// <returns>An index definition for the specified range.</returns>
        public abstract IndexDefinition Take(params int[] values);

        /// <summary>
        /// Takes a subset of the current index definition, expressed as a relative range within the [0, Count-1] range.
        /// </summary>
        /// <param name="start">The start of the range of relative index values to take. Must be a in [0, Count-1].</param>
        /// <param name="end">The end of the range of relative index values to take. Must be a in [0, Count-1].</param>
        /// <returns>An index definition for the specified range.</returns>
        public IndexDefinition Slice(int start, int end) => this.Slice(new Range(start, end));

        /// <summary>
        /// Merges two index definitions into one dicontinuous index. The two are assumed to belong to the same dimension.
        /// </summary>
        /// <param name="other">The other definition.</param>
        /// <returns>A combined definition.</returns>
        public virtual IndexDefinition Merge(IndexDefinition other)
        {
            if (this.ElementStride != other.ElementStride)
            {
                throw new InvalidOperationException("The index definitions are incompatible.");
            }

            return new DiscreteIndexDefinition(this.elementStride, this.Values.Concat(other.Values).ToArray());
        }

        /// <summary>
        /// Attempts to combine this index definition with a subdimension definition.
        /// This is an optimization for range indexes, see <see cref="RangeIndexDefinition.TryReduce(IndexDefinition, out IndexDefinition)"/>.
        /// </summary>
        /// <param name="subdimension">A subdimension of the current index.</param>
        /// <param name="combinedDefinition">The resulting combined definition, if any.</param>
        /// <returns>True if the two dimensions can be combined, false otherwise.</returns>
        internal virtual bool TryReduce(IndexDefinition subdimension, out IndexDefinition combinedDefinition)
        {
            combinedDefinition = null;
            return false;
        }
    }
}

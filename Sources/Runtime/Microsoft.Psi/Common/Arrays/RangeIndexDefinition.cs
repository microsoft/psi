// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.

namespace Microsoft.Psi.Arrays
{
    using System;
    using System.Collections.Generic;
    using System.Linq;

    /// <summary>
    /// Defines the possible values of an index as a continguous range.
    /// </summary>
    internal class RangeIndexDefinition : IndexDefinition
    {
        private readonly Range range;

        /// <summary>
        /// Initializes a new instance of the <see cref="RangeIndexDefinition"/> class.
        /// </summary>
        /// <param name="size">The size of the 0-based range.</param>
        /// <param name="elementStride">The spacing between consecutive cvalues of the index.</param>
        public RangeIndexDefinition(int size, int elementStride = 1)
            : this(new Range(0, size - 1), elementStride)
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="RangeIndexDefinition"/> class.
        /// </summary>
        /// <param name="range">The possible range of values.</param>
        /// <param name="elementStride">The spacing between consecutive cvalues of the index.</param>
        public RangeIndexDefinition(Range range, int elementStride = 1)
            : base(range.Size, elementStride)
        {
            this.range = range;
        }

        /// <summary>
        /// Gets the first valid value in the range.
        /// </summary>
        public int Start => this.range.Start;

        /// <summary>
        /// Gets the last valid value in the range.
        /// </summary>
        public int End => this.range.End;

        /// <summary>
        /// Gets the set of possible values an index of this type can take.
        /// These values need to be multiplied by <see cref="IndexDefinition.ElementStride"/> when computing absolute values.
        /// </summary>
        public override IEnumerable<int> Values => this.range.IsIncreasing ? Enumerable.Range(this.Start, this.Count) : Enumerable.Range(this.End, this.Count).Reverse();

        /// <summary>
        /// Gets the set of possible values an index can take, expressed as ranges.
        /// These values need to be multiplied by <see cref="IndexDefinition.ElementStride"/> when computing absolute ranges.
        /// </summary>
        public override IEnumerable<Range> Ranges => new[] { this.range };

        /// <summary>
        /// Gets the domain-relative value of the specified index value.
        /// Example: if the index definition is [100-200], then index[1] == 101.
        /// Note: the returned value needs to be multiplied by <see cref="IndexDefinition.ElementStride"/> to obtain an absolute value.
        /// </summary>
        /// <param name="index">The index value to use.</param>
        /// <returns>The domain-relative value.</returns>
        public override int this[int index]
        {
            get
            {
                if (index > this.Count)
                {
                    throw new IndexOutOfRangeException();
                }

                return this.Start + index;
            }
        }

        /// <summary>
        /// Takes a subset of the current index definition, expressed as a relative range within the [0, Count-1] range.
        /// </summary>
        /// <param name="subRange">The range of relative index values to take. Must be a subset of [0, Count-1].</param>
        /// <returns>An index definition for the specified range.</returns>
        public override IndexDefinition Slice(Range subRange)
        {
            if (subRange == Range.All)
            {
                return this;
            }

            if (subRange.Start >= this.Count || subRange.End >= this.Count)
            {
                throw new IndexOutOfRangeException();
            }

            int sign = this.range.IsIncreasing ? 1 : -1;
            var domainRange = new Range(this.range.Start + (sign * subRange.Start), this.range.Start + (sign * subRange.End));
            return new RangeIndexDefinition(domainRange,  this.ElementStride);
        }

        /// <summary>
        /// Takes a subset of the current index definition, expressed as a discrete set of relative values in [0, Count-1] range.
        /// </summary>
        /// <param name="values">The set of relative index values to take. The values must be in the [0, Count-1] range.</param>
        /// <returns>An index definition for the specified range.</returns>
        public override IndexDefinition Take(params int[] values)
        {
            int sign = this.range.IsIncreasing ? 1 : -1;
            var mappedValues = new int[values.Length];
            for (int i = 0; i < values.Length; i++)
            {
                if (values[i] >= this.range.Size)
                {
                    throw new IndexOutOfRangeException();
                }

                mappedValues[i] = (sign * values[i]) + this.range.Start;
            }

            return new DiscreteIndexDefinition(this.ElementStride, mappedValues);
        }

        /// <summary>
        /// Attempts to combine this index definition with a subdimension definition.
        /// The two index definitions can be combined as long as
        /// - the subdimension size is the same as the stride of this definition, and
        /// - the two definitions have the same direction.
        /// </summary>
        /// <param name="subdimension">A subdimension of the current index.</param>
        /// <param name="combinedDefinition">The resulting combined definition, if any.</param>
        /// <returns>True if the two dimensions can be combined, false otherwise.</returns>
        internal override bool TryReduce(IndexDefinition subdimension, out IndexDefinition combinedDefinition)
        {
            if (subdimension == null && this.ElementStride == 1)
            {
                combinedDefinition = this;
                return true;
            }

            var subd = subdimension as RangeIndexDefinition;
            if (subd != null && subd.Count == this.ElementStride && subd.range.IsIncreasing == this.range.IsIncreasing)
            {
                combinedDefinition = new RangeIndexDefinition(new Range(this.Start * this.ElementStride, (this.End * this.ElementStride) + subd.End), 1);
                return true;
            }

            combinedDefinition = null;
            return false;
        }
    }
}

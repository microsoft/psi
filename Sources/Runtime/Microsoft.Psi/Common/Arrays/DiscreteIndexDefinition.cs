// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.

namespace Microsoft.Psi.Arrays
{
    using System;
    using System.Collections.Generic;
    using System.Linq;

    /// <summary>
    /// Represents the set of discrete values an index can take.
    /// </summary>
    internal class DiscreteIndexDefinition : IndexDefinition
    {
        private int[] values;

        /// <summary>
        /// Initializes a new instance of the <see cref="DiscreteIndexDefinition"/> class.
        /// </summary>
        /// <param name="values">The set of discrete values an index of this kind can take.</param>
        public DiscreteIndexDefinition(params int[] values)
            : this(1, values)
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="DiscreteIndexDefinition"/> class.
        /// </summary>
        /// <param name="elementStride">The spacing between consecutive values of this index definition.</param>
        /// <param name="values">The set of discrete values an index of this kind can take.</param>
        public DiscreteIndexDefinition(int elementStride, params int[] values)
            : base(values.Length, elementStride)
        {
            this.values = values;
        }

        /// <summary>
        /// Gets the set of possible values an index of this type can take.
        /// These values need to be multiplied by <see cref="IndexDefinition.ElementStride"/> when computing absolute values.
        /// </summary>
        public override IEnumerable<int> Values => this.values;

        /// <summary>
        /// Gets he set of possible values an index can take, expressed as ranges.
        /// These values need to be multiplied by <see cref="IndexDefinition.ElementStride"/> when computing absolute ranges.
        /// </summary>
        public override IEnumerable<Range> Ranges => this.Values.Select(v => new Range(v, v));

        /// <summary>
        /// Gets the domain-relative value of the specified index value.
        /// Example: if the index definition consists of a set of values {128, 256, 1024}, then index[1] == 256.
        /// Note: the returned value needs to be multiplied by <see cref="IndexDefinition.ElementStride"/> to obtain an absolute value.
        /// </summary>
        /// <param name="index">The index value to use.</param>
        /// <returns>The domain-relative value.</returns>
        public override int this[int index] => this.values[index];

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

            var range = subRange.IsIncreasing ? subRange : new Range(subRange.End, subRange.Start);

            var v = this.values.Skip(range.Start).Take(range.Size);
            if (!subRange.IsIncreasing)
            {
                v = v.Reverse();
            }

            return new DiscreteIndexDefinition(this.ElementStride, v.ToArray());
        }

        /// <summary>
        /// Takes a subset of the current index definition, expressed as a discrete set of relative values in [0, Count-1] range.
        /// </summary>
        /// <param name="valuesToKeep">The set of relative index values to take. The values must be in the [0, Count-1] range.</param>
        /// <returns>An index definition for the specified range.</returns>
        public override IndexDefinition Take(params int[] valuesToKeep)
        {
            var mappedValues = new int[valuesToKeep.Length];
            for (int i = 0; i < this.values.Length; i++)
            {
                mappedValues[i] = this.values[valuesToKeep[i]];
            }

            return new DiscreteIndexDefinition(this.ElementStride, mappedValues);
        }

        /// <summary>
        /// Returns false. DiscreteIndexDefinition instances cannot reduce.
        /// </summary>
        /// <param name="subdimension">A subdimension of the current index.</param>
        /// <param name="combinedDefinition">Always null.</param>
        /// <returns>Always false.</returns>
        internal override bool TryReduce(IndexDefinition subdimension, out IndexDefinition combinedDefinition)
        {
            combinedDefinition = null;
            return false;
        }
    }
}

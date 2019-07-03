// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.

namespace Microsoft.Psi.Arrays
{
    using System;
    using System.Collections.Generic;

    /// <summary>
    /// Represents a 2d index domain.
    /// </summary>
    public class Indexer2d : Indexer, IIndexer
    {
        private readonly IndexDefinition rowIndexDef;
        private readonly IndexDefinition columnIndexDef;

        /// <summary>
        /// Initializes a new instance of the <see cref="Indexer2d"/> class.
        /// </summary>
        /// <param name="rows">The row definition (most significant dimension).</param>
        /// <param name="columns">The column definition (least significant dimension).</param>
        public Indexer2d(IndexDefinition rows, IndexDefinition columns)
            : base(rows.Count * columns.Count)
        {
            this.rowIndexDef = rows;
            this.columnIndexDef = columns;
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="Indexer2d"/> class.
        /// </summary>
        /// <param name="rows">The count of rows (most significant dimension).</param>
        /// <param name="columns">The count of columns (least significant dimension).</param>
        public Indexer2d(int rows, int columns)
            : this(rows, columns, columns)
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="Indexer2d"/> class.
        /// </summary>
        /// <param name="rows">The count of rows (most significant dimension).</param>
        /// <param name="columns">The count of columns (least significant dimension).</param>
        /// <param name="stride">The spacing between rows. Must be greater than the column count.</param>
        public Indexer2d(int rows, int columns, int stride)
            : base(rows * columns)
        {
            if (stride < columns)
            {
                throw new ArgumentException(nameof(stride));
            }

            this.rowIndexDef = new RangeIndexDefinition(rows, stride);
            this.columnIndexDef = new RangeIndexDefinition(columns);
        }

        /// <summary>
        /// Gets the set of contiguous ranges of absolute values in this index domain.
        /// </summary>
        public override IEnumerable<Range> Ranges
        {
            get
            {
                if (this.rowIndexDef.TryReduce(this.columnIndexDef, out IndexDefinition combined))
                {
                    return combined.Ranges;
                }

                return this.EnumerateRangesExplicit();
            }
        }

        /// <summary>
        /// Gets the absolute values over the index domain.
        /// </summary>
        public override IEnumerable<int> Values
        {
            get
            {
                foreach (var row in this.rowIndexDef.Values)
                {
                    foreach (var column in this.columnIndexDef.Values)
                    {
                        yield return (row * this.rowIndexDef.ElementStride) + column;
                    }
                }
            }
        }

        /// <summary>
        /// Returns the absolute value of the index given the row and column values.
        /// </summary>
        /// <param name="row">The row index.</param>
        /// <param name="column">The column index.</param>
        /// <returns>The absolute value, computed as row *stride + column.</returns>
        public int this[int row, int column]
        {
            get
            {
                return (this.rowIndexDef[row] * this.rowIndexDef.ElementStride) + this.columnIndexDef[column];
            }
        }

        /// <summary>
        /// Creates an indexer based on a rectangular slice of the index domain.
        /// </summary>
        /// <param name="rowRange">The set of rows to include.</param>
        /// <param name="columnRange">The set of columns to include.</param>
        /// <returns>A new indexer over the specified rectangular slice.</returns>
        public Indexer2d Slice(Range rowRange, Range columnRange)
        {
            return new Indexer2d(this.rowIndexDef.Slice(rowRange), this.columnIndexDef.Slice(columnRange));
        }

        /// <summary>
        /// Creates an indexer based on a rectangular slice of the index domain.
        /// </summary>
        /// <param name="ranges">The row and column ranges to include.</param>
        /// <returns>A new indexer over the specified rectangular slice.</returns>
        Indexer IIndexer.Slice(params Range[] ranges)
        {
            if (ranges.Length != 2)
            {
                throw new ArgumentException(nameof(ranges));
            }

            return this.Slice(ranges[0], ranges[1]);
        }

        private IEnumerable<Range> EnumerateRangesExplicit()
        {
            foreach (var row in this.rowIndexDef.Values)
            {
                var start = row * this.rowIndexDef.ElementStride;
                foreach (var columnRange in this.columnIndexDef.Ranges)
                {
                    yield return new Range(start + columnRange.Start, start + columnRange.End);
                }
            }
        }
    }
}

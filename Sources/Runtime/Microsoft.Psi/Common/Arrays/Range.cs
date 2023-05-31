// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.

namespace Microsoft.Psi.Arrays
{
    using System;
    using System.Diagnostics;

    /// <summary>
    /// Defines an inclusive range of int values.
    /// </summary>
    [DebuggerDisplay("[{start}-{end}]")]
    public struct Range
    {
        /// <summary>
        /// An empty range.
        /// </summary>
        public static Range Empty = new Range(0, -1);

        /// <summary>
        /// An all-inclusive range. Useful when slicing, to keep a dimension unchanged.
        /// </summary>
        public static Range All = new Range(int.MinValue, int.MaxValue);

        private int start;
        private int end;

        /// <summary>
        /// Initializes a new instance of the <see cref="Range"/> struct.
        /// </summary>
        /// <param name="start">The first value in the range.</param>
        /// <param name="end">The last value in the range.</param>
        public Range(int start, int end)
        {
            this.start = start;
            this.end = end;
        }

        /// <summary>
        /// Gets the first value in the range.
        /// </summary>
        public int Start => this.start;

        /// <summary>
        /// Gets the last value in the range.
        /// </summary>
        public int End => this.end;

        /// <summary>
        /// Gets a value indicating whether this range is in increasing (true) or decreasing (false) order.
        /// </summary>
        public bool IsIncreasing => this.end >= this.start;

        /// <summary>
        /// Gets a value indicating whether the range consists of a single value or not. Same as Size == 0;.
        /// </summary>
        public bool IsSingleValued => this.end == this.start;

        /// <summary>
        /// Gets the size of the range, computed as Math.Abs(end-start) + 1.
        /// </summary>
        public int Size => Math.Abs(this.end - this.start) + 1;

        /// <summary>
        /// Converts a tuple to a range.
        /// </summary>
        /// <param name="def">The tuple to convert to a range.</param>
        public static implicit operator Range((int start, int end) def)
        {
            return new Range(def.start, def.end);
        }

        /// <summary>
        /// Equality comparer. Returns true if the two ranges have the same start and end, false otherwise.
        /// </summary>
        /// <param name="first">The first value to compare.</param>
        /// <param name="second">The second value to compare.</param>
        /// <returns>True if the two ranges have the same start and end, false otherwise.</returns>
        public static bool operator ==(Range first, Range second)
        {
            return first.start == second.start && first.end == second.end;
        }

        /// <summary>
        /// Inequality comparer. Returns true if the two ranges have a different start and/or end, false otherwise.
        /// </summary>
        /// <param name="first">The first value to compare.</param>
        /// <param name="second">The second value to compare.</param>
        /// <returns>True if the two ranges have a different start and/or end, false otherwise.</returns>
        public static bool operator !=(Range first, Range second)
        {
            return first.start != second.start || first.end != second.end;
        }

        /// <summary>
        /// Equality comparer. Returns true if the current range have the same start and end as the specified range, false otherwise.
        /// </summary>
        /// <param name="obj">The value to compare to.</param>
        /// <returns>True if the two ranges have the same start and end, false otherwise.</returns>
        public override bool Equals(object obj)
        {
            if (obj is Range)
            {
                return this == (Range)obj;
            }

            return false;
        }

        /// <summary>
        /// Computes a hashcode based on start and end.
        /// </summary>
        /// <returns>A hash code for the range.</returns>
        public override int GetHashCode()
        {
            return ((((long)this.start) << 32) + this.end).GetHashCode();
        }

        /// <summary>
        /// Returns a string representation of the range.
        /// </summary>
        /// <returns>A string that represents the range.</returns>
        public override string ToString()
        {
            return $"[{this.start}-{this.end}]";
        }
    }
}

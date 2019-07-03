// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.

namespace Microsoft.Psi.Persistence
{
    using System;

    /// <summary>
    /// Structure describing a position in a data file.
    /// </summary>
    /// <remarks>
    /// This structure is used in two places: the index file and the large data file.
    /// To facilitate seeking, each data file is accompanied by an index file containing records of this type.
    /// Each record indicates the largest time and orginating time values seen up to the specified position.
    /// The position is a composite value, consisting of the extent and the relative position within the extent.
    /// These records allow seeking close to (but guaranteed before) a given time.
    /// Reading from the position provided by the index entry guarantees that all the messages with the
    /// time specified by the index entry will be read.
    ///
    /// To enable efficient reading of streams, the Store breaks streams in two categories: small and large.
    /// When writing large messages, an index entry is written into the main data file,
    /// pointing ot a location in the large data file where the actual message resides.
    /// </remarks>
    public struct IndexEntry
    {
        /// <summary>
        /// The largest time value seen up to the position specified by this entry.
        /// </summary>
        public DateTime Time;

        /// <summary>
        /// The largest originating time value seen up to the position specified by this entry.
        /// </summary>
        public DateTime OriginatingTime;

        /// <summary>
        /// The id of the extent this index entry refers to.
        /// A negative extentId indicates an entry in the large file.
        /// </summary>
        public int ExtentId;

        /// <summary>
        /// The position (in bytes) within the extent that this index entry points to.
        /// </summary>
        public int Position;
    }
}

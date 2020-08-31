// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.

namespace Microsoft.Psi.Data
{
    using System;

    /// <summary>
    /// Structure describing a position in a data file.
    /// </summary>
    /// <remarks>
    /// This structure is used in two places: the index file and the large data file.
    /// To facilitate seeking, each data file is accompanied by an index file containing records of this type.
    /// Each record indicates the largest time and originating time values seen up to the specified position.
    /// The position is a composite value, consisting of the extent and the relative position within the extent.
    /// These records allow seeking close to (but guaranteed before) a given time.
    /// Reading from the position provided by the index entry guarantees that all the messages with the
    /// time specified by the index entry will be read.
    ///
    /// To enable efficient reading of streams, the Store breaks streams in two categories: small and large.
    /// When writing large messages, an index entry is written into the main data file,
    /// pointing to a location in the large data file where the actual message resides.
    /// </remarks>
    public struct IndexEntry
    {
        /// <summary>
        /// The largest time value seen up to the position specified by this entry.
        /// </summary>
        public DateTime CreationTime;

        /// <summary>
        /// The largest originating time value seen up to the position specified by this entry.
        /// </summary>
        public DateTime OriginatingTime;

        /// <summary>
        /// The id of the extent this index entry refers to.
        /// A negative extentId indicates an entry in the large file for \psi stores.
        /// </summary>
        public int ExtentId;

        /// <summary>
        /// The position within the extent to which this index entry points.
        /// </summary>
        public int Position;

        /// <summary>
        /// Initializes a new instance of the <see cref="IndexEntry"/> struct.
        /// </summary>
        /// <param name="creationTIme">The largest creation time value seen up to the position specified by this entry.</param>
        /// <param name="originatingTime">The largest originating time value seen up to the position specified by this entry.</param>
        /// <param name="extentId">The id of the extent this index entry refers to.</param>
        /// <param name="position">The position within the extent to which this index entry points.</param>
        public IndexEntry(DateTime creationTIme, DateTime originatingTime, int extentId, int position)
        {
            this.CreationTime = creationTIme;
            this.OriginatingTime = originatingTime;
            this.ExtentId = extentId;
            this.Position = position;
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="IndexEntry"/> struct.
        /// </summary>
        /// <param name="envelope">Envelope from which to get Time and OriginatingTime.</param>
        /// <param name="extentId">The id of the extent this index entry refers to.</param>
        /// <param name="position">The position within the extent to which this index entry points.</param>
        public IndexEntry(Envelope envelope, int extentId, int position)
            : this(envelope.CreationTime, envelope.OriginatingTime, extentId, position)
        {
            this.OriginatingTime = envelope.OriginatingTime;
            this.CreationTime = envelope.CreationTime;
        }
    }
}

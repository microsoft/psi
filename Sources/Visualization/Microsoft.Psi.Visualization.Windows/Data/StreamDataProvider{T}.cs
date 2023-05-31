// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.

namespace Microsoft.Psi.Visualization.Data
{
    using System;
    using System.Collections.Generic;
    using System.Collections.ObjectModel;
    using System.Runtime.Serialization;
    using Microsoft.Psi.Data;

    /// <summary>
    /// Represents an object used to read streams.
    /// </summary>
    /// <typeparam name="T">NOTE: Due to the differing ways that the derived classes <see cref="StreamValueProvider{T}"/> and <see cref="StreamIntervalProvider{T}"/>
    /// deal with data coming from the store, the type T also differs. StreamValueProvider reads raw messages from the stream, then adapts it (if applicable), so T is
    /// the type of messages in the stream.  StreamIntervalProvider on the other hand uses its internal message cache that represents messages that have been
    /// read from the store and then adapted (if applicable), so in this case T represents the type of messages after they have passed through the data adapter.</typeparam>
    public abstract class StreamDataProvider<T> : IStreamDataProvider
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="StreamDataProvider{T}"/> class.
        /// </summary>
        /// <param name="streamSource">The stream source.</param>
        public StreamDataProvider(StreamSource streamSource)
        {
            this.Allocator = streamSource.Allocator != null ? () => streamSource.Allocator() : null;
            this.Deallocator = streamSource.Deallocator != null ? t => streamSource.Deallocator(t) : null;

            this.StreamName = streamSource.StreamName;

            this.ReadRequestsInternal = new List<ReadRequest>();
            this.ReadRequests = new ReadOnlyCollection<ReadRequest>(this.ReadRequestsInternal);
        }

        /// <inheritdoc/>
        public event EventHandler NoRemainingSubscribers;

        /// <summary>
        /// Event that fires when a stream is unable to be read from.
        /// </summary>
        public event EventHandler<StreamReadErrorEventArgs> StreamReadError;

        /// <summary>
        /// Gets a list of outstanding read requests.
        /// </summary>
        public IReadOnlyList<ReadRequest> ReadRequests { get; }

        /// <summary>
        /// Gets the stream name.
        /// </summary>
        public string StreamName { get; private set; }

        /// <inheritdoc/>
        public abstract bool HasSubscribers { get; }

        /// <summary>
        /// Gets the internal list of read requests.
        /// </summary>
        protected List<ReadRequest> ReadRequestsInternal { get; }

        /// <summary>
        /// Gets the allocator.
        /// </summary>
        protected Func<T> Allocator { get; }

        /// <summary>
        /// Gets the deallocator.
        /// </summary>
        protected Action<T> Deallocator { get; }

        /// <summary>
        /// Gets or sets a value indicating whether the stream data provider has been stopped.
        /// </summary>
        protected bool IsStopped { get; set; } = false;

        /// <summary>
        /// Stops the provider from publishing additional data.
        /// </summary>
        public void Stop()
        {
            this.IsStopped = true;
        }

        /// <inheritdoc />
        public void RemoveReadRequest(DateTime startTime, DateTime endTime)
        {
            lock (this.ReadRequestsInternal)
            {
                this.ReadRequestsInternal.RemoveAll(r => r.StartTime == startTime && r.EndTime == endTime);
            }
        }

        /// <inheritdoc />
        public abstract void DispatchData();

        /// <inheritdoc />
        public abstract void Dispose();

        /// <inheritdoc/>
        public abstract void OpenStream(IStreamReader streamReader);

        /// <inheritdoc/>
        public abstract DateTime? GetTimeOfNearestMessage(DateTime time, NearestType nearestType);

        /// <summary>
        /// Called by a derived class when it no longer has any subscribers.
        /// </summary>
        protected void OnNoRemainingSubscribers()
        {
            this.NoRemainingSubscribers?.Invoke(this, EventArgs.Empty);
        }

        /// <summary>
        /// Called when a read error arises.
        /// </summary>
        /// <param name="ex">The exception.</param>
        protected void OnReadError(SerializationException ex)
        {
            // Notify the data store reader
            this.StreamReadError?.Invoke(this, new StreamReadErrorEventArgs() { StreamName = this.StreamName, Exception = ex });
        }
    }
}

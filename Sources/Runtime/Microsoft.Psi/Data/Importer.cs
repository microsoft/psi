// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.

namespace Microsoft.Psi.Data
{
    using System;
    using System.Collections.Generic;
    using Microsoft.Psi;
    using Microsoft.Psi.Common;
    using Microsoft.Psi.Components;
    using Microsoft.Psi.Serialization;

    /// <summary>
    /// Component that reads messages via a specified <see cref="IStreamReader"/> and publishes them on streams.
    /// </summary>
    /// <remarks>
    /// Reads either at the full speed allowed by available resources or at the desired rate
    /// specified by the <see cref="Pipeline"/>. The store metadata is available immediately after open
    /// (before the pipeline is running) via the <see cref="AvailableStreams"/> property.
    /// </remarks>
    public class Importer : ISourceComponent, IDisposable
    {
        private readonly IStreamReader streamReader;
        private readonly Dictionary<string, object> streams = new Dictionary<string, object>();
        private readonly Pipeline pipeline;
        private readonly Receiver<bool> loopBack;
        private readonly Emitter<bool> next;

        private bool stopping;
        private long finalTicks = 0;
        private Action<DateTime> notifyCompletionTime;

        /// <summary>
        /// Initializes a new instance of the <see cref="Importer"/> class.
        /// </summary>
        /// <param name="pipeline">The pipeline to add the component to.</param>
        /// <param name="streamReader">Stream reader.</param>
        public Importer(Pipeline pipeline, IStreamReader streamReader)
        {
            this.streamReader = streamReader;
            this.pipeline = pipeline;
            this.Name = streamReader.Name;
            this.Path = streamReader.Path;
            this.next = pipeline.CreateEmitter<bool>(this, nameof(this.Next));
            this.loopBack = pipeline.CreateReceiver<bool>(this, this.Next, nameof(this.loopBack));
            this.next.PipeTo(this.loopBack, DeliveryPolicy.Unlimited);
        }

        /// <summary>
        /// Gets the name of the store, or null if this is a volatile store.
        /// </summary>
        public string Name { get; private set; }

        /// <summary>
        /// Gets the path of the store, or null if this is a volatile store.
        /// </summary>
        public string Path { get; private set; }

        /// <summary>
        /// Gets the set of types that this Importer can deserialize.
        /// Types can be added or re-mapped using the <see cref="KnownSerializers.Register{T}(string, CloningFlags)"/> method.
        /// </summary>
        public KnownSerializers Serializers
        {
            get
            {
                var storeStreamReader = this.streamReader as PsiStoreStreamReader;
                if (storeStreamReader != null)
                {
                    return storeStreamReader.GetSerializers();
                }

                return KnownSerializers.Default;
            }
        }

        /// <summary>
        /// Gets the metadata of all the streams in this store.
        /// </summary>
        public IEnumerable<IStreamMetadata> AvailableStreams => this.streamReader.AvailableStreams;

        /// <summary>
        /// Gets the interval between the creation times of the first and last messages written to this store, across all streams.
        /// </summary>
        public TimeInterval MessageCreationTimeInterval => this.streamReader.MessageCreationTimeInterval;

        /// <summary>
        /// Gets the interval between the originating times of the first and last messages written to this store, across all streams.
        /// </summary>
        public TimeInterval MessageOriginatingTimeInterval => this.streamReader.MessageOriginatingTimeInterval;

        /// <summary>
        /// Closes the store and disposes of the current instance.
        /// </summary>
        public void Dispose()
        {
            this.streamReader.Dispose();
            this.loopBack.Dispose();
        }

        /// <summary>
        /// Returns the metadata for a specified stream.
        /// </summary>
        /// <param name="streamName">The name of the stream.</param>
        /// <returns>The metadata associated with the stream.</returns>
        public IStreamMetadata GetMetadata(string streamName) => this.streamReader.GetStreamMetadata(streamName);

        /// <summary>
        /// Returns the supplemental metadata for a specified stream.
        /// </summary>
        /// <typeparam name="T">Type of supplemental metadata.</typeparam>
        /// <param name="streamName">The name of the stream.</param>
        /// <returns>The metadata associated with the stream.</returns>
        public T GetSupplementalMetadata<T>(string streamName)
        {
            var meta = this.streamReader.GetStreamMetadata(streamName) as PsiStreamMetadata;
            if (meta == null)
            {
                throw new NotSupportedException("Supplemental metadata is only available on PsiStreamMetadata from a PsiStoreStreamReader.");
            }

            return meta.GetSupplementalMetadata<T>(this.Serializers);
        }

        /// <summary>
        /// Indicates whether the store contains the specified stream.
        /// </summary>
        /// <param name="streamName">The name of the stream.</param>
        /// <returns>True if the store contains a stream with the specified name, false otherwise.</returns>
        public bool Contains(string streamName) => this.streamReader.ContainsStream(streamName);

        /// <summary>
        /// Copies the specified stream to an exporter without deserializing the data.
        /// </summary>
        /// <param name="streamName">The name of the stream to copy.</param>
        /// <param name="writer">The store to copy to.</param>
        /// <param name="deliveryPolicy">An optional delivery policy.</param>
        public void CopyStream(string streamName, Exporter writer, DeliveryPolicy<Message<BufferReader>> deliveryPolicy = null)
        {
            // create the copy pipeline
            var meta = this.streamReader.GetStreamMetadata(streamName) as PsiStreamMetadata;
            if (meta == null)
            {
                throw new NotSupportedException("Copying streams is supported only with PsiStoreStreamReader.");
            }

            var raw = this.OpenRawStream(meta);
            writer.Write(raw.Out, meta, deliveryPolicy);
        }

        /// <summary>
        /// Opens the specified stream for reading and returns a stream instance that can be used to consume the messages.
        /// The returned stream will publish data read from the store once the pipeline is running.
        /// </summary>
        /// <typeparam name="T">The expected type of the stream to open.
        /// This type will be used to deserialize the stream messages.</typeparam>
        /// <param name="streamName">The name of the stream to open.</param>
        /// <returns>A stream that publishes the data read from the store.</returns>
        public IProducer<T> OpenStream<T>(string streamName)
        {
            if (this.streams.TryGetValue(streamName, out object stream))
            {
                return (IProducer<T>)stream; // if the types don't match, invalid cast exception is the appropriate error
            }

            var meta = this.streamReader.GetStreamMetadata(streamName);

            var originatingLifetime = meta.MessageCount == 0 ? TimeInterval.Empty : new TimeInterval(meta.FirstMessageOriginatingTime, meta.LastMessageOriginatingTime);
            if (originatingLifetime != null && !originatingLifetime.IsEmpty && originatingLifetime.IsFinite)
            {
                // propose a replay time that covers the stream lifetime
                this.pipeline.ProposeReplayTime(originatingLifetime);
            }

            // register this stream with the store catalog
            this.pipeline.ConfigurationStore.Set(Exporter.StreamMetadataNamespace, streamName, meta);

            var emitter = this.pipeline.CreateEmitter<T>(this, streamName);
            this.streamReader.OpenStream<T>(streamName, (message, envelope) =>
            {
                // do not deliver messages past the stream closing time
                if (meta.ClosedTime == default || envelope.OriginatingTime <= meta.ClosedTime)
                {
                    // call Deliver rather than Post to preserve the original envelope
                    emitter.Deliver(message, envelope);
                }
            });

            // preserve the envelope of the deserialized message in the output connector
            var outConnector = new Connector<T>(this.pipeline, this.pipeline, $"connectorOut{streamName}", preserveEnvelope: true);
            emitter.PipeTo(outConnector, DeliveryPolicy.SynchronousOrThrottle);
            outConnector.Out.Name = streamName;
            this.streams[streamName] = outConnector.Out;
            return outConnector.Out;
        }

        /// <summary>
        /// Opens the specified stream as dynamic for reading and returns a stream instance that can be used to consume the messages.
        /// The returned stream will publish data read from the store once the pipeline is running.
        /// </summary>
        /// <remarks>Messages are deserialized as dynamic primitives and/or ExpandoObject of dynamic.</remarks>
        /// <param name="streamName">The name of the stream to open.</param>
        /// <returns>A stream of dynamic that publishes the data read from the store.</returns>
        public IProducer<dynamic> OpenDynamicStream(string streamName)
        {
            var storeStreamReader = this.streamReader as PsiStoreStreamReader;
            if (storeStreamReader == null)
            {
                throw new NotSupportedException("Opening dynamic streams requires PsiStoreStreamReader.");
            }

            return this.OpenStream<dynamic>(streamName);
        }

        /// <inheritdoc />
        public void Start(Action<DateTime> notifyCompletionTime)
        {
            this.notifyCompletionTime = notifyCompletionTime;
            var replay = this.pipeline.ReplayDescriptor;
            this.streamReader.Seek(replay.Interval, true);
            this.next.Post(true, replay.Start);
        }

        /// <inheritdoc />
        public void Stop(DateTime finalOriginatingTime, Action notifyCompleted)
        {
            this.stopping = true;
            notifyCompleted();
        }

        /// <summary>
        /// Opens the specified stream as raw `Message` of `BufferReader` for reading and returns a stream instance that can be used to consume the messages.
        /// The returned stream will publish data read from the store once the pipeline is running.
        /// </summary>
        /// <remarks>Messages are not deserialized.</remarks>
        /// <param name="meta">The meta of the stream to open.</param>
        /// <returns>A stream of raw messages that publishes the data read from the store.</returns>
        internal IProducer<Message<BufferReader>> OpenRawStream(PsiStreamMetadata meta)
        {
            var storeStreamReader = this.streamReader as PsiStoreStreamReader;
            if (storeStreamReader == null)
            {
                throw new NotSupportedException("Opening raw streams only words with PsiStoreStreamReader as underlying IStreamReader.");
            }

            return this.OpenStream<Message<BufferReader>>(meta.Name);
        }

        /// <summary>
        /// Attempts to move the reader to the next message (across all streams).
        /// </summary>
        /// <param name="moreDataPromised">Indicates whether an absence of messages should be reported as the end of the store.</param>
        /// <param name="env">The envelope of the last message we read.</param>
        private void Next(bool moreDataPromised, Envelope env)
        {
            if (this.stopping)
            {
                return;
            }

            var result = this.streamReader.MoveNext(out Envelope e); // causes target to be called
            if (result)
            {
                // we want messages to be scheduled and delivered based on their original creation time, not originating time
                // the check below is just to ensure we don't fail because of some timing issue when writing the data (since there is no ordering guarantee across streams)
                // note that we are posting a message of a message, and once the outer message is stripped by the splitter, the inner message will still have the correct originating time
                var nextTime = (env.OriginatingTime > e.CreationTime) ? env.OriginatingTime : e.CreationTime;
                this.next.Post(true, nextTime.AddTicks(1));
                this.finalTicks = Math.Max(this.finalTicks, Math.Max(e.OriginatingTime.Ticks, nextTime.Ticks));
            }
            else
            {
                // retry at least once, even if there is no active writer
                bool willHaveMoreData = this.streamReader.IsLive();
                if (willHaveMoreData || moreDataPromised)
                {
                    this.next.Post(willHaveMoreData, env.OriginatingTime.AddTicks(1));
                }
                else
                {
                    this.notifyCompletionTime(new DateTime(this.finalTicks));
                }
            }
        }
    }
}

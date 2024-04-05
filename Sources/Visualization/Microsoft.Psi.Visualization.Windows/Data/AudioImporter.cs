// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.

namespace Microsoft.Psi.Visualization.Data
{
    using System;
    using System.Collections.Generic;
    using Microsoft.Psi;
    using Microsoft.Psi.Audio;
    using Microsoft.Psi.Components;
    using Microsoft.Psi.Data;
    using Microsoft.Psi.Executive;
    using Microsoft.Psi.Visualization.Navigation;

    /// <summary>
    /// Component that reads audio messages and publishes them on streams.
    /// </summary>
    /// <remarks>
    /// This component follows a similar implementation pattern the with runtime <see cref="Importer"/>,
    /// with the crucial difference that the <see cref="AudioBuffer"/> messages output by this importer
    /// component have their creation time set to the same value as the originating time. This enables
    /// the <see cref="Navigator"/> class to playback audio in originating time and therefore align it
    /// correctly with the rest of the visuals in PsiStudio.
    /// </remarks>
    internal class AudioImporter : Subpipeline, IDisposable
    {
        private readonly Pipeline pipeline;
        private readonly IStreamReader streamReader;
        private readonly Func<StreamImporter> getStreamImporter;

        /// <summary>
        /// Initializes a new instance of the <see cref="AudioImporter"/> class.
        /// </summary>
        /// <param name="pipeline">The pipeline to add the component to.</param>
        /// <param name="streamReader">Stream reader.</param>
        /// <param name="usePerStreamReaders">Flag indicating whether to use per-stream readers.</param>
        public AudioImporter(Pipeline pipeline, IStreamReader streamReader, bool usePerStreamReaders)
            : base(pipeline, $"{nameof(AudioImporter)}[{streamReader.Name}]")
        {
            this.pipeline = pipeline;
            this.streamReader = streamReader;

            this.getStreamImporter = () => new StreamImporter(this, pipeline.ConfigurationStore, streamReader.OpenNew());
            if (!usePerStreamReaders)
            {
                // cache single shared importer
                var sharedImporter = this.getStreamImporter();
                this.getStreamImporter = () => sharedImporter;
            }
        }

        /// <summary>
        /// Opens the specified stream for reading and returns a stream instance that can be used to consume the messages.
        /// </summary>
        /// <param name="streamName">The name of the stream to open.</param>
        /// <returns>A stream that publishes the data read from the store.</returns>
        public IProducer<AudioBuffer> OpenAudioStream(string streamName)
            => this.BridgeOut(this.getStreamImporter().OpenAudioStream(streamName), streamName);

        /// <inheritdoc />
        public override void Dispose()
        {
            base.Dispose();
            this.streamReader.Dispose();
        }

        /// <summary>
        /// Bridge output stream out to parent pipeline.
        /// </summary>
        /// <typeparam name="T">Type of stream messages.</typeparam>
        /// <param name="stream">Stream of messages.</param>
        /// <param name="name">Stream name.</param>
        /// <returns>Bridged stream.</returns>
        private IProducer<T> BridgeOut<T>(IProducer<T> stream, string name)
        {
            // preserve the envelope of the deserialized message in the output connector
            var connector = new Connector<T>(this, this.pipeline, name, true);
            return stream.PipeTo(connector, DeliveryPolicy.SynchronousOrThrottle);
        }

        /// <summary>
        /// Component that reads messages via a specified <see cref="IStreamReader"/> and publishes them on streams.
        /// </summary>
        private class StreamImporter : ISourceComponent, IDisposable
        {
            private readonly IStreamReader streamReader;
            private readonly Dictionary<string, object> streams = new ();
            private readonly Pipeline pipeline;
            private readonly KeyValueStore configurationStore;
            private readonly Receiver<bool> loopBack;
            private readonly Emitter<bool> next;

            private bool stopping;
            private long finalTicks = 0;
            private Action<DateTime> notifyCompletionTime;
            private Action outputPreviousMessage = () => { };

            /// <summary>
            /// Initializes a new instance of the <see cref="StreamImporter"/> class.
            /// </summary>
            /// <param name="pipeline">The pipeline to add the component to.</param>
            /// <param name="configurationStore">Configuration store in which to store catalog meta.</param>
            /// <param name="streamReader">Stream reader.</param>
            internal StreamImporter(Pipeline pipeline, KeyValueStore configurationStore, IStreamReader streamReader)
            {
                this.streamReader = streamReader;
                this.pipeline = pipeline;
                this.configurationStore = configurationStore;
                this.next = pipeline.CreateEmitter<bool>(this, nameof(this.Next));
                this.loopBack = pipeline.CreateReceiver<bool>(this, this.Next, nameof(this.loopBack));
                this.next.PipeTo(this.loopBack, DeliveryPolicy.Unlimited);
            }

            /// <summary>
            /// Closes the store and disposes of the current instance.
            /// </summary>
            public void Dispose()
            {
                this.streamReader.Dispose();
                this.loopBack.Dispose();
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
            /// Opens the specified stream for reading and returns a stream instance that can be used to consume the messages.
            /// The returned stream will publish data read from the store once the pipeline is running.
            /// </summary>
            /// <param name="streamName">The name of the stream to open.</param>
            /// <returns>A stream that publishes the data read from the store.</returns>
            internal IProducer<AudioBuffer> OpenAudioStream(string streamName)
            {
                if (this.streams.TryGetValue(streamName, out object stream))
                {
                    return (IProducer<AudioBuffer>)stream; // if the types don't match, invalid cast exception is the appropriate error
                }

                var meta = this.streamReader.GetStreamMetadata(streamName);

                var originatingLifetime = meta.MessageCount == 0 ? TimeInterval.Empty : new TimeInterval(meta.FirstMessageOriginatingTime, meta.LastMessageOriginatingTime);
                if (originatingLifetime != null && !originatingLifetime.IsEmpty && originatingLifetime.IsFinite)
                {
                    // propose a replay time that covers the stream lifetime
                    this.pipeline.ProposeReplayTime(originatingLifetime);
                }

                // register this stream with the store catalog
                this.configurationStore.Set(Exporter.StreamMetadataNamespace, streamName, meta);

                var emitter = this.pipeline.CreateEmitter<AudioBuffer>(this, streamName);
                this.streamReader.OpenStream<AudioBuffer>(
                    streamName,
                    (data, envelope) =>
                    {
                        // For the audio importer used in visualization we want messages to be scheduled and delivered
                        // based on their original times, so overwrite the creation time to the original time
                        envelope.CreationTime = envelope.OriginatingTime;

                        // And then follow a similar logic to the default Importer

                        // do not deliver messages past the stream closing time
                        if (meta.ClosedTime == default || envelope.OriginatingTime <= meta.ClosedTime)
                        {
                            // If the replay descriptor enforces the replay clock and the message creation time is ahead
                            // of the pipeline time
                            if (this.pipeline.ReplayDescriptor.EnforceReplayClock && envelope.CreationTime > this.pipeline.GetCurrentTime())
                            {
                                // Then clone the message in order to hold on to it and publish it later.
                                var clone = default(AudioBuffer);
                                data.DeepClone(ref clone);

                                // Hold onto the data in the outputPreviousMessage closure, which is called
                                // in Next(). Persisting as a closure allows for capturing data of varying types (T).
                                this.outputPreviousMessage = () =>
                                {
                                    emitter.Deliver(clone, envelope);
                                };
                            }
                            else
                            {
                                // call Deliver rather than Post to preserve the original envelope
                                emitter.Deliver(data, envelope);
                            }
                        }
                    });

                this.streams[streamName] = emitter;
                return emitter;
            }

            /// <summary>
            /// Attempts to move the reader to the next message (across all streams).
            /// </summary>
            /// <param name="moreDataPromised">Indicates whether an absence of messages should be reported as the end of the store.</param>
            /// <param name="env">The envelope of the last message we read.</param>
            private void Next(bool moreDataPromised, Envelope env)
            {
                this.outputPreviousMessage(); // deliver previous message (if any)
                this.outputPreviousMessage = () => { };

                if (this.stopping)
                {
                    return;
                }

                var result = this.streamReader.MoveNext(out Envelope e); // causes target to be called
                if (result)
                {
                    // For the audio importer used in visualization we want messages to be scheduled and delivered
                    // based on their original times, so overwrite the creation time to the original time,
                    e.CreationTime = e.OriginatingTime;

                    // And then follow the same logic like the regular Importer
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
}

// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.

namespace Microsoft.Psi.Diagnostics
{
    using System;
    using System.Collections.Concurrent;
    using System.Collections.Generic;
    using System.Linq;

    /// <summary>
    /// Represents diagnostic information about a pipeline.
    /// </summary>
    /// <remarks>
    /// This is a summarized snapshot of the graph with aggregated message statistics which is posted to the
    /// diagnostics stream. It has a much smaller memory footprint compared with PipelineDiagnosticsInternal.
    /// </remarks>
    public class PipelineDiagnostics
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="PipelineDiagnostics"/> class.
        /// </summary>
        /// <param name="pipelineDiagnosticsInternal">Internal pipeline diagnostics.</param>
        /// <param name="includeStoppedPipelines">Whether to include stopped pipelines.</param>
        /// <param name="includeStoppedPipelineElements">Whether to include stopped pipeline elements.</param>
        internal PipelineDiagnostics(PipelineDiagnosticsInternal pipelineDiagnosticsInternal, bool includeStoppedPipelines, bool includeStoppedPipelineElements)
        {
            // This is the constructor used when creating a summary snapshot to be posted.
            // The Builder is used to construct one-and-only-one instance of each part of the
            // graph and to initialize circular references (via delayed thunks).
            var builder = new Builder();
            this.Initialize(pipelineDiagnosticsInternal, null, builder, includeStoppedPipelines, includeStoppedPipelineElements);
            builder.InvokeThunks();
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="PipelineDiagnostics"/> class.
        /// </summary>
        /// <param name="pipelineDiagnosticsInternal">Internal pipeline diagnostics.</param>
        /// <param name="parent">Parent pipeline diagnostics to this pipeline diagnostics.</param>
        /// <param name="builder">Builder of pipeline parts used during construction.</param>
        /// <param name="includeStoppedPipelines">Whether to include stopped pipelines.</param>
        /// <param name="includeStoppedPipelineElements">Whether to include stopped pipeline elements.</param>
        private PipelineDiagnostics(PipelineDiagnosticsInternal pipelineDiagnosticsInternal, PipelineDiagnostics parent, Builder builder, bool includeStoppedPipelines, bool includeStoppedPipelineElements)
        {
            this.Initialize(pipelineDiagnosticsInternal, parent, builder, includeStoppedPipelines, includeStoppedPipelineElements);
        }

        /// <summary>
        /// Gets pipeline ID.
        /// </summary>
        public int Id { get; private set; }

        /// <summary>
        /// Gets pipeline name.
        /// </summary>
        public string Name { get; private set; }

        /// <summary>
        /// Gets a value indicating whether the pipeline is running (after started, before stopped).
        /// </summary>
        public bool IsPipelineRunning { get; private set; }

        /// <summary>
        /// Gets elements in this pipeline.
        /// </summary>
        public PipelineElementDiagnostics[] PipelineElements { get; private set; }

        /// <summary>
        /// Gets parent pipeline of this pipeline (it any).
        /// </summary>
        public PipelineDiagnostics ParentPipelineDiagnostics { get; private set; }

        /// <summary>
        /// Gets subpipelines of this pipeline.
        /// </summary>
        public PipelineDiagnostics[] SubpipelineDiagnostics { get; private set; }

        /// <summary>
        /// Gets ancestor pipeline diagnostics.
        /// </summary>
        public IEnumerable<PipelineDiagnostics> AncestorPipelines
        {
            get
            {
                var parent = this.ParentPipelineDiagnostics;
                while (parent != null)
                {
                    yield return parent;
                    parent = parent.ParentPipelineDiagnostics;
                }
            }
        }

        /// <summary>
        /// Gets descendant pipeline diagnostics.
        /// </summary>
        public IEnumerable<PipelineDiagnostics> DescendantPipelines
        {
            get
            {
                foreach (var sub in this.SubpipelineDiagnostics)
                {
                    yield return sub;
                    foreach (var subsub in sub.DescendantPipelines)
                    {
                        yield return subsub;
                    }
                }
            }
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="PipelineDiagnostics"/> class.
        /// </summary>
        /// <param name="pipelineDiagnosticsInternal">Internal pipeline diagnostics.</param>
        /// <param name="parent">Parent pipeline diagnostics to this pipeline diagnostics.</param>
        /// <param name="builder">Builder of pipeline parts used during construction.</param>
        /// <param name="includeStoppedPipelines">Whether to include stopped pipelines.</param>
        /// <param name="includeStoppedPipelineElements">Whether to include stopped pipeline element .</param>
        private void Initialize(PipelineDiagnosticsInternal pipelineDiagnosticsInternal, PipelineDiagnostics parent, Builder builder, bool includeStoppedPipelines, bool includeStoppedPipelineElements)
        {
            this.Id = pipelineDiagnosticsInternal.Id;
            this.Name = pipelineDiagnosticsInternal.Name;
            this.IsPipelineRunning = pipelineDiagnosticsInternal.IsPipelineRunning;
            this.ParentPipelineDiagnostics = parent;
            this.SubpipelineDiagnostics =
                pipelineDiagnosticsInternal.Subpipelines.Values
                    .Where(s => includeStoppedPipelines || s.IsPipelineRunning)
                    .Select(s => builder.GetOrCreatePipelineDiagnostics(s, this, includeStoppedPipelines, includeStoppedPipelineElements))
                    .ToArray();
            this.PipelineElements =
                pipelineDiagnosticsInternal.PipelineElements.Values
                    .Where(pe => includeStoppedPipelineElements || (pe.IsRunning && (pe.RepresentsSubpipeline == null || pe.RepresentsSubpipeline.IsPipelineRunning)))
                    .Select(pe => builder.GetOrCreatePipelineElementDiagnostics(pe))
                    .ToArray();
        }

        /// <summary>
        /// Represents diagnostic information about a pipeline element.
        /// </summary>
        public class PipelineElementDiagnostics
        {
            /// <summary>
            /// Initializes a new instance of the <see cref="PipelineElementDiagnostics"/> class.
            /// </summary>
            /// <param name="pipelineElementDiagnosticsInternal">Internal pipeline element diagnostics.</param>
            /// <param name="builder">Builder of pipeline parts used during construction.</param>
            internal PipelineElementDiagnostics(PipelineDiagnosticsInternal.PipelineElementDiagnostics pipelineElementDiagnosticsInternal, Builder builder)
            {
                this.Id = pipelineElementDiagnosticsInternal.Id;
                this.Name = pipelineElementDiagnosticsInternal.Name;
                this.TypeName = pipelineElementDiagnosticsInternal.TypeName;
                this.Kind = pipelineElementDiagnosticsInternal.Kind;
                this.IsRunning = pipelineElementDiagnosticsInternal.IsRunning;
                this.Finalized = pipelineElementDiagnosticsInternal.Finalized;
                this.DiagnosticState = pipelineElementDiagnosticsInternal.DiagnosticState;
                this.Emitters = pipelineElementDiagnosticsInternal.Emitters.Values.Select(e => builder.GetOrCreateEmitterDiagnostics(e)).ToArray();
                this.Receivers = pipelineElementDiagnosticsInternal.Receivers.Values.Select(r => builder.GetOrCreateReceiverDiagnostics(r)).ToArray();
                this.PipelineId = pipelineElementDiagnosticsInternal.PipelineId;
                if (pipelineElementDiagnosticsInternal.RepresentsSubpipeline != null)
                {
                    builder.EnqueueThunk(() =>
                    {
                        if (builder.Pipelines.TryGetValue(pipelineElementDiagnosticsInternal.RepresentsSubpipeline.Id, out var pipeline))
                        {
                            this.RepresentsSubpipeline = pipeline;
                        }
                    });
                }

                if (pipelineElementDiagnosticsInternal.ConnectorBridgeToPipelineElement != null)
                {
                    builder.EnqueueThunk(() =>
                    {
                        if (builder.PipelineElements.TryGetValue(pipelineElementDiagnosticsInternal.ConnectorBridgeToPipelineElement.Id, out var bridge))
                        {
                            this.ConnectorBridgeToPipelineElement = bridge;
                        }
                    });
                }
            }

            /// <summary>
            /// Gets pipeline element ID.
            /// </summary>
            public int Id { get; }

            /// <summary>
            /// Gets pipeline element name.
            /// </summary>
            public string Name { get; }

            /// <summary>
            /// Gets pipeline element component type name.
            /// </summary>
            public string TypeName { get; private set; }

            /// <summary>
            /// Gets pipeline element kind.
            /// </summary>
            public PipelineElementKind Kind { get; }

            /// <summary>
            /// Gets a value indicating whether the pipeline element is running (after started, before stopped).
            /// </summary>
            public bool IsRunning { get; }

            /// <summary>
            /// Gets a value indicating whether the pipeline element is finalized.
            /// </summary>
            public bool Finalized { get; }

            /// <summary>
            /// Gets the diagnostic state for the pipeline element.
            /// </summary>
            public string DiagnosticState { get; }

            /// <summary>
            /// Gets pipeline element emitters.
            /// </summary>
            public EmitterDiagnostics[] Emitters { get; }

            /// <summary>
            /// Gets pipeline element receivers.
            /// </summary>
            public ReceiverDiagnostics[] Receivers { get; }

            /// <summary>
            /// Gets ID of pipeline to which this element belongs.
            /// </summary>
            public int PipelineId { get; }

            /// <summary>
            /// Gets pipeline which this element represents (e.g. Subpipeline).
            /// </summary>
            /// <remarks>This is used when a pipeline element is a pipeline (e.g. Subpipeline).</remarks>
            public PipelineDiagnostics RepresentsSubpipeline { get; private set; }

            /// <summary>
            /// Gets bridge to pipeline element in another pipeline (e.g. Connectors).
            /// </summary>
            public PipelineElementDiagnostics ConnectorBridgeToPipelineElement { get; private set; }
        }

        /// <summary>
        /// Represents diagnostic information about a pipeline element emitter.
        /// </summary>
        public class EmitterDiagnostics
        {
            /// <summary>
            /// Initializes a new instance of the <see cref="EmitterDiagnostics"/> class.
            /// </summary>
            /// <param name="emitterDiagnostics">Internal emitter diagnostics.</param>
            /// <param name="builder">Builder of pipeline parts used during construction.</param>
            internal EmitterDiagnostics(PipelineDiagnosticsInternal.EmitterDiagnostics emitterDiagnostics, Builder builder)
            {
                this.Id = emitterDiagnostics.Id;
                this.Name = emitterDiagnostics.Name;
                this.Type = emitterDiagnostics.Type;
                builder.EnqueueThunk(() =>
                {
                    if (builder.PipelineElements.TryGetValue(emitterDiagnostics.PipelineElement.Id, out var element))
                    {
                        this.PipelineElement = element;
                    }
                });
                builder.EnqueueThunk(() => this.Targets = emitterDiagnostics.Targets.Values.Select(r => builder.Receivers.TryGetValue(r.Id, out var receiver) ? receiver : null).Where(r => r != null).ToArray());
            }

            /// <summary>
            /// Gets emitter ID.
            /// </summary>
            public int Id { get; }

            /// <summary>
            /// Gets emitter name.
            /// </summary>
            public string Name { get; }

            /// <summary>
            /// Gets emitter type.
            /// </summary>
            public string Type { get; }

            /// <summary>
            /// Gets pipeline element to which emitter belongs.
            /// </summary>
            public PipelineElementDiagnostics PipelineElement { get; private set; }

            /// <summary>
            /// Gets emitter target receivers.
            /// </summary>
            public ReceiverDiagnostics[] Targets { get; private set; }
        }

        /// <summary>
        /// Represents diagnostic information about a pipeline element receiver.
        /// </summary>
        public class ReceiverDiagnostics
        {
            /// <summary>
            /// Initializes a new instance of the <see cref="ReceiverDiagnostics"/> class.
            /// </summary>
            /// <param name="receiverDiagnostics">Internal receiver diagnostics.</param>
            /// <param name="builder">Builder of pipeline parts used during construction.</param>
            internal ReceiverDiagnostics(PipelineDiagnosticsInternal.ReceiverDiagnostics receiverDiagnostics, Builder builder)
            {
                this.Id = receiverDiagnostics.Id;
                this.ReceiverName = receiverDiagnostics.ReceiverName;
                this.DeliveryPolicyName = receiverDiagnostics.DeliveryPolicyName;
                this.TypeName = receiverDiagnostics.TypeName;
                this.ReceiverIsThrottled = receiverDiagnostics.ReceiverIsThrottled;
                this.LastDeliveryQueueSize = receiverDiagnostics.DeliveryQueueSizeHistory.LastOrDefault().Item1;
                this.AvgDeliveryQueueSize = receiverDiagnostics.DeliveryQueueSizeHistory.AverageSize();
                this.TotalMessageEmittedCount = receiverDiagnostics.TotalMessageEmittedCount;
                this.WindowMessageEmittedCount = receiverDiagnostics.MessageEmittedCountHistory.Count;
                this.TotalMessageProcessedCount = receiverDiagnostics.TotalMessageProcessedCount;
                this.WindowMessageProcessedCount = receiverDiagnostics.MessageProcessedCountHistory.Count;
                this.TotalMessageDroppedCount = receiverDiagnostics.TotalMessageDroppedCount;
                this.WindowMessageDroppedCount = receiverDiagnostics.MessageDroppedCountHistory.Count;
                this.LastMessageCreatedLatency = receiverDiagnostics.MessageCreatedLatencyHistory.LastOrDefault().Item1.TotalMilliseconds;
                this.AvgMessageCreatedLatency = receiverDiagnostics.MessageCreatedLatencyHistory.AverageTime().TotalMilliseconds;
                this.LastMessageEmittedLatency = receiverDiagnostics.MessageEmittedLatencyHistory.LastOrDefault().Item1.TotalMilliseconds;
                this.AvgMessageEmittedLatency = receiverDiagnostics.MessageEmittedLatencyHistory.AverageTime().TotalMilliseconds;
                this.LastMessageReceivedLatency = receiverDiagnostics.MessageReceivedLatencyHistory.LastOrDefault().Item1.TotalMilliseconds;
                this.AvgMessageReceivedLatency = receiverDiagnostics.MessageReceivedLatencyHistory.AverageTime().TotalMilliseconds;
                this.LastMessageProcessTime = receiverDiagnostics.MessageProcessTimeHistory.LastOrDefault().Item1.TotalMilliseconds;
                this.AvgMessageProcessTime = receiverDiagnostics.MessageProcessTimeHistory.AverageTime().TotalMilliseconds;
                this.LastMessageSize = receiverDiagnostics.MessageSizeHistory.LastOrDefault().Item1;
                this.AvgMessageSize = receiverDiagnostics.MessageSizeHistory.AverageSize();
                builder.EnqueueThunk(() =>
                {
                    if (builder.PipelineElements.TryGetValue(receiverDiagnostics.PipelineElement.Id, out var element))
                    {
                        this.PipelineElement = element;
                    }
                });
                if (receiverDiagnostics.Source != null)
                {
                    builder.EnqueueThunk(() =>
                    {
                        if (receiverDiagnostics.Source != null && builder.Emitters.TryGetValue(receiverDiagnostics.Source.Id, out var source))
                        {
                            this.Source = source;
                        }
                    });
                }
            }

            /// <summary>
            /// Gets receiver ID.
            /// </summary>
            public int Id { get; }

            /// <summary>
            /// Gets receiver name.
            /// </summary>
            public string ReceiverName { get; }

            /// <summary>
            /// Gets name of delivery policy used by receiver.
            /// </summary>
            public string DeliveryPolicyName { get; }

            /// <summary>
            /// Gets receiver type.
            /// </summary>
            public string TypeName { get; }

            /// <summary>
            /// Gets a value indicating whether receiver is throttled.
            /// </summary>
            public bool ReceiverIsThrottled { get; }

            /// <summary>
            /// Gets delivery queue size at last message.
            /// </summary>
            public double LastDeliveryQueueSize { get; }

            /// <summary>
            /// Gets average delivery queue size.
            /// </summary>
            public double AvgDeliveryQueueSize { get; }

            /// <summary>
            /// Gets total count of emitted messages.
            /// </summary>
            public int TotalMessageEmittedCount { get; }

            /// <summary>
            /// Gets count of emitted messages in last averaging time window.
            /// </summary>
            public int WindowMessageEmittedCount { get; }

            /// <summary>
            /// Gets total count of processed messages.
            /// </summary>
            public int TotalMessageProcessedCount { get; }

            /// <summary>
            /// Gets count of processed messages in last averaging time window.
            /// </summary>
            public int WindowMessageProcessedCount { get; }

            /// <summary>
            /// Gets total count of dropped messages.
            /// </summary>
            public int TotalMessageDroppedCount { get; }

            /// <summary>
            /// Gets count of dropped messages in last averaging time window.
            /// </summary>
            public int WindowMessageDroppedCount { get; }

            /// <summary>
            /// Gets latency with which the last message was created.
            /// </summary>
            public double LastMessageCreatedLatency { get; }

            /// <summary>
            /// Gets average message created latency in last averaging time window.
            /// </summary>
            public double AvgMessageCreatedLatency { get; }

            /// <summary>
            /// Gets latency with which the last message was emitted.
            /// </summary>
            public double LastMessageEmittedLatency { get; }

            /// <summary>
            /// Gets average message emitted latency in last averaging time window.
            /// </summary>
            public double AvgMessageEmittedLatency { get; }

            /// <summary>
            /// Gets latency with which the last message was received.
            /// </summary>
            public double LastMessageReceivedLatency { get; }

            /// <summary>
            /// Gets average message received latency in last averaging time window.
            /// </summary>
            public double AvgMessageReceivedLatency { get; }

            /// <summary>
            /// Gets receiver processing time for the last message.
            /// </summary>
            public double LastMessageProcessTime { get; }

            /// <summary>
            /// Gets average receiver processing time in last averaging time window.
            /// </summary>
            public double AvgMessageProcessTime { get; }

            /// <summary>
            /// Gets message size for the last message.
            /// </summary>
            public double LastMessageSize { get; }

            /// <summary>
            /// Gets average message size over in last averaging time window.
            /// </summary>
            public double AvgMessageSize { get; }

            /// <summary>
            /// Gets pipeline element to which emitter belongs.
            /// </summary>
            public PipelineElementDiagnostics PipelineElement { get; private set; }

            /// <summary>
            /// Gets receiver's source emitter.
            /// </summary>
            public EmitterDiagnostics Source { get; private set; }
        }

        /// <summary>
        /// Builder of graph elements used during construction.
        /// </summary>
        internal class Builder
        {
            private readonly ConcurrentQueue<Action> thunks = new ConcurrentQueue<Action>();

            public Builder()
            {
                this.Pipelines = new ConcurrentDictionary<int, PipelineDiagnostics>();
                this.PipelineElements = new ConcurrentDictionary<int, PipelineElementDiagnostics>();
                this.Emitters = new ConcurrentDictionary<int, EmitterDiagnostics>();
                this.Receivers = new ConcurrentDictionary<int, ReceiverDiagnostics>();
            }

            public ConcurrentDictionary<int, PipelineDiagnostics> Pipelines { get; }

            public ConcurrentDictionary<int, PipelineElementDiagnostics> PipelineElements { get; }

            public ConcurrentDictionary<int, EmitterDiagnostics> Emitters { get; }

            public ConcurrentDictionary<int, ReceiverDiagnostics> Receivers { get; }

            /// <summary>
            /// Enqueue thunk to be executed once all pipelines, elements, emitters and receivers are created.
            /// </summary>
            /// <param name="thunk">Thunk to be executed.</param>
            public void EnqueueThunk(Action thunk)
            {
                this.thunks.Enqueue(thunk);
            }

            /// <summary>
            /// Execute enqueued thunks.
            /// </summary>
            public void InvokeThunks()
            {
                foreach (var t in this.thunks)
                {
                    t();
                }
            }

            /// <summary>
            /// Get or create external pipeline diagnostics representation.
            /// </summary>
            /// <param name="pipelineDiagnosticsInternal">Internal pipeline diagnostics representation.</param>
            /// <param name="parentPipelineDiagnostics">Parent pipeline diagnostics.</param>
            /// <param name="includeStoppedPipelines">Whether to include stopped pipelines.</param>
            /// <param name="includeStoppedPipelineElements">Whether to include stopped pipeline element .</param>
            /// <returns>External pipeline diagnostics representation.</returns>
            public PipelineDiagnostics GetOrCreatePipelineDiagnostics(PipelineDiagnosticsInternal pipelineDiagnosticsInternal, PipelineDiagnostics parentPipelineDiagnostics, bool includeStoppedPipelines, bool includeStoppedPipelineElements)
            {
                return this.GetOrCreate(this.Pipelines, pipelineDiagnosticsInternal, p => p.Id, (p, c) => new PipelineDiagnostics(p, parentPipelineDiagnostics, c, includeStoppedPipelines, includeStoppedPipelineElements));
            }

            /// <summary>
            /// Get or create external pipeline element diagnostics representation.
            /// </summary>
            /// <param name="pipelineElementDiagnosticsInternal">Internal pipeline element diagnostics representation.</param>
            /// <returns>External pipeline element diagnostics representation.</returns>
            public PipelineElementDiagnostics GetOrCreatePipelineElementDiagnostics(PipelineDiagnosticsInternal.PipelineElementDiagnostics pipelineElementDiagnosticsInternal)
            {
                return this.GetOrCreate(this.PipelineElements, pipelineElementDiagnosticsInternal, e => e.Id, (e, c) => new PipelineElementDiagnostics(e, c));
            }

            /// <summary>
            /// Get or create external emitter diagnostics representation.
            /// </summary>
            /// <param name="emitterDiagnosticsInternal">Internal pipeline element diagnostics representation.</param>
            /// <returns>External pipeline element diagnostics representation.</returns>
            public EmitterDiagnostics GetOrCreateEmitterDiagnostics(PipelineDiagnosticsInternal.EmitterDiagnostics emitterDiagnosticsInternal)
            {
                return this.GetOrCreate(this.Emitters, emitterDiagnosticsInternal, e => e.Id, (e, c) => new EmitterDiagnostics(e, c));
            }

            /// <summary>
            /// Get or create external receiver diagnostics representation.
            /// </summary>
            /// <param name="receiverDiagnosticsInternal">Internal pipeline element diagnostics representation.</param>
            /// <returns>External pipeline element diagnostics representation.</returns>
            public ReceiverDiagnostics GetOrCreateReceiverDiagnostics(PipelineDiagnosticsInternal.ReceiverDiagnostics receiverDiagnosticsInternal)
            {
                return this.GetOrCreate(this.Receivers, receiverDiagnosticsInternal, r => r.Id, (r, c) => new ReceiverDiagnostics(r, c));
            }

            private TExternal GetOrCreate<TExternal, TInternal>(ConcurrentDictionary<int, TExternal> dict, TInternal intern, Func<TInternal, int> getId, Func<TInternal, Builder, TExternal> ctor)
            {
                var id = getId(intern);
                if (!dict.TryGetValue(id, out TExternal ext))
                {
                    ext = ctor(intern, this);
                    dict.TryAdd(id, ext);
                }

                return ext;
            }
        }
    }
}
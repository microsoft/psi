// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.

namespace Microsoft.Psi.Diagnostics
{
    using System;
    using System.Collections.Generic;
    using Microsoft.Psi.Executive;

    /// <summary>
    /// Represents diagnostic information about a pipeline.
    /// </summary>
    public class PipelineDiagnostics
    {
        private const int MaxProcessingHistory = 10;
        private const int MaxReceiverLatencyHistory = 10;
        private const int MaxMessageSizeHistory = 10;

        /// <summary>
        /// Initializes a new instance of the <see cref="PipelineDiagnostics"/> class.
        /// </summary>
        /// <param name="id">Pipeline ID.</param>
        /// <param name="name">Pipeline name.</param>
        public PipelineDiagnostics(int id, string name)
        {
            this.Id = id;
            this.Name = name;
            this.PipelineElements = new Dictionary<int, PipelineElementDiagnostics>();
            this.Subpipelines = new Dictionary<int, PipelineDiagnostics>();
        }

        /// <summary>
        /// Gets pipeline ID.
        /// </summary>
        public int Id { get; }

        /// <summary>
        /// Gets pipeline name.
        /// </summary>
        public string Name { get; }

        /// <summary>
        /// Gets a value indicating whether the pipeline is running (after started, before stopped).
        /// </summary>
        public bool IsPipelineRunning { get; internal set; }

        /// <summary>
        /// Gets elements in this pipeline.
        /// </summary>
        public IDictionary<int, PipelineElementDiagnostics> PipelineElements { get; private set; }

        /// <summary>
        /// Gets subpipelines of this pipeline.
        /// </summary>
        public IDictionary<int, PipelineDiagnostics> Subpipelines { get; private set; }

        /// <summary>
        /// Represents diagnostic information about a pipeline element.
        /// </summary>
        public class PipelineElementDiagnostics
        {
            /// <summary>
            /// Initializes a new instance of the <see cref="PipelineElementDiagnostics"/> class.
            /// </summary>
            /// <param name="id">Pipeline element ID.</param>
            /// <param name="name">Pipeline element name.</param>
            /// <param name="kind">Pipeline element kind.</param>
            /// <param name="parentPipeline">Pipeline to which this element belongs.</param>
            public PipelineElementDiagnostics(int id, string name, PipelineElementKind kind, PipelineDiagnostics parentPipeline)
            {
                this.Id = id;
                this.Name = name;
                this.Kind = kind;
                this.ParentPipeline = parentPipeline;
                this.Emitters = new Dictionary<int, EmitterDiagnostics>();
                this.Receivers = new Dictionary<int, ReceiverDiagnostics>();
            }

            /// <summary>
            /// Initializes a new instance of the <see cref="PipelineElementDiagnostics"/> class.
            /// </summary>
            /// <param name="element">Pipeline element which this diagnostic information represents.</param>
            /// <param name="pipeline">Pipeline to which this pipeline element belongs.</param>
            internal PipelineElementDiagnostics(PipelineElement element, PipelineDiagnostics pipeline)
                : this(element.Id, element.Name, element.IsConnector ? PipelineElementKind.Connector : element.StateObject is Subpipeline ? PipelineElementKind.Subpipeline : element.IsSource ? PipelineElementKind.Source : PipelineElementKind.Reactive, pipeline)
            {
            }

            /// <summary>
            /// Pipeline element kind.
            /// </summary>
            public enum PipelineElementKind
            {
                /// <summary>
                /// Represents a source component.
                /// </summary>
                Source,

                /// <summary>
                /// Represents a purely reactive component.
                /// </summary>
                Reactive,

                /// <summary>
                /// Represents a Connector component.
                /// </summary>
                Connector,

                /// <summary>
                /// Represents a Subpipeline component.
                /// </summary>
                Subpipeline,
            }

            /// <summary>
            /// Gets pipeline element ID.
            /// </summary>
            public int Id { get; internal set; }

            /// <summary>
            /// Gets pipeline element name.
            /// </summary>
            public string Name { get; internal set; }

            /// <summary>
            /// Gets pipeline element kind.
            /// </summary>
            public PipelineElementKind Kind { get; internal set; }

            /// <summary>
            /// Gets a value indicating whether the pipeline element is running (after started, before stopped).
            /// </summary>
            public bool IsRunning { get; internal set; }

            /// <summary>
            /// Gets a value indicating whether the pipeline element is finalized.
            /// </summary>
            public bool Finalized { get; internal set; }

            /// <summary>
            /// Gets pipeline element emitters.
            /// </summary>
            public IDictionary<int, EmitterDiagnostics> Emitters { get; private set; }

            /// <summary>
            /// Gets pipeline element receivers.
            /// </summary>
            public IDictionary<int, ReceiverDiagnostics> Receivers { get; private set; }

            /// <summary>
            /// Gets pipeline to which this element belongs.
            /// </summary>
            public PipelineDiagnostics ParentPipeline { get; }

            /// <summary>
            /// Gets or sets pipeline which this element represents (e.g. Subpipeline).
            /// </summary>
            /// <remarks>This is used when a pipeline element is a pipeline (e.g. Subpipeline).</remarks>
            public PipelineDiagnostics RepresentsSubpipeline { get; set; }

            /// <summary>
            /// Gets or sets bridge to pipeline element in another pipeline (e.g. Connectors).
            /// </summary>
            public PipelineElementDiagnostics ConnectorBridgeToPipelineElement { get; set; }
        }

        /// <summary>
        /// Represents diagnostic information about a pipeline element receiver.
        /// </summary>
        public class ReceiverDiagnostics
        {
            /// <summary>
            /// Initializes a new instance of the <see cref="ReceiverDiagnostics"/> class.
            /// </summary>
            /// <param name="id">Receiver ID.</param>
            /// <param name="name">Receiver name.</param>
            /// <param name="type">Receiver type.</param>
            /// <param name="pipelineElement">Pipeline element to which receiver belongs.</param>
            public ReceiverDiagnostics(int id, string name, string type, PipelineElementDiagnostics pipelineElement)
            {
                this.Id = id;
                this.Name = name;
                this.Type = type;
                this.PipelineElement = pipelineElement;
                this.MessageLatencyAtEmitterHistory = new Queue<TimeSpan>();
                this.MessageLatencyAtReceiverHistory = new Queue<TimeSpan>();
                this.ProcessingTimeHistory = new Queue<TimeSpan>();
                this.MessageSizeHistory = new Queue<int>();
            }

            /// <summary>
            /// Gets receiver ID.
            /// </summary>
            public int Id { get; }

            /// <summary>
            /// Gets receiver name.
            /// </summary>
            public string Name { get; }

            /// <summary>
            /// Gets receiver type.
            /// </summary>
            public string Type { get; }

            /// <summary>
            /// Gets pipeline element to which emitter belongs.
            /// </summary>
            public PipelineElementDiagnostics PipelineElement { get; }

            /// <summary>
            /// Gets receiver's source emitter.
            /// </summary>
            public EmitterDiagnostics Source { get; internal set; }

            /// <summary>
            /// Gets a value indicating whether receiver is throttled.
            /// </summary>
            public bool Throttled { get; internal set; }

            /// <summary>
            /// Gets current awaiting delivery queue size.
            /// </summary>
            public int QueueSize { get; internal set; }

            /// <summary>
            /// Gets total count of dropped messages.
            /// </summary>
            public int ProcessedCount { get; internal set; }

            /// <summary>
            /// Gets total count of dropped messages.
            /// </summary>
            public int DroppedCount { get; internal set; }

            /// <summary>
            /// Gets history of message latency at emitter (when queued/dropped) over past n-messages.
            /// </summary>
            public IEnumerable<TimeSpan> MessageLatencyAtEmitterHistory { get; internal set; }

            /// <summary>
            /// Gets history of message latency at receiver (when delivered/processed) over past n-messages.
            /// </summary>
            public IEnumerable<TimeSpan> MessageLatencyAtReceiverHistory { get; internal set; }

            /// <summary>
            /// Gets component processing time over the past n-messages.
            /// </summary>
            public IEnumerable<TimeSpan> ProcessingTimeHistory { get; private set; }

            /// <summary>
            /// Gets message size history over the past n-messages.
            /// </summary>
            public IEnumerable<int> MessageSizeHistory { get; private set; }

            /// <summary>
            /// Add message latency at emitter (when queued/dropped) to pipeline element statistics.
            /// </summary>
            /// <param name="envelope">Message envelope.</param>
            internal void AddMessageLatencyAtEmitter(Envelope envelope)
            {
                this.AddMessageLatency(envelope, this.MessageLatencyAtEmitterHistory as Queue<TimeSpan>);
            }

            /// <summary>
            /// Add message latency at receiver (when delivered/processed) to pipeline element statistics.
            /// </summary>
            /// <param name="envelope">Message envelope.</param>
            internal void AddMessageLatencyAtReceiver(Envelope envelope)
            {
                this.AddMessageLatency(envelope, this.MessageLatencyAtReceiverHistory as Queue<TimeSpan>);
            }

            /// <summary>
            /// Add message processing time to pipeline element statistics.
            /// </summary>
            /// <param name="time">Time spent processing message.</param>
            internal void AddProcessingTime(TimeSpan time)
            {
                var queue = this.ProcessingTimeHistory as Queue<TimeSpan>;
                queue.Enqueue(time);
                while (queue.Count > PipelineDiagnostics.MaxProcessingHistory)
                {
                    queue.Dequeue();
                }
            }

            /// <summary>
            /// Add message size to pipeline element statistics.
            /// </summary>
            /// <param name="size">Message size (bytes).</param>
            internal void AddMessageSize(int size)
            {
                var queue = this.MessageSizeHistory as Queue<int>;
                queue.Enqueue(size);
                while (queue.Count > PipelineDiagnostics.MaxMessageSizeHistory)
                {
                    queue.Dequeue();
                }
            }

            private void AddMessageLatency(Envelope envelope, Queue<TimeSpan> queue)
            {
                queue.Enqueue(envelope.Time - envelope.OriginatingTime);
                while (queue.Count > PipelineDiagnostics.MaxReceiverLatencyHistory)
                {
                    queue.Dequeue();
                }
            }
        }

        /// <summary>
        /// Represents diagnostic information about a pipeline element emitter.
        /// </summary>
        public class EmitterDiagnostics
        {
            /// <summary>
            /// Initializes a new instance of the <see cref="EmitterDiagnostics"/> class.
            /// </summary>
            /// <param name="id">Emitter ID.</param>
            /// <param name="name">Emitter name.</param>
            /// <param name="type">Emitter type.</param>
            /// <param name="element">Pipeline element to which emitter belongs.</param>
            public EmitterDiagnostics(int id, string name, string type, PipelineElementDiagnostics element)
            {
                this.Id = id;
                this.Name = name;
                this.Type = type;
                this.PipelineElement = element;
                this.Targets = new Dictionary<int, ReceiverDiagnostics>();
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
            public PipelineElementDiagnostics PipelineElement { get; }

            /// <summary>
            /// Gets emitter target receivers.
            /// </summary>
            public IDictionary<int, ReceiverDiagnostics> Targets { get; private set; }
        }
    }
}
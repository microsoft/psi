// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.

namespace Microsoft.Psi.Diagnostics
{
    using System;
    using System.Collections.Concurrent;
    using Microsoft.Psi.Executive;

    /// <summary>
    /// Represents diagnostic information about a pipeline.
    /// </summary>
    /// <remarks>
    /// This is used while gathering live diagnostics information. It is optimized for lookups with Dictionaries and
    /// maintains latency, processing time, message size histories. This information is summarized before being posted
    /// as PipelineDiagnostics.
    /// </remarks>
    internal class PipelineDiagnosticsInternal
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="PipelineDiagnosticsInternal"/> class.
        /// </summary>
        /// <param name="id">Pipeline ID.</param>
        /// <param name="name">Pipeline name.</param>
        public PipelineDiagnosticsInternal(int id, string name)
        {
            this.Id = id;
            this.Name = name;
            this.PipelineElements = new ConcurrentDictionary<int, PipelineElementDiagnostics>();
            this.Subpipelines = new ConcurrentDictionary<int, PipelineDiagnosticsInternal>();
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
        /// Gets or sets a value indicating whether the pipeline is running (after started, before stopped).
        /// </summary>
        public bool IsPipelineRunning { get; internal set; }

        /// <summary>
        /// Gets elements in this pipeline.
        /// </summary>
        public ConcurrentDictionary<int, PipelineElementDiagnostics> PipelineElements { get; private set; }

        /// <summary>
        /// Gets subpipelines of this pipeline.
        /// </summary>
        public ConcurrentDictionary<int, PipelineDiagnosticsInternal> Subpipelines { get; private set; }

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
            /// <param name="typeName">Component type name.</param>
            /// <param name="kind">Pipeline element kind.</param>
            /// <param name="pipelineId">ID of Pipeline to which this element belongs.</param>
            public PipelineElementDiagnostics(int id, string name, string typeName, PipelineElementKind kind, int pipelineId)
            {
                this.Id = id;
                this.Name = name;
                this.TypeName = typeName;
                this.Kind = kind;
                this.PipelineId = pipelineId;
                this.Emitters = new ConcurrentDictionary<int, EmitterDiagnostics>();
                this.Receivers = new ConcurrentDictionary<int, ReceiverDiagnostics>();
            }

            /// <summary>
            /// Initializes a new instance of the <see cref="PipelineElementDiagnostics"/> class.
            /// </summary>
            /// <param name="element">Pipeline element which this diagnostic information represents.</param>
            /// <param name="pipelineId">ID of Pipeline to which this element belongs.</param>
            internal PipelineElementDiagnostics(PipelineElement element, int pipelineId)
                : this(element.Id, element.StateObject.ToString(), element.StateObject.GetType().Name, element.IsConnector ? PipelineElementKind.Connector : element.StateObject is Subpipeline ? PipelineElementKind.Subpipeline : element.IsSource ? PipelineElementKind.Source : PipelineElementKind.Reactive, pipelineId)
            {
            }

            /// <summary>
            /// Gets pipeline element ID.
            /// </summary>
            public int Id { get; private set; }

            /// <summary>
            /// Gets pipeline element name.
            /// </summary>
            public string Name { get; private set; }

            /// <summary>
            /// Gets pipeline element component type name.
            /// </summary>
            public string TypeName { get; private set; }

            /// <summary>
            /// Gets pipeline element kind.
            /// </summary>
            public PipelineElementKind Kind { get; private set; }

            /// <summary>
            /// Gets or sets a value indicating whether the pipeline element is running (after started, before stopped).
            /// </summary>
            public bool IsRunning { get; internal set; }

            /// <summary>
            /// Gets or sets a value indicating whether the pipeline element is finalized.
            /// </summary>
            public bool Finalized { get; internal set; }

            /// <summary>
            /// Gets pipeline element emitters.
            /// </summary>
            public ConcurrentDictionary<int, EmitterDiagnostics> Emitters { get; private set; }

            /// <summary>
            /// Gets pipeline element receivers.
            /// </summary>
            public ConcurrentDictionary<int, ReceiverDiagnostics> Receivers { get; private set; }

            /// <summary>
            /// Gets ID of pipeline to which this element belongs.
            /// </summary>
            public int PipelineId { get; private set; }

            /// <summary>
            /// Gets or sets pipeline which this element represents (e.g. Subpipeline).
            /// </summary>
            /// <remarks>This is used when a pipeline element is a pipeline (e.g. Subpipeline).</remarks>
            public PipelineDiagnosticsInternal RepresentsSubpipeline { get; set; }

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
            /// <param name="receiverName">Receiver name.</param>
            /// <param name="typeName">Receiver type.</param>
            /// <param name="pipelineElement">Pipeline element to which receiver belongs.</param>
            public ReceiverDiagnostics(int id, string receiverName, string typeName, PipelineElementDiagnostics pipelineElement)
            {
                this.Id = id;
                this.ReceiverName = receiverName;
                this.TypeName = typeName;
                this.PipelineElement = pipelineElement;
                this.MessageLatencyAtEmitterHistory = new ConcurrentQueue<(TimeSpan, DateTime)>();
                this.MessageLatencyAtReceiverHistory = new ConcurrentQueue<(TimeSpan, DateTime)>();
                this.ProcessingTimeHistory = new ConcurrentQueue<(TimeSpan, DateTime)>();
                this.MessageSizeHistory = new ConcurrentQueue<(int, DateTime)>();
                this.DroppedHistory = new ConcurrentQueue<(int, DateTime)>();
                this.ProcessedHistory = new ConcurrentQueue<(int, DateTime)>();
                this.QueueSizeHistory = new ConcurrentQueue<(int, DateTime)>();
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
            /// Gets or sets delivery policy name.
            /// </summary>
            public string DeliveryPolicyName { get; set; }

            /// <summary>
            /// Gets receiver type.
            /// </summary>
            public string TypeName { get; }

            /// <summary>
            /// Gets pipeline element to which emitter belongs.
            /// </summary>
            public PipelineElementDiagnostics PipelineElement { get; }

            /// <summary>
            /// Gets or sets receiver's source emitter.
            /// </summary>
            public EmitterDiagnostics Source { get; internal set; }

            /// <summary>
            /// Gets or sets a value indicating whether receiver is throttled.
            /// </summary>
            public bool Throttled { get; internal set; }

            /// <summary>
            /// Gets or sets total count of dropped messages.
            /// </summary>
            public int ProcessedCount { get; internal set; }

            /// <summary>
            /// Gets or sets total count of dropped messages.
            /// </summary>
            public int DroppedCount { get; internal set; }

            /// <summary>
            /// Gets or sets history of processed messages.
            /// </summary>
            public ConcurrentQueue<(int, DateTime)> ProcessedHistory { get; internal set; }

            /// <summary>
            /// Gets or sets history of dropped messages.
            /// </summary>
            public ConcurrentQueue<(int, DateTime)> DroppedHistory { get; internal set; }

            /// <summary>
            /// Gets or sets history of awaiting delivery queue size.
            /// </summary>
            public ConcurrentQueue<(int, DateTime)> QueueSizeHistory { get; internal set; }

            /// <summary>
            /// Gets or sets history of message latency at emitter (when queued/dropped) over past averaging time window.
            /// </summary>
            public ConcurrentQueue<(TimeSpan, DateTime)> MessageLatencyAtEmitterHistory { get; internal set; }

            /// <summary>
            /// Gets or sets history of message latency at receiver (when delivered/processed) over past averaging time window.
            /// </summary>
            public ConcurrentQueue<(TimeSpan, DateTime)> MessageLatencyAtReceiverHistory { get; internal set; }

            /// <summary>
            /// Gets component processing time over the past averaging time window.
            /// </summary>
            public ConcurrentQueue<(TimeSpan, DateTime)> ProcessingTimeHistory { get; private set; }

            /// <summary>
            /// Gets message size history over the past averaging time window (if TrackMessageSize configured).
            /// </summary>
            public ConcurrentQueue<(int, DateTime)> MessageSizeHistory { get; private set; }

            /// <summary>
            /// Add processed message time to pipeline element statistics.
            /// </summary>
            /// <param name="diagnosticsTime">Time at which to record the diagnostic information.</param>
            /// <param name="averagingWindow">Window in which to compute averages.</param>
            internal void AddProcessedMessage(DateTime diagnosticsTime, TimeSpan averagingWindow)
            {
                this.AddStatistic(++this.ProcessedCount, this.ProcessedHistory, diagnosticsTime, averagingWindow);
            }

            /// <summary>
            /// Add dropped message time to pipeline element statistics.
            /// </summary>
            /// <param name="diagnosticsTime">Time at which to record the diagnostic information.</param>
            /// <param name="averagingWindow">Window in which to compute averages.</param>
            internal void AddDroppedMessage(DateTime diagnosticsTime, TimeSpan averagingWindow)
            {
                this.AddStatistic(++this.DroppedCount, this.DroppedHistory, diagnosticsTime, averagingWindow);
            }

            /// <summary>
            /// Add current delivery queue size to pipeline element statistics.
            /// </summary>
            /// <param name="size">Current delivery queue size.</param>
            /// <param name="diagnosticsTime">Time at which to record the diagnostic information.</param>
            /// <param name="averagingWindow">Window in which to compute averages.</param>
            internal void AddCurrentQueueSize(int size, DateTime diagnosticsTime, TimeSpan averagingWindow)
            {
                this.AddStatistic(size, this.QueueSizeHistory, diagnosticsTime, averagingWindow);
            }

            /// <summary>
            /// Add message latency at emitter (when queued/dropped) to pipeline element statistics.
            /// </summary>
            /// <param name="latency">The message latency.</param>
            /// <param name="diagnosticsTime">Time at which to record the diagnostic information.</param>
            /// <param name="averagingWindow">Window in which to compute averages.</param>
            internal void AddMessageLatencyAtEmitter(TimeSpan latency, DateTime diagnosticsTime, TimeSpan averagingWindow)
            {
                this.AddStatistic(latency, this.MessageLatencyAtEmitterHistory, diagnosticsTime, averagingWindow);
            }

            /// <summary>
            /// Add message latency at receiver (when delivered/processed) to pipeline element statistics.
            /// </summary>
            /// <param name="latency">The message latency.</param>
            /// <param name="diagnosticsTime">Time at which to record the diagnostic information.</param>
            /// <param name="averagingWindow">Window in which to compute averages.</param>
            internal void AddMessageLatencyAtReceiver(TimeSpan latency, DateTime diagnosticsTime, TimeSpan averagingWindow)
            {
                this.AddStatistic(latency, this.MessageLatencyAtReceiverHistory, diagnosticsTime, averagingWindow);
            }

            /// <summary>
            /// Add message processing time to pipeline element statistics.
            /// </summary>
            /// <param name="processingTime">Time spent processing message.</param>
            /// <param name="diagnosticsTime">Time at which to record the diagnostic information.</param>
            /// <param name="averagingWindow">Window in which to compute averages.</param>
            internal void AddProcessingTime(TimeSpan processingTime, DateTime diagnosticsTime, TimeSpan averagingWindow)
            {
                this.AddStatistic(processingTime, this.ProcessingTimeHistory, diagnosticsTime, averagingWindow);
            }

            /// <summary>
            /// Add message size to pipeline element statistics.
            /// </summary>
            /// <param name="size">Message size (bytes).</param>
            /// <param name="diagnosticsTime">Time at which to record the diagnostic information.</param>
            /// <param name="averagingWindow">Window in which to compute averages.</param>
            internal void AddMessageSize(int size, DateTime diagnosticsTime, TimeSpan averagingWindow)
            {
                this.AddStatistic(size, this.MessageSizeHistory, diagnosticsTime, averagingWindow);
            }

            private void AddStatistic<T>(T val, ConcurrentQueue<(T, DateTime)> queue, DateTime current, TimeSpan averagingWindow)
            {
                queue.Enqueue((val, current));
                var cutoff = current - averagingWindow;
                while (queue.TryPeek(out var result) && result.Item2 < cutoff)
                {
                    queue.TryDequeue(out var _);
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
                this.Targets = new ConcurrentDictionary<int, ReceiverDiagnostics>();
            }

            /// <summary>
            /// Gets emitter ID.
            /// </summary>
            public int Id { get; }

            /// <summary>
            /// Gets or sets emitter name.
            /// </summary>
            public string Name { get; set; }

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
            public ConcurrentDictionary<int, ReceiverDiagnostics> Targets { get; private set; }
        }
    }
}
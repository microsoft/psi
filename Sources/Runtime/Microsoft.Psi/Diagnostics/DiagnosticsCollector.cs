// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.

namespace Microsoft.Psi.Diagnostics
{
    using System;
    using System.Collections.Concurrent;
    using Microsoft.Psi.Executive;

    /// <summary>
    /// Class that collects diagnostics information from a running pipeline; including graph structure changes and message flow statistics.
    /// </summary>
    internal class DiagnosticsCollector
    {
        private readonly DiagnosticsConfiguration diagnosticsConfig;

        private readonly ConcurrentDictionary<int, PipelineDiagnosticsInternal> graphs = new ConcurrentDictionary<int, PipelineDiagnosticsInternal>();
        private readonly ConcurrentDictionary<int, PipelineDiagnosticsInternal.EmitterDiagnostics> outputs = new ConcurrentDictionary<int, PipelineDiagnosticsInternal.EmitterDiagnostics>();
        private readonly ConcurrentDictionary<object, PipelineDiagnosticsInternal.PipelineElementDiagnostics> connectors = new ConcurrentDictionary<object, PipelineDiagnosticsInternal.PipelineElementDiagnostics>();

        public DiagnosticsCollector(DiagnosticsConfiguration diagnosticsConfig)
        {
            this.diagnosticsConfig = diagnosticsConfig ?? DiagnosticsConfiguration.Default;
        }

        /// <summary>
        /// Gets current root graph (if any).
        /// </summary>
        public PipelineDiagnosticsInternal CurrentRoot { get; private set; }

        /// <summary>
        /// Pipeline creation.
        /// </summary>
        /// <remarks>Called upon pipeline construction.</remarks>
        /// <param name="pipeline">Pipeline being created.</param>
        public void PipelineCreate(Pipeline pipeline)
        {
            var graph = new PipelineDiagnosticsInternal(pipeline.Id, pipeline.Name);
            if (!this.graphs.TryAdd(pipeline.Id, graph))
            {
                throw new InvalidOperationException("Failed to add created graph");
            }

            if (this.CurrentRoot == null && !(pipeline is Subpipeline))
            {
                this.CurrentRoot = graph;
            }
        }

        /// <summary>
        /// Pipeline start.
        /// </summary>
        /// <remarks>Called upon pipeline run, before child components started.</remarks>
        /// <param name="pipeline">Pipeline being started.</param>
        public void PipelineStart(Pipeline pipeline)
        {
            this.graphs[pipeline.Id].IsPipelineRunning = true;
        }

        /// <summary>
        /// Pipeline stopped.
        /// </summary>
        /// <remarks>Called after child components finalized, before scheduler stopped.</remarks>
        /// <param name="pipeline">Pipeline being stopped.</param>
        public void PipelineStopped(Pipeline pipeline)
        {
            this.graphs[pipeline.Id].IsPipelineRunning = false;
        }

        /// <summary>
        /// Pipeline disposal.
        /// </summary>
        /// <remarks>Called after pipeline disposal.</remarks>
        /// <param name="pipeline">Pipeline being disposed.</param>
        public void PipelineDisposed(Pipeline pipeline)
        {
            if (!this.graphs.TryRemove(pipeline.Id, out _))
            {
                throw new InvalidOperationException("Failed to remove disposed graph");
            }
        }

        /// <summary>
        /// Element (representing component) created.
        /// </summary>
        /// <remarks>Called upon element construction (first moment component becomes a pipeline element).</remarks>
        /// <param name="pipeline">The pipeline to add the component to.</param>
        /// <param name="element">Element being created.</param>
        /// <param name="component">Component associated with this pipeline element.</param>
        public void PipelineElementCreate(Pipeline pipeline, PipelineElement element, object component)
        {
            var node = new PipelineDiagnosticsInternal.PipelineElementDiagnostics(element, pipeline.Id);
            if (node.Kind == PipelineElementKind.Subpipeline)
            {
                node.RepresentsSubpipeline = this.graphs[((Pipeline)component).Id];
                this.graphs[pipeline.Id].Subpipelines.TryAdd(node.RepresentsSubpipeline.Id, node.RepresentsSubpipeline);
            }
            else if (node.Kind == PipelineElementKind.Connector)
            {
                if (this.connectors.TryGetValue(component, out PipelineDiagnosticsInternal.PipelineElementDiagnostics bridge))
                {
                    node.ConnectorBridgeToPipelineElement = bridge;
                    bridge.ConnectorBridgeToPipelineElement = node;
                }
                else
                {
                    if (!this.connectors.TryAdd(component, node))
                    {
                        throw new InvalidOperationException("Failed to add connector");
                    }
                }
            }

            this.graphs[pipeline.Id].PipelineElements.TryAdd(element.Id, node);
        }

        /// <summary>
        /// Element (representing component) being started.
        /// </summary>
        /// <remarks>Called after scheduling calls to start handler.</remarks>
        /// <param name="pipeline">The pipeline to add the component to.</param>
        /// <param name="element">Element being started.</param>
        public void PipelineElementStart(Pipeline pipeline, PipelineElement element)
        {
            this.graphs[pipeline.Id].PipelineElements[element.Id].IsRunning = true;
            this.graphs[pipeline.Id].PipelineElements[element.Id].DiagnosticState = element.StateObject.ToString();
        }

        /// <summary>
        /// Element (representing component) being stopped.
        /// </summary>
        /// <remarks>Called after scheduling calls to stop handler.</remarks>
        /// <param name="pipeline">The pipeline to add the component to.</param>
        /// <param name="element">Element being stopped.</param>
        public void PipelineElementStop(Pipeline pipeline, PipelineElement element)
        {
            this.graphs[pipeline.Id].PipelineElements[element.Id].IsRunning = false;
            this.graphs[pipeline.Id].PipelineElements[element.Id].DiagnosticState = element.StateObject.ToString();
        }

        /// <summary>
        /// Element (representing component) being finalized.
        /// </summary>
        /// <remarks>Called after scheduling calls to final handler.</remarks>
        /// <param name="pipeline">The pipeline to add the component to.</param>
        /// <param name="element">Element being finalized.</param>
        public void PipelineElementFinal(Pipeline pipeline, PipelineElement element)
        {
            this.graphs[pipeline.Id].PipelineElements[element.Id].Finalized = true;
        }

        /// <summary>
        /// Element (representing component) created.
        /// </summary>
        /// <remarks>Called upon element disposal.</remarks>
        /// <param name="pipeline">The pipeline to add the component to.</param>
        /// <param name="element">Element being created.</param>
        public void PipelineElementDisposed(Pipeline pipeline, PipelineElement element)
        {
            this.graphs[pipeline.Id].PipelineElements.TryRemove(element.Id, out var _);
        }

        /// <summary>
        /// Output (emitter) added to element.
        /// </summary>
        /// <remarks>Called just after element start (or dynamically if added once pipeline running).</remarks>
        /// <param name="pipeline">The pipeline to add the component to.</param>
        /// <param name="element">Element to which emitter is being added.</param>
        /// <param name="emitter">Emitter being added.</param>
        public void PipelineElementAddEmitter(Pipeline pipeline, PipelineElement element, IEmitter emitter)
        {
            var node = this.graphs[pipeline.Id].PipelineElements[element.Id];
            var output = new PipelineDiagnosticsInternal.EmitterDiagnostics(emitter.Id, emitter.Name, emitter.Type.FullName, node);
            node.Emitters.TryAdd(output.Id, output);
            if (!this.outputs.TryAdd(output.Id, output))
            {
                throw new InvalidOperationException("Failed to add emitter/output");
            }
        }

        /// <summary>
        /// Emitter had been renamed.
        /// </summary>
        /// <remarks>Called when IEmitter.Name property set post-construction.</remarks>
        /// <param name="emitter">Emitter being renamed.</param>
        public void EmitterRenamed(IEmitter emitter)
        {
            this.outputs[emitter.Id].Name = emitter.Name;
        }

        /// <summary>
        /// Input (receiver) added to element.
        /// </summary>
        /// <remarks>Called just after element start (or dynamically if added once pipeline running).</remarks>
        /// <param name="pipeline">The pipeline to add the component to.</param>
        /// <param name="element">Element to which receiver is being added.</param>
        /// <param name="receiver">Receiver being added.</param>
        public void PipelineElementAddReceiver(Pipeline pipeline, PipelineElement element, IReceiver receiver)
        {
            var node = this.graphs[pipeline.Id].PipelineElements[element.Id];
            node.Receivers.TryAdd(receiver.Id, new PipelineDiagnosticsInternal.ReceiverDiagnostics(receiver.Id, receiver.Name, receiver.Type.FullName, node));
        }

        /// <summary>
        /// Input subscribed to input.
        /// </summary>
        /// <remarks>Called just after element start (or dynamically if subscribed once pipeline running).</remarks>
        /// <param name="pipeline">The pipeline to add the component to.</param>
        /// <param name="element">Element to which receiver belongs.</param>
        /// <param name="receiver">Receiver subscribing to emitter.</param>
        /// <param name="emitter">Emitter to which receiver is subscribing.</param>
        /// <param name="deliveryPolicyName">The name of the delivery policy used.</param>
        public void PipelineElementReceiverSubscribe(Pipeline pipeline, PipelineElement element, IReceiver receiver, IEmitter emitter, string deliveryPolicyName)
        {
            var input = this.graphs[pipeline.Id].PipelineElements[element.Id].Receivers[receiver.Id];
            var output = this.outputs[emitter.Id];
            input.Source = output;
            input.DeliveryPolicyName = deliveryPolicyName;
            output.Targets.TryAdd(input.Id, input);
        }

        /// <summary>
        /// Input unsubscribed to input.
        /// </summary>
        /// <remarks>Called upon unsubscribe (only if pipeline running).</remarks>
        /// <param name="pipeline">The pipeline to add the component to.</param>
        /// <param name="element">Element to which receiver belongs.</param>
        /// <param name="receiver">Receiver unsubscribing to emitter.</param>
        /// <param name="emitter">Emitter from which receiver is unsubscribing.</param>
        public void PipelineElementReceiverUnsubscribe(Pipeline pipeline, PipelineElement element, IReceiver receiver, IEmitter emitter)
        {
            this.outputs[emitter.Id].Targets.TryRemove(receiver.Id, out var _);
            this.graphs[pipeline.Id].PipelineElements[element.Id].Receivers[receiver.Id].Source = null;
        }

        /// <summary>
        /// Get collector of diagnostics message flow statistics for a single receiver.
        /// </summary>
        /// <param name="pipeline">The pipeline to add the component to.</param>
        /// <param name="element">Element to which receiver belongs.</param>
        /// <param name="receiver">Receiver having completed processing.</param>
        public ReceiverCollector GetReceiverDiagnosticsCollector(Pipeline pipeline, PipelineElement element, IReceiver receiver)
        {
            return new ReceiverCollector(this.graphs[pipeline.Id].PipelineElements[element.Id], receiver.Id, this.diagnosticsConfig);
        }

        /// <summary>
        /// Class that collects diagnostics message flow statistics for a single receiver.
        /// </summary>
        public class ReceiverCollector
        {
            private readonly PipelineDiagnosticsInternal.PipelineElementDiagnostics pipelineElementDiagnostics;
            private readonly PipelineDiagnosticsInternal.ReceiverDiagnostics receiverDiagnostics;
            private readonly DiagnosticsConfiguration diagnosticsConfig;

            /// <summary>
            /// Initializes a new instance of the <see cref="ReceiverCollector"/> class.
            /// </summary>
            /// <param name="pipelineElementDiagnostics">Pipeline element diagnostics instance associated with this receiver.</param>
            /// <param name="receiverId">The id for the receiver to collect diagnostics about.</param>
            /// <param name="diagnosticsConfig">Diagnostics configuration.</param>
            internal ReceiverCollector(
                PipelineDiagnosticsInternal.PipelineElementDiagnostics pipelineElementDiagnostics,
                int receiverId,
                DiagnosticsConfiguration diagnosticsConfig)
            {
                this.pipelineElementDiagnostics = pipelineElementDiagnostics;
                this.receiverDiagnostics = this.pipelineElementDiagnostics.Receivers[receiverId];
                this.diagnosticsConfig = diagnosticsConfig;
            }

            /// <summary>
            /// Update of the pipeline element diagnostic state.
            /// </summary>
            /// <param name="diagnosticState">The new diagnostic state.</param>
            public void UpdateDiagnosticState(string diagnosticState)
            {
                this.pipelineElementDiagnostics.DiagnosticState = diagnosticState;
            }

            /// <summary>
            /// Message was emitted towards a receiver.
            /// </summary>
            /// <param name="envelope">Message envelope.</param>
            /// <param name="diagnosticsTime">Time at which to record the diagnostic information.</param>
            public void MessageEmitted(Envelope envelope, DateTime diagnosticsTime)
            {
                this.receiverDiagnostics.AddMessageEmitted(diagnosticsTime, this.diagnosticsConfig.AveragingTimeSpan);
                this.receiverDiagnostics.AddMessageCreatedLatency(envelope.CreationTime - envelope.OriginatingTime, diagnosticsTime, this.diagnosticsConfig.AveragingTimeSpan);
                this.receiverDiagnostics.AddMessageEmittedLatency(diagnosticsTime - envelope.OriginatingTime, diagnosticsTime, this.diagnosticsConfig.AveragingTimeSpan);
            }

            /// <summary>
            /// Capture a queue size update.
            /// </summary>
            /// <param name="queueSize">Awaiting delivery queue size.</param>
            /// <param name="diagnosticsTime">Time at which to record the diagnostic information.</param>
            public void QueueSizeUpdate(int queueSize, DateTime diagnosticsTime)
            {
                this.receiverDiagnostics.AddDeliveryQueueSize(queueSize, diagnosticsTime, this.diagnosticsConfig.AveragingTimeSpan);
            }

            /// <summary>
            /// Message was dropped by receiver.
            /// </summary>
            /// <param name="diagnosticsTime">Time at which to record the diagnostic information.</param>
            public void MessageDropped(DateTime diagnosticsTime)
            {
                this.receiverDiagnostics.AddMessageDropped(diagnosticsTime, this.diagnosticsConfig.AveragingTimeSpan);
            }

            /// <summary>
            /// Capture throttle status update.
            /// </summary>
            /// <param name="receiverIsThrottled">Whether input is throttled.</param>
            public void PipelineElementReceiverThrottle(bool receiverIsThrottled)
            {
                this.receiverDiagnostics.ReceiverIsThrottled = receiverIsThrottled;
            }

            /// <summary>
            /// Message was processed by component.
            /// </summary>
            /// <param name="envelope">Message envelope.</param>
            /// <param name="receiverStartTime">The time the runtime started executing the receiver for the message.</param>
            /// <param name="receiverEndTime">The time the runtime finished executing the receiver for the message.</param>
            /// <param name="messageSize">Message size (bytes).</param>
            /// <param name="diagnosticsTime">Time at which to record the diagnostic information.</param>
            public void MessageProcessed(Envelope envelope, DateTime receiverStartTime, DateTime receiverEndTime, int messageSize, DateTime diagnosticsTime)
            {
                this.receiverDiagnostics.AddMessageProcessed(diagnosticsTime, this.diagnosticsConfig.AveragingTimeSpan);
                this.receiverDiagnostics.AddMessageSize(messageSize, diagnosticsTime, this.diagnosticsConfig.AveragingTimeSpan);
                this.receiverDiagnostics.AddMessageReceivedLatency(receiverStartTime - envelope.OriginatingTime, diagnosticsTime, this.diagnosticsConfig.AveragingTimeSpan);
                this.receiverDiagnostics.AddMessageProcessTime(receiverEndTime - receiverStartTime, diagnosticsTime, this.diagnosticsConfig.AveragingTimeSpan);
            }
        }
    }
}
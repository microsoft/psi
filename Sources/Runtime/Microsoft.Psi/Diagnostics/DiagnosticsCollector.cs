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
        private ConcurrentDictionary<int, PipelineDiagnostics> graphs = new ConcurrentDictionary<int, PipelineDiagnostics>();
        private ConcurrentDictionary<int, PipelineDiagnostics.EmitterDiagnostics> outputs = new ConcurrentDictionary<int, PipelineDiagnostics.EmitterDiagnostics>();
        private ConcurrentDictionary<object, PipelineDiagnostics.PipelineElementDiagnostics> connectors = new ConcurrentDictionary<object, PipelineDiagnostics.PipelineElementDiagnostics>();
        private ConcurrentDictionary<int, DateTime> receiverProcessingStart = new ConcurrentDictionary<int, DateTime>();

        /// <summary>
        /// Gets current root graph (if any).
        /// </summary>
        public PipelineDiagnostics CurrentRoot { get; private set; }

        /// <summary>
        /// Pipeline creation.
        /// </summary>
        /// <remarks>Called upon pipeline construction.</remarks>
        /// <param name="pipeline">Pipeline being created.</param>
        public void PipelineCreate(Pipeline pipeline)
        {
            var graph = new PipelineDiagnostics(pipeline.Id, pipeline.Name);
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
            this.graphs[pipeline.Id].IsPipelineRunning = true;
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
        /// <param name="pipeline">Pipeline to which the element belongs.</param>
        /// <param name="element">Element being created.</param>
        /// <param name="component">Component associated with this pipeline element.</param>
        public void PipelineElementCreate(Pipeline pipeline, PipelineElement element, object component)
        {
            var node = new PipelineDiagnostics.PipelineElementDiagnostics(element, this.graphs[pipeline.Id]);
            if (node.Kind == PipelineDiagnostics.PipelineElementDiagnostics.PipelineElementKind.Subpipeline)
            {
                node.RepresentsSubpipeline = this.graphs[((Pipeline)component).Id];
                this.graphs[pipeline.Id].Subpipelines.Add(node.RepresentsSubpipeline.Id, node.RepresentsSubpipeline);
            }
            else if (node.Kind == PipelineDiagnostics.PipelineElementDiagnostics.PipelineElementKind.Connector)
            {
                if (this.connectors.TryGetValue(component, out PipelineDiagnostics.PipelineElementDiagnostics bridge))
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

            this.graphs[pipeline.Id].PipelineElements.Add(element.Id, node);
        }

        /// <summary>
        /// Element (representing component) being started.
        /// </summary>
        /// <remarks>Called after scheduling calls to start handler.</remarks>
        /// <param name="pipeline">Pipeline to which the element belongs.</param>
        /// <param name="element">Element being started.</param>
        public void PipelineElementStart(Pipeline pipeline, PipelineElement element)
        {
            this.graphs[pipeline.Id].PipelineElements[element.Id].IsRunning = true;
        }

        /// <summary>
        /// Element (representing component) being stopped.
        /// </summary>
        /// <remarks>Called after scheduling calls to stop handler.</remarks>
        /// <param name="pipeline">Pipeline to which the element belongs.</param>
        /// <param name="element">Element being stopped.</param>
        public void PipelineElementStop(Pipeline pipeline, PipelineElement element)
        {
            this.graphs[pipeline.Id].PipelineElements[element.Id].IsRunning = false;
        }

        /// <summary>
        /// Element (representing component) being finalized.
        /// </summary>
        /// <remarks>Called after scheduling calls to final handler.</remarks>
        /// <param name="pipeline">Pipeline to which the element belongs.</param>
        /// <param name="element">Element being finalized.</param>
        public void PipelineElementFinal(Pipeline pipeline, PipelineElement element)
        {
            this.graphs[pipeline.Id].PipelineElements[element.Id].Finalized = true;
        }

        /// <summary>
        /// Element (representing component) created.
        /// </summary>
        /// <remarks>Called upon element disposal.</remarks>
        /// <param name="pipeline">Pipeline to which the element belongs.</param>
        /// <param name="element">Element being created.</param>
        public void PipelineElementDisposed(Pipeline pipeline, PipelineElement element)
        {
            this.graphs[pipeline.Id].PipelineElements.Remove(element.Id);
        }

        /// <summary>
        /// Output (emitter) added to element.
        /// </summary>
        /// <remarks>Called just after element start (or dynamically if added once pipeline running).</remarks>
        /// <param name="pipeline">Pipeline to which the element belongs.</param>
        /// <param name="element">Element to which emitter is being added.</param>
        /// <param name="emitter">Emitter being added.</param>
        public void PipelineElementAddEmitter(Pipeline pipeline, PipelineElement element, IEmitter emitter)
        {
            var node = this.graphs[pipeline.Id].PipelineElements[element.Id];
            var output = new PipelineDiagnostics.EmitterDiagnostics(emitter.Id, emitter.Name, emitter.Type.FullName, node);
            node.Emitters.Add(output.Id, output);
            if (!this.outputs.TryAdd(output.Id, output))
            {
                throw new InvalidOperationException("Failed to add emitter/output");
            }
        }

        /// <summary>
        /// Input (receiver) added to element.
        /// </summary>
        /// <remarks>Called just after element start (or dynamically if added once pipeline running).</remarks>
        /// <param name="pipeline">Pipeline to which the element belongs.</param>
        /// <param name="element">Element to which receiver is being added.</param>
        /// <param name="receiver">Receiver being added.</param>
        public void PipelineElementAddReceiver(Pipeline pipeline, PipelineElement element, IReceiver receiver)
        {
            var node = this.graphs[pipeline.Id].PipelineElements[element.Id];
            node.Receivers.Add(receiver.Id, new PipelineDiagnostics.ReceiverDiagnostics(receiver.Id, receiver.Name, receiver.Type.FullName, node));
        }

        /// <summary>
        /// Input subscribed to input.
        /// </summary>
        /// <remarks>Called just after element start (or dynamically if subscribed once pipeline running).</remarks>
        /// <param name="pipeline">Pipeline to which the element belongs.</param>
        /// <param name="element">Element to which receiver belongs.</param>
        /// <param name="receiver">Receiver subscribing to emitter.</param>
        /// <param name="emitter">Emitter to which receiver is subscribing.</param>
        public void PipelineElementReceiverSubscribe(Pipeline pipeline, PipelineElement element, IReceiver receiver, IEmitter emitter)
        {
            var input = this.graphs[pipeline.Id].PipelineElements[element.Id].Receivers[receiver.Id];
            var output = this.outputs[emitter.Id];
            input.Source = output;
            output.Targets.Add(input.Id, input);
        }

        /// <summary>
        /// Input unsubscribed to input.
        /// </summary>
        /// <remarks>Called upon unsubscribe (only if pipeline running).</remarks>
        /// <param name="pipeline">Pipeline to which the element belongs.</param>
        /// <param name="element">Element to which receiver belongs.</param>
        /// <param name="receiver">Receiver unsubscribing to emitter.</param>
        /// <param name="emitter">Emitter from which receiver is unsubscribing.</param>
        public void PipelineElementReceiverUnsubscribe(Pipeline pipeline, PipelineElement element, IReceiver receiver, IEmitter emitter)
        {
            this.graphs[pipeline.Id].PipelineElements[element.Id].Receivers[receiver.Id].Source = null;
            var targets = this.outputs[emitter.Id].Targets.Remove(receiver.Id);
        }

        /// <summary>
        /// Message enqueued by receiver.
        /// </summary>
        /// <param name="pipeline">Pipeline to which the element belongs.</param>
        /// <param name="element">Element to which receiver belongs.</param>
        /// <param name="receiver">Receiver being throttled/unthrottled.</param>
        /// <param name="throttled">Whether input is throttled.</param>
        public void PipelineElementReceiverThrottle(Pipeline pipeline, PipelineElement element, IReceiver receiver, bool throttled)
        {
            this.graphs[pipeline.Id].PipelineElements[element.Id].Receivers[receiver.Id].Throttled = throttled;
        }

        /// <summary>
        /// Message enqueued by receiver.
        /// </summary>
        /// <param name="pipeline">Pipeline to which the element belongs.</param>
        /// <param name="element">Element to which receiver belongs.</param>
        /// <param name="receiver">Receiver being enqueued.</param>
        /// <param name="queueSize">Awaiting delivery queue size.</param>
        /// <param name="envelope">Message envelope.</param>
        public void MessageEnqueued(Pipeline pipeline, PipelineElement element, IReceiver receiver, int queueSize, Envelope envelope)
        {
            var input = this.graphs[pipeline.Id].PipelineElements[element.Id].Receivers[receiver.Id];
            input.QueueSize = queueSize;
            input.AddMessageLatencyAtEmitter(envelope);
        }

        /// <summary>
        /// Message dropped by receiver.
        /// </summary>
        /// <param name="pipeline">Pipeline to which the element belongs.</param>
        /// <param name="element">Element to which receiver belongs.</param>
        /// <param name="receiver">Receiver being dropped.</param>
        /// <param name="queueSize">Awaiting delivery queue size.</param>
        /// <param name="envelope">Message envelope.</param>
        public void MessageDropped(Pipeline pipeline, PipelineElement element, IReceiver receiver, int queueSize, Envelope envelope)
        {
            var input = this.graphs[pipeline.Id].PipelineElements[element.Id].Receivers[receiver.Id];
            input.DroppedCount++;
            input.QueueSize = queueSize;
        }

        /// <summary>
        /// Message being processed by component.
        /// </summary>
        /// <param name="pipeline">Pipeline to which the element belongs.</param>
        /// <param name="element">Element to which receiver belongs.</param>
        /// <param name="receiver">Receiver being processed.</param>
        /// <param name="queueSize">Awaiting delivery queue size.</param>
        /// <param name="envelope">Message envelope.</param>
        /// <param name="messageSize">Message size (bytes).</param>
        public void MessageProcessStart(Pipeline pipeline, PipelineElement element, IReceiver receiver, int queueSize, Envelope envelope, int messageSize)
        {
            this.receiverProcessingStart[receiver.Id] = pipeline.GetCurrentTime();
            var input = this.graphs[pipeline.Id].PipelineElements[element.Id].Receivers[receiver.Id];
            input.ProcessedCount++;
            input.QueueSize = queueSize;
            input.AddMessageLatencyAtReceiver(envelope);
            input.AddMessageSize(messageSize);
        }

        /// <summary>
        /// Message processed by component.
        /// </summary>
        /// <param name="pipeline">Pipeline to which the element belongs.</param>
        /// <param name="element">Element to which receiver belongs.</param>
        /// <param name="receiver">Receiver having completed processing.</param>
        /// <param name="envelope">Message envelope.</param>
        public void MessageProcessComplete(Pipeline pipeline, PipelineElement element, IReceiver receiver, Envelope envelope)
        {
            this.graphs[pipeline.Id].PipelineElements[element.Id].Receivers[receiver.Id].AddProcessingTime(pipeline.GetCurrentTime() - this.receiverProcessingStart[receiver.Id]);
        }

        /// <summary>
        /// Message processed synchronously by receiver.
        /// </summary>
        /// <param name="pipeline">Pipeline to which the element belongs.</param>
        /// <param name="element">Element to which receiver belongs.</param>
        /// <param name="receiver">Receiver having been processed synchronously.</param>
        /// <param name="queueSize">Awaiting delivery queue size.</param>
        /// <param name="envelope">Message envelope.</param>
        /// <param name="messageSize">Message size (bytes).</param>
        public void MessageProcessedSynchronously(Pipeline pipeline, PipelineElement element, IReceiver receiver, int queueSize, Envelope envelope, int messageSize)
        {
            this.MessageProcessStart(pipeline, element, receiver, queueSize, envelope, messageSize);
            this.MessageProcessComplete(pipeline, element, receiver, envelope);
        }
    }
}
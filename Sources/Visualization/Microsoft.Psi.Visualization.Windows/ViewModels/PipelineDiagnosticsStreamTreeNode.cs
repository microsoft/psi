// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.

namespace Microsoft.Psi.Visualization.ViewModels
{
    using System.ComponentModel;
    using System.Linq;
    using Microsoft.Psi.Diagnostics;
    using Microsoft.Psi.Visualization.Data;

    /// <summary>
    /// Implements a stream tree node for pipeline diagnostics streams.
    /// </summary>
    public class PipelineDiagnosticsStreamTreeNode : StreamTreeNode
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="PipelineDiagnosticsStreamTreeNode"/> class.
        /// </summary>
        /// <param name="partitionViewModel">The partition for the stream tree node.</param>
        /// <param name="path">The path to the stream tree node.</param>
        /// <param name="name">The name of the stream tree node.</param>
        /// <param name="streamMetadata">The stream metadata.</param>
        public PipelineDiagnosticsStreamTreeNode(PartitionViewModel partitionViewModel, string path, string name, IStreamMetadata streamMetadata)
            : base(partitionViewModel, path, name, streamMetadata)
        {
        }

        /// <summary>
        /// Gets the path to the stream's icon.
        /// </summary>
        [Browsable(false)]
        public override string IconSource => this.PartitionViewModel.IsLivePartition ? IconSourcePath.DiagnosticsLive : IconSourcePath.Diagnostics;

        /// <summary>
        /// Creates a set of receiver diagnostics children corresponding to a given receiver id.
        /// </summary>
        /// <param name="receiverId">The receiver id.</param>
        /// <returns>The stream container tree node for those receiver diagnostics streams.</returns>
        public DerivedStreamContainerTreeNode AddDerivedReceiverDiagnosticsChildren(int receiverId)
        {
            var receiverDiagnostics = this.InternalChildren.FirstOrDefault(c => c.Name == "ReceiverDiagnostics");
            if (receiverDiagnostics == null)
            {
                receiverDiagnostics = new DerivedStreamContainerTreeNode(this.PartitionViewModel, $"{this.Path}.ReceiverDiagnostics", "ReceiverDiagnostics");

                // Insert the child into the existing list, before all non-member sub-streams, and in alphabetical order
                var lastOrDefault = this.InternalChildren.LastOrDefault(stn => string.Compare(stn.Name, "0") < 0 && stn is StreamTreeNode);
                var index = lastOrDefault != null ? this.InternalChildren.IndexOf(lastOrDefault) + 1 : 0;
                this.InternalChildren.Insert(index, receiverDiagnostics);
            }

            if (receiverDiagnostics.Children.FirstOrDefault(c => c.Name == $"{receiverId}") is not DerivedStreamContainerTreeNode receiverContainer)
            {
                receiverContainer = new DerivedStreamContainerTreeNode(this.PartitionViewModel, $"{this.Path}.ReceiverDiagnostics.{receiverId}", $"{receiverId}");
                receiverDiagnostics.AddChildTreeNode(receiverContainer);

                receiverContainer.AddChildTreeNode(
                    new DerivedReceiverDiagnosticsStreamTreeNode<double>(
                        this.PartitionViewModel,
                        $"{this.Path}.ReceiverDiagnostics.{receiverId}.{nameof(PipelineDiagnostics.ReceiverDiagnostics.AvgMessageEmittedLatency)}",
                        nameof(PipelineDiagnostics.ReceiverDiagnostics.AvgMessageEmittedLatency),
                        this.SourceStreamMetadata,
                        receiverId,
                        pd => pd.AvgMessageEmittedLatency));

                receiverContainer.AddChildTreeNode(
                    new DerivedReceiverDiagnosticsStreamTreeNode<double>(
                        this.PartitionViewModel,
                        $"{this.Path}.ReceiverDiagnostics.{receiverId}.{nameof(PipelineDiagnostics.ReceiverDiagnostics.AvgMessageCreatedLatency)}",
                        nameof(PipelineDiagnostics.ReceiverDiagnostics.AvgMessageCreatedLatency),
                        this.SourceStreamMetadata,
                        receiverId,
                        pd => pd.AvgMessageCreatedLatency));

                receiverContainer.AddChildTreeNode(
                    new DerivedReceiverDiagnosticsStreamTreeNode<double>(
                        this.PartitionViewModel,
                        $"{this.Path}.ReceiverDiagnostics.{receiverId}.{nameof(PipelineDiagnostics.ReceiverDiagnostics.AvgMessageProcessTime)}",
                        nameof(PipelineDiagnostics.ReceiverDiagnostics.AvgMessageProcessTime),
                        this.SourceStreamMetadata,
                        receiverId,
                        pd => pd.AvgMessageProcessTime));

                receiverContainer.AddChildTreeNode(
                    new DerivedReceiverDiagnosticsStreamTreeNode<double>(
                        this.PartitionViewModel,
                        $"{this.Path}.ReceiverDiagnostics.{receiverId}.{nameof(PipelineDiagnostics.ReceiverDiagnostics.AvgMessageReceivedLatency)}",
                        nameof(PipelineDiagnostics.ReceiverDiagnostics.AvgMessageReceivedLatency),
                        this.SourceStreamMetadata,
                        receiverId,
                        pd => pd.AvgMessageReceivedLatency));

                receiverContainer.AddChildTreeNode(
                    new DerivedReceiverDiagnosticsStreamTreeNode<double>(
                        this.PartitionViewModel,
                        $"{this.Path}.ReceiverDiagnostics.{receiverId}.{nameof(PipelineDiagnostics.ReceiverDiagnostics.AvgMessageSize)}",
                        nameof(PipelineDiagnostics.ReceiverDiagnostics.AvgMessageSize),
                        this.SourceStreamMetadata,
                        receiverId,
                        pd => pd.AvgMessageSize));

                receiverContainer.AddChildTreeNode(
                    new DerivedReceiverDiagnosticsStreamTreeNode<double>(
                        this.PartitionViewModel,
                        $"{this.Path}.ReceiverDiagnostics.{receiverId}.{nameof(PipelineDiagnostics.ReceiverDiagnostics.AvgDeliveryQueueSize)}",
                        nameof(PipelineDiagnostics.ReceiverDiagnostics.AvgDeliveryQueueSize),
                        this.SourceStreamMetadata,
                        receiverId,
                        pd => pd.AvgDeliveryQueueSize));

                receiverContainer.AddChildTreeNode(
                    new DerivedReceiverDiagnosticsStreamTreeNode<double>(
                        this.PartitionViewModel,
                        $"{this.Path}.ReceiverDiagnostics.{receiverId}.{nameof(PipelineDiagnostics.ReceiverDiagnostics.LastMessageEmittedLatency)}",
                        nameof(PipelineDiagnostics.ReceiverDiagnostics.LastMessageEmittedLatency),
                        this.SourceStreamMetadata,
                        receiverId,
                        pd => pd.LastMessageEmittedLatency));

                receiverContainer.AddChildTreeNode(
                    new DerivedReceiverDiagnosticsStreamTreeNode<double>(
                        this.PartitionViewModel,
                        $"{this.Path}.ReceiverDiagnostics.{receiverId}.{nameof(PipelineDiagnostics.ReceiverDiagnostics.LastMessageCreatedLatency)}",
                        nameof(PipelineDiagnostics.ReceiverDiagnostics.LastMessageCreatedLatency),
                        this.SourceStreamMetadata,
                        receiverId,
                        pd => pd.LastMessageCreatedLatency));

                receiverContainer.AddChildTreeNode(
                    new DerivedReceiverDiagnosticsStreamTreeNode<double>(
                        this.PartitionViewModel,
                        $"{this.Path}.ReceiverDiagnostics.{receiverId}.{nameof(PipelineDiagnostics.ReceiverDiagnostics.LastMessageProcessTime)}",
                        nameof(PipelineDiagnostics.ReceiverDiagnostics.LastMessageProcessTime),
                        this.SourceStreamMetadata,
                        receiverId,
                        pd => pd.LastMessageProcessTime));

                receiverContainer.AddChildTreeNode(
                    new DerivedReceiverDiagnosticsStreamTreeNode<double>(
                        this.PartitionViewModel,
                        $"{this.Path}.ReceiverDiagnostics.{receiverId}.{nameof(PipelineDiagnostics.ReceiverDiagnostics.LastMessageReceivedLatency)}",
                        nameof(PipelineDiagnostics.ReceiverDiagnostics.LastMessageReceivedLatency),
                        this.SourceStreamMetadata,
                        receiverId,
                        pd => pd.LastMessageReceivedLatency));

                receiverContainer.AddChildTreeNode(
                    new DerivedReceiverDiagnosticsStreamTreeNode<double>(
                        this.PartitionViewModel,
                        $"{this.Path}.ReceiverDiagnostics.{receiverId}.{nameof(PipelineDiagnostics.ReceiverDiagnostics.LastMessageSize)}",
                        nameof(PipelineDiagnostics.ReceiverDiagnostics.LastMessageSize),
                        this.SourceStreamMetadata,
                        receiverId,
                        pd => pd.LastMessageSize));

                receiverContainer.AddChildTreeNode(
                    new DerivedReceiverDiagnosticsStreamTreeNode<double>(
                        this.PartitionViewModel,
                        $"{this.Path}.ReceiverDiagnostics.{receiverId}.{nameof(PipelineDiagnostics.ReceiverDiagnostics.LastDeliveryQueueSize)}",
                        nameof(PipelineDiagnostics.ReceiverDiagnostics.LastDeliveryQueueSize),
                        this.SourceStreamMetadata,
                        receiverId,
                        pd => pd.LastDeliveryQueueSize));

                receiverContainer.AddChildTreeNode(
                    new DerivedReceiverDiagnosticsStreamTreeNode<bool>(
                        this.PartitionViewModel,
                        $"{this.Path}.ReceiverDiagnostics.{receiverId}.{nameof(PipelineDiagnostics.ReceiverDiagnostics.ReceiverIsThrottled)}",
                        nameof(PipelineDiagnostics.ReceiverDiagnostics.ReceiverIsThrottled),
                        this.SourceStreamMetadata,
                        receiverId,
                        pd => pd.ReceiverIsThrottled));

                receiverContainer.AddChildTreeNode(
                    new DerivedReceiverDiagnosticsStreamTreeNode<int>(
                        this.PartitionViewModel,
                        $"{this.Path}.ReceiverDiagnostics.{receiverId}.{nameof(PipelineDiagnostics.ReceiverDiagnostics.TotalMessageDroppedCount)}",
                        nameof(PipelineDiagnostics.ReceiverDiagnostics.TotalMessageDroppedCount),
                        this.SourceStreamMetadata,
                        receiverId,
                        pd => pd.TotalMessageDroppedCount));

                receiverContainer.AddChildTreeNode(
                    new DerivedReceiverDiagnosticsStreamTreeNode<int>(
                        this.PartitionViewModel,
                        $"{this.Path}.ReceiverDiagnostics.{receiverId}.{nameof(PipelineDiagnostics.ReceiverDiagnostics.TotalMessageEmittedCount)}",
                        nameof(PipelineDiagnostics.ReceiverDiagnostics.TotalMessageEmittedCount),
                        this.SourceStreamMetadata,
                        receiverId,
                        pd => pd.TotalMessageEmittedCount));

                receiverContainer.AddChildTreeNode(
                    new DerivedReceiverDiagnosticsStreamTreeNode<int>(
                        this.PartitionViewModel,
                        $"{this.Path}.ReceiverDiagnostics.{receiverId}.{nameof(PipelineDiagnostics.ReceiverDiagnostics.TotalMessageProcessedCount)}",
                        nameof(PipelineDiagnostics.ReceiverDiagnostics.TotalMessageProcessedCount),
                        this.SourceStreamMetadata,
                        receiverId,
                        pd => pd.TotalMessageProcessedCount));

                receiverContainer.AddChildTreeNode(
                    new DerivedReceiverDiagnosticsStreamTreeNode<int>(
                        this.PartitionViewModel,
                        $"{this.Path}.ReceiverDiagnostics.{receiverId}.{nameof(PipelineDiagnostics.ReceiverDiagnostics.WindowMessageDroppedCount)}",
                        nameof(PipelineDiagnostics.ReceiverDiagnostics.WindowMessageDroppedCount),
                        this.SourceStreamMetadata,
                        receiverId,
                        pd => pd.WindowMessageDroppedCount));

                receiverContainer.AddChildTreeNode(
                    new DerivedReceiverDiagnosticsStreamTreeNode<int>(
                        this.PartitionViewModel,
                        $"{this.Path}.ReceiverDiagnostics.{receiverId}.{nameof(PipelineDiagnostics.ReceiverDiagnostics.WindowMessageEmittedCount)}",
                        nameof(PipelineDiagnostics.ReceiverDiagnostics.WindowMessageEmittedCount),
                        this.SourceStreamMetadata,
                        receiverId,
                        pd => pd.WindowMessageEmittedCount));

                receiverContainer.AddChildTreeNode(
                    new DerivedReceiverDiagnosticsStreamTreeNode<int>(
                        this.PartitionViewModel,
                        $"{this.Path}.ReceiverDiagnostics.{receiverId}.{nameof(PipelineDiagnostics.ReceiverDiagnostics.WindowMessageProcessedCount)}",
                        nameof(PipelineDiagnostics.ReceiverDiagnostics.WindowMessageProcessedCount),
                        this.SourceStreamMetadata,
                        receiverId,
                        pd => pd.WindowMessageProcessedCount));
            }

            return receiverContainer;
        }

        /// <inheritdoc/>
        public override void EnsureDerivedStreamExists(StreamBinding streamBinding)
        {
            var receiverId = (int)streamBinding.StreamAdapterArguments[0];
            this.AddDerivedReceiverDiagnosticsChildren(receiverId);
        }

        /// <inheritdoc/>
        protected override bool CanExpandDerivedMemberStreams() => false;
    }
}
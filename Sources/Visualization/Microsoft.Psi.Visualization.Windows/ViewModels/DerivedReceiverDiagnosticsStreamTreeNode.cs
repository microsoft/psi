// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.

namespace Microsoft.Psi.Visualization.ViewModels
{
    using System;
    using System.Collections.Generic;
    using System.ComponentModel;
    using Microsoft.Psi.Diagnostics;
    using Microsoft.Psi.Visualization.Adapters;
    using Microsoft.Psi.Visualization.Data;
    using Microsoft.Psi.Visualization.VisualizationObjects;

    /// <summary>
    /// Implements a node in the stream tree that holds information about a derived
    /// receiver diagnostic statistic.
    /// </summary>
    /// <typeparam name="T">The type of the diagnostic statistic.</typeparam>
    public class DerivedReceiverDiagnosticsStreamTreeNode<T> : DerivedStreamTreeNode
        where T : struct
    {
        private readonly Func<PipelineDiagnostics.ReceiverDiagnostics, T> memberFunc;

        /// <summary>
        /// Initializes a new instance of the <see cref="DerivedReceiverDiagnosticsStreamTreeNode{T}"/> class.
        /// </summary>
        /// <param name="partitionViewModel">The partition where this stream tree node can be found.</param>
        /// <param name="path">The path to the stream tree node.</param>
        /// <param name="name">The name of the stream tree node.</param>
        /// <param name="sourceStreamMetadata">The source stream metadata.</param>
        /// <param name="receiverId">The receiver id.</param>
        /// <param name="memberFunc">A function that given the receiver diagnostics provides the statistic of interest.</param>
        public DerivedReceiverDiagnosticsStreamTreeNode(
            PartitionViewModel partitionViewModel,
            string path,
            string name,
            IStreamMetadata sourceStreamMetadata,
            int receiverId,
            Func<PipelineDiagnostics.ReceiverDiagnostics, T> memberFunc)
            : base(partitionViewModel, path, name, sourceStreamMetadata)
        {
            this.DataTypeName = typeof(T?).FullName;
            this.ReceiverId = receiverId;
            this.memberFunc = memberFunc;
        }

        /// <summary>
        /// Gets the receiver id.
        /// </summary>
        [DisplayName("Receiver Id")]
        [Description("The receiver id.")]
        public int ReceiverId { get; private set; }

        /// <inheritdoc/>
        public override StreamBinding CreateStreamBinding(VisualizerMetadata visualizerMetadata) =>
            new StreamBinding(
                this.SourceStreamMetadata.Name,
                this.PartitionViewModel.Name,
                this.Path,
                visualizerMetadata.StreamAdapterType,
                visualizerMetadata.VisualizationObjectType == typeof(LatencyVisualizationObject) || visualizerMetadata.VisualizationObjectType == typeof(MessageVisualizationObject) ? null : new object[] { this.ReceiverId, this.memberFunc },
                null,
                null,
                true);

        /// <inheritdoc/>
        protected override void InsertCustomAdapters(List<VisualizerMetadata> metadatas)
        {
            var streamSourceDataType = VisualizationContext.Instance.GetDataType(this.SourceStreamMetadata.TypeName);

            // For each of the non-universal visualization objects, add a data adapter from the stream data type to the subfield data type
            for (int index = 0; index < metadatas.Count; index++)
            {
                // For message visualization object insert a custom object adapter so values can be displayed for known types.
                if (metadatas[index].VisualizationObjectType == typeof(MessageVisualizationObject))
                {
                    var objectAdapterType = typeof(ObjectAdapter<>).MakeGenericType(streamSourceDataType);
                    metadatas[index] = metadatas[index].GetCloneWithNewStreamAdapterType(objectAdapterType);
                }
                else if (metadatas[index].VisualizationObjectType == typeof(LatencyVisualizationObject))
                {
                    // O/w for latency visualization object insert a custom object adapter so values can be displayed for known types.
                    var objectToLatencyAdapterType = typeof(ObjectToLatencyAdapter<>).MakeGenericType(streamSourceDataType);
                    metadatas[index] = metadatas[index].GetCloneWithNewStreamAdapterType(objectToLatencyAdapterType);
                }
                else
                {
                    // If the visualizer metadata already contains a stream adapter, create a stream member adapter that
                    // encapsulates it, otherwise create a stream member adapter that adapts directly from the message
                    // type to the member type.
                    Type streamMemberAdapterType;
                    if (metadatas[index].StreamAdapterType != null)
                    {
                        throw new NotSupportedException("Recevider diagnostics member adapter cannot be applied in conjunction with an existing adapter.");
                    }
                    else
                    {
                        streamMemberAdapterType = typeof(PipelineDiagnosticsToReceiverDiagnosticsMemberStreamAdapter<>).MakeGenericType(metadatas[index].DataType.GetGenericArguments()[0]);
                    }

                    metadatas[index] = metadatas[index].GetCloneWithNewStreamAdapterType(streamMemberAdapterType);
                }
            }
        }

        /// <inheritdoc/>
        protected override bool CanExpandDerivedMemberStreams() => false;
    }
}
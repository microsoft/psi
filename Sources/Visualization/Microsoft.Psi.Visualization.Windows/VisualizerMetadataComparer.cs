// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.

namespace Microsoft.Psi.Visualization
{
    using System;
    using System.Collections.Generic;

    /// <summary>
    /// Implements a comparer for visualizers to decide an ordering of visualizers
    /// for a given data type.
    /// </summary>
    public class VisualizerMetadataComparer : Comparer<VisualizerMetadata>
    {
        private readonly Type dataType;

        /// <summary>
        /// Initializes a new instance of the <see cref="VisualizerMetadataComparer"/> class.
        /// </summary>
        /// <param name="dataType">The data type.</param>
        public VisualizerMetadataComparer(Type dataType)
        {
            this.dataType = dataType;
        }

        /// <inheritdoc/>
        public override int Compare(VisualizerMetadata x, VisualizerMetadata y)
        {
            // Visualizers without adapters take precedence
            if (x.StreamAdapterType == null && y.StreamAdapterType != null)
            {
                return -1;
            }
            else if (x.StreamAdapterType != null && y.StreamAdapterType == null)
            {
                return 1;
            }
            else
            {
                // O/w assume the visualizers are of a derived type, so compute the distance to
                // the dataType
                return this.GetDistanceToDataType(x.DataType).CompareTo(this.GetDistanceToDataType(y.DataType));
            }
        }

        private int GetDistanceToDataType(Type visualizerMetadataType)
        {
            if (visualizerMetadataType == this.dataType)
            {
                return 0;
            }
            else if (visualizerMetadataType == typeof(object) || visualizerMetadataType.IsInterface)
            {
                return int.MaxValue;
            }
            else
            {
                var baseTypeRank = this.GetDistanceToDataType(visualizerMetadataType.BaseType);
                return baseTypeRank == int.MaxValue ? int.MaxValue : baseTypeRank + 1;
            }
        }
    }
}

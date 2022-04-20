// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.

namespace Microsoft.Psi.MixedReality.Visualization
{
    using System.Collections.Generic;
    using System.Linq;
    using MathNet.Spatial.Euclidean;
    using Microsoft.Psi.MixedReality;
    using Microsoft.Psi.Visualization.Adapters;
    using Microsoft.Psi.Visualization.Data;
    using Microsoft.Psi.Visualization.DataTypes;

    /// <summary>
    /// Implements a stream adapter for visualizing tracked hands as graphs of 3D points.
    /// </summary>
    [StreamAdapter("Tracked Hand to Graph")]
    public class TrackedHandVisualizationObjectAdapter : StreamAdapter<Hand, Graph<HandJointIndex, Point3D, bool>>
    {
        private static Dictionary<(HandJointIndex Start, HandJointIndex End), bool> HandJointHierarchy { get; } = new List<(HandJointIndex, HandJointIndex)>
        {
            (HandJointIndex.Wrist, HandJointIndex.Palm),

            (HandJointIndex.Wrist, HandJointIndex.ThumbMetacarpal),
            (HandJointIndex.ThumbMetacarpal, HandJointIndex.ThumbProximal),
            (HandJointIndex.ThumbProximal, HandJointIndex.ThumbDistal),
            (HandJointIndex.ThumbDistal, HandJointIndex.ThumbTip),

            (HandJointIndex.Wrist, HandJointIndex.IndexMetacarpal),
            (HandJointIndex.IndexMetacarpal, HandJointIndex.IndexProximal),
            (HandJointIndex.IndexProximal, HandJointIndex.IndexIntermediate),
            (HandJointIndex.IndexIntermediate, HandJointIndex.IndexDistal),
            (HandJointIndex.IndexDistal, HandJointIndex.IndexTip),

            (HandJointIndex.Wrist, HandJointIndex.MiddleMetacarpal),
            (HandJointIndex.MiddleMetacarpal, HandJointIndex.MiddleProximal),
            (HandJointIndex.MiddleProximal, HandJointIndex.MiddleIntermediate),
            (HandJointIndex.MiddleIntermediate, HandJointIndex.MiddleDistal),
            (HandJointIndex.MiddleDistal, HandJointIndex.MiddleTip),

            (HandJointIndex.Wrist, HandJointIndex.RingMetacarpal),
            (HandJointIndex.RingMetacarpal, HandJointIndex.RingProximal),
            (HandJointIndex.RingProximal, HandJointIndex.RingIntermediate),
            (HandJointIndex.RingIntermediate, HandJointIndex.RingDistal),
            (HandJointIndex.RingDistal, HandJointIndex.RingTip),

            (HandJointIndex.Wrist, HandJointIndex.PinkyMetacarpal),
            (HandJointIndex.PinkyMetacarpal, HandJointIndex.PinkyProximal),
            (HandJointIndex.PinkyProximal, HandJointIndex.PinkyIntermediate),
            (HandJointIndex.PinkyIntermediate, HandJointIndex.PinkyDistal),
            (HandJointIndex.PinkyDistal, HandJointIndex.PinkyTip),
        }.ToDictionary(j => j, j => true);

        /// <inheritdoc/>
        public override Graph<HandJointIndex, Point3D, bool> GetAdaptedValue(Hand source, Envelope envelope)
        {
            if (source != null && source.IsTracked)
            {
                var dictionary = new Dictionary<HandJointIndex, Point3D>();

                for (int jointIndex = 0; jointIndex < (int)HandJointIndex.MaxIndex; jointIndex++)
                {
                    if (source.Joints[jointIndex] != null)
                    {
                        dictionary.Add((HandJointIndex)jointIndex, source.Joints[jointIndex].Origin);
                    }
                }

                return new Graph<HandJointIndex, Point3D, bool>(dictionary, HandJointHierarchy);
            }
            else
            {
                return null;
            }
        }
    }
}

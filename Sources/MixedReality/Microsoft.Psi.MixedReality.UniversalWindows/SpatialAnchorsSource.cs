// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.

namespace Microsoft.Psi.MixedReality
{
    using System;
    using System.Collections.Generic;
    using MathNet.Spatial.Euclidean;
    using Microsoft.Psi.Components;

    /// <summary>
    /// Component representing a spatial anchor source.
    /// </summary>
    public class SpatialAnchorsSource : Generator, IProducer<Dictionary<string, CoordinateSystem>>
    {
        private readonly TimeSpan interval;

        /// <summary>
        /// Initializes a new instance of the <see cref="SpatialAnchorsSource"/> class.
        /// </summary>
        /// <param name="pipeline">The pipeline to add the component to.</param>
        /// <param name="interval">The configuration for the component.</param>
        /// <param name="name">An optional name for the component.</param>
        public SpatialAnchorsSource(Pipeline pipeline, TimeSpan interval, string name = nameof(SpatialAnchorsSource))
            : base(pipeline, name: name)
        {
            this.interval = interval;
            this.Out = pipeline.CreateEmitter<Dictionary<string, CoordinateSystem>>(this, nameof(this.Out));
            this.Update = pipeline.CreateReceiver<Dictionary<string, CoordinateSystem>>(this, this.ReceiveUpdate, nameof(this.Update));
        }

        /// <summary>
        /// Gets the stream of spatial anchor poses.
        /// </summary>
        public Emitter<Dictionary<string, CoordinateSystem>> Out { get; private set; }

        /// <summary>
        /// Gets the receiver for spatial anchor updates.
        /// </summary>
        public Receiver<Dictionary<string, CoordinateSystem>> Update { get; private set; }

        /// <inheritdoc />
        protected override DateTime GenerateNext(DateTime currentTime)
        {
            this.Out.Post(MixedReality.SpatialAnchorHelper.GetAllSpatialAnchorCoordinateSystems(), currentTime);
            return currentTime + this.interval;
        }

        private void ReceiveUpdate(Dictionary<string, CoordinateSystem> spatialAnchors, Envelope envelope)
        {
            foreach (var spatialAnchor in spatialAnchors)
            {
                MixedReality.SpatialAnchorHelper.TryUpdateSpatialAnchor(spatialAnchor.Key, spatialAnchor.Value);
            }
        }
    }
}

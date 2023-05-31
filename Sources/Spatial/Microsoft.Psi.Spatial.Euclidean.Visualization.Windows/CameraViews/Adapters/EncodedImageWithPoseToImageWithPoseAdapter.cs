// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.

namespace Microsoft.Psi.Spatial.Euclidean.Visualization
{
    using MathNet.Spatial.Euclidean;
    using Microsoft.Psi.Imaging;
    using Microsoft.Psi.Visualization.Adapters;
    using Microsoft.Psi.Visualization.Data;

    /// <summary>
    /// Implements a stream adapter from encoded image with position to an image with position.
    /// </summary>
    [StreamAdapter]
    public class EncodedImageWithPoseToImageWithPoseAdapter : StreamAdapter<(Shared<EncodedImage>, CoordinateSystem), (Shared<Image>, CoordinateSystem)>
    {
        private readonly EncodedImageToImageAdapter imageAdapter = new ();

        /// <inheritdoc/>
        public override (Shared<Image>, CoordinateSystem) GetAdaptedValue((Shared<EncodedImage>, CoordinateSystem) source, Envelope envelope)
            => (this.imageAdapter.GetAdaptedValue(source.Item1, envelope), source.Item2);

        /// <inheritdoc/>
        public override void Dispose((Shared<Image>, CoordinateSystem) destination)
            => this.imageAdapter.Dispose(destination.Item1);
    }
}

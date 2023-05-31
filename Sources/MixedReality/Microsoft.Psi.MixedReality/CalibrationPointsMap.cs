// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.

namespace Microsoft.Psi.MixedReality
{
    /// <summary>
    /// Represents a calibration mapping of points, along with sensor width and height. Includes XY values of points
    /// on the camera unit plane, one for each image pixel (corresponding to the center of the pixel).
    /// These points are laid out row-wise, X then Y, repeating.
    /// For the image pixel each point corresponds to, (i,j), it was sampled at
    /// the center of the pixel, at position: (i+0.5, j+0.5).
    /// </summary>
    public sealed class CalibrationPointsMap
    {
        /// <summary>
        /// Gets or sets the sensor image width.
        /// </summary>
        public int Width { get; set; }

        /// <summary>
        /// Gets or sets the sensor image height.
        /// </summary>
        public int Height { get; set; }

        /// <summary>
        /// Gets or sets the set of XY points on the camera unit plane, one for the center of each image pixel.
        /// </summary>
        public double[] CameraUnitPlanePoints { get; set; }
    }
}

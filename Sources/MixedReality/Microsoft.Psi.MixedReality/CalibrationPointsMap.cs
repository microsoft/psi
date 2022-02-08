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
    public readonly struct CalibrationPointsMap
    {
        /// <summary>
        /// Gets the sensor image width.
        /// </summary>
        public readonly int Width;

        /// <summary>
        /// Gets the sensor image height.
        /// </summary>
        public readonly int Height;

        /// <summary>
        /// Gets the set of XY points on the camera unit plane, one for the center of each image pixel.
        /// </summary>
        public readonly float[] CameraUnitPlanePoints;

        /// <summary>
        /// Initializes a new instance of the <see cref="CalibrationPointsMap"/> struct.
        /// </summary>
        /// <param name="width">The sensor image width.</param>
        /// <param name="height">The sensor image height.</param>
        /// <param name="cameraUnitPlanePoints">The set of XY points on the camera unit plane.
        /// These points are laid out row-wise, X then Y, repeating.
        /// For the image pixel each point corresponds to, (i,j), it was sampled at
        /// the center of the pixel, at position: (i+0.5, j+0.5).</param>
        public CalibrationPointsMap(int width, int height, float[] cameraUnitPlanePoints)
        {
            this.Width = width;
            this.Height = height;
            this.CameraUnitPlanePoints = cameraUnitPlanePoints;
        }
    }
}

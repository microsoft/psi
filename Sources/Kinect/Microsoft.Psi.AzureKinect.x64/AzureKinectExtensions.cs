// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.

namespace Microsoft.Psi.AzureKinect
{
    using System;
    using Microsoft.Azure.Kinect.Sensor;

    /// <summary>
    /// Helper class with extension methods for Azure Kinect data types.
    /// </summary>
    public static class AzureKinectExtensions
    {
        /// <summary>
        /// Gets the range of pixel values for a specified depth mode.
        /// </summary>
        /// <param name="depthMode">The depth mode.</param>
        /// <returns>A tuple indicating the range of pixel values, in millimeters.</returns>
        public static (ushort MinValue, ushort MaxValue) GetRange(this DepthMode depthMode)
        {
            // Using the same values as in:
            // https://github.com/microsoft/Azure-Kinect-Sensor-SDK/blob/develop/tools/k4aviewer/k4astaticimageproperties.h
            return depthMode switch
            {
                DepthMode.NFOV_2x2Binned => (500, 5800),
                DepthMode.NFOV_Unbinned => (500, 4000),
                DepthMode.WFOV_2x2Binned => (250, 3000),
                DepthMode.WFOV_Unbinned => (250, 2500),
                _ => throw new Exception("Invalid depth mode."),
            };
        }
    }
}

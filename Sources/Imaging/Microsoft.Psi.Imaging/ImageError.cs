// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.

namespace Microsoft.Psi.Imaging
{
    /// <summary>
    /// Defines error metrics returned by Image.Compare().
    /// </summary>
    public class ImageError
    {
        /// <summary>
        /// Gets or sets the maximum distance between all pixels.
        /// </summary>
        public double MaxError { get; set; }

        /// <summary>
        /// Gets or sets the average distance across all pixels.
        /// </summary>
        public double AvgError { get; set; }

        /// <summary>
        /// Gets or sets the number of outliers (pixels outside of the specified tolerance).
        /// </summary>
        public int NumberOutliers { get; set; }
    }
}

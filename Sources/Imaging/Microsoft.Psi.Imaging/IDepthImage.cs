// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.

namespace Microsoft.Psi.Imaging
{
    /// <summary>
    /// Interface that defines a depth image.
    /// </summary>
    public interface IDepthImage : IImage
    {
        /// <summary>
        /// Gets depth value semantics.
        /// </summary>
        DepthValueSemantics DepthValueSemantics { get; }

        /// <summary>
        /// Gets the scale factor to convert depth values to meters.
        /// </summary>
        double DepthValueToMetersScaleFactor { get; }
    }
}

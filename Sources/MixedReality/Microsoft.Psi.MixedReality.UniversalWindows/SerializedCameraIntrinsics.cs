// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.

namespace Microsoft.Psi.MixedReality
{
    using System.Xml;
    using System.Xml.Serialization;

    /// <summary>
    /// Represents a camera's serialized camera intrinsics information.
    /// </summary>
    [XmlRoot]
    public sealed class SerializedCameraIntrinsics
    {
        /// <summary>
        /// Gets or sets the image width.
        /// </summary>
        [XmlElement]
        public int ImageWidth { get; set; }

        /// <summary>
        /// Gets or sets the image height.
        /// </summary>
        [XmlElement]
        public int ImageHeight { get; set; }

        /// <summary>
        /// Gets or sets the camera intrinsics transform matrix.
        /// Intrinsics defines a 3x3 matrix stored in column-major order and assumes column-vectors
        /// (i.e. Matrix * Point rather than Point * Matrix).
        /// </summary>
        [XmlArray]
        public double[] Transform { get; set; }

        /// <summary>
        /// Gets or sets the radial distortion coefficients.
        /// </summary>
        [XmlArray]
        public double[] RadialDistortion { get; set; } = null;

        /// <summary>
        /// Gets or sets the tangential distortion coefficients.
        /// </summary>
        [XmlArray]
        public double[] TangentialDistortion { get; set; } = null;

        /// <summary>
        /// Gets or sets a value indicating whether the closed form equation of the Brown-Conrady Distortion model
        /// distorts or undistorts. i.e. if true then:
        ///      Xdistorted = Xundistorted * (1+K1*R2+K2*R3+...
        /// otherwise:
        ///      Xundistorted = Xdistorted * (1+K1*R2+K2*R3+...
        /// </summary>
        [XmlElement]
        public bool ClosedFormDistorts { get; set; } = true;
    }
}

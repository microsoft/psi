// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.

namespace Microsoft.Psi.Calibration
{
    using System.Collections.Generic;
    using System.Xml.Serialization;

    /// <summary>
    /// Used for serializing out the results of our multi-camera calibration
    /// </summary>
    [XmlRoot]
    public class SystemCalibration
    {
        /// <summary>
        /// Defines the method used to perform the calibration
        /// </summary>
        public enum CalibrationMethod
        {
            /// <summary>
            /// Indicates we used a checkerboard
            /// </summary>
            CheckerBoard
        }

        /// <summary>
        /// Gets or sets the method of calibration used
        /// </summary>
        [XmlElement]
        public CalibrationMethod MethodOfCalibration { get; set; }

        /// <summary>
        /// Gets or sets the number of corners across the checker board
        /// </summary>
        [XmlElement]
        public int CheckerBoardWidth { get; set; }

        /// <summary>
        /// Gets or sets the number of corners down the checker board
        /// </summary>
        [XmlElement]
        public int CheckerBoardHeight { get; set; }

        /// <summary>
        /// Gets or sets the size of each square (in millimeters) on the checker board
        /// </summary>
        [XmlElement]
        public double CheckerBoardSquareSize { get; set; }

        /// <summary>
        /// Gets or sets the width of each captured image
        /// </summary>
        [XmlElement]
        public double ImageWidth { get; set; }

        /// <summary>
        /// Gets or sets the height of each captured image
        /// </summary>
        [XmlElement]
        public double ImageHeight { get; set; }

        /// <summary>
        /// Gets or sets the number of frames captured
        /// </summary>
        [XmlElement]
        public double NumberOfFrames { get; set; }

        /// <summary>
        /// Gets or sets the camera calibration for each camera
        /// </summary>
        [XmlArray("CameraPoses")]
        [XmlArrayItem(typeof(CameraCalibration), ElementName ="CameraCalibration")]
        public List<CameraCalibration> CameraPoses { get; set; }

        /// <summary>
        /// Defines the calibration results for a single camera
        /// </summary>
        [XmlRoot]
        public class CameraCalibration
        {
            /// <summary>
            /// Gets or sets the name of the camera
            /// </summary>
            [XmlElement]
            public string CameraName { get; set; }

            /// <summary>
            /// Gets or sets the name of the machine that was controlling this camera
            /// </summary>
            [XmlElement]
            public string MachineName { get; set; }

            /// <summary>
            /// Gets or sets the camera intrinsics
            /// </summary>
            [XmlArray]
            public double[] Intrinsics { get; set; }

            /// <summary>
            /// Gets or sets the camera distortion coefficients
            /// </summary>
            [XmlArray]
            public double[] DistortionCoefficients { get; set; }

            /// <summary>
            /// Gets or sets the camera extrinsics
            /// </summary>
            [XmlArray]
            public double[] Extrinsics { get; set; }
        }
    }
}

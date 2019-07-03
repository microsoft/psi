// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.

namespace Microsoft.Psi.Calibration
{
    using System.Collections.Generic;
    using System.IO;
    using System.Xml.Serialization;

    /// <summary>
    /// Used for serializing out the results of our multi-camera calibration.
    /// </summary>
    [XmlRoot]
    public class SystemCalibration
    {
        /// <summary>
        /// Defines the method used to perform the calibration.
        /// </summary>
        public enum CalibrationMethod
        {
            /// <summary>
            /// Indicates we used a checkerboard
            /// </summary>
            CheckerBoard,
        }

        /// <summary>
        /// Gets or sets the method of calibration used.
        /// </summary>
        [XmlElement]
        public CalibrationMethod MethodOfCalibration { get; set; }

        /// <summary>
        /// Gets or sets the number of corners across the checker board.
        /// </summary>
        [XmlElement]
        public int CheckerBoardWidth { get; set; }

        /// <summary>
        /// Gets or sets the number of corners down the checker board.
        /// </summary>
        [XmlElement]
        public int CheckerBoardHeight { get; set; }

        /// <summary>
        /// Gets or sets the size of each square (in millimeters) on the checker board.
        /// </summary>
        [XmlElement]
        public double CheckerBoardSquareSize { get; set; }

        /// <summary>
        /// Gets or sets the camera calibration for each camera.
        /// </summary>
        [XmlArray("CameraPoses")]
        [XmlArrayItem(typeof(CameraCalibration), ElementName = "CameraCalibration")]
        public List<CameraCalibration> CameraPoses { get; set; }

        /// <summary>
        /// Gets or sets the solved board positions.
        /// </summary>
        [XmlArray("SolvedBoards")]
        [XmlArrayItem(typeof(SolvedBoard), ElementName = "SolvedBoard")]
        public List<SolvedBoard> SolvedBoards { get; set; }

        /// <summary>
        /// Loads the system calibration from an xml file.
        /// </summary>
        /// <param name="xmlFileName">The path to the xml file containing the system calibration information.</param>
        /// <returns>The system calibration.</returns>
        public static SystemCalibration LoadFromFile(string xmlFileName)
        {
            // Load the calibration file
            var serializer = new XmlSerializer(typeof(SystemCalibration));
            using (var fs = new FileStream(xmlFileName, FileMode.Open))
            {
                return serializer.Deserialize(fs) as SystemCalibration;
            }
        }

        /// <summary>
        /// Defines the calibration results for a single camera.
        /// </summary>
        [XmlRoot]
        public class CameraCalibration
        {
            /// <summary>
            /// Gets or sets the name of the camera.
            /// </summary>
            [XmlElement]
            public string CameraName { get; set; }

            /// <summary>
            /// Gets or sets the name of the machine that was controlling this camera.
            /// </summary>
            [XmlElement]
            public string MachineName { get; set; }

            /// <summary>
            /// Gets or sets the path to the video store (for this camera) used for this calibration.
            /// </summary>
            [XmlElement]
            public string SourceVideo { get; set; }

            /// <summary>
            /// Gets or sets the camera intrinsics.
            /// </summary>
            [XmlArray]
            public double[] Intrinsics { get; set; }

            /// <summary>
            /// Gets or sets the camera distortion coefficients
            /// These coefficients are in the same order as openCV, i.e.
            /// k1,k2,p1,p2,[k3,[k4,k5,k6]]
            /// where k are the radial distortion coefficients and p are
            /// the tangential distortion coefficients.
            /// </summary>
            [XmlArray]
            public double[] DistortionCoefficients { get; set; }

            /// <summary>
            /// Gets or sets the camera extrinsics.
            /// </summary>
            [XmlArray]
            public double[] Extrinsics { get; set; }

            /// <summary>
            /// Gets or sets the camera's intrinsics reprojection error.
            /// </summary>
            [XmlElement]
            public double IntrinsicsReprojectionError { get; set; }

            /// <summary>
            /// Gets or sets the camera's extrinsics reprojection error.
            /// </summary>
            [XmlElement]
            public double ExtrinsicsReprojectionError { get; set; }

            /// <summary>
            /// Gets or sets the width of each captured image.
            /// </summary>
            [XmlElement]
            public double ImageWidth { get; set; }

            /// <summary>
            /// Gets or sets the height of each captured image.
            /// </summary>
            [XmlElement]
            public double ImageHeight { get; set; }

            /// <summary>
            /// Gets or sets the number of frames captured.
            /// </summary>
            [XmlElement]
            public double NumberOfFrames { get; set; }

            /// <summary>
            /// Gets the camera's intrinsics.
            /// </summary>
            public CameraIntrinsics CameraIntrinsics
            {
                get
                {
                    var radialDistortion = MathNet.Numerics.LinearAlgebra.Vector<double>.Build.Dense(2);
                    radialDistortion[0] = this.DistortionCoefficients[0];
                    radialDistortion[1] = this.DistortionCoefficients[1];

                    var tangentialDistortion = MathNet.Numerics.LinearAlgebra.Vector<double>.Build.Dense(2);
                    tangentialDistortion[0] = this.DistortionCoefficients[2];
                    tangentialDistortion[1] = this.DistortionCoefficients[3];

                    var invMtx = MathNet.Numerics.LinearAlgebra.Matrix<double>.Build.Dense(3, 3);
                    for (int i = 0; i < 9; i++)
                    {
                        invMtx[i % 3, i / 3] = this.Intrinsics[i];
                    }

                    return new CameraIntrinsics((int)this.ImageWidth, (int)this.ImageHeight, invMtx, radialDistortion, tangentialDistortion);
                }
            }

            /// <summary>
            /// Gets the camera's extrinsics.
            /// </summary>
            public MathNet.Spatial.Euclidean.CoordinateSystem CoordinateSystem
            {
                get
                {
                    var mtx = MathNet.Numerics.LinearAlgebra.Matrix<double>.Build.Dense(4, 4);
                    for (int i = 0; i < 16; i++)
                    {
                        mtx[i % 4, i / 4] = this.Extrinsics[i];
                    }

                    var cs = new MathNet.Spatial.Euclidean.CoordinateSystem(mtx);
                    return cs;
                }
            }
        }

        /// <summary>
        /// Defines the relative orientation of a solved board.
        /// </summary>
        [XmlRoot("SolvedBoards")]
        public class SolvedBoard
        {
            /// <summary>
            /// Gets or sets the board position.
            /// </summary>
            [XmlArray]
            public double[] Position { get; set; }

            /// <summary>
            /// Gets or sets the board orientation.
            /// </summary>
            [XmlArray]
            public double[] Orientation { get; set; }
        }
    }
}

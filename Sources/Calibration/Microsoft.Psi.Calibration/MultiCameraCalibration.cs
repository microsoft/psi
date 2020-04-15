// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.

namespace Microsoft.Psi.Calibration
{
    using System.Collections.Generic;
    using System.IO;
    using System.Xml;
    using System.Xml.Serialization;

    /// <summary>
    /// Used for serializing out the results of multi-camera calibration (a system of multiple cameras).
    /// </summary>
    [XmlRoot]
    public class MultiCameraCalibration
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
        [XmlArrayItem(typeof(CameraCalibrationResult), ElementName = "CameraCalibrationResult")]
        public List<CameraCalibrationResult> CameraCalibrationResults { get; set; }

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
        public static MultiCameraCalibration LoadFromFile(string xmlFileName)
        {
            // Load the calibration file
            var serializer = new XmlSerializer(typeof(MultiCameraCalibration));
            using (var fs = new FileStream(xmlFileName, FileMode.Open))
            {
                return serializer.Deserialize(fs) as MultiCameraCalibration;
            }
        }

        /// <summary>
        /// Defines the calibration results for a single camera.
        /// </summary>
        [XmlRoot]
        public class CameraCalibrationResult
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
            /// Gets or sets the name of the sensor (i.e. IR, Color, or Depth).
            /// </summary>
            [XmlElement]
            public string SensorName { get; set; }

            /// <summary>
            /// Gets or sets the path to the video store (for this camera) used for this calibration.
            /// </summary>
            [XmlElement]
            public string SourceVideo { get; set; }

            /// <summary>
            /// Gets or sets the camera intrinsics.
            /// Intrinsics defines a 3x3 matrix stored in column-major order and assumes column-vectors
            /// (i.e. Matrix * Point versus Point * Matrix).
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
            /// This array contains a 4x4 extrinsics matrix.
            /// Values are stored in column-major order and assumes column-vectors
            /// (i.e. Matrix * Point versus Point * Matrix).
            /// Units are millimeters.
            /// OpenCV basis is asssumed here (Forward=Z, Right=X, Down=Y):
            ///           Z (forward)
            ///          /
            ///         /
            ///        +----> X (right)
            ///        |
            ///        |
            ///        Y (down).
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
                    var radialDistortion = MathNet.Numerics.LinearAlgebra.Vector<double>.Build.Dense(6);
                    radialDistortion[0] = this.DistortionCoefficients[0];
                    radialDistortion[1] = this.DistortionCoefficients[1];
                    var tangentialDistortion = MathNet.Numerics.LinearAlgebra.Vector<double>.Build.Dense(2);
                    tangentialDistortion[0] = this.DistortionCoefficients[2];
                    tangentialDistortion[1] = this.DistortionCoefficients[3];

                    if (this.DistortionCoefficients.Length > 4)
                    {
                        // DistortionCoefficients assumes OpenCV layout (k1,k2,p1,p2,k3,k4,k5,k6). Thus skip [2]/[3].
                        radialDistortion[2] = this.DistortionCoefficients[4];
                        if (this.DistortionCoefficients.Length > 5)
                        {
                            radialDistortion[3] = this.DistortionCoefficients[5];
                            radialDistortion[4] = this.DistortionCoefficients[6];
                            radialDistortion[5] = this.DistortionCoefficients[7];
                        }
                    }

                    var mtx = MathNet.Numerics.LinearAlgebra.Matrix<double>.Build.Dense(3, 3);
                    for (int i = 0; i < 9; i++)
                    {
                        // Fill in the intrinsincs matrix, assuming column-major ordering.
                        mtx[i % 3, i / 3] = this.Intrinsics[i];
                    }

                    return new CameraIntrinsics((int)this.ImageWidth, (int)this.ImageHeight, mtx, radialDistortion, tangentialDistortion);
                }
            }

            /// <summary>
            /// Gets the camera's pose. The pose is used to convert from device=>world coordinates
            /// (i.e. given 0,0,0 it will tell you where the camera is located in world coordinates).
            /// Pose is essentially the *inverse* transformation as that defined by Extrinsics.
            /// Units are converted to meters.
            /// The matrix assumes column-vectors: (i.e. Matrix * Point versus Point * Matrix).
            /// The MathNet basis is used:
            ///        Z (up)
            ///        |
            ///        |
            ///        +----> Y (left)
            ///       /
            ///      /
            ///     X (forward).
            /// </summary>
            public MathNet.Spatial.Euclidean.CoordinateSystem Pose
            {
                get
                {
                    var mtx = MathNet.Numerics.LinearAlgebra.Matrix<double>.Build.Dense(4, 4);
                    for (int i = 0; i < 16; i++)
                    {
                        // Fill in the matrix. Extrinsics are assumed to be stored in column-major order.
                        mtx[i % 4, i / 4] = this.Extrinsics[i];
                    }

                    // Extrinsics is in millimeters, but Pose should be in meters.
                    mtx.SetColumn(3, mtx.Column(3) / 1000.0);
                    mtx[3, 3] = 1;

                    // Extrinsics are stored in OpenCV basis, so convert here to MathNet basis.
                    var openCVBasis = new MathNet.Spatial.Euclidean.CoordinateSystem(
                        default,
                        MathNet.Spatial.Euclidean.UnitVector3D.ZAxis,
                        MathNet.Spatial.Euclidean.UnitVector3D.XAxis.Negate(),
                        MathNet.Spatial.Euclidean.UnitVector3D.YAxis.Negate());
                    return new MathNet.Spatial.Euclidean.CoordinateSystem(openCVBasis.Invert() * mtx.Inverse() * openCVBasis);
                }
            }
        }

        /// <summary>
        /// Defines a 3D point in our calibration.
        /// </summary>
        [XmlRoot("CalibrationVector")]
        public class CalibrationVector
        {
            /// <summary>
            /// Gets or sets the X component.
            /// </summary>
            public double X { get; set; }

            /// <summary>
            /// Gets or sets the Y component.
            /// </summary>
            public double Y { get; set; }

            /// <summary>
            /// Gets or sets the Z component.
            /// </summary>
            public double Z { get; set; }
        }

        /// <summary>
        /// Defines the relative orientation of a solved board.
        /// </summary>
        [XmlRoot("SolvedBoard")]
        public class SolvedBoard
        {
            /// <summary>
            /// Gets or sets the position of each board's detected corner points.
            /// </summary>
            [XmlArray]
            public CalibrationVector[] CornerPositions { get; set; }

            /// <summary>
            /// Gets or sets the board position.
            /// </summary>
            [XmlElement]
            public CalibrationVector Position { get; set; }

            /// <summary>
            /// Gets or sets the board orientation.
            /// </summary>
            [XmlElement]
            public CalibrationVector Orientation { get; set; }
        }
    }
}

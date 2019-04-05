// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.

namespace Microsoft.Psi.Kinect
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.IO.Compression;
    using System.Linq;
    using MathNet.Numerics.LinearAlgebra;
    using MathNet.Spatial.Euclidean;
    using Microsoft.Kinect;
    using Microsoft.Psi.Imaging;

    /// <summary>
    /// Implements stream operator methods for Kinect
    /// </summary>
    public static class KinectExtensions
    {
        /// <summary>
        /// Returns the position of a given joint in the body.
        /// </summary>
        /// <param name="source">The stream of kinect body.</param>
        /// <param name="jointType">The type of joint.</param>
        /// <param name="deliveryPolicy">An optional delivery policy.</param>
        /// <returns>The joint position as a 3D point in Kinect camera space.</returns>
        public static IProducer<Point3D> GetJointPosition(this IProducer<KinectBody> source, JointType jointType, DeliveryPolicy deliveryPolicy = null)
        {
            return source.Select(kb => kb.Joints[jointType].Position.ToPoint3D(), deliveryPolicy);
        }

        /// <summary>
        /// Projects set of 2D image points into 3D.
        /// </summary>
        /// <param name="source">Tuple of depth image, list of points to project, and calibration information.</param>
        /// <param name="deliveryPolicy">An optional delivery policy.</param>
        /// <returns>Returns a producer that generates a list of corresponding 3D points in Kinect camera space.</returns>
        public static IProducer<List<Point3D>> ProjectTo3D(
            this IProducer<(Shared<Image>, List<Point2D>, IKinectCalibration)> source, DeliveryPolicy deliveryPolicy = null)
        {
            var projectTo3D = new ProjectTo3D(source.Out.Pipeline);
            source.PipeTo(projectTo3D, deliveryPolicy);
            return projectTo3D;
        }

        /// <summary>
        /// Transforms a Kinect CameraSpacePoint to another coordinate system.
        /// </summary>
        /// <param name="cs">Coordinate system to transform to.</param>
        /// <param name="cameraSpacePoint">Point in Kinect camera space.</param>
        /// <returns>Tranformed point in Kinect camera space.</returns>
        public static CameraSpacePoint Transform(this CoordinateSystem cs, CameraSpacePoint cameraSpacePoint)
        {
            return cs.Transform(cameraSpacePoint.ToPoint3D()).ToCameraSpacePoint();
        }

        /// <summary>
        /// Rebases the kinect body in a specified coordinate system.
        /// </summary>
        /// <param name="cs">Coordinate system to transform to</param>
        /// <param name="kinectBody">Body to transform</param>
        /// <returns>The body rebased in the specified coordinate system.</returns>
        /// <remarks>The method rebases all the joints, including position and orientation, in the specified coordinate system.</remarks>
        public static KinectBody Rebase(this KinectBody kinectBody, CoordinateSystem cs)
        {
            var transformed = kinectBody.DeepClone();

            foreach (JointType jointType in Enum.GetValues(typeof(JointType)))
            {
                // Create a CoordinateSystem from the joint
                CoordinateSystem kinectJointCS = transformed.GetJointCoordinateSystem(jointType);

                // Transform by the given coordinate system
                kinectJointCS = kinectJointCS.TransformBy(cs);

                // Convert rotation back to Kinect joint orientation
                if (transformed.JointOrientations.ContainsKey(jointType))
                {
                    var rot = kinectJointCS.GetRotationSubMatrix();
                    var q = MatrixToQuaternion(rot);
                    JointOrientation jointOrientation = transformed.JointOrientations[jointType];
                    jointOrientation.Orientation.X = q.X;
                    jointOrientation.Orientation.Y = q.Y;
                    jointOrientation.Orientation.Z = q.Z;
                    jointOrientation.Orientation.W = q.W;
                    transformed.JointOrientations[jointType] = jointOrientation;
                }

                // Convert position back to camera space point
                if (transformed.Joints.ContainsKey(jointType))
                {
                    var joint = transformed.Joints[jointType];
                    joint.Position = kinectJointCS.Origin.ToCameraSpacePoint();
                    transformed.Joints[jointType] = joint;
                }
            }

            return transformed;
        }

        /// <summary>
        /// Creates a CoordinateSystem from a given kinect joint
        /// </summary>
        /// <param name="kinectBody">Kinect body containing the joint</param>
        /// <param name="jointType">Which joint to create a CoordinateSystem from </param>
        /// <returns>CoordinateSystem capturing the given joint's orientation and position</returns>
        public static CoordinateSystem GetJointCoordinateSystem(this KinectBody kinectBody, JointType jointType)
        {
            if (!kinectBody.Joints.ContainsKey(jointType))
            {
                throw new Exception($"Cannot create a coordinate system out of non-existent joint: {jointType}");
            }

            // Create a CoordinateSystem, starting from the Kinect's defined basis
            CoordinateSystem kinectJointCS = new CoordinateSystem();

            // Get the orientation as a rotation
            if (kinectBody.JointOrientations.ContainsKey(jointType))
            {
                var jointOrientation = kinectBody.JointOrientations[jointType].Orientation;
                kinectJointCS = kinectJointCS.SetRotationSubMatrix(QuaternionToMatrix(jointOrientation));
            }

            // Get the position as a translation
            var cameraSpacePoint = kinectBody.Joints[jointType].Position;
            return kinectJointCS.SetTranslation(cameraSpacePoint.ToPoint3D().ToVector3D());
        }

        /// <summary>
        /// Transforms the specified 3D point into a 2D point via the specified calibration.
        /// </summary>
        /// <param name="source">A stream of tuples containing the 3D point and calibration inforamtion.</param>
        /// <param name="deliveryPolicy">An optional delivery policy.</param>
        /// <returns>A producer that generates the 2D transformed points.</returns>
        public static IProducer<Point2D> ToColorSpace(this IProducer<(Point3D, IKinectCalibration)> source, DeliveryPolicy deliveryPolicy = null)
        {
            return source.Select(m =>
            {
                var (point3D, calibration) = m;
                if (calibration != default(IKinectCalibration))
                {
                    return calibration.ToColorSpace(point3D);
                }
                else
                {
                    return default(Point2D?);
                }
            }, deliveryPolicy).Where(p => p.HasValue, deliveryPolicy).Select(p => p.Value);
        }

        /// <summary>
        /// Converts points in from Kinect color space into 2D points.
        /// </summary>
        /// <param name="source">A stream of points in color space.</param>
        /// <param name="deliveryPolicy">An optional delivery policy.</param>
        /// <returns>A producer that generates transformed 2D points.</returns>
        public static IProducer<Point2D?> ToPoint2D(this IProducer<ColorSpacePoint?> source, DeliveryPolicy deliveryPolicy = null)
        {
            return source.NullableSelect(p => new Point2D(p.X, p.Y), deliveryPolicy);
        }

        /// <summary>
        /// Converts points in from Kinect color space into 2D points.
        /// </summary>
        /// <param name="source">A stream of points in color space.</param>
        /// <param name="deliveryPolicy">An optional delivery policy.</param>
        /// <returns>A producer that generates transformed 2D points.</returns>
        public static IProducer<Point2D> ToPoint2D(this IProducer<ColorSpacePoint> source, DeliveryPolicy deliveryPolicy = null)
        {
            return source.Select(p => new Point2D(p.X, p.Y), deliveryPolicy);
        }

        /// <summary>
        /// Returns the coordinate system corresponding to a tracked joint from the kinect body.
        /// </summary>
        /// <param name="source">The stream of Kinect body.</param>
        /// <param name="jointType">The joint to return.</param>
        /// <param name="deliveryPolicy">An optional delivery policy.</param>
        /// <returns>A producer that generates the coordinate system for the specified joint if the joint is tracked. If the joint is not tracked, no message is posted on the return stream.</returns>
        public static IProducer<CoordinateSystem> GetTrackedJointPosition(this IProducer<KinectBody> source, JointType jointType, DeliveryPolicy deliveryPolicy = null)
        {
            return source.GetTrackedJointPositionOrDefault(jointType, deliveryPolicy).Where(cs => cs != null, DeliveryPolicy.Unlimited);
        }

        /// <summary>
        /// Returns the coordinate system corresponding to a tracked joint from the kinect body, or null if the specified joint is not currently tracked.
        /// </summary>
        /// <param name="source">The stream of Kinect body.</param>
        /// <param name="jointType">The joint to return.</param>
        /// <param name="deliveryPolicy">An optional delivery policy.</param>
        /// <returns>A producer that generates the coordinate system for the specified joint if the joint is tracker, or null otherwise.</returns>
        public static IProducer<CoordinateSystem> GetTrackedJointPositionOrDefault(this IProducer<KinectBody> source, JointType jointType, DeliveryPolicy deliveryPolicy = null)
        {
            return source.Select(
                body =>
                {
                    var joint = body.Joints.Values.FirstOrDefault(j => j.JointType == jointType && j.TrackingState == TrackingState.Tracked);
                    if (joint == default(Joint))
                    {
                        return null;
                    }
                    else
                    {
                        var jointOrientation = body.JointOrientations.Values.FirstOrDefault(j => j.JointType == jointType);
                        var quaternion = new Quaternion(jointOrientation.Orientation.W, jointOrientation.Orientation.X, jointOrientation.Orientation.Y, jointOrientation.Orientation.Z);
                        var euler = quaternion.ToEulerAngles();
                        var cs = CoordinateSystem.Rotation(euler.Gamma, euler.Beta, euler.Alpha);
                        return cs.TransformBy(CoordinateSystem.Translation(new Vector3D(joint.Position.X, joint.Position.Y, joint.Position.Z)));
                    }
                }, deliveryPolicy);
        }

        /// <summary>
        /// Converts a point from Kinect camera space to a 3D point.
        /// </summary>
        /// <param name="cameraSpacePoint">The Kinect camera space point to convert.</param>
        /// <returns>The corresponding 3D point.</returns>
        public static Point3D ToPoint3D(this CameraSpacePoint cameraSpacePoint)
        {
            return new Point3D(cameraSpacePoint.X, cameraSpacePoint.Y, cameraSpacePoint.Z);
        }

        /// <summary>
        /// Converts points from Kinect camera space to 3D points.
        /// </summary>
        /// <param name="source">Stream of Kinect camera space points.</param>
        /// <param name="deliveryPolicy">An optional delivery policy.</param>
        /// <returns>A producer that generates the corresponding 3D points.</returns>
        public static IProducer<Point3D> ToPoint3D(this IProducer<CameraSpacePoint> source, DeliveryPolicy deliveryPolicy = null)
        {
            return source.Select(p => new Point3D(p.X, p.Y, p.Z), deliveryPolicy);
        }

        /// <summary>
        /// Converts a 3D point to a Kinect camera space point.
        /// </summary>
        /// <param name="point">The 3D point to convert.</param>
        /// <returns>The corresponding Kinect camera space point.</returns>
        public static CameraSpacePoint ToCameraSpacePoint(this Point3D point)
        {
            CameraSpacePoint cameraSpacePoint;
            cameraSpacePoint.X = (float)point.X;
            cameraSpacePoint.Y = (float)point.Y;
            cameraSpacePoint.Z = (float)point.Z;
            return cameraSpacePoint;
        }

        /// <summary>
        /// Converts 3D points to Kinect camera space points.
        /// </summary>
        /// <param name="point">Stream of 3D points to convert.</param>
        /// <param name="deliveryPolicy">An optional delivery policy.</param>
        /// <returns>A producer that generates the corresponding Kinect camera space points.</returns>
        public static IProducer<CameraSpacePoint> ToCameraSpacePoint(this IProducer<Point3D> point, DeliveryPolicy deliveryPolicy = null)
        {
            return point.Select(p => ToCameraSpacePoint(p), deliveryPolicy);
        }

        /// <summary>
        /// Compresses a list of Kinect camera space points.
        /// </summary>
        /// <param name="cameraSpacePoints">Stream of list of Kinect camera space points.</param>
        /// <param name="deliveryPolicy">An optional delivery policy.</param>
        /// <returns>A producer that generates a compressed byte array representation of the given Kinect camera space points.</returns>
        public static IProducer<byte[]> GZipCompressImageProjection(this IProducer<CameraSpacePoint[]> cameraSpacePoints, DeliveryPolicy deliveryPolicy = null)
        {
            var memoryStream = new MemoryStream();
            var memoryStreamLo = new MemoryStream();
            var memoryStreamHi = new MemoryStream();
            byte[] buffer = null;
            return cameraSpacePoints.Select(
                pointArray =>
                {
                    var flatPointList = new List<float>();
                    foreach (var cp in pointArray)
                    {
                        flatPointList.Add(cp.X);
                        flatPointList.Add(cp.Y);
                        flatPointList.Add(cp.Z);
                    }

                    if (buffer == null)
                    {
                        buffer = new byte[flatPointList.Count * 4];
                    }

                    Buffer.BlockCopy(flatPointList.ToArray(), 0, buffer, 0, buffer.Length);
                    memoryStream.Seek(0, SeekOrigin.Begin);
                    using (var compressedStream = new GZipStream(memoryStream, CompressionLevel.Optimal, true))
                    {
                        compressedStream.Write(buffer, 0, buffer.Length);
                    }
                    var output = new byte[memoryStream.Position];
                    memoryStream.Seek(0, SeekOrigin.Begin);
                    memoryStream.Read(output, 0, output.Length);
                    return output;
                }, deliveryPolicy);
        }

        /// <summary>
        /// Uncompresses a list of Kinect 3D points.
        /// </summary>
        /// <param name="compressedBytes">A stream containing a compressed representation of Kinect camera space points.</param>
        /// <param name="deliveryPolicy">An optional delivery policy.</param>
        /// <returns>A producer that generates the corresponding list of 3D points.</returns>
        public static IProducer<List<Point3D>> GZipUncompressImageProjection(this IProducer<byte[]> compressedBytes, DeliveryPolicy deliveryPolicy = null)
        {
            var buffer = new byte[1920 * 1080 * 12];
            return compressedBytes.Select(
                bytes =>
                {
                    using (var compressedStream = new GZipStream(new MemoryStream(bytes), CompressionMode.Decompress))
                    {
                        compressedStream.Read(buffer, 0, buffer.Length);
                    }

                    var floatArray = new float[buffer.Length / 4];
                    Buffer.BlockCopy(buffer, 0, floatArray, 0, buffer.Length);

                    List<Point3D> pointList = new List<Point3D>();
                    for (int i = 0; i < floatArray.Length; i += 3)
                    {
                        pointList.Add(new Point3D(floatArray[i], floatArray[i + 1], floatArray[i + 2]));
                    }
                    return pointList;
                }, deliveryPolicy);
        }

        /// <summary>
        /// Converts a quaternion into an axis/angle representation.
        /// </summary>
        /// <param name="quaternion">Quaternion to convert.</param>
        /// <returns>Axis angle representation corresponding to the quaternion.</returns>
        internal static Vector4 QuaternionAsAxisAngle(Vector4 quaternion)
        {
            Vector4 v;
            float len = (float)Math.Sqrt(quaternion.X * quaternion.X + quaternion.Y * quaternion.Y + quaternion.Z * quaternion.Z);
            v.X = quaternion.X / len;
            v.Y = quaternion.Y / len;
            v.Z = quaternion.Z / len;
            v.W = 2.0f * (float)Math.Atan2(len, quaternion.W);
            return v;
        }

        /// <summary>
        /// Converts a quaternion into a matrix.
        /// </summary>
        /// <param name="quaternion">Quaternion to convert.</param>
        /// <returns>Rotation matrix corresponding to the quaternion.</returns>
        internal static Matrix<double> QuaternionToMatrix(Vector4 quaternion)
        {
            var s = (float)Math.Sqrt(quaternion.X * quaternion.X + quaternion.Y * quaternion.Y + quaternion.Z * quaternion.Z + quaternion.W * quaternion.W);

            if (s  <= float.Epsilon)
            {
                return CoordinateSystem.CreateIdentity(3);
            }

            quaternion.X /= s;
            quaternion.Y /= s;
            quaternion.Z /= s;
            quaternion.W /= s;
            var qij = quaternion.X * quaternion.Y;
            var qik = quaternion.X * quaternion.Z;
            var qir = quaternion.X * quaternion.W;
            var qjk = quaternion.Y * quaternion.Z;
            var qjr = quaternion.Y * quaternion.W;
            var qkr = quaternion.Z * quaternion.W;
            var qii = quaternion.X * quaternion.X;
            var qjj = quaternion.Y * quaternion.Y;
            var qkk = quaternion.Z * quaternion.Z;
            var a00 = 1.0 - 2.0 * (qjj + qkk);
            var a11 = 1.0 - 2.0 * (qii + qkk);
            var a22 = 1.0 - 2.0 * (qii + qjj);
            var a01 = 2.0 * (qij - qkr);
            var a10 = 2.0 * (qij + qkr);
            var a02 = 2.0 * (qik + qjr);
            var a20 = 2.0 * (qik - qjr);
            var a12 = 2.0 * (qjk - qir);
            var a21 = 2.0 * (qjk + qir);
            double[] data =
            {
                a00, a01, a02,
                a10, a11, a12,
                a20, a21, a22
            };
            return MathNet.Numerics.LinearAlgebra.CreateMatrix.Dense(3, 3, data);
        }

        /// <summary>
        /// Converts a rotation matrix to a quaternion.
        /// </summary>
        /// <param name="matrix">Rotation matrix to convert.</param>
        /// <returns>Quaternion that represents the rotation.</returns>
        // Derived from:
        // http://www.euclideanspace.com/maths/geometry/rotations/conversions/matrixToQuaternion/index.htm
        public static Vector4 MatrixToQuaternion(Matrix<double> matrix)
        {
            Vector4 v;
            float trace = (float)(1.0 + matrix[0, 0] + matrix[1, 1] + matrix[2, 2]);
            if (trace > 0)
            {
                var s = Math.Sqrt(trace) * 2.0;
                v.X = (float)((matrix[2, 1] - matrix[1, 2]) / s);
                v.Y = (float)((matrix[0, 2] - matrix[2, 0]) / s);
                v.Z = (float)((matrix[1, 0] - matrix[0, 1]) / s);
                v.W = (float)(s / 4.0);
            }
            else if ((matrix[0, 0] > matrix[1, 1]) && (matrix[0, 0] > matrix[2, 2]))
            {
                var s = Math.Sqrt(1.0 + matrix[0, 0] - matrix[1, 1] - matrix[2, 2]) * 2.0;
                v.X = (float)(s / 4.0);
                v.Y = (float)((matrix[0, 1] + matrix[1, 0]) / s);
                v.Z = (float)((matrix[0, 2] + matrix[2, 0]) / s);
                v.W = (float)((matrix[2, 1] - matrix[1, 2]) / s);
            }
            else if (matrix[1, 1] > matrix[2, 2])
            {
                var s = Math.Sqrt(1.0 + matrix[1, 1] - matrix[0, 0] - matrix[2, 2]) * 2.0;
                v.X = (float)((matrix[0, 1] + matrix[1, 0]) / s);
                v.Y = (float)(s / 4.0);
                v.Z = (float)((matrix[1, 2] + matrix[2, 1]) / s);
                v.W = (float)((matrix[0, 2] - matrix[2, 0]) / s);
            }
            else
            {
                var s = Math.Sqrt(1.0 + matrix[2, 2] - matrix[0, 0] - matrix[1, 1]) * 2.0;
                v.X = (float)((matrix[0, 2] + matrix[2, 0]) / s);
                v.Y = (float)((matrix[1, 2] + matrix[2, 1]) / s);
                v.Z = (float)(s / 4.0);
                v.W = (float)((matrix[1, 0] - matrix[0, 1]) / s);
            }

            return v;
        }
    }
}

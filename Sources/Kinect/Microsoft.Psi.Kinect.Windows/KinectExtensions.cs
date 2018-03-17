// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.

namespace Microsoft.Psi.Kinect
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.IO.Compression;
    using System.Linq;
    using MathNet.Spatial.Euclidean;
    using Microsoft.Kinect;
    using Microsoft.Psi.Imaging;

    /// <summary>
    /// Implements stream operator methods for Kinect
    /// </summary>
    public static class KinectExtensions
    {
        /// <summary>
        /// Returns the position of a given joint in the body
        /// </summary>
        /// <param name="source">The stream of kinect body</param>
        /// <param name="jointType">The type of joint</param>
        /// <param name="deliveryPolicy">Delivery policy</param>
        /// <returns>The joint position as a camera space point</returns>
        public static IProducer<Point3D> GetJointPosition(this IProducer<KinectBody> source, JointType jointType, DeliveryPolicy deliveryPolicy = null)
        {
            return source.Select(kb => kb.Joints[jointType].Position.ToPoint3D(), deliveryPolicy);
        }

        /// <summary>
        /// Projects set of points into 3D
        /// </summary>
        /// <param name="source">Tuple of image, list of points to project, and calibration</param>
        /// <param name="deliveryPolicy">Delivery policy</param>
        /// <returns>Returns a producer that generates a list of transformed points</returns>
        public static IProducer<List<Point3D>> ProjectTo3D(
            this IProducer<(Shared<Image>, List<Point2D>, IKinectCalibration)> source, DeliveryPolicy deliveryPolicy = null)
        {
            var projectTo3D = new ProjectTo3D(source.Out.Pipeline);
            source.PipeTo(projectTo3D.In, deliveryPolicy);
            return projectTo3D.Out;
        }

        /// <summary>
        /// Projects set of points into 3D
        /// </summary>
        /// <param name="source">Producer of image and list of points</param>
        /// <param name="calibration">Producer of IKinectCalibration</param>
        /// <param name="deliveryPolicy">Delivery policy</param>
        /// <returns>Returns a producer that generates a list of transformed points</returns>
        public static IProducer<List<Point3D>> ProjectTo3D(
            this IProducer<(Shared<Image>, List<Point2D>)> source, IProducer<IKinectCalibration> calibration, DeliveryPolicy deliveryPolicy = null)
        {
            return source.Join(calibration, Match.AnyOrDefault<IKinectCalibration>(RelativeTimeInterval.RightBounded(TimeSpan.Zero))).ProjectTo3D(deliveryPolicy);
        }

        /// <summary>
        /// Transforms a point in camera space to another coordinate system
        /// </summary>
        /// <param name="cs">Coordinate system to transform to</param>
        /// <param name="csp">point in camera space</param>
        /// <returns>New point</returns>
        public static CameraSpacePoint Transform(this CoordinateSystem cs, CameraSpacePoint csp)
        {
            return cs.Transform(csp.ToPoint3D()).ToCameraSpacePoint();
        }

        /// <summary>
        /// Transforms a joint to the specified coordinate system
        /// </summary>
        /// <param name="cs">Coordinate system to transform to</param>
        /// <param name="joint">Joint to be transformed</param>
        /// <returns>Transformed joint</returns>
        public static Joint Transform(this CoordinateSystem cs, Joint joint)
        {
            joint.Position = cs.Transform(joint.Position);
            return joint;
        }

        /// <summary>
        /// Transforms joint orientations in the specified body to the specified coordinate system.
        /// </summary>
        /// <param name="cs">Cordinate systemt to transform to</param>
        /// <param name="psiBody">Body to transform</param>
        /// <returns>Body with transformed joints</returns>
        public static KinectBody Transform(this CoordinateSystem cs, KinectBody psiBody)
        {
            var transformed = psiBody.DeepClone();
            foreach (JointType jointType in Enum.GetValues(typeof(JointType)))
            {
                if (transformed.JointOrientations.ContainsKey(jointType))
                {
                    var rot = cs.GetRotationSubMatrix();
                    JointOrientation jointOrientation = transformed.JointOrientations[jointType];
                    var qrot = QuaternionToMatrix(jointOrientation.Orientation);
                    var q = MatrixToQuaternion(rot * qrot);
                    jointOrientation.Orientation.X = q.X;
                    jointOrientation.Orientation.Y = q.Y;
                    jointOrientation.Orientation.Z = q.Z;
                    jointOrientation.Orientation.W = q.W;
                    transformed.JointOrientations[jointType] = jointOrientation;
                }

                if (transformed.Joints.ContainsKey(jointType))
                {
                    transformed.Joints[jointType] = cs.Transform(transformed.Joints[jointType]);
                }
            }

            return transformed;
        }

        /// <summary>
        /// Transforms the specified point by the specified calibration.
        /// NOTE: This method has been deprecated. Use version that takes a tuple instead.
        /// </summary>
        /// <param name="point">Point/Calibration to transform</param>
        /// <param name="calibration">Kinect calibration object</param>
        /// <param name="deliveryPolicy">Delivery policy</param>
        /// <returns>A producer that generates transformed points</returns>
        public static IProducer<Point2D> ToColorSpace(this IProducer<Point3D> point, IProducer<IKinectCalibration> calibration, DeliveryPolicy deliveryPolicy = null)
        {
            return point.Join(calibration, Match.AnyOrDefault<IKinectCalibration>(RelativeTimeInterval.RightBounded(TimeSpan.Zero))).ToColorSpace(deliveryPolicy);
        }

        /// <summary>
        /// Transforms the specified point by the specified calibration
        /// </summary>
        /// <param name="point">Point/Calibration to transform</param>
        /// <param name="deliveryPolicy">Delivery policy</param>
        /// <returns>A producer that generates transformed points</returns>
        public static IProducer<Point2D> ToColorSpace(this IProducer<(Point3D, IKinectCalibration)> point, DeliveryPolicy deliveryPolicy = null)
        {
            return point.Select(m =>
            {
                var (pointMessage, calibrationMessage) = m;
                Point2D? colorSpacePoint = null;
                if (calibrationMessage != default(IKinectCalibration))
                {
                    Point3D pt = new Point3D(pointMessage.X, pointMessage.Y, pointMessage.Z);
                    Point2D pixelPt = calibrationMessage.ColorIntrinsics.ToPixelSpace(pt, true);
                    return pixelPt;
                }
                return colorSpacePoint;
            }).Where(p => p.HasValue, deliveryPolicy).Select(p => p.Value, deliveryPolicy);
        }

        /// <summary>
        /// Converts points in color space into pixel coordinates
        /// </summary>
        /// <param name="source">Source of color space points</param>
        /// <param name="deliveryPolicy">Delivery policy</param>
        /// <returns>Returns a producer that generates transformed points</returns>
        public static IProducer<Point2D?> ToPoint2D(this IProducer<ColorSpacePoint?> source, DeliveryPolicy deliveryPolicy = null)
        {
            return source.NullableSelect(p => new Point2D(p.X, p.Y), deliveryPolicy);
        }

        /// <summary>
        /// Converts points in color space into pixel coordinates
        /// </summary>
        /// <param name="source">Source of color space points</param>
        /// <param name="deliveryPolicy">Delivery policy</param>
        /// <returns>Returns a producer that generates transformed points</returns>
        public static IProducer<Point2D> ToPoint2D(this IProducer<ColorSpacePoint> source, DeliveryPolicy deliveryPolicy = null)
        {
            return source.Select(p => new Point2D(p.X, p.Y), deliveryPolicy);
        }

        /// <summary>
        /// Converts points in color space into 3D coordinates
        /// </summary>
        /// <param name="source">Source of color space points</param>
        /// <param name="deliveryPolicy">Delivery policy</param>
        /// <returns>Returns a producer that generates transformed points</returns>
        public static IProducer<Point3D> ToPoint3D(this IProducer<CameraSpacePoint> source, DeliveryPolicy deliveryPolicy = null)
        {
            return source.Select(p => new Point3D(p.X, p.Y, p.Z), deliveryPolicy);
        }

        /// <summary>
        /// Tracks a joint on the Kinect Body
        /// </summary>
        /// <param name="source">Source of Kinect Bodies</param>
        /// <param name="jointType">Which joint to track</param>
        /// <param name="deliveryPolicy">Delivery policy</param>
        /// <returns>Returns a producer that generates the transformed joint's coordinate system</returns>
        public static IProducer<CoordinateSystem> GetTrackedJointPosition(this IProducer<KinectBody> source, JointType jointType, DeliveryPolicy deliveryPolicy = null)
        {
            return source.GetTrackedJointPositionOrDefault(jointType).Where(cs => cs != null, deliveryPolicy);
        }

        /// <summary>
        /// Tracks a joint on the Kinect Body
        /// </summary>
        /// <param name="source">Source of Kinect Bodies</param>
        /// <param name="jointType">Which joint to track</param>
        /// <param name="deliveryPolicy">Delivery policy</param>
        /// <returns>Returns a producer that generates the coordiante system for the tracked joint</returns>
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
        /// Converts a point from camera space to a 3D point
        /// </summary>
        /// <param name="cameraSpacePoint">Point to convert</param>
        /// <returns>Returns the point as a Point3D</returns>
        public static Point3D ToPoint3D(this CameraSpacePoint cameraSpacePoint)
        {
            return new Point3D(cameraSpacePoint.X, cameraSpacePoint.Y, cameraSpacePoint.Z);
        }

        /// <summary>
        /// Converts a generate 3D point to a point in camera space
        /// </summary>
        /// <param name="point">Point to convert</param>
        /// <returns>Returns the generate 3D point as a point in camera space</returns>
        public static CameraSpacePoint ToCameraSpacePoint(this Point3D point)
        {
            CameraSpacePoint cameraSpacePoint;
            cameraSpacePoint.X = (float)point.X;
            cameraSpacePoint.Y = (float)point.Y;
            cameraSpacePoint.Z = (float)point.Z;
            return cameraSpacePoint;
        }

        /// <summary>
        /// Given a producer of generic 3D points this method returns the point as a camera space point
        /// </summary>
        /// <param name="point">Point to convert</param>
        /// <param name="deliveryPolicy">Delivery policy</param>
        /// <returns>Returns a producer that generates of camera space points</returns>
        public static IProducer<CameraSpacePoint> ToCameraSpacePoint(this IProducer<Point3D> point, DeliveryPolicy deliveryPolicy = null)
        {
            return point.Select(p => ToCameraSpacePoint(p), deliveryPolicy);
        }

        /// <summary>
        /// Compresses a list of camera space points
        /// </summary>
        /// <param name="cameraSpacePoints">List of points in camera space</param>
        /// <param name="deliveryPolicy">Delivery policy</param>
        /// <returns>Returns a producer that generates a compressed byte array of the camera space points</returns>
        public static IProducer<byte[]> GZipCompressImageProjection(this IProducer<CameraSpacePoint[]> cameraSpacePoints, DeliveryPolicy deliveryPolicy = null)
        {
            deliveryPolicy = deliveryPolicy ?? DeliveryPolicy.Unlimited;
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
        /// Given a producer of compressed image projection points this method generates
        /// the uncompressed points
        /// </summary>
        /// <param name="compressedBytes">Producer that generates compressed image projection points</param>
        /// <param name="deliveryPolicy">Delivery policy</param>
        /// <returns>Returns a generator that returns a list of points</returns>
        public static IProducer<List<Point3D>> GZipUncompressImageProjection(this IProducer<byte[]> compressedBytes, DeliveryPolicy deliveryPolicy = null)
        {
            deliveryPolicy = deliveryPolicy ?? DeliveryPolicy.Unlimited;
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
        /// Converts a quaternion into an axis/angle representation
        /// </summary>
        /// <param name="quat">Quaternion to convert</param>
        /// <returns>Axis angle representation</returns>
        internal static Vector4 QuaterionAsAxisAngle(Vector4 quat)
        {
            Vector4 v;
            float len = (float)Math.Sqrt(quat.X * quat.X + quat.Y * quat.Y + quat.Z * quat.Z);
            v.X = quat.X / len;
            v.Y = quat.Y / len;
            v.Z = quat.Z / len;
            v.W = 2.0f * (float)Math.Atan2(len, quat.W);
            return v;
        }

        /// <summary>
        /// Converts a quaternion into a matrix
        /// </summary>
        /// <param name="quat">Quaternion to convert</param>
        /// <returns>Rotation matrix that quaternion represents</returns>
        internal static MathNet.Numerics.LinearAlgebra.Matrix<double> QuaternionToMatrix(Vector4 quat)
        {
            var s = (float)Math.Sqrt(quat.X * quat.X + quat.Y * quat.Y + quat.Z * quat.Z + quat.W * quat.W);
            quat.X /= s;
            quat.Y /= s;
            quat.Z /= s;
            quat.W /= s;
            var qij = quat.X * quat.Y;
            var qik = quat.X * quat.Z;
            var qir = quat.X * quat.W;
            var qjk = quat.Y * quat.Z;
            var qjr = quat.Y * quat.W;
            var qkr = quat.Z * quat.W;
            var qii = quat.X * quat.X;
            var qjj = quat.Y * quat.Y;
            var qkk = quat.Z * quat.Z;
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
        /// Converts a rotation matrix to a quaternion
        /// </summary>
        /// <param name="mat">Rotation matrix to convert</param>
        /// <returns>Quaternion that represents the rotation</returns>
        // Derived from:
        // http://www.euclideanspace.com/maths/geometry/rotations/conversions/matrixToQuaternion/index.htm
        internal static Vector4 MatrixToQuaternion(MathNet.Numerics.LinearAlgebra.Matrix<double> mat)
        {
            Vector4 v;
            float trace = (float)(1.0 + mat[0, 0] + mat[1, 1] + mat[2, 2]);
            if (trace > 0)
            {
                var s = Math.Sqrt(trace) * 2.0;
                v.X = (float)((mat[2, 1] - mat[1, 2]) / s);
                v.Y = (float)((mat[0, 2] - mat[2, 0]) / s);
                v.Z = (float)((mat[1, 0] - mat[0, 1]) / s);
                v.W = (float)(s / 4.0);
            }
            else if ((mat[0, 0] > mat[1, 1]) && (mat[0, 0] > mat[2, 2]))
            {
                var s = Math.Sqrt(1.0 + mat[0, 0] - mat[1, 1] - mat[2, 2]) * 2.0;
                v.X = (float)(s / 4.0);
                v.Y = (float)((mat[0, 1] + mat[1, 0]) / s);
                v.Z = (float)((mat[0, 2] + mat[2, 0]) / s);
                v.W = (float)((mat[2, 1] - mat[1, 2]) / s);
            }
            else if (mat[1, 1] > mat[2, 2])
            {
                var s = Math.Sqrt(1.0 + mat[1, 1] - mat[0, 0] - mat[2, 2]) * 2.0;
                v.X = (float)((mat[0, 1] + mat[1, 0]) / s);
                v.Y = (float)(s / 4.0);
                v.Z = (float)((mat[1, 2] + mat[2, 1]) / s);
                v.W = (float)((mat[0, 2] - mat[2, 0]) / s);
            }
            else
            {
                var s = Math.Sqrt(1.0 + mat[2, 2] - mat[0, 0] - mat[1, 1]) * 2.0;
                v.X = (float)((mat[0, 2] + mat[2, 0]) / s);
                v.Y = (float)((mat[1, 2] + mat[2, 1]) / s);
                v.Z = (float)(s / 4.0);
                v.W = (float)((mat[1, 0] - mat[0, 1]) / s);
            }

            return v;
        }
    }
}

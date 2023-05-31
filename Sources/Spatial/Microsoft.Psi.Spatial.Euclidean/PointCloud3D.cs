// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.

namespace Microsoft.Psi.Spatial.Euclidean
{
    using System.Collections;
    using System.Collections.Generic;
    using System.Linq;
    using MathNet.Numerics.LinearAlgebra;
    using MathNet.Spatial.Euclidean;
    using Microsoft.Psi.Calibration;
    using Microsoft.Psi.Imaging;

    /// <summary>
    /// Represents a point cloud in 3D space.
    /// </summary>
    public class PointCloud3D : IEnumerable<Point3D>
    {
        /// <summary>
        /// Gets the empty point cloud.
        /// </summary>
        public static readonly PointCloud3D Empty = new ();

        private readonly Matrix<double> points = null;

        /// <summary>
        /// Initializes a new instance of the <see cref="PointCloud3D"/> class.
        /// </summary>
        /// <param name="points">The set of points.</param>
        public PointCloud3D(IEnumerable<Point3D> points)
        {
            var count = points.Count();
            if (count == 0)
            {
                return;
            }

            this.points = Matrix<double>.Build.Dense(4, count);
            int i = 0;
            foreach (var point in points)
            {
                this.points[0, i] = point.X;
                this.points[1, i] = point.Y;
                this.points[2, i] = point.Z;
                this.points[3, i] = 1;
                i++;
            }
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="PointCloud3D"/> class.
        /// </summary>
        /// <param name="points">The set of points expressed in a count x 4 matrix.</param>
        public PointCloud3D(Matrix<double> points)
        {
            if (points != null && points.RowCount != 4)
            {
                throw new System.Exception("The points matrix should have 4 rows when constructing a point cloud.");
            }

            this.points = points;
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="PointCloud3D"/> class.
        /// </summary>
        /// <remarks>This private constructor creates an empty point cloud.</remarks>
        private PointCloud3D()
        {
        }

        /// <summary>
        /// Gets a value indicating whether the point cloud is empty.
        /// </summary>
        public bool IsEmpty => this.points == null;

        /// <summary>
        /// Gets the number of points in the point cloud.
        /// </summary>
        public int NumberOfPoints => this.points != null ? this.points.ColumnCount : 0;

        /// <summary>
        /// Gets the centroid of the point cloud.
        /// </summary>
        public Point3D Centroid
        {
            get
            {
                var x = this.points.Row(0).Average();
                var y = this.points.Row(1).Average();
                var z = this.points.Row(2).Average();
                return new Point3D(x, y, z);
            }
        }

        /// <summary>
        /// Create a point cloud from a shared depth image.
        /// </summary>
        /// <param name="depthImage">The depth image.</param>
        /// <param name="depthCameraIntrinsics">The depth camera intrinsics.</param>
        /// <param name="sparsity">An optional parameter to specify how sparsely to sample pixels (by default 1).</param>
        /// <param name="undistort">An optional parameter that specifies whether to undistort when projecting through the intrinsics.</param>
        /// <param name="robustPointsOnly">An optional parameter that indicates to return only robust points (where the nearby depth estimates are not zero).</param>
        /// <returns>The corresponding point cloud.</returns>
        public static PointCloud3D FromDepthImage(Shared<DepthImage> depthImage, ICameraIntrinsics depthCameraIntrinsics, int sparsity = 1, bool undistort = true, bool robustPointsOnly = false)
            => FromDepthImage(depthImage?.Resource, depthCameraIntrinsics, sparsity, undistort, robustPointsOnly);

        /// <summary>
        /// Create a point cloud from a shared depth image.
        /// </summary>
        /// <param name="depthImage">The depth image.</param>
        /// <param name="cameraSpaceMapping">A camera space mapping matrix.</param>
        /// <param name="sparsity">An optional parameter to specify how sparsely to sample pixels (by default 1).</param>
        /// <param name="robustPointsOnly">An optional parameter that indicates to return only robust points (where the nearby depth estimates are not zero).</param>
        /// <returns>The corresponding point cloud.</returns>
        public static PointCloud3D FromDepthImage(Shared<DepthImage> depthImage, Point3D[,] cameraSpaceMapping, int sparsity = 1, bool robustPointsOnly = false)
            => FromDepthImage(depthImage?.Resource, cameraSpaceMapping, sparsity, robustPointsOnly);

        /// <summary>
        /// Create a point cloud from a depth image.
        /// </summary>
        /// <param name="depthImage">The depth image.</param>
        /// <param name="depthCameraIntrinsics">The depth camera intrinsics.</param>
        /// <param name="sparsity">An optional parameter to specify how sparsely to sample pixels (by default 1).</param>
        /// <param name="undistort">An optional parameter that specifies whether to undistort when projecting through the intrinsics.</param>
        /// <param name="robustPointsOnly">An optional parameter that indicates to return only robust points (where the nearby depth estimates are not zero).</param>
        /// <returns>The corresponding point cloud.</returns>
        public static PointCloud3D FromDepthImage(DepthImage depthImage, ICameraIntrinsics depthCameraIntrinsics, int sparsity = 1, bool undistort = true, bool robustPointsOnly = false)
            => FromDepthImage(
                depthImage,
                depthCameraIntrinsics?.GetPixelToCameraSpaceMapping(depthImage.DepthValueSemantics, undistort),
                sparsity,
                robustPointsOnly);

        /// <summary>
        /// Create a point cloud from a depth image.
        /// </summary>
        /// <param name="depthImage">The depth image.</param>
        /// <param name="cameraSpaceMapping">A camera space mapping matrix.</param>
        /// <param name="sparsity">An optional parameter to specify how sparsely to sample pixels (by default 1).</param>
        /// <param name="robustPointsOnly">An optional parameter that indicates to return only robust points (where the nearby depth estimates are not zero).</param>
        /// <returns>The corresponding point cloud.</returns>
        public static PointCloud3D FromDepthImage(DepthImage depthImage, Point3D[,] cameraSpaceMapping, int sparsity = 1, bool robustPointsOnly = false)
        {
            if (depthImage == null || cameraSpaceMapping == null)
            {
                return Empty;
            }

            unsafe
            {
                // First count how many non-zero depth points are there in the image region
                int count = 0;
                ushort* depthFrame = (ushort*)depthImage.ImageData.ToPointer();
                for (int iy = 0; iy < depthImage.Height; iy += sparsity)
                {
                    var previousRow = (iy - 1) * depthImage.Width;
                    var nextRow = (iy + 1) * depthImage.Width;
                    var row = iy * depthImage.Width;

                    for (int ix = 0; ix < depthImage.Width; ix += sparsity)
                    {
                        if (robustPointsOnly)
                        {
                            if (iy > 0 && iy < depthImage.Height - 1 &&
                                ix > 0 && ix < depthImage.Width - 1 &&
                                depthFrame[previousRow + ix - 1] != 0 &&
                                depthFrame[previousRow + ix] != 0 &&
                                depthFrame[previousRow + ix + 1] != 0 &&
                                depthFrame[row + ix - 1] != 0 &&
                                depthFrame[row + ix] != 0 &&
                                depthFrame[row + ix + 1] != 0 &&
                                depthFrame[nextRow + ix - 1] != 0 &&
                                depthFrame[nextRow + ix] != 0 &&
                                depthFrame[nextRow + ix + 1] != 0)
                            {
                                count++;
                            }
                        }
                        else if (depthFrame[row + ix] != 0)
                        {
                            count++;
                        }
                    }
                }

                if (count == 0)
                {
                    return Empty;
                }

                // Then iterate again and compute the points
                var scalingFactor = depthImage.DepthValueToMetersScaleFactor;
                var points = Matrix<double>.Build.Dense(4, count);
                int index = 0;
                for (int iy = 0; iy < depthImage.Height; iy += sparsity)
                {
                    var previousRow = (iy - 1) * depthImage.Width;
                    var nextRow = (iy + 1) * depthImage.Width;
                    var row = iy * depthImage.Width;
                    for (int ix = 0; ix < depthImage.Width; ix += sparsity)
                    {
                        var d = depthFrame[row + ix];
                        var isPointEstimate = robustPointsOnly ?
                            iy > 0 && iy < depthImage.Height - 1 &&
                            ix > 0 && ix < depthImage.Width - 1 &&
                            depthFrame[previousRow + ix - 1] != 0 &&
                            depthFrame[previousRow + ix] != 0 &&
                            depthFrame[previousRow + ix + 1] != 0 &&
                            depthFrame[row + ix - 1] != 0 &&
                            d != 0 &&
                            depthFrame[row + ix + 1] != 0 &&
                            depthFrame[nextRow + ix - 1] != 0 &&
                            depthFrame[nextRow + ix] != 0 &&
                            depthFrame[nextRow + ix + 1] != 0
                            :
                            d != 0;
                        if (isPointEstimate)
                        {
                            var dscaled = d * scalingFactor;
                            var cameraSpacePoint = cameraSpaceMapping[ix, iy];
                            points[0, index] = dscaled * cameraSpacePoint.X;
                            points[1, index] = dscaled * cameraSpacePoint.Y;
                            points[2, index] = dscaled * cameraSpacePoint.Z;
                            points[3, index] = 1;
                            index++;
                        }
                    }
                }

                return new (points);
            }
        }

        /// <summary>
        /// Computes the distance from every point in the cloud to a specified <see cref="Ray3D"/>.
        /// </summary>
        /// <param name="ray3D">The <see cref="Ray3D"/> to compute the distance to.</param>
        /// <returns>A vector containing distances from every point in the cloud to the <see cref="Ray3D"/>.</returns>
        public Vector<double> DistanceTo(Ray3D ray3D)
        {
            if (this.IsEmpty)
            {
                return null;
            }

            // The algorithm computes distances from points to the ray using linear algebra,
            // as described in https://en.wikipedia.org/wiki/Distance_from_a_point_to_a_line
            var p = Matrix<double>.Build.DenseOfRowArrays(
                this.points.Row(0).ToArray(),
                this.points.Row(1).ToArray(),
                this.points.Row(2).ToArray());
            var a = Vector<double>.Build.DenseOfArray(new double[] { ray3D.ThroughPoint.X, ray3D.ThroughPoint.Y, ray3D.ThroughPoint.Z });
            var n = Vector<double>.Build.DenseOfArray(new double[] { ray3D.Direction.X, ray3D.Direction.Y, ray3D.Direction.Z });

            var pointsMinusA = p - Matrix<double>.Build.Dense(3, this.points.ColumnCount, (r, c) => a[r]);

            var pointsMinusADotProductN = pointsMinusA.TransposeThisAndMultiply(n);
            var pointsMinusADotProductNTimesN = Matrix<double>.Build.DenseOfRowArrays(
                pointsMinusADotProductN.Multiply(n[0]).ToArray(),
                pointsMinusADotProductN.Multiply(n[1]).ToArray(),
                pointsMinusADotProductN.Multiply(n[2]).ToArray());

            var final = pointsMinusA - pointsMinusADotProductNTimesN;
            var distances = final.PointwiseMultiply(final).ColumnSums().PointwiseSqrt();

            return distances;
        }

        /// <summary>
        /// Gets the closest point in the cloud to a specified <see cref="Ray3D"/>.
        /// </summary>
        /// <param name="ray3D">The ray to compute the closest point to.</param>
        /// <returns>The closest point in the cloud to the specified ray.</returns>
        /// <remarks>
        /// If multiple points are at the minimum distance, the method returns the
        /// first of these (in the order the points appear in the cloud).
        /// </remarks>
        public Point3D? ClosestPointTo(Ray3D ray3D)
        {
            if (this.IsEmpty)
            {
                return null;
            }

            var distances = this.DistanceTo(ray3D);
            var minDistance = double.MaxValue;
            var minIndex = -1;
            for (int i = 0; i < distances.Count; i++)
            {
                if (distances[i] < minDistance)
                {
                    minDistance = distances[i];
                    minIndex = i;
                }
            }

            return new Point3D(this.points[0, minIndex], this.points[1, minIndex], this.points[2, minIndex]);
        }

        /// <summary>
        /// Gets the intersection between the point cloud and a specified 3D box.
        /// </summary>
        /// <param name="box3D">The specify 3D box.</param>
        /// <returns>A point cloud that falls within the specified 3D box.</returns>
        public PointCloud3D IntersectionWith(Box3D box3D)
        {
            if (this.IsEmpty)
            {
                return Empty;
            }

            // Get the points in box coordinates
            var pointsInBox3DCoordinates = box3D.Pose.Inverse().Multiply(this.points);

            var insidePoints = new List<double[]>();
            for (int i = 0; i < pointsInBox3DCoordinates.ColumnCount; i++)
            {
                if (pointsInBox3DCoordinates[0, i] >= box3D.Bounds.Min.X &&
                    pointsInBox3DCoordinates[0, i] <= box3D.Bounds.Max.X &&
                    pointsInBox3DCoordinates[1, i] >= box3D.Bounds.Min.Y &&
                    pointsInBox3DCoordinates[1, i] <= box3D.Bounds.Max.Y &&
                    pointsInBox3DCoordinates[2, i] >= box3D.Bounds.Min.Z &&
                    pointsInBox3DCoordinates[2, i] <= box3D.Bounds.Max.Z)
                {
                    insidePoints.Add(new double[] { this.points[0, i], this.points[1, i], this.points[2, i], 1 });
                }
            }

            return (insidePoints.Count > 0) ? new PointCloud3D(Matrix<double>.Build.DenseOfColumnArrays(insidePoints.ToArray())) : new ();
        }

        /// <summary>
        /// Transforms the point cloud by a coordinate system.
        /// </summary>
        /// <param name="coordinateSystem">The coordinate system to transform the point cloud by.</param>
        /// <returns>The transformed point cloud.</returns>
        public PointCloud3D TransformBy(CoordinateSystem coordinateSystem) =>
            new (this.IsEmpty ? null : coordinateSystem.Multiply(this.points));

        /// <summary>
        /// Scales the point cloud by a specified value.
        /// </summary>
        /// <param name="scalar">The value to scale the point cloud by.</param>
        /// <returns>The scaled point cloud.</returns>
        public PointCloud3D Multiply(double scalar) =>
            new (this.points?.Multiply(scalar));

        /// <summary>
        /// Converts the point cloud to a list of <see cref="Point3D"/>.
        /// </summary>
        /// <returns>The list of <see cref="Point3D"/>.</returns>
        public IEnumerable<Point3D> ToList()
        {
            if (this.IsEmpty)
            {
                return Enumerable.Empty<Point3D>();
            }

            var list = new List<Point3D>(this.points.ColumnCount);
            for (int i = 0; i < this.points.ColumnCount; i++)
            {
                list[i] = new (this.points[0, i], this.points[1, i], this.points[2, i]);
            }

            return list;
        }

        /// <inheritdoc/>
        public IEnumerator<Point3D> GetEnumerator()
        {
            if (this.points != null)
            {
                for (int i = 0; i < this.points.ColumnCount; i++)
                {
                    yield return new (this.points[0, i], this.points[1, i], this.points[2, i]);
                }
            }
        }

        /// <inheritdoc/>
        IEnumerator IEnumerable.GetEnumerator() => this.GetEnumerator();
    }
}
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.

#pragma warning disable SA1649 // File name should match first type name
#pragma warning disable SA1402 // File may only contain a single type

namespace Microsoft.Psi.Spatial.Euclidean.Visualization
{
    using System.Collections.Generic;
    using System.Linq;
    using MathNet.Spatial.Euclidean;
    using Microsoft.Psi.Spatial.Euclidean;
    using Microsoft.Psi.Visualization.Adapters;
    using Microsoft.Psi.Visualization.Data;

    /// <summary>
    /// Adapter for <see cref="Rectangle3D"/> to nullable <see cref="Rectangle3D"/>.
    /// </summary>
    [StreamAdapter]
    public class Rectangle3DToNullableRectangle3DAdapter : StreamAdapter<Rectangle3D, Rectangle3D?>
    {
        /// <inheritdoc/>
        public override Rectangle3D? GetAdaptedValue(Rectangle3D source, Envelope envelope)
            => source;
    }

    /// <summary>
    /// Adapter for list of <see cref="Rectangle3D"/> to list of nullable <see cref="Rectangle3D"/>.
    /// </summary>
    [StreamAdapter]
    public class Rectangle3DListToNullableRectangle3DListAdapter : StreamAdapter<List<Rectangle3D>, List<Rectangle3D?>>
    {
        /// <inheritdoc/>
        public override List<Rectangle3D?> GetAdaptedValue(List<Rectangle3D> source, Envelope envelope)
            => source?.Select(p => p as Rectangle3D?).ToList();
    }

    /// <summary>
    /// Adapter from list of <see cref="Mesh3D"/> to <see cref="PointCloud3D"/>.
    /// </summary>
    [StreamAdapter]
    public class Mesh3DListToPointCloud3DAdapter : StreamAdapter<List<Mesh3D>, PointCloud3D>
    {
        /// <inheritdoc/>
        public override PointCloud3D GetAdaptedValue(List<Mesh3D> source, Envelope envelope)
        {
            if (source == null)
            {
                return PointCloud3D.Empty;
            }

            var points = new List<Point3D>();
            foreach (var mesh in source)
            {
                points.AddRange(mesh.Vertices);
            }

            return new PointCloud3D(points);
        }
    }

    /// <summary>
    /// Adapter from <see cref="Mesh3D"/> to <see cref="PointCloud3D"/>.
    /// </summary>
    [StreamAdapter]
    public class Mesh3DToPointCloud3DAdapter : StreamAdapter<Mesh3D, PointCloud3D>
    {
        /// <inheritdoc/>
        public override PointCloud3D GetAdaptedValue(Mesh3D source, Envelope envelope)
            => source?.ToPointCloud3D();
    }
}
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.

namespace Microsoft.Psi.Visualization.Views.Visuals3D
{
    using System;
    using System.Collections.Generic;
    using System.Windows;
    using System.Windows.Media.Media3D;
    using HelixToolkit.Wpf;

    /// <summary>
    /// Represents a pie slice arrows visual 3D.
    /// </summary>
    public class PieSliceArrowsVisual3D : PieSliceVisual3D
    {
        /// <summary>
        /// The arrows count dependency property.
        /// </summary>
        public static readonly DependencyProperty ArrowsCountProperty =
            DependencyProperty.Register(
                "ArrowsCount",
                typeof(int),
                typeof(PieSliceArrowsVisual3D),
                new UIPropertyMetadata(20, GeometryChanged));

        /// <summary>
        /// Gets or sets the number of arrows.
        /// </summary>
        /// <value>The end angle.</value>
        public int ArrowsCount
        {
            get { return (int)this.GetValue(ArrowsCountProperty); }
            set { this.SetValue(ArrowsCountProperty, value); }
        }

        /// <summary>
        /// Creates a tessellation mesh.
        /// </summary>
        /// <returns>The tessellation mesh.</returns>
        protected override MeshGeometry3D Tessellate()
        {
            var b = new MeshBuilder(false, false);
            var right = Vector3D.CrossProduct(this.UpVector, this.Normal);
            for (int ac = 0; ac < this.ArrowsCount; ac++)
            {
                var middleAngle = this.StartAngle + ((this.EndAngle - this.StartAngle) * (ac + 0.5) / this.ArrowsCount);
                var middleAngleRad = middleAngle / 180 * Math.PI;
                var middleDir = (right * Math.Cos(middleAngleRad)) + (this.UpVector * Math.Sin(middleAngleRad));
                var startAngle = this.StartAngle + ((this.EndAngle - this.StartAngle) * ac / this.ArrowsCount);
                var endAngle = this.StartAngle + ((this.EndAngle - this.StartAngle) * (ac + 1) / this.ArrowsCount);
                var pts = new List<Point3D>();
                for (int i = 0; i < this.ThetaDiv; i++)
                {
                    double angle = startAngle + ((endAngle - startAngle) * i / (this.ThetaDiv - 1));
                    double angleRad = angle / 180 * Math.PI;
                    Vector3D dir = (right * Math.Cos(angleRad)) + (this.UpVector * Math.Sin(angleRad));
                    pts.Add(this.Center + (dir * this.InnerRadius));
                    pts.Add(this.Center + (middleDir * this.OuterRadius));
                }

                b.AddTriangleStrip(pts);
            }

            return b.ToMesh();
        }
    }
}

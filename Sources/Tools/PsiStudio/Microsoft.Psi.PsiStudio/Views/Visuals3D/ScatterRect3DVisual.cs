// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.

namespace Microsoft.Psi.Visualization.Views.Visuals3D
{
    using System.Collections.Generic;
    using System.ComponentModel;
    using System.Linq;
    using System.Windows.Media.Media3D;
    using HelixToolkit.Wpf;
    using MathNet.Spatial.Euclidean;
    using Microsoft.Psi.Visualization.Config;
    using Microsoft.Psi.Visualization.Extensions;
    using Microsoft.Psi.Visualization.VisualizationObjects;

    /// <summary>
    /// Represents a scatter Rect3D visual.
    /// </summary>
    public class ScatterRect3DVisual : ModelVisual3D
    {
        private ScatterRect3DVisualizationObject visualizationObject;
        private List<LinesVisual3D[]> rectangles = null;

        /// <summary>
        /// Initializes a new instance of the <see cref="ScatterRect3DVisual"/> class.
        /// </summary>
        /// <param name="visualizationObject">The scatter Rect3D visualization object.</param>
        public ScatterRect3DVisual(ScatterRect3DVisualizationObject visualizationObject)
        {
            this.rectangles = new List<LinesVisual3D[]>();
            this.visualizationObject = visualizationObject;
            this.visualizationObject.PropertyChanged += this.VisualizationObject_PropertyChanged;
            this.visualizationObject.Configuration.PropertyChanged += this.Configuration_PropertyChanged;
        }

        private void AddRectangle()
        {
            var rectangle = new LinesVisual3D[12];
            for (int i = 0; i < rectangle.Length; i++)
            {
                rectangle[i] = new LinesVisual3D() { Color = this.visualizationObject.Configuration.Color };
                rectangle[i].Points.Add(new System.Windows.Media.Media3D.Point3D(0, 0, 0));
                rectangle[i].Points.Add(new System.Windows.Media.Media3D.Point3D(0, 0, 0));
                this.Children.Add(rectangle[i]);
            }

            this.rectangles.Add(rectangle);
        }

        private void UpdateRectangles()
        {
            foreach (var rectangle in this.rectangles)
            {
                foreach (var line in rectangle)
                {
                    line.Color = this.visualizationObject.Configuration.Color;
                    line.Thickness = this.visualizationObject.Configuration.Thickness;
                }
            }
        }

        private void UpdateProperties()
        {
            var data = this.visualizationObject.CurrentValue.GetValueOrDefault().Data;
            if (data != null)
            {
                for (int i = this.rectangles.Count; i < data.Count; i++)
                {
                    this.AddRectangle();
                }

                for (int i = 0; i < data.Count; i++)
                {
                    var (cs, rect3D) = data[i];
                    this.SetLocation(this.rectangles[i], cs, rect3D);
                }
            }

            var count = data == null ? 0 : data.Count;
            for (int i = count; i < this.rectangles.Count; i++)
            {
                foreach (var line in this.rectangles[i])
                {
                    this.Children.Remove(line);
                }
            }

            if (count < this.rectangles.Count)
            {
                this.rectangles = this.rectangles.Take(count).ToList();
            }
        }

        private void SetLocation(LinesVisual3D[] linesVisual, CoordinateSystem cs, Rect3D rect3D)
        {
            var p0 = cs.Transform(new MathNet.Spatial.Euclidean.Point3D(rect3D.Location.X, rect3D.Location.Y, rect3D.Location.Z)).ToPoint3D();
            var p1 = cs.Transform(new MathNet.Spatial.Euclidean.Point3D(rect3D.Location.X + rect3D.SizeX, rect3D.Location.Y, rect3D.Location.Z)).ToPoint3D();
            var p2 = cs.Transform(new MathNet.Spatial.Euclidean.Point3D(rect3D.Location.X + rect3D.SizeX, rect3D.Location.Y + rect3D.SizeY, rect3D.Location.Z)).ToPoint3D();
            var p3 = cs.Transform(new MathNet.Spatial.Euclidean.Point3D(rect3D.Location.X, rect3D.Location.Y + rect3D.SizeY, rect3D.Location.Z)).ToPoint3D();
            var p4 = cs.Transform(new MathNet.Spatial.Euclidean.Point3D(rect3D.Location.X, rect3D.Location.Y, rect3D.Location.Z + rect3D.SizeZ)).ToPoint3D();
            var p5 = cs.Transform(new MathNet.Spatial.Euclidean.Point3D(rect3D.Location.X + rect3D.SizeX, rect3D.Location.Y, rect3D.Location.Z + rect3D.SizeZ)).ToPoint3D();
            var p6 = cs.Transform(new MathNet.Spatial.Euclidean.Point3D(rect3D.Location.X + rect3D.SizeX, rect3D.Location.Y + rect3D.SizeY, rect3D.Location.Z + rect3D.SizeZ)).ToPoint3D();
            var p7 = cs.Transform(new MathNet.Spatial.Euclidean.Point3D(rect3D.Location.X, rect3D.Location.Y + rect3D.SizeY, rect3D.Location.Z + rect3D.SizeZ)).ToPoint3D();

            linesVisual[0].Points[0] = p0;
            linesVisual[0].Points[1] = p1;
            linesVisual[1].Points[0] = p1;
            linesVisual[1].Points[1] = p2;
            linesVisual[2].Points[0] = p2;
            linesVisual[2].Points[1] = p3;
            linesVisual[3].Points[0] = p3;
            linesVisual[3].Points[1] = p0;
            linesVisual[4].Points[0] = p4;
            linesVisual[4].Points[1] = p5;
            linesVisual[5].Points[0] = p5;
            linesVisual[5].Points[1] = p6;
            linesVisual[6].Points[0] = p6;
            linesVisual[6].Points[1] = p7;
            linesVisual[7].Points[0] = p7;
            linesVisual[7].Points[1] = p4;
            linesVisual[8].Points[0] = p0;
            linesVisual[8].Points[1] = p4;
            linesVisual[9].Points[0] = p1;
            linesVisual[9].Points[1] = p5;
            linesVisual[10].Points[0] = p2;
            linesVisual[10].Points[1] = p6;
            linesVisual[11].Points[0] = p3;
            linesVisual[11].Points[1] = p7;
        }

        private void Configuration_PropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            if ((e.PropertyName == nameof(ScatterRect3DVisualizationObjectConfiguration.Color)) ||
                (e.PropertyName == nameof(ScatterRect3DVisualizationObjectConfiguration.Thickness)))
            {
                this.UpdateRectangles();
            }

            this.UpdateProperties();
        }

        private void VisualizationObject_PropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName == nameof(ScatterRect3DVisualizationObject.Configuration))
            {
                this.visualizationObject.Configuration.PropertyChanged += this.Configuration_PropertyChanged;
                this.UpdateRectangles();
            }

            this.UpdateProperties();
        }
    }
}

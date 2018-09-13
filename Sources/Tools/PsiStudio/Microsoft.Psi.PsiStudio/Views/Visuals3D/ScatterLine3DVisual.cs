// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.

namespace Microsoft.Psi.Visualization.Views.Visuals3D
{
    using System.Collections.Generic;
    using System.ComponentModel;
    using System.Linq;
    using System.Windows.Media.Media3D;
    using HelixToolkit.Wpf;
    using Microsoft.Psi.Visualization.Config;
    using Microsoft.Psi.Visualization.Extensions;
    using Microsoft.Psi.Visualization.VisualizationObjects;

    /// <summary>
    /// Represents a scallter line 3D visual.
    /// </summary>
    public class ScatterLine3DVisual : ModelVisual3D
    {
        private ScatterLine3DVisualizationObject visualizationObject;
        private List<LinesVisual3D> lines = null;

        /// <summary>
        /// Initializes a new instance of the <see cref="ScatterLine3DVisual"/> class.
        /// </summary>
        /// <param name="visualizationObject">The scater line 3D visualization object.</param>
        public ScatterLine3DVisual(ScatterLine3DVisualizationObject visualizationObject)
        {
            this.lines = new List<LinesVisual3D>();
            this.visualizationObject = visualizationObject;
            this.visualizationObject.PropertyChanged += this.VisualizationObject_PropertyChanged;
            this.visualizationObject.Configuration.PropertyChanged += this.Configuration_PropertyChanged;
        }

        private void AddLine()
        {
            var line = new LinesVisual3D() { Color = this.visualizationObject.Configuration.Color };
            this.lines.Add(line);
            this.Children.Add(line);
        }

        private void UpdateLines()
        {
            foreach (var line in this.lines)
            {
                line.Color = this.visualizationObject.Configuration.Color;
                line.Thickness = this.visualizationObject.Configuration.Thickness;
            }
        }

        private void UpdateProperties()
        {
            var linesList = this.visualizationObject.CurrentValue.GetValueOrDefault().Data;
            if (linesList != null)
            {
                for (int i = this.lines.Count; i < linesList.Count; i++)
                {
                    this.AddLine();
                }

                for (int i = 0; i < linesList.Count; i++)
                {
                    this.lines[i].Points[0] = linesList[i].StartPoint.ToPoint3D();
                    this.lines[i].Points[1] = linesList[i].StartPoint.ToPoint3D();
                }
            }

            var count = linesList == null ? 0 : linesList.Count;
            for (int i = count; i < this.lines.Count; i++)
            {
                this.Children.Remove(this.lines[i]);
            }

            if (count < this.lines.Count)
            {
                this.lines = this.lines.Take(count).ToList();
            }
        }

        private void Configuration_PropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            if ((e.PropertyName == nameof(ScatterLine3DVisualizationObjectConfiguration.Color)) ||
                (e.PropertyName == nameof(ScatterLine3DVisualizationObjectConfiguration.Thickness)))
            {
                this.UpdateLines();
            }

            this.UpdateProperties();
        }

        private void VisualizationObject_PropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName == nameof(ScatterLine3DVisualizationObject.Configuration))
            {
                this.visualizationObject.Configuration.PropertyChanged += this.Configuration_PropertyChanged;
                this.UpdateLines();
            }

            this.UpdateProperties();
        }
    }
}

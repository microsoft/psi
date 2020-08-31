// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.

#pragma warning disable SA1305 // Field names must not use Hungarian notation (Fixed many, but numerous xFoo, yFoo remain)

namespace Microsoft.Psi.Visualization.Views.Visuals2D
{
    using System.ComponentModel;
    using System.Windows;
    using System.Windows.Controls;
    using Microsoft.Psi.Visualization.Converters;
    using Microsoft.Psi.Visualization.VisualizationObjects;

    /// <summary>
    /// Interaction logic for ScatterPlotVisualizationObjectView.xaml.
    /// </summary>
    public partial class ScatterPlotVisualizationObjectView : UserControl
    {
        private PlacementConverter xPlacementConverter;
        private PlacementConverter yPlacementConverter;

        /// <summary>
        /// Initializes a new instance of the <see cref="ScatterPlotVisualizationObjectView"/> class.
        /// </summary>
        public ScatterPlotVisualizationObjectView()
        {
            this.InitializeComponent();
            this.xPlacementConverter = this.Resources["XPlacementConverter"] as PlacementConverter;
            this.yPlacementConverter = this.Resources["YPlacementConverter"] as PlacementConverter;
            this.DataContextChanged += this.ScatterPlotVisualizationObjectView_DataContextChanged;
            this.SizeChanged += this.ScatterPlotVisualizationObjectView_SizeChanged;
        }

        /// <summary>
        /// Gets the scatter plot visualization object.
        /// </summary>
        public ScatterPlotVisualizationObject ScatterPlotVisualizationObject { get; private set; }

        private void ScatterPlotVisualizationObjectView_SizeChanged(object sender, SizeChangedEventArgs e)
        {
            this.CalculatePlacementTransform();
        }

        private void ScatterPlotVisualizationObjectView_DataContextChanged(object sender, DependencyPropertyChangedEventArgs e)
        {
            this.ScatterPlotVisualizationObject = (ScatterPlotVisualizationObject)this.DataContext;
            this.ScatterPlotVisualizationObject.PropertyChanged += this.ScatterPlotVisualizationObject_PropertyChanged;
        }

        private void ScatterPlotVisualizationObject_PropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName == nameof(this.ScatterPlotVisualizationObject.XMax) ||
                e.PropertyName == nameof(this.ScatterPlotVisualizationObject.XMin) ||
                e.PropertyName == nameof(this.ScatterPlotVisualizationObject.YMax) ||
                e.PropertyName == nameof(this.ScatterPlotVisualizationObject.YMin))
            {
                this.CalculatePlacementTransform();
            }
        }

        private void CalculatePlacementTransform()
        {
            var contentHeight = this.ScatterPlotVisualizationObject.YMax - this.ScatterPlotVisualizationObject.YMin;
            var contentWidth = this.ScatterPlotVisualizationObject.XMax - this.ScatterPlotVisualizationObject.XMin;

            if (contentHeight == 0 && contentWidth == 0)
            {
                contentHeight = 1;
                contentWidth = 1;
            }

            double aspectRatioContent = contentWidth / contentHeight;
            double aspectRatioControl = this.ActualWidth / this.ActualHeight;

            if (aspectRatioControl > aspectRatioContent)
            {
                // control is wider than needed so content will stretch full height
                this.xPlacementConverter.Scale = this.yPlacementConverter.Scale = this.ActualHeight / contentHeight;
                double leftoverWidth = this.ActualWidth - (this.xPlacementConverter.Scale * contentWidth);
                this.Inset.Width = this.ActualWidth - leftoverWidth;
                this.Inset.Height = this.ActualHeight;
            }
            else
            {
                // control is taller than needed so content will stretch full width
                this.xPlacementConverter.Scale = this.yPlacementConverter.Scale = this.ActualWidth / contentWidth;
                double leftoverHeight = this.ActualHeight - (this.yPlacementConverter.Scale * contentHeight);
                this.Inset.Height = this.ActualHeight - leftoverHeight;
                this.Inset.Width = this.ActualWidth;
            }

            this.xPlacementConverter.Offset = -this.ScatterPlotVisualizationObject.XMin;
            this.yPlacementConverter.Offset = -this.ScatterPlotVisualizationObject.YMin;
        }
    }
}

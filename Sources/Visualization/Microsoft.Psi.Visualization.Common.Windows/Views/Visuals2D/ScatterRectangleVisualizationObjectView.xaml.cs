// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.

namespace Microsoft.Psi.Visualization.Views.Visuals2D
{
    using System;
    using System.ComponentModel;
    using System.Drawing;
    using System.Windows;
    using System.Windows.Controls;
    using Microsoft.Psi.Visualization.Collections;
    using Microsoft.Psi.Visualization.Converters;
    using Microsoft.Psi.Visualization.VisualizationObjects;

    /// <summary>
    /// Interaction logic for ScatterRectangleVisualizationObjectView.xaml.
    /// </summary>
    public partial class ScatterRectangleVisualizationObjectView : UserControl
    {
        private PlacementConverter placementConverter;
        private ObservableDataCollection<Tuple<Rectangle, string>> dataCollection = new ObservableDataCollection<Tuple<Rectangle, string>>();

        /// <summary>
        /// Initializes a new instance of the <see cref="ScatterRectangleVisualizationObjectView"/> class.
        /// </summary>
        public ScatterRectangleVisualizationObjectView()
        {
            this.InitializeComponent();
            this.placementConverter = this.Resources["PlacementConverter"] as PlacementConverter;
            this.DataContextChanged += this.OnDataContextChanged;
            this.SizeChanged += this.OnSizeChanged;
        }

        /// <summary>
        /// Gets the scatter rectangle visualization object.
        /// </summary>
        public ScatterRectangleVisualizationObject ScatterRectangleVisualizationObject { get; private set; }

        /// <summary>
        /// Gets the data collection.
        /// </summary>
        public ObservableDataCollection<Tuple<Rectangle, string>> DataCollection => this.dataCollection;

        private void CalculatePlacementTransform()
        {
            double aspectRatioContent = this.ScatterRectangleVisualizationObject.Width / this.ScatterRectangleVisualizationObject.Height;
            double aspectRatioControl = this.ActualWidth / this.ActualHeight;

            if (aspectRatioControl > aspectRatioContent)
            {
                // control is wider than needed so content will stretch full height
                this.placementConverter.Scale = this.ActualHeight / this.ScatterRectangleVisualizationObject.Height;
                double leftoverWidth = this.ActualWidth - (this.placementConverter.Scale * this.ScatterRectangleVisualizationObject.Width);
                this.Inset.Width = this.ActualWidth - leftoverWidth;
                this.Inset.Height = this.ActualHeight;
            }
            else
            {
                // control is taller than needed so content will stretch full width
                this.placementConverter.Scale = this.ActualWidth / this.ScatterRectangleVisualizationObject.Width;
                double leftoverHeight = this.ActualHeight - (this.placementConverter.Scale * this.ScatterRectangleVisualizationObject.Height);
                this.Inset.Height = this.ActualHeight - leftoverHeight;
                this.Inset.Width = this.ActualWidth;
            }
        }

        private void OnDataContextChanged(object sender, DependencyPropertyChangedEventArgs e)
        {
            this.ScatterRectangleVisualizationObject = this.DataContext as ScatterRectangleVisualizationObject;
            this.ScatterRectangleVisualizationObject.PropertyChanged += this.OnDataContextPropertyChanged;
        }

        private void OnDataContextPropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName == nameof(this.ScatterRectangleVisualizationObject.CurrentValue))
            {
                this.DataCollection.SetSource(this.ScatterRectangleVisualizationObject.CurrentValue.GetValueOrDefault().Data);
            }
            else if (e.PropertyName == nameof(this.ScatterRectangleVisualizationObject.Width) || e.PropertyName == nameof(this.ScatterRectangleVisualizationObject.Height))
            {
                this.CalculatePlacementTransform();
            }
        }

        private void OnSizeChanged(object sender, SizeChangedEventArgs e)
        {
            this.CalculatePlacementTransform();
        }
    }
}

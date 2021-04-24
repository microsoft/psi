// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.

namespace Microsoft.Psi.Visualization.Views.Visuals2D
{
    using System.ComponentModel;
    using Microsoft.Psi.Visualization.VisualizationObjects;

    /// <summary>
    /// Provides an abstract base class for a canvas-based view of a <see cref="XYValueVisualizationObject{TData}"/>.
    /// </summary>
    /// <typeparam name="TXYValueVisualizationObject">The type of the XY value visualization object.</typeparam>
    /// <typeparam name="TData">The type of the stream data.</typeparam>
    public abstract class XYValueVisualizationObjectCanvasView<TXYValueVisualizationObject, TData> :
        StreamValueVisualizationObjectCanvasView<TXYValueVisualizationObject, TData>
        where TXYValueVisualizationObject : XYValueVisualizationObject<TData>, new()
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="XYValueVisualizationObjectCanvasView{TXYVisualizationObject, TData}"/> class.
        /// </summary>
        public XYValueVisualizationObjectCanvasView()
            : base()
        {
        }

        /// <summary>
        /// Gets the XY visualization object.
        /// </summary>
        public TXYValueVisualizationObject XYValueVisualizationObject =>
            this.VisualizationObject as TXYValueVisualizationObject;

        /// <inheritdoc/>
        protected override void OnVisualizationObjectPropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            base.OnVisualizationObjectPropertyChanged(sender, e);

            if (e.PropertyName == nameof(this.XYValueVisualizationObject.XAxis) ||
                e.PropertyName == nameof(this.XYValueVisualizationObject.YAxis))
            {
                if (this.UpdateTransforms())
                {
                    this.OnTransformsChanged();
                }
            }
        }

        /// <inheritdoc/>
        protected override bool UpdateTransforms()
        {
            double oldScaleX = this.ScaleTransform.ScaleX;
            double oldScaleY = this.ScaleTransform.ScaleY;
            double oldTranslateX = this.TranslateTransform.X;
            double oldTranslateY = this.TranslateTransform.Y;

            double contentWidth = this.XYValueVisualizationObject.XAxis.Maximum - this.XYValueVisualizationObject.XAxis.Minimum;
            double contentHeight = this.XYValueVisualizationObject.YAxis.Maximum - this.XYValueVisualizationObject.YAxis.Minimum;

            double contentAspectRatio = contentWidth / contentHeight;
            double controlAspectRatio = this.ActualWidth / this.ActualHeight;

            if (controlAspectRatio > contentAspectRatio)
            {
                // control is wider than needed so content will stretch full height
                var scaleFactor = contentHeight == 0 ? 0 : this.ActualHeight / contentHeight;
                this.ScaleTransform.ScaleX = scaleFactor;
                this.ScaleTransform.ScaleY = scaleFactor;
                double leftoverWidth = this.ActualWidth - (this.ScaleTransform.ScaleX * contentWidth);
                this.TranslateTransform.X = leftoverWidth / (2 * this.ScaleTransform.ScaleX);
                this.TranslateTransform.Y = 0;
            }
            else
            {
                // control is taller than needed so content will stretch full width
                var scaleFactor = contentWidth == 0 ? 0 : this.ActualWidth / contentWidth;
                this.ScaleTransform.ScaleX = scaleFactor;
                this.ScaleTransform.ScaleY = scaleFactor;
                double leftoverHeight = this.ActualHeight - (this.ScaleTransform.ScaleY * contentHeight);
                this.TranslateTransform.X = 0;
                this.TranslateTransform.Y = leftoverHeight / (2 * this.ScaleTransform.ScaleY);
            }

            return
                oldScaleX != this.ScaleTransform.ScaleX ||
                oldScaleY != this.ScaleTransform.ScaleY ||
                oldTranslateX != this.TranslateTransform.X ||
                oldTranslateY != this.TranslateTransform.Y;
        }
    }
}

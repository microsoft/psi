// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.

namespace Microsoft.Psi.Visualization.Views.Visuals2D
{
    using System.Collections.Generic;
    using System.ComponentModel;
    using System.Windows.Media;
    using Microsoft.Psi.Data.Annotations;
    using Microsoft.Psi.Visualization.VisualizationObjects;

    /// <summary>
    /// Interaction logic for TimeIntervalAnnotationVisualizationObjectView.xaml.
    /// </summary>
    public partial class TimeIntervalAnnotationVisualizationObjectView : TimelineCanvasVisualizationObjectView<TimeIntervalAnnotationVisualizationObject, TimeIntervalAnnotation>
    {
        private List<TimeIntervalAnnotationVisualizationObjectViewItem> items = new List<TimeIntervalAnnotationVisualizationObjectViewItem>();

        /// <summary>
        /// The collection of brushes used in rendering.
        /// </summary>
        private Dictionary<Color, Brush> brushes = new Dictionary<Color, Brush>();

        /// <summary>
        /// Initializes a new instance of the <see cref="TimeIntervalAnnotationVisualizationObjectView"/> class.
        /// </summary>
        public TimeIntervalAnnotationVisualizationObjectView()
        {
            this.InitializeComponent();
            this.Canvas = this._DynamicCanvas;
        }

        /// <summary>
        /// Returns a brush with the requested System.Drawing.Color from the brushes cache.
        /// </summary>
        /// <param name="systemDrawingColor">The color of the brush to return.</param>
        /// <returns>A brush of the requested color.</returns>
        internal Brush GetBrush(System.Drawing.Color systemDrawingColor)
        {
            Color color = Color.FromArgb(systemDrawingColor.A, systemDrawingColor.R, systemDrawingColor.G, systemDrawingColor.B);

            if (!this.brushes.ContainsKey(color))
            {
                this.brushes.Add(color, new SolidColorBrush(color));
            }

            return this.brushes[color];
        }

        /// <inheritdoc/>
        protected override void OnVisualizationObjectPropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            base.OnVisualizationObjectPropertyChanged(sender, e);
            if (e.PropertyName == nameof(this.VisualizationObject.DisplayData) ||
                e.PropertyName == nameof(this.VisualizationObject.LineWidth) ||
                e.PropertyName == nameof(this.VisualizationObject.Padding) ||
                e.PropertyName == nameof(this.VisualizationObject.FontSize))
            {
                this.OnDisplayDataChanged();
            }
        }

        /// <summary>
        /// Implements a response to display data changing.
        /// </summary>
        protected virtual void OnDisplayDataChanged()
        {
            this.Rerender();
        }

        /// <inheritdoc/>
        protected override void OnTransformsChanged()
        {
            this.Rerender();
        }

        private void Rerender()
        {
            for (int i = 0; i < this.VisualizationObject.DisplayData.Count; i++)
            {
                TimeIntervalAnnotationVisualizationObjectViewItem item;

                if (i < this.items.Count)
                {
                    item = this.items[i];
                }
                else
                {
                    item = new TimeIntervalAnnotationVisualizationObjectViewItem(this, this.VisualizationObject.DisplayData[i]);
                    this.items.Add(item);
                }

                item.Update(this.VisualizationObject.DisplayData[i]);
            }

            // remove the remaining figures
            for (int i = this.VisualizationObject.DisplayData.Count; i < this.items.Count; i++)
            {
                var item = this.items[this.VisualizationObject.DisplayData.Count];
                item.RemoveFromCanvas();
                this.items.Remove(item);
            }
        }
    }
}
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.

namespace Microsoft.Psi.Visualization.Views.Visuals2D
{
    using System.Windows;
    using System.Windows.Controls;
    using System.Windows.Media;

    /// <summary>
    /// Represents a track view item, used to display the track label.
    /// </summary>
    internal class TimeIntervalAnnotationTrackVisualizationObjectViewItem
    {
        private readonly TimeIntervalAnnotationVisualizationObjectView parent;
        private readonly Grid grid;

        /// <summary>
        /// Initializes a new instance of the <see cref="TimeIntervalAnnotationTrackVisualizationObjectViewItem"/> class.
        /// </summary>
        /// <param name="parent">The parent view.</param>
        /// <param name="trackIndex">The track index.</param>
        /// <param name="trackCount">The number of tracks.</param>
        /// <param name="trackName">The track name.</param>
        internal TimeIntervalAnnotationTrackVisualizationObjectViewItem(TimeIntervalAnnotationVisualizationObjectView parent, int trackIndex, int trackCount, string trackName)
        {
            this.parent = parent;

            this.grid = new Grid
            {
                RenderTransform = new TranslateTransform(),
                IsHitTestVisible = false,
                Height = this.parent.Canvas.ActualHeight / trackCount,
            };

            (this.grid.RenderTransform as TranslateTransform).X = 0;
            (this.grid.RenderTransform as TranslateTransform).Y = trackIndex * this.parent.Canvas.ActualHeight / trackCount;

            var border = new Border()
            {
                BorderBrush = new SolidColorBrush(Colors.DarkGray),
                Background = new SolidColorBrush(Color.FromRgb(0x2D, 0x2D, 0x30)),
                BorderThickness = new Thickness(1),
                VerticalAlignment = VerticalAlignment.Center,
                Child = new TextBlock()
                {
                    Foreground = new SolidColorBrush(Colors.DarkGray),
                    VerticalAlignment = VerticalAlignment.Center,
                    HorizontalAlignment = HorizontalAlignment.Center,
                    TextWrapping = TextWrapping.Wrap,
                    Margin = new Thickness(3),
                    IsHitTestVisible = false,
                    Text = trackName,
                    FontSize = this.parent.StreamVisualizationObject.FontSize,
                },
            };

            this.grid.Children.Add(border);
            this.parent.Canvas.Children.Add(this.grid);
        }

        /// <summary>
        /// Updates the track view.
        /// </summary>
        /// <param name="trackIndex">The new track index.</param>
        /// <param name="trackCount">The new track count.</param>
        /// <param name="trackName">The new track name.</param>
        internal void Update(int trackIndex, int trackCount, string trackName)
        {
            this.grid.Height = this.parent.Canvas.ActualHeight / trackCount;
            ((this.grid.Children[0] as Border).Child as TextBlock).Text = trackName;
            (this.grid.RenderTransform as TranslateTransform).X = 0;
            (this.grid.RenderTransform as TranslateTransform).Y = trackIndex * this.parent.Canvas.ActualHeight / trackCount;
        }

        /// <summary>
        /// Removes the item from the parent canvas.
        /// </summary>
        internal void RemoveFromCanvas() => this.parent.Canvas.Children.Remove(this.grid);
    }
}
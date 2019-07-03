// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.

namespace Microsoft.Psi.Visualization.Views
{
    using System.Windows;
    using System.Windows.Controls;
    using System.Windows.Documents;
    using System.Windows.Media;
    using System.Windows.Media.Imaging;

    /// <summary>
    /// UI Element to render a Visualization Panel's outline during drag and drop operations.
    /// </summary>
    public class VisualizationContainerDragDropAdorner : Adorner
    {
        private readonly SolidColorBrush renderBrush;
        private Rect panelRect = new Rect(0, 0, 0, 0);
        private ImageSource image = null;

        // The distance the mouse pointer was away from the panel's top edge
        // when the drag operation was initiated.  Used when calculating the new
        // rectangle position in response to the user dragging the adorner.
        private double mouseOffsetFromTop = 0d;

        /// <summary>
        /// Initializes a new instance of the <see cref="VisualizationContainerDragDropAdorner"/> class.
        /// </summary>
        /// <param name="visualizationContainerItemsGrid">The Items DataGrid of the Visualization Container View.</param>
        public VisualizationContainerDragDropAdorner(DataGrid visualizationContainerItemsGrid)
            : base(visualizationContainerItemsGrid)
        {
            this.IsHitTestVisible = false;
            this.IsEnabled = false;
            this.Visibility = Visibility.Hidden;
            this.Opacity = 0.4;
            this.renderBrush = new SolidColorBrush(Colors.LightGray)
            {
                Opacity = 0.2,
            };
        }

        /// <summary>
        /// Gets the current vertical center of the Panel image.
        /// </summary>
        public int VerticalCenter => (int)((this.panelRect.Top + this.panelRect.Bottom) / 2);

        /// <summary>
        /// Sets the new location and size of the Adorner and makes it visible.
        /// </summary>
        /// <param name="mousePosition">The current position of the mouse cursor.</param>
        /// <param name="mouseOffsetFromTop">The distance the mouse was from the top edge of the panel when dragging was initiated.</param>
        /// <param name="panelSize">The size of the Visualization Panel being dragged.</param>
        /// <param name="bitmap">An image of the Visualization Panel being dragged.</param>
        public void Show(Point mousePosition, double mouseOffsetFromTop, Size panelSize, BitmapSource bitmap)
        {
            this.mouseOffsetFromTop = mouseOffsetFromTop;
            this.panelRect.Size = panelSize;
            this.SetPanelLocation(mousePosition);
            this.image = bitmap;
            this.Visibility = Visibility.Visible;
        }

        /// <summary>
        /// Sets the new location of the Adorner in response to a mouse move while dragging.
        /// </summary>
        /// <param name="mousePosition">The current Mouse position.</param>
        public void SetPanelLocation(Point mousePosition)
        {
            this.panelRect.Location = new Point(0, mousePosition.Y - this.mouseOffsetFromTop);
            this.InvalidateVisual();
        }

        /// <summary>
        /// Hides the Visualization Panel drag and drop adorner.
        /// </summary>
        public void Hide()
        {
            this.Visibility = Visibility.Hidden;
        }

        /// <summary>
        /// Called when the Adorner is to be rendered.
        /// </summary>
        /// <param name="drawingContext">The drawing context.</param>
        protected override void OnRender(DrawingContext drawingContext)
        {
            if (this.Visibility == Visibility.Visible)
            {
                drawingContext.DrawRectangle(this.renderBrush, null, this.panelRect);
                drawingContext.DrawImage(this.image, this.panelRect);
            }
        }
    }
}

// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.

namespace Microsoft.Psi.Visualization.Views
{
    using System;
    using System.Windows;
    using System.Windows.Input;
    using System.Windows.Media;
    using System.Windows.Media.Imaging;
    using Microsoft.Psi.Visualization;
    using Microsoft.Psi.Visualization.Helpers;
    using Microsoft.Psi.Visualization.Navigation;
    using Microsoft.Psi.Visualization.VisualizationPanels;

    /// <summary>
    /// Interaction logic for TimelineVisualizationPanelView.xaml.
    /// </summary>
    public partial class TimelineVisualizationPanelView : VisualizationPanelView
    {
        private Point lastMousePosition = new (0, 0);
        private DragOperation currentDragOperation = DragOperation.None;

        /// <summary>
        /// Initializes a new instance of the <see cref="TimelineVisualizationPanelView"/> class.
        /// </summary>
        public TimelineVisualizationPanelView()
        {
            this.InitializeComponent();
        }

        private enum DragOperation
        {
            None,
            PanelReorder,
            TimelineScroll,
        }

        /// <summary>
        /// Gets the timeline visualization panel.
        /// </summary>
        private TimelineVisualizationPanel VisualizationPanel => this.DataContext as TimelineVisualizationPanel;

        /// <summary>
        /// Signals to the panel that a drag and drop operation it may have initiated has been completed.
        /// </summary>
        public void FinishDragDrop()
        {
            this.currentDragOperation = DragOperation.None;
        }

        /// <summary>
        /// Notifies of a change in mouse position while a context menu is being displayed.
        /// </summary>
        /// <param name="sender">The sender of the event.</param>
        /// <param name="e">The mouse event args.</param>
        public void ContextMenuMouseMove(object sender, MouseEventArgs e)
        {
            this.lastMousePosition = e.GetPosition(this);
        }

        /// <inheritdoc />
        protected override void OnRenderSizeChanged(SizeChangedInfo sizeInfo)
        {
            base.OnRenderSizeChanged(sizeInfo);
            if (sizeInfo.WidthChanged)
            {
                // Update panel Width property which some TimelineVisualizationObjects
                // need in order to perform data summarization based on the view width.
                // Not updating the Height property as it is bound to the panel view.
                this.VisualizationPanel.Width = sizeInfo.NewSize.Width;
            }
        }

        private void Root_MouseMove(object sender, MouseEventArgs e)
        {
            Point mousePosition = e.GetPosition(this);

            // If the user has the Left Mouse button pressed, initiate a Drag & Drop reorder operation.
            if ((e.LeftButton == MouseButtonState.Pressed) && (mousePosition != this.lastMousePosition))
            {
                switch (this.currentDragOperation)
                {
                    case DragOperation.None:
                        this.BeginDragOperation();
                        break;
                    case DragOperation.TimelineScroll:
                        this.DoDragTimeline(mousePosition);
                        break;
                }
            }
            else
            {
                this.currentDragOperation = DragOperation.None;
                this.Cursor = Cursors.Arrow;
            }

            this.lastMousePosition = mousePosition;
        }

        private void ReorderThumb_MouseMove(object sender, MouseEventArgs e)
        {
            // If the user has the Left Mouse button pressed, and we're not near the bottom edge
            // of the panel (where resizing occurs), then initiate a Drag & Drop reorder operation
            var mousePosition = e.GetPosition(this);

            if (e.LeftButton == MouseButtonState.Pressed && !DragDropHelper.MouseNearPanelBottomEdge(mousePosition, this.ActualHeight))
            {
                var data = new DataObject();
                data.SetData(DragDropDataName.DragDropOperation, DragDropOperation.ReorderPanel);
                data.SetData(DragDropDataName.VisualizationPanel, this.VisualizationPanel);
                data.SetData(DragDropDataName.MouseOffsetFromTop, mousePosition.Y);
                data.SetData(DragDropDataName.PanelSize, new Size?(new Size(this.ActualWidth, this.ActualHeight)));
                var renderTargetBitmap = new RenderTargetBitmap((int)this.ActualWidth, (int)this.ActualHeight, 96, 96, PixelFormats.Pbgra32);
                renderTargetBitmap.Render(this);
                data.SetImage(renderTargetBitmap);

                DragDrop.DoDragDrop(this, data, DragDropEffects.Move);
            }
        }

        private void BeginDragOperation()
        {
            // Only drag the timeline if the navigator is currently paused
            if (VisualizationContext.Instance.VisualizationContainer.Navigator.CursorMode == CursorMode.Manual)
            {
                this.currentDragOperation = DragOperation.TimelineScroll;
                this.Cursor = Cursors.Hand;
            }
        }

        private void DoDragTimeline(Point mousePosition)
        {
            // Calculate how far we dragged the panel (timewise)
            double percent = (mousePosition.X - this.lastMousePosition.X) / this.ActualWidth;
            NavigatorRange viewRange = this.VisualizationPanel.Navigator.ViewRange;
            TimeSpan timeMoved = TimeSpan.FromTicks((long)((double)viewRange.Duration.Ticks * percent));

            // Scroll the view
            viewRange.ScrollBy(-timeMoved);
        }
    }
}

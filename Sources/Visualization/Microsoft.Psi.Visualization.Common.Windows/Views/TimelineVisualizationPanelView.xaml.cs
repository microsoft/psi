// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.

namespace Microsoft.Psi.Visualization.Views
{
    using System;
    using System.Windows;
    using System.Windows.Controls;
    using System.Windows.Input;
    using System.Windows.Media;
    using System.Windows.Media.Imaging;
    using Microsoft.Psi.PsiStudio;
    using Microsoft.Psi.PsiStudio.Common;
    using Microsoft.Psi.Visualization.Common;
    using Microsoft.Psi.Visualization.Helpers;
    using Microsoft.Psi.Visualization.Navigation;
    using Microsoft.Psi.Visualization.VisualizationPanels;

    /// <summary>
    /// Interaction logic for TimelineVisualizationPanelView.xaml.
    /// </summary>
    public partial class TimelineVisualizationPanelView : VisualizationPanelViewBase
    {
        private Point lastMousePosition = new Point(0, 0);
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

        private void Root_ContextMenuOpening(object sender, ContextMenuEventArgs e)
        {
            if ((Keyboard.Modifiers & ModifierKeys.Shift) == ModifierKeys.Shift)
            {
                // eat context menu opening, when shift key is pressed (dropping end selection marker)
                e.Handled = true;
            }
        }

        private void Root_MouseMove(object sender, MouseEventArgs e)
        {
            Point mousePosition = e.GetPosition(this);

            // If the user has the Left Mouse button pressed, initiate a Drag & Drop reorder operation
            if (e.LeftButton == MouseButtonState.Pressed)
            {
                switch (this.currentDragOperation)
                {
                    case DragOperation.None:
                        this.BeginDragOperation(mousePosition);
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
            e.Handled = true;
        }

        private void BeginDragOperation(Point mousePosition)
        {
            // If the mouse moved mostly horizontally, then we'll begin a timeline scroll
            // operation, otherwise we'll begin a Visualization Panel reorder operation
            if (this.IsHorizontalDrag(mousePosition))
            {
                // Only drag the timeline if the navigator is currently paused
                if (VisualizationContext.Instance.VisualizationContainer.Navigator.CursorMode == CursorMode.Manual)
                {
                    this.currentDragOperation = DragOperation.TimelineScroll;
                    this.DoDragTimeline(mousePosition);
                    this.Cursor = Cursors.Hand;
                }
            }
            else
            {
                if (!DragDropHelper.MouseNearPanelBottomEdge(mousePosition, this.ActualHeight))
                {
                    this.currentDragOperation = DragOperation.PanelReorder;

                    DataObject data = new DataObject();
                    data.SetData(DragDropDataName.DragDropOperation, DragDropOperation.ReorderPanel);
                    data.SetData(DragDropDataName.VisualizationPanel, this.VisualizationPanel);
                    data.SetData(DragDropDataName.VisualizationPanelView, this);
                    data.SetData(DragDropDataName.MouseOffsetFromTop, mousePosition.Y);
                    data.SetData(DragDropDataName.PanelSize, new Size?(new Size(this.ActualWidth, this.ActualHeight)));
                    RenderTargetBitmap renderTargetBitmap = new RenderTargetBitmap((int)this.ActualWidth, (int)this.ActualHeight, 96, 96, PixelFormats.Pbgra32);
                    renderTargetBitmap.Render(this);
                    data.SetImage(renderTargetBitmap);

                    DragDrop.DoDragDrop(this, data, DragDropEffects.Move);
                    this.Cursor = Cursors.Hand;
                }
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

        private bool IsHorizontalDrag(Point mousePosition)
        {
            // Users will most likely be wanting to scroll the panel horizontally much more often
            // than they'll re-order the panels, so only call this a Vertical drag if the Y mouse
            // movement is at least 3 times the X mouse movement.
            return 3 * Math.Abs(mousePosition.X - this.lastMousePosition.X) > Math.Abs(mousePosition.Y - this.lastMousePosition.Y);
        }
    }
}

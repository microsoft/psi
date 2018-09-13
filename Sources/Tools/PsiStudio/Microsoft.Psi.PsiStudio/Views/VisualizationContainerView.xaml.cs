// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.

namespace Microsoft.Psi.Visualization.Views
{
    using System;
    using System.Windows;
    using System.Windows.Controls;
    using System.Windows.Documents;
    using System.Windows.Input;
    using System.Windows.Media.Imaging;
    using Microsoft.Psi.Visualization.VisualizationObjects;
    using Microsoft.Psi.Visualization.VisualizationPanels;

    /// <summary>
    /// Interaction logic for VisualizationContainerView.xaml
    /// </summary>
    public partial class VisualizationContainerView : UserControl
    {
        // This adorner renders a shadow of a Visualization Panel while it's being dragged by the mouse
        private VisualizationContainerDragDropAdorner dragDropAdorner = null;

        /// <summary>
        /// Initializes a new instance of the <see cref="VisualizationContainerView"/> class.
        /// </summary>
        public VisualizationContainerView()
        {
            this.InitializeComponent();
        }

        private void Items_DragEnter(object sender, DragEventArgs e)
        {
            // Make sure this is an object we care about
            string dragOperation = e.Data.GetData("DragOperation") as string;
            if (dragOperation == "ReorderPanels")
            {
                // Make sure the Drag & Drop adorner exists
                this.CreateDragDropAdorner();

                // Get the data we'll need to render the adorner
                double mouseOffset = (double)e.Data.GetData("MouseOffsetFromTop");
                Size panelSize = (e.Data.GetData("PanelSize") as Size?).Value;
                BitmapSource bitmap = ((DataObject)e.Data).GetImage();

                // Show the adorner
                this.dragDropAdorner.Show(e.GetPosition(this.Items), mouseOffset, panelSize, bitmap);

                this.Cursor = Cursors.Hand;

                e.Handled = true;
            }
        }

        private void Items_DragOver(object sender, DragEventArgs e)
        {
            string dragOperation = e.Data.GetData("DragOperation") as string;
            if (dragOperation == "ReorderPanels")
            {
                this.dragDropAdorner.SetPanelLocation(e.GetPosition(this.Items));
                this.Cursor = Cursors.Hand;
                e.Handled = true;
            }
        }

        private void Items_DragLeave(object sender, DragEventArgs e)
        {
            string dragOperation = e.Data.GetData("DragOperation") as string;
            if (dragOperation == "ReorderPanels")
            {
                this.dragDropAdorner.Hide();
                this.Cursor = Cursors.Arrow;
                e.Handled = true;
            }
        }

        private void Items_Drop(object sender, DragEventArgs e)
        {
            if (e.Handled == false)
            {
                string dragOperation = e.Data.GetData("DragOperation") as string;
                if (dragOperation == "ReorderPanels")
                {
                    // Get the VisualizationPanel that's being dropped
                    VisualizationPanel droppedPanel = e.Data.GetData("VisualizationPanel") as VisualizationPanel;
                    if (droppedPanel != null)
                    {
                        // Find the index of the panel being moved, and the index we should move it to
                        int moveToIndex = this.FindPanelMoveIndices(droppedPanel, this.dragDropAdorner.VerticalCenter, out int moveFromIndex);

                        // Check that we're not just trying to put the panel back where it started
                        if (moveFromIndex != moveToIndex)
                        {
                            droppedPanel.Container.Panels.Move(moveFromIndex, moveToIndex);
                        }

                        // Timeline Visualization Panels have multiple drag & drop operation types, only one of which
                        // can be in effect at any time.  If the panel being dropped is one of those then we need to
                        // signal to it that this drag operation is done.
                        TimelineVisualizationPanelView visualizationPanelView = e.Data.GetData("VisualizationPanelView") as TimelineVisualizationPanelView;
                        if (visualizationPanelView != null)
                        {
                            visualizationPanelView.FinishDragDrop();
                        }
                    }

                    this.dragDropAdorner.Hide();
                    this.Cursor = Cursors.Arrow;
                }

                e.Handled = true;
            }
        }

        private void CreateDragDropAdorner()
        {
            if (this.dragDropAdorner == null)
            {
                this.dragDropAdorner = new VisualizationContainerDragDropAdorner(this.Items);
                AdornerLayer.GetAdornerLayer(this.Items).Add(this.dragDropAdorner);
            }
        }

        private int FindPanelMoveIndices(VisualizationPanel droppedPanel, int panelVerticalCenter, out int currentPanelIndex)
        {
            // Find the index of the panel whose vertical center is closest the the panel being dragged's vertical center
            VisualizationContainer visualizationContainer = droppedPanel.Container;
            currentPanelIndex = -1;

            // Work out which Visualization Panel's vertical center is closest to the vertical center of the panel being dropped
            double currentVerticalCenter = 0;
            double minDelta = double.MaxValue;
            int targetIndex = -1;
            for (int index = 0; index < visualizationContainer.Panels.Count; index++)
            {
                VisualizationPanel visualizationPanel = visualizationContainer.Panels[index];

                // If this is the panel we're dropping, we need that info too
                if (visualizationPanel == droppedPanel)
                {
                    currentPanelIndex = index;
                }

                // Is this panel's vertical center closer to our panel's vertical center
                currentVerticalCenter += visualizationPanel.Height / 2;
                double deltaY = Math.Abs(panelVerticalCenter - currentVerticalCenter);
                if (deltaY < minDelta)
                {
                    targetIndex = index;
                    minDelta = deltaY;
                }

                currentVerticalCenter += visualizationPanel.Height / 2;
            }

            return targetIndex;
        }
    }
}

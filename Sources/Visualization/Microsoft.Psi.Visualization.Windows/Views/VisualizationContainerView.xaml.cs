// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.

namespace Microsoft.Psi.Visualization.Views
{
    using System;
    using System.Linq;
    using System.Windows;
    using System.Windows.Controls;
    using System.Windows.Documents;
    using System.Windows.Input;
    using System.Windows.Media;
    using System.Windows.Media.Imaging;
    using Microsoft.Psi.Visualization;
    using Microsoft.Psi.Visualization.ViewModels;
    using Microsoft.Psi.Visualization.VisualizationObjects;
    using Microsoft.Psi.Visualization.VisualizationPanels;

    /// <summary>
    /// Interaction logic for VisualizationContainerView.xaml.
    /// </summary>
    public partial class VisualizationContainerView : UserControl
    {
        // This adorner renders a shadow of a Visualization Panel while it's being dragged by the mouse
        private VisualizationContainerDragDropAdorner dragDropAdorner = null;
        private VisualizationPanelViewBase hitTestResult = null;

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
            string dragOperation = e.Data.GetData(DragDropDataName.DragDropOperation) as string;
            if (dragOperation == DragDropOperation.ReorderPanel)
            {
                // Make sure the Drag & Drop adorner exists
                this.CreateDragDropAdorner();

                // Get the data we'll need to render the adorner
                double mouseOffset = (double)e.Data.GetData(DragDropDataName.MouseOffsetFromTop);
                Size panelSize = (e.Data.GetData(DragDropDataName.PanelSize) as Size?).Value;
                BitmapSource bitmap = ((DataObject)e.Data).GetImage();

                // Show the adorner
                this.dragDropAdorner.Show(e.GetPosition(this.Items), mouseOffset, panelSize, bitmap);

                this.Cursor = Cursors.Hand;

                e.Handled = true;
            }
        }

        private void Items_DragOver(object sender, DragEventArgs e)
        {
            string dragOperation = e.Data.GetData(DragDropDataName.DragDropOperation) as string;
            if (dragOperation == DragDropOperation.ReorderPanel)
            {
                this.dragDropAdorner.SetPanelLocation(e.GetPosition(this.Items));
                this.Cursor = Cursors.Hand;
                e.Handled = true;
            }
            else if (dragOperation == DragDropOperation.DragDropStream)
            {
                StreamTreeNode streamTreeNode = e.Data.GetData(DragDropDataName.StreamTreeNode) as StreamTreeNode;
                Point mousePosition = e.GetPosition(this.Items);
                var visualizers = VisualizationContext.Instance.PluginMap.GetCompatibleVisualizers(
                    streamTreeNode, this.GetVisualizationPanelUnderMouse(mousePosition), isUniversal: false, isInNewPanel: false);
                e.Effects = visualizers.Any() ? DragDropEffects.Move : DragDropEffects.None;
                e.Handled = true;
            }
        }

        private void Items_DragLeave(object sender, DragEventArgs e)
        {
            string dragOperation = e.Data.GetData(DragDropDataName.DragDropOperation) as string;
            if (dragOperation == DragDropOperation.ReorderPanel)
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
                string dragOperation = e.Data.GetData(DragDropDataName.DragDropOperation) as string;
                if (dragOperation == DragDropOperation.ReorderPanel)
                {
                    this.DropReorderPanels(e);
                }
                else if (dragOperation == DragDropOperation.DragDropStream)
                {
                    this.DropStream(e);
                }

                this.Cursor = Cursors.Arrow;
                e.Handled = true;
            }
        }

        private void DropReorderPanels(DragEventArgs e)
        {
            // Get the VisualizationPanel that's being dropped
            VisualizationPanel droppedPanel = e.Data.GetData(DragDropDataName.VisualizationPanel) as VisualizationPanel;
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
                TimelineVisualizationPanelView visualizationPanelView = e.Data.GetData(DragDropDataName.VisualizationPanelView) as TimelineVisualizationPanelView;
                if (visualizationPanelView != null)
                {
                    visualizationPanelView.FinishDragDrop();
                }
            }

            this.dragDropAdorner.Hide();
        }

        private void DropStream(DragEventArgs e)
        {
            StreamTreeNode streamTreeNode = e.Data.GetData(DragDropDataName.StreamTreeNode) as StreamTreeNode;
            if (streamTreeNode != null)
            {
                // Get the mouse position
                Point mousePosition = e.GetPosition(this.Items);

                // Get the visualization panel (if any) that the mouse is above
                VisualizationPanel visualizationPanel = this.GetVisualizationPanelUnderMouse(mousePosition);

                // Get the type of messages in the stream
                Type dataType = VisualizationContext.Instance.GetDataType(streamTreeNode.NodeTypeName);

                // Get the list of commands that are compatible with the user dropping the stream here
                var visualizers = VisualizationContext.Instance.PluginMap.GetCompatibleVisualizers(
                    streamTreeNode, visualizationPanel, isUniversal: false, isInNewPanel: false);

                // If there's any compatible visualization commands, select the most appropriate one and execute it
                if (visualizers.Any())
                {
                    VisualizationContext.Instance.VisualizeStream(streamTreeNode, VisualizerMetadata.GetClosestVisualizerMetadata(dataType, visualizers), visualizationPanel);
                }
            }
        }

        private VisualizationPanel GetVisualizationPanelUnderMouse(Point mousePosition)
        {
            // Find out if the mouse is above an existing Visualization Panel
            this.hitTestResult = null;
            VisualTreeHelper.HitTest(
                this.Items,
                new HitTestFilterCallback(this.HitTestFilter),
                new HitTestResultCallback(this.HitTestResultCallback),
                new PointHitTestParameters(mousePosition));

            // Get the visualization panel that the stream was dropped over (if any)
            return this.hitTestResult != null ? this.hitTestResult.DataContext as VisualizationPanel : null;
        }

        private HitTestFilterBehavior HitTestFilter(DependencyObject dependencyObject)
        {
            if (dependencyObject is VisualizationPanelViewBase)
            {
                this.hitTestResult = dependencyObject as VisualizationPanelViewBase;
                return HitTestFilterBehavior.Stop;
            }

            return HitTestFilterBehavior.Continue;
        }

        private HitTestResultBehavior HitTestResultCallback(HitTestResult result)
        {
            return HitTestResultBehavior.Continue;
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
            // Find the index of the panel whose vertical center is closest the panel being dragged's vertical center
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

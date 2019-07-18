// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.

namespace Microsoft.Psi.Visualization.Views
{
    using System;
    using System.Collections.Generic;
    using System.Windows;
    using System.Windows.Controls;
    using System.Windows.Documents;
    using System.Windows.Input;
    using System.Windows.Media;
    using System.Windows.Media.Imaging;
    using Microsoft.Psi.PsiStudio;
    using Microsoft.Psi.PsiStudio.Common;
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
        private TimelineVisualizationPanelView hitTestResult = null;

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
                // Get the list of Visualization Commands we can execute on this stream
                List<TypeKeyedActionCommand> commands = PsiStudioContext.Instance.GetVisualizeStreamCommands(streamTreeNode);

                // Find out if the mouse is above an existing Visualization Panel
                this.hitTestResult = null;
                Point pt = e.GetPosition(this.Items);
                VisualTreeHelper.HitTest(
                    this.Items,
                    new HitTestFilterCallback(this.HitTestFilter),
                    new HitTestResultCallback(this.HitTestResultCallback),
                    new PointHitTestParameters(pt));

                // If we're above an existing Visualization Panel and there exists some "plot" commands, then execute
                // the "plot in new panel" command, otherwise execute whatever plot or visualize command we can find.
                if (this.hitTestResult != null)
                {
                    if (this.ExecuteCommandIfPresent(commands, ContextMenuName.Visualize, streamTreeNode))
                    {
                        return;
                    }

                    if (this.ExecuteCommandIfPresent(commands, ContextMenuName.VisualizeAsMilliseconds, streamTreeNode))
                    {
                        return;
                    }
                }
                else
                {
                    if (this.ExecuteCommandIfPresent(commands, ContextMenuName.VisualizeInNewPanel, streamTreeNode))
                    {
                        return;
                    }

                    if (this.ExecuteCommandIfPresent(commands, ContextMenuName.VisualizeAsMillisecondsInNewPanel, streamTreeNode))
                    {
                        return;
                    }

                    if (this.ExecuteCommandIfPresent(commands, ContextMenuName.Visualize, streamTreeNode))
                    {
                        return;
                    }

                    if (this.ExecuteCommandIfPresent(commands, ContextMenuName.VisualizeAs2DDepth, streamTreeNode))
                    {
                        return;
                    }

                    if (this.ExecuteCommandIfPresent(commands, ContextMenuName.VisualizeAs3DDepth, streamTreeNode))
                    {
                        return;
                    }

                    if (this.ExecuteCommandIfPresent(commands, ContextMenuName.VisualizeAsPlanarDirection, streamTreeNode))
                    {
                        return;
                    }
                }
            }
        }

        private bool ExecuteCommandIfPresent(List<TypeKeyedActionCommand> commands, string commandName, StreamTreeNode streamTreeNode)
        {
            // Check if the command is in the list
            TypeKeyedActionCommand command = commands.Find(o => o.DisplayName.Equals(commandName, StringComparison.Ordinal));
            if (command != null)
            {
                VisualizationContainer visualizationContainer = this.DataContext as VisualizationContainer;

                // If we're adding the stream to an existing Visualization Panel, then make sure it's the selected panel first.
                // Otherwise, make sure the last current panel is selected so that the new panel is created at the bottom.
                if (this.hitTestResult != null)
                {
                    VisualizationPanel visualizationPanel = this.hitTestResult.DataContext as VisualizationPanel;
                    visualizationContainer.CurrentPanel = visualizationPanel;
                }
                else if (visualizationContainer.Panels.Count > 0)
                {
                    visualizationContainer.CurrentPanel = visualizationContainer.Panels[visualizationContainer.Panels.Count - 1];
                }

                // Execute the command
                command.Execute(streamTreeNode);
                return true;
            }

            return false;
        }

        private HitTestFilterBehavior HitTestFilter(DependencyObject dependencyObject)
        {
            // We only want to "add to current panel" if that panel is a Timeline panel, other types don't support this
            if (dependencyObject is TimelineVisualizationPanelView)
            {
                this.hitTestResult = dependencyObject as TimelineVisualizationPanelView;
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

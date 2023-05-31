// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.

namespace Microsoft.Psi.Visualization.Views
{
    using System.Collections.Generic;
    using System.Linq;
    using System.Windows;
    using System.Windows.Controls;
    using System.Windows.Documents;
    using System.Windows.Input;
    using System.Windows.Media;
    using System.Windows.Media.Imaging;
    using Microsoft.Psi.Visualization;
    using Microsoft.Psi.Visualization.Helpers;
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
        private VisualizationPanelView hitTestResult = null;

        // The collection of context menu sources for the context menu that's about to be displayed.
        private List<IContextMenuItemsSource> contextMenuItemsSources;

        /// <summary>
        /// Initializes a new instance of the <see cref="VisualizationContainerView"/> class.
        /// </summary>
        public VisualizationContainerView()
        {
            this.InitializeComponent();
            this.ContextMenu = new ContextMenu();
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

                this.Cursor = Cursors.ScrollNS;

                e.Handled = true;
            }
        }

        private void Items_DragOver(object sender, DragEventArgs e)
        {
            string dragOperation = e.Data.GetData(DragDropDataName.DragDropOperation) as string;
            if (dragOperation == DragDropOperation.ReorderPanel)
            {
                this.dragDropAdorner.SetPanelLocation(e.GetPosition(this.Items));
                this.Cursor = Cursors.ScrollNS;
                e.Handled = true;
            }
            else if (dragOperation == DragDropOperation.DragDropStream)
            {
                var streamTreeNode = e.Data.GetData(DragDropDataName.StreamTreeNode) as StreamTreeNode;
                var mousePosition = e.GetPosition(this.Items);
                var visualizers = streamTreeNode.GetCompatibleVisualizers(
                    this.GetVisualizationPanelUnderMouse(mousePosition),
                    isUniversal: false,
                    isInNewPanel: false);
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
            if (e.Data.GetData(DragDropDataName.VisualizationPanel) is VisualizationPanel droppedVisualizationPanel)
            {
                // Find the index of the panel being moved, and the index we should move it to
                int moveToIndex = this.FindPanelMoveIndices(droppedVisualizationPanel, this.dragDropAdorner.VerticalCenter, out int moveFromIndex);

                // Check that we're not just trying to put the panel back where it started
                if (moveFromIndex != moveToIndex)
                {
                    droppedVisualizationPanel.Container.Panels.Move(moveFromIndex, moveToIndex);
                }

                // Timeline Visualization Panels have multiple drag & drop operation types, only one of which
                // can be in effect at any time.  If the panel being dropped is one of those then we need to
                // signal to it that this drag operation is done.
                if (e.Data.GetData(DragDropDataName.VisualizationPanelView) is TimelineVisualizationPanelView timelineVisualizationPanelView)
                {
                    timelineVisualizationPanelView.FinishDragDrop();
                }
            }

            this.dragDropAdorner.Hide();
        }

        private void DropStream(DragEventArgs e)
        {
            if (e.Data.GetData(DragDropDataName.StreamTreeNode) is StreamTreeNode streamTreeNode)
            {
                // Get the mouse position
                Point mousePosition = e.GetPosition(this.Items);

                // Get the visualization panel (if any) that the mouse is above
                VisualizationPanel visualizationPanel = this.GetVisualizationPanelUnderMouse(mousePosition);

                // Get the list of commands that are compatible with the user dropping the stream here
                var visualizers = streamTreeNode.GetCompatibleVisualizers(visualizationPanel, isUniversal: false, isInNewPanel: false);

                // If there are compatible visualization commands, select the first one
                if (visualizers.Any())
                {
                    VisualizationContext.Instance.VisualizeStream(streamTreeNode, visualizers.First(), visualizationPanel);
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
            if (dependencyObject is VisualizationPanelView visualizationPanelView &&
                (visualizationPanelView.DataContext as VisualizationPanel).IsShown)
            {
                this.hitTestResult = visualizationPanelView;
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

        private int FindPanelMoveIndices(VisualizationPanel droppedPanel, int position, out int currentPanelIndex)
        {
            // Find the index of the panel whose vertical center is closest the panel being dragged's vertical center
            var visualizationContainer = droppedPanel.Container;
            currentPanelIndex = visualizationContainer.Panels.IndexOf(droppedPanel);

            // Look through the shown panels and figure out when we are below the previous vertical center by above
            // the next vertical center
            double previousVerticalCenter = double.NaN;
            double currentLocation = double.NaN;

            for (int i = 0; i < visualizationContainer.Panels.Count; i++)
            {
                if (visualizationContainer.Panels[i].IsShown)
                {
                    if (double.IsNaN(previousVerticalCenter))
                    {
                        currentLocation = visualizationContainer.Panels[i].Height / 2;
                    }
                    else
                    {
                        currentLocation += visualizationContainer.Panels[i].Height / 2;

                        // Now, if we are after the previous location but before the current one
                        if (position > previousVerticalCenter && position <= currentLocation)
                        {
                            // Then we've found the drop position
                            return currentPanelIndex < i ? i - 1 : i;
                        }
                    }

                    // Save the previous vertical center
                    previousVerticalCenter = currentLocation;

                    // Increment the position
                    currentLocation += visualizationContainer.Panels[i].Height / 2;
                }
            }

            return visualizationContainer.Panels.Count - 1;
        }

        private void OnContextMenuOpening(object sender, ContextMenuEventArgs e)
        {
            // If the shift key is down, the the user is dropping the end selection
            // marker, so in this case we won't show the context menu.
            if ((Keyboard.Modifiers & ModifierKeys.Shift) == ModifierKeys.Shift)
            {
                e.Handled = true;
                return;
            }

            // Clear the existing context menu
            this.ContextMenu.Items.Clear();

            // Create a new context menu metadata to be filled by the hit tester
            this.contextMenuItemsSources = new List<IContextMenuItemsSource>();

            // Run a hit test at the mouse cursor
            VisualTreeHelper.HitTest(
                this,
                new HitTestFilterCallback(this.ContestMenuHitTestFilter),
                new HitTestResultCallback(this.ContextMenuHitTestResult),
                new PointHitTestParameters(Mouse.GetPosition(this)));

            // If a visualization panel is amount the sources, set it as the current visualization panel.
            if (this.contextMenuItemsSources.FirstOrDefault(s => s is VisualizationPanel) is VisualizationPanel visualizationPanel)
            {
                // Compute the visualization panel corresponding for that view
                visualizationPanel.IsTreeNodeSelected = true;

                // If there's only a single visualization object view then insert its context menu items
                // inline, otherwise generate a separate cascading menu for each visualization object view.
                bool addVisualizationObjectCommandsInSubmenus = visualizationPanel.VisualizationObjects.Count() > 1;
                foreach (var visualizationObject in visualizationPanel.VisualizationObjects)
                {
                    ItemsControl root = this.ContextMenu;

                    // If we're adding a cascading menu
                    if (addVisualizationObjectCommandsInSubmenus)
                    {
                        // Then add the top level of the cascading menu to the main context menu and set the root
                        // to it instead.
                        if (root.Items.Count > 0)
                        {
                            root.Items.Add(new Separator());
                        }

                        var newRoot = MenuItemHelper.CreateMenuItem(IconSourcePath.Stream, visualizationObject.Name, null);
                        root.Items.Add(newRoot);
                        root = newRoot;
                    }

                    this.AddContextMenuItems(root, visualizationObject.ContextMenuItemsInfo());
                }
            }

            // Add the context menu items for the visualization panel, instant panel, and visualization container.
            foreach (var contextMenuItemSource in this.contextMenuItemsSources)
            {
                this.AddContextMenuItems(this.ContextMenu, contextMenuItemSource.ContextMenuItemsInfo());
            }
        }

        // Filter to exclude invisible UI elements from hit test
        private HitTestFilterBehavior ContestMenuHitTestFilter(DependencyObject dependencyObject)
        {
            if (dependencyObject is UIElement element && !element.IsVisible)
            {
                return HitTestFilterBehavior.ContinueSkipSelfAndChildren;
            }

            return HitTestFilterBehavior.Continue;
        }

        // Return the result of the hit test to the callback.
        private HitTestResultBehavior ContextMenuHitTestResult(HitTestResult result)
        {
            var dependencyObject = result.VisualHit;
            while (dependencyObject != null)
            {
                // If the dependency object is a framework element
                if (dependencyObject is FrameworkElement frameworkElement)
                {
                    // Optimization: If we have reached the visualization container view level,
                    // we can stop since it's the top level visual we care about.
                    if (frameworkElement is VisualizationContainerView)
                    {
                        break;
                    }

                    // If the dependency object is not a hidden panel
                    if (!(frameworkElement is VisualizationPanelView visualizationPanelView && !(visualizationPanelView.DataContext as VisualizationPanel).IsShown))
                    {
                        // If the corresponding data context is a context menu item source and not a
                        // visualization object view, then add it to the collection. The context menu
                        // items for visualization objects will be collected later from the
                        // visualization panel.
                        if (frameworkElement.DataContext is IContextMenuItemsSource contextMenuItemsSource &&
                            frameworkElement.DataContext is not VisualizationObject)
                        {
                            // If the source has not been already added
                            if (!this.contextMenuItemsSources.Contains(contextMenuItemsSource))
                            {
                                // Add the source to the set of sources
                                this.contextMenuItemsSources.Add(contextMenuItemsSource);
                            }
                        }
                    }
                }

                // Traverse up the tree
                dependencyObject = VisualTreeHelper.GetParent(dependencyObject);
            }

            // Set the behavior to return visuals at all z-order levels.
            return HitTestResultBehavior.Continue;
        }

        private void AddContextMenuItems(ItemsControl itemsControl, List<ContextMenuItemInfo> commands)
        {
            // Add the context menu items to the context menu root.
            if (commands != null && commands.Any())
            {
                if (itemsControl.Items.Count > 0)
                {
                    itemsControl.Items.Add(new Separator());
                }

                foreach (var command in commands)
                {
                    if (command != null)
                    {
                        if (command.HasSubItems)
                        {
                            var subMenu = MenuItemHelper.CreateMenuItem(null, command.DisplayName, null);
                            itemsControl.Items.Add(subMenu);
                            this.AddContextMenuItems(subMenu, command.SubItems);
                        }
                        else
                        {
                            itemsControl.Items.Add(
                                MenuItemHelper.CreateMenuItem(
                                    command.IconSourcePath,
                                    command.DisplayName,
                                    command.Command,
                                    command.Tag,
                                    command.IsEnabled,
                                    command.CommandParameter));
                        }
                    }
                    else
                    {
                        itemsControl.Items.Add(new Separator());
                    }
                }
            }
        }
    }
}

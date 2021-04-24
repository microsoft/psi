// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.

namespace Microsoft.Psi.Visualization.Views
{
    using System;
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
    using Xceed.Wpf.AvalonDock.Controls;

    /// <summary>
    /// Interaction logic for VisualizationContainerView.xaml.
    /// </summary>
    public partial class VisualizationContainerView : UserControl, IContextMenuItemsSource
    {
        // This adorner renders a shadow of a Visualization Panel while it's being dragged by the mouse
        private VisualizationContainerDragDropAdorner dragDropAdorner = null;
        private VisualizationPanelView hitTestResult = null;

        // The collection of context menu sources for the context menu that's about to be displayed.
        private Dictionary<ContextMenuItemsSourceType, IContextMenuItemsSource> contextMenuSources;

        /// <summary>
        /// Initializes a new instance of the <see cref="VisualizationContainerView"/> class.
        /// </summary>
        public VisualizationContainerView()
        {
            this.InitializeComponent();
            this.ContextMenu = new ContextMenu();
        }

        /// <inheritdoc/>
        public ContextMenuItemsSourceType ContextMenuItemsSourceType => ContextMenuItemsSourceType.VisualizationContainer;

        /// <inheritdoc/>
        public string ContextMenuObjectName => string.Empty;

        /// <inheritdoc/>
        public void AppendContextMenuItems(List<MenuItem> menuItems)
        {
            if (this.DataContext is VisualizationContainer visualizationContainer)
            {
                menuItems.Add(MenuItemHelper.CreateMenuItem(IconSourcePath.ZoomToSelection, "Zoom to Selection", visualizationContainer.ZoomToSelectionCommand));
                menuItems.Add(MenuItemHelper.CreateMenuItem(IconSourcePath.ClearSelection, "Clear Selection", visualizationContainer.ClearSelectionCommand));
                menuItems.Add(MenuItemHelper.CreateMenuItem(IconSourcePath.ZoomToSession, "Zoom to Session Extents", visualizationContainer.ZoomToSessionExtentsCommand));
            }
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
            if (dependencyObject is VisualizationPanelView)
            {
                this.hitTestResult = dependencyObject as VisualizationPanelView;
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

        private void OnContextMenuOpening(object sender, ContextMenuEventArgs e)
        {
            // If the shift key is down, the the user is dropping the end selection
            // marker, so in this case we won't show the context menu.
            if ((Keyboard.Modifiers & ModifierKeys.Shift) == ModifierKeys.Shift)
            {
                e.Handled = true;
                return;
            }

            // Create a new context menu metadata to be filled by the hit tester
            this.contextMenuSources = new Dictionary<ContextMenuItemsSourceType, IContextMenuItemsSource>();

            // Run a hit test at the mouse cursor
            VisualTreeHelper.HitTest(
                this,
                null,
                new HitTestResultCallback(this.ContextMenuHitTestResult),
                new PointHitTestParameters(Mouse.GetPosition(this)));

            // If a visualization panel was found, set it as the current visualization panel.
            if (this.contextMenuSources.ContainsKey(ContextMenuItemsSourceType.VisualizationPanel))
            {
                VisualizationPanel visualizationPanel = (this.contextMenuSources[ContextMenuItemsSourceType.VisualizationPanel] as VisualizationPanelView).DataContext as VisualizationPanel;
                visualizationPanel.IsTreeNodeSelected = true;
            }

            // Clear the existing context menu
            this.ContextMenu.Items.Clear();

            if (this.contextMenuSources.ContainsKey(ContextMenuItemsSourceType.VisualizationPanel))
            {
                // Find all of the visualization object views that are children of the panel view.
                VisualizationPanelView panelView = this.contextMenuSources[ContextMenuItemsSourceType.VisualizationPanel] as VisualizationPanelView;
                IEnumerable<VisualizationObjectView> visualizationObjectViews = panelView.FindVisualChildren<VisualizationObjectView>();
                IEnumerator<VisualizationObjectView> viewEnumerator = visualizationObjectViews.GetEnumerator();

                // If there's only a single visualization object view then insert its context menu items
                // inline, otherwise generate a separate cascading menu for each visualization object view.
                if (visualizationObjectViews.Count() == 1)
                {
                    viewEnumerator.MoveNext();
                    this.AddContextMenuItems(viewEnumerator.Current, false);
                }
                else
                {
                    while (viewEnumerator.MoveNext())
                    {
                        this.AddContextMenuItems(viewEnumerator.Current, true);
                    }
                }
            }

            // Add the context menu items for the visualization panel, instant panel, and visualization conatiner.
            foreach (IContextMenuItemsSource panelViewSource in this.contextMenuSources.Values)
            {
                this.AddContextMenuItems(panelViewSource, false);
            }
        }

        // Return the result of the hit test to the callback.
        private HitTestResultBehavior ContextMenuHitTestResult(HitTestResult result)
        {
            DependencyObject dependencyObject = result.VisualHit;
            while (dependencyObject != null)
            {
                // If the object is a context menu item source and not a visualization object view, add it to the collection.
                // (The context menu items for the visualization object views will be collected later from the panel)
                if (dependencyObject is IContextMenuItemsSource contextMenuSource && contextMenuSource.ContextMenuItemsSourceType != ContextMenuItemsSourceType.VisualizationObject)
                {
                    this.contextMenuSources[contextMenuSource.ContextMenuItemsSourceType] = contextMenuSource;
                }

                // Optimization: If we're at the visualization container view level,
                // we can stop since it's the top level visual we care about.
                if (dependencyObject is VisualizationContainerView)
                {
                    break;
                }
                else
                {
                    dependencyObject = VisualTreeHelper.GetParent(dependencyObject);
                }
            }

            // Set the behavior to return visuals at all z-order levels.
            return HitTestResultBehavior.Continue;
        }

        private void AddContextMenuItems(IContextMenuItemsSource menuItemSource, bool addAsCascadingMenu)
        {
            // Assume the menu root is the main context menu.
            ItemsControl root = this.ContextMenu;

            // If we're adding a cascading menu, add the top level of the cascading
            // menu to the main context menu and set the root to it instead.
            if (addAsCascadingMenu)
            {
                if (root.Items.Count > 0)
                {
                    root.Items.Add(new Separator());
                }

                ItemsControl newRoot = MenuItemHelper.CreateMenuItem(IconSourcePath.Stream, menuItemSource.ContextMenuObjectName, null);
                root.Items.Add(newRoot);
                root = newRoot;
            }

            // Get the list of context menu items from the context menu items source
            List<MenuItem> menuItems = new List<MenuItem>();
            menuItemSource.AppendContextMenuItems(menuItems);

            // Add the context menu items to the context menu root.
            if (menuItems != null && menuItems.Any())
            {
                if (root.Items.Count > 0)
                {
                    root.Items.Add(new Separator());
                }

                foreach (MenuItem menuItem in menuItems)
                {
                    root.Items.Add(menuItem);
                }
            }
        }
    }
}

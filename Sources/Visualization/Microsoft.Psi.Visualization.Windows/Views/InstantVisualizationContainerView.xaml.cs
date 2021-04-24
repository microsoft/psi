// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.

namespace Microsoft.Psi.Visualization.Views
{
    using System;
    using System.Collections.Generic;
    using System.Collections.Specialized;
    using System.Windows;
    using System.Windows.Controls;
    using System.Windows.Input;
    using System.Windows.Media;
    using System.Windows.Media.Imaging;
    using Microsoft.Psi.Visualization;
    using Microsoft.Psi.Visualization.Helpers;
    using Microsoft.Psi.Visualization.VisualizationPanels;

    /// <summary>
    /// Interaction logic for InstantVisualizationContainerView.xaml.
    /// </summary>
    public partial class InstantVisualizationContainerView : UserControl, IContextMenuItemsSource
    {
        private VisualizationPanel mouseOverVisualizationPanel;

        /// <summary>
        /// Initializes a new instance of the <see cref="InstantVisualizationContainerView"/> class.
        /// </summary>
        public InstantVisualizationContainerView()
        {
            this.InitializeComponent();
            this.DataContextChanged += this.InstantVisualizationContainerView_DataContextChanged;
            this.SizeChanged += this.InstantVisualizationContainerView_SizeChanged;
        }

        /// <inheritdoc/>
        public ContextMenuItemsSourceType ContextMenuItemsSourceType => ContextMenuItemsSourceType.VisualizationPanelMatrixContainer;

        /// <inheritdoc/>
        public string ContextMenuObjectName => string.Empty;

        /// <summary>
        /// Gets the visualization panel.
        /// </summary>
        protected InstantVisualizationContainer VisualizationPanel => (InstantVisualizationContainer)this.DataContext;

        /// <inheritdoc/>
        public void AppendContextMenuItems(List<MenuItem> menuItems)
        {
            if (this.DataContext is InstantVisualizationContainer instantVisualizationContainer)
            {
                // Find the child panel that the mouse is over
                // Run a hit test at the mouse cursor
                this.mouseOverVisualizationPanel = null;
                VisualTreeHelper.HitTest(
                    this,
                    null,
                    new HitTestResultCallback(this.ContextMenuHitTestResult),
                    new PointHitTestParameters(Mouse.GetPosition(this)));

                if (this.mouseOverVisualizationPanel != null)
                {
                    menuItems.Add(MenuItemHelper.CreateMenuItem(IconSourcePath.InstantContainerAddCellLeft, $"Insert Cell to the Left", instantVisualizationContainer.CreateIncreaseCellCountCommand(this.mouseOverVisualizationPanel, true)));
                    menuItems.Add(MenuItemHelper.CreateMenuItem(IconSourcePath.InstantContainerAddCellRight, $"Insert Cell to the Right", instantVisualizationContainer.CreateIncreaseCellCountCommand(this.mouseOverVisualizationPanel, false)));
                    menuItems.Add(MenuItemHelper.CreateMenuItem(null, $"Remove Cell", instantVisualizationContainer.CreateRemoveCellCommand(this.mouseOverVisualizationPanel)));
                    menuItems.Add(MenuItemHelper.CreateMenuItem(IconSourcePath.InstantContainerRemoveCell, $"Remove {instantVisualizationContainer.Name}", instantVisualizationContainer.RemovePanelCommand));
                }
            }
        }

        private HitTestResultBehavior ContextMenuHitTestResult(HitTestResult result)
        {
            // Find the visualization panel view that the mouse is over
            DependencyObject dependencyObject = result.VisualHit;
            while (dependencyObject != null)
            {
                if (dependencyObject is IContextMenuItemsSource contextMenuItemsSource && contextMenuItemsSource.ContextMenuItemsSourceType == ContextMenuItemsSourceType.VisualizationPanel)
                {
                    // Get the visualization panel related to the visualization panel view
                    this.mouseOverVisualizationPanel = (contextMenuItemsSource as VisualizationPanelView).DataContext as VisualizationPanel;
                    return HitTestResultBehavior.Stop;
                }

                dependencyObject = VisualTreeHelper.GetParent(dependencyObject);
            }

            return HitTestResultBehavior.Continue;
        }

        private void InstantVisualizationContainerView_DataContextChanged(object sender, DependencyPropertyChangedEventArgs e)
        {
            if (e.OldValue is InstantVisualizationContainer oldContainer)
            {
                oldContainer.Panels.CollectionChanged -= this.Panels_CollectionChanged;
                oldContainer.ChildVisualizationPanelWidthChanged -= this.ChildVisualizationPanelWidthChanged;
            }

            if (e.NewValue is InstantVisualizationContainer newContainer)
            {
                newContainer.Panels.CollectionChanged += this.Panels_CollectionChanged;
                newContainer.ChildVisualizationPanelWidthChanged += this.ChildVisualizationPanelWidthChanged;
            }
        }

        private void ChildVisualizationPanelWidthChanged(object sender, EventArgs e)
        {
            this.ResizeChildVisualizationPanels();
        }

        private void Panels_CollectionChanged(object sender, NotifyCollectionChangedEventArgs e)
        {
            if (e.Action == NotifyCollectionChangedAction.Add || e.Action == NotifyCollectionChangedAction.Remove)
            {
                this.ResizeChildVisualizationPanels();
            }
        }

        private void InstantVisualizationContainerView_SizeChanged(object sender, SizeChangedEventArgs e)
        {
            this.ResizeChildVisualizationPanels();
        }

        private void ResizeChildVisualizationPanels()
        {
            if (this.DataContext is InstantVisualizationContainer instantVisualizationContainer)
            {
                var totalWidth = 0;

                foreach (var panel in instantVisualizationContainer.Panels)
                {
                    if (panel is XYVisualizationPanel visualizationPanelXY)
                    {
                        totalWidth += visualizationPanelXY.RelativeWidth;
                    }
                    else if (panel is XYZVisualizationPanel visualizationPanelXYZ)
                    {
                        totalWidth += visualizationPanelXYZ.RelativeWidth;
                    }
                    else if (panel is InstantVisualizationPlaceholderPanel instantVisualizationPlaceholderPanel)
                    {
                        totalWidth += instantVisualizationPlaceholderPanel.RelativeWidth;
                    }
                    else
                    {
                        throw new Exception("Encountered an unsupported panel type.");
                    }
                }

                foreach (var panel in instantVisualizationContainer.Panels)
                {
                    if (panel is XYVisualizationPanel visualizationPanelXY)
                    {
                        visualizationPanelXY.Width = visualizationPanelXY.RelativeWidth * this.ActualWidth / totalWidth;
                    }
                    else if (panel is XYZVisualizationPanel visualizationPanelXYZ)
                    {
                        visualizationPanelXYZ.Width = visualizationPanelXYZ.RelativeWidth * this.ActualWidth / totalWidth;
                    }
                    else if (panel is InstantVisualizationPlaceholderPanel instantVisualizationPlaceholderPanel)
                    {
                        instantVisualizationPlaceholderPanel.Width = instantVisualizationPlaceholderPanel.RelativeWidth * this.ActualWidth / totalWidth;
                    }
                    else
                    {
                        throw new Exception("Encountered an unsupported panel type.");
                    }
                }
            }
        }

        private void Root_MouseMove(object sender, MouseEventArgs e)
        {
            // If the user has the Left Mouse button pressed, and we're not near the bottom edge
            // of the panel (where resizing occurs), then initiate a Drag & Drop reorder operation
            Point mousePosition = e.GetPosition(this);
            if (e.LeftButton == MouseButtonState.Pressed && !DragDropHelper.MouseNearPanelBottomEdge(mousePosition, this.ActualHeight))
            {
                DataObject data = new DataObject();
                data.SetData(DragDropDataName.DragDropOperation, DragDropOperation.ReorderPanel);
                data.SetData(DragDropDataName.VisualizationPanel, this.VisualizationPanel);
                data.SetData(DragDropDataName.MouseOffsetFromTop, mousePosition.Y);
                data.SetData(DragDropDataName.PanelSize, new Size?(new Size(this.ActualWidth, this.ActualHeight)));
                RenderTargetBitmap renderTargetBitmap = new RenderTargetBitmap((int)this.ActualWidth, (int)this.ActualHeight, 96, 96, PixelFormats.Pbgra32);
                renderTargetBitmap.Render(this);
                data.SetImage(renderTargetBitmap);

                DragDrop.DoDragDrop(this, data, DragDropEffects.Move);
            }
        }
    }
}

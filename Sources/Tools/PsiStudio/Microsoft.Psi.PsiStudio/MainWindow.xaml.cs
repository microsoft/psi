// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.

namespace Microsoft.Psi.PsiStudio
{
    using System;
    using System.Collections.Generic;
    using System.ComponentModel;
    using System.Windows;
    using System.Windows.Controls;
    using System.Windows.Input;
    using Microsoft.Psi.Visualization;
    using Microsoft.Psi.Visualization.ViewModels;
    using Microsoft.Psi.Visualization.VisualizationObjects;
    using Microsoft.Psi.Visualization.VisualizationPanels;
    using Xceed.Wpf.Toolkit.PropertyGrid;

    /// <summary>
    /// Interaction logic for MainWindow.xaml.
    /// </summary>
    public partial class MainWindow : Window
    {
        /// <summary>
        /// The list of object property names that should automatically be expanded in the property browser if they are expandable properties.
        /// </summary>
        private readonly List<string> autoExpandedProperties = new ()
        {
            nameof(XYVisualizationPanel.XAxisPropertyBrowser),
            nameof(XYVisualizationPanel.YAxisPropertyBrowser),
            nameof(TimelineVisualizationPanel.Threshold),
        };

        /// <summary>
        /// Initializes a new instance of the <see cref="MainWindow"/> class.
        /// </summary>
        public MainWindow()
        {
            this.InitializeComponent();

            // Create the visualization container and set the navigator range to an arbitrary default
            VisualizationContext visualizationContext = VisualizationContext.Instance;
            visualizationContext.VisualizationContainer = new VisualizationContainer();
            visualizationContext.VisualizationContainer.Navigator.ViewRange.Set(DateTime.UtcNow, TimeSpan.FromSeconds(60));

            // Create the context
            var viewModel = new MainWindowViewModel();

            // Set the values for the timing buttons on the navigator
            visualizationContext.VisualizationContainer.Navigator.ShowAbsoluteTiming = viewModel.Settings.ShowAbsoluteTiming;
            visualizationContext.VisualizationContainer.Navigator.ShowTimingRelativeToSessionStart = viewModel.Settings.ShowTimingRelativeToSessionStart;
            visualizationContext.VisualizationContainer.Navigator.ShowTimingRelativeToSelectionStart = viewModel.Settings.ShowTimingRelativeToSelectionStart;

            // Set the data context
            this.DataContext = viewModel;

            // Listen for items being added to the property grid.
            this.PropertyGrid.PreparePropertyItem += this.OnPropertyGridPreparePropertyItem;
        }

        /// <inheritdoc/>
        protected override void OnClosing(CancelEventArgs e)
        {
            // Notify the main window that we're closing the application.  The return value indicates whether the user elected to cancel closing.
            if (!(this.DataContext as MainWindowViewModel).OnClosing())
            {
                e.Cancel = true;
            }

            base.OnClosing(e);
        }

        private void ToolBar_Loaded(object sender, RoutedEventArgs e)
        {
            ToolBar toolBar = sender as ToolBar;
            if (toolBar.Template.FindName("OverflowGrid", toolBar) is FrameworkElement overflowGrid)
            {
                overflowGrid.Visibility = Visibility.Collapsed;
            }

            if (toolBar.Template.FindName("MainPanelBorder", toolBar) is FrameworkElement mainPanelBorder)
            {
                mainPanelBorder.Margin = default;
            }
        }

        private void StreamTreeNode_MouseMove(object sender, MouseEventArgs e)
        {
            // If the left button is also pressed, then the user is probably wanting to
            // initiate a drag operation of the stream into the Visualization Container
            if (e.LeftButton == MouseButtonState.Pressed)
            {
                // Get the tree item that sent the event
                if (sender is Grid treeNodeItem)
                {
                    if (treeNodeItem.DataContext is StreamTreeNode streamTreeNode && streamTreeNode.IsInCurrentSession)
                    {
                        // Begin the Drag & Drop operation
                        var data = new DataObject();
                        data.SetData(DragDropDataName.DragDropOperation, DragDropOperation.DragDropStream);
                        data.SetData(DragDropDataName.StreamTreeNode, streamTreeNode);

                        DragDrop.DoDragDrop(treeNodeItem, data, DragDropEffects.Move);
                    }
                }
            }
        }

        private void OnPropertyGridPreparePropertyItem(object sender, PropertyItemEventArgs e)
        {
            // In a later version of the Xceed PropertyGrid we can specify if a property is initially
            // expanded when shown in the property grid by setting a property on the ExpandableObject
            // attribute.  This workaround lets us initially expand any expandable property whose name
            // is in the list of properties we wish to automatically expand.
            if (e.PropertyItem is PropertyItem propertyItem)
            {
                if (this.autoExpandedProperties.Contains(propertyItem.PropertyName) && propertyItem.IsExpandable && !propertyItem.IsExpanded)
                {
                    propertyItem.IsExpanded = true;
                }
            }
        }
    }
}

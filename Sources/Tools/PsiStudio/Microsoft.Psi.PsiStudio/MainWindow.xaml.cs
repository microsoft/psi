// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.

namespace Microsoft.Psi.PsiStudio
{
    using System;
    using System.ComponentModel;
    using System.Windows;
    using System.Windows.Controls;
    using System.Windows.Input;
    using Microsoft.Psi.Visualization;
    using Microsoft.Psi.Visualization.ViewModels;
    using Microsoft.Psi.Visualization.Views.Visuals2D;
    using Microsoft.Psi.Visualization.VisualizationObjects;

    /// <summary>
    /// Interaction logic for MainWindow.xaml.
    /// </summary>
    public partial class MainWindow : Window
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="MainWindow"/> class.
        /// </summary>
        public MainWindow()
        {
            this.InitializeComponent();

            // Create the context
            MainWindowViewModel viewModel = new MainWindowViewModel();

            // Create the visualization container and set the navigator range to an arbitrary default
            VisualizationContext visualizationContext = VisualizationContext.Instance;
            visualizationContext.VisualizationContainer = new VisualizationContainer();
            visualizationContext.VisualizationContainer.Navigator.ViewRange.SetRange(DateTime.UtcNow, TimeSpan.FromSeconds(60));

            // Set the values for the timing buttons on the navigator
            visualizationContext.VisualizationContainer.Navigator.ShowAbsoluteTiming = viewModel.AppSettings.ShowAbsoluteTiming;
            visualizationContext.VisualizationContainer.Navigator.ShowTimingRelativeToSessionStart = viewModel.AppSettings.ShowTimingRelativeToSessionStart;
            visualizationContext.VisualizationContainer.Navigator.ShowTimingRelativeToSelectionStart = viewModel.AppSettings.ShowTimingRelativeToSelectionStart;

            // Set the data context
            this.DataContext = viewModel;
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
            var overflowGrid = toolBar.Template.FindName("OverflowGrid", toolBar) as FrameworkElement;
            if (overflowGrid != null)
            {
                overflowGrid.Visibility = Visibility.Collapsed;
            }

            var mainPanelBorder = toolBar.Template.FindName("MainPanelBorder", toolBar) as FrameworkElement;
            if (mainPanelBorder != null)
            {
                mainPanelBorder.Margin = default(Thickness);
            }
        }

        private void StreamTreeNode_MouseMove(object sender, System.Windows.Input.MouseEventArgs e)
        {
            // If the left button is also pressed, then the user is probably wanting to
            // initiate a drag operation of the stream into the Visualization Container
            if (e.LeftButton == MouseButtonState.Pressed)
            {
                // Get the Tree Item that sent the event
                StackPanel treeNode = sender as StackPanel;
                if (treeNode != null)
                {
                    StreamTreeNode streamTreeNode = treeNode.DataContext as StreamTreeNode;
                    if (streamTreeNode != null && streamTreeNode.CanVisualize)
                    {
                        // Begin the Drag & Drop operation
                        DataObject data = new DataObject();
                        data.SetData(DragDropDataName.DragDropOperation, DragDropOperation.DragDropStream);
                        data.SetData(DragDropDataName.StreamTreeNode, streamTreeNode);

                        DragDrop.DoDragDrop(treeNode, data, DragDropEffects.Move);
                    }
                }
            }
        }
    }
}

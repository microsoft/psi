// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.

namespace Microsoft.Psi.PsiStudio
{
    using System;
    using System.IO;
    using System.Linq;
    using System.Reflection;
    using System.Windows;
    using System.Windows.Controls;
    using System.Windows.Input;
    using Microsoft.Psi.PsiStudio.Common;
    using Microsoft.Psi.Visualization.ViewModels;
    using Microsoft.Psi.Visualization.Views.Visuals2D;
    using Microsoft.Psi.Visualization.VisualizationObjects;
    using Microsoft.Win32;

    /// <summary>
    /// Interaction logic for MainWindow.xaml.
    /// </summary>
    public partial class MainWindow : Window
    {
        private PsiStudioContext context = PsiStudioContext.Instance;

        /// <summary>
        /// Initializes a new instance of the <see cref="MainWindow"/> class.
        /// </summary>
        public MainWindow()
        {
            this.InitializeComponent();

            // Check if the argument list includes a store to open.
            // First arg is this exe's filename, second arg (if it exists) is the store to open
            var args = Environment.GetCommandLineArgs();
            string filename = null;
            if (args.Length > 1)
            {
                filename = args[1];
            }

            this.Loaded += (s, e) => this.Activate();

            this.context.VisualizationContainer = new VisualizationContainer();
            this.context.VisualizationContainer.Navigator.ViewRange.SetRange(DateTime.UtcNow, TimeSpan.FromSeconds(60));

            // register an async handler to load the current layout once the main window has finished loading
            this.Loaded += this.MainWindow_Loaded;

            if (!string.IsNullOrWhiteSpace(filename))
            {
                // register an async handler to open the dataset once the main window has finished loading
                this.Loaded += async (s, e) => await this.context.OpenDatasetAsync(filename);
            }

            this.DataContext = this.context;

            PipelineDiagnosticsVisualizationModel.RegisterKnownSerializationTypes(); // necessary for .NET Core types
        }

        private void MainWindow_Loaded(object sender, RoutedEventArgs e)
        {
            this.context.OpenLayout(this.context.CurrentLayout);
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

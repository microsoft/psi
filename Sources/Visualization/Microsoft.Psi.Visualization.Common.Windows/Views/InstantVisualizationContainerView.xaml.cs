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
    using Microsoft.Psi.Data;
    using Microsoft.Psi.PsiStudio;
    using Microsoft.Psi.PsiStudio.Common;
    using Microsoft.Psi.Visualization.Common;
    using Microsoft.Psi.Visualization.Data;
    using Microsoft.Psi.Visualization.Helpers;
    using Microsoft.Psi.Visualization.ViewModels;
    using Microsoft.Psi.Visualization.VisualizationObjects;
    using Microsoft.Psi.Visualization.VisualizationPanels;

    /// <summary>
    /// Interaction logic for InstantVisualizationContainerView.xaml.
    /// </summary>
    public partial class InstantVisualizationContainerView : UserControl
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="InstantVisualizationContainerView"/> class.
        /// </summary>
        public InstantVisualizationContainerView()
        {
            this.InitializeComponent();
            this.DataContextChanged += this.InstantVisualizationContainerView_DataContextChanged;
            this.SizeChanged += this.InstantVisualizationContainerView_SizeChanged;
        }

        /// <summary>
        /// Gets ths visualization panel.
        /// </summary>
        protected InstantVisualizationContainer VisualizationPanel => (InstantVisualizationContainer)this.DataContext;

        private void InstantVisualizationContainerView_DataContextChanged(object sender, DependencyPropertyChangedEventArgs e)
        {
            if (e.OldValue is InstantVisualizationContainer oldContainer)
            {
                oldContainer.Panels.CollectionChanged -= this.Panels_CollectionChanged;
            }

            if (e.NewValue is InstantVisualizationContainer newContainer)
            {
                newContainer.Panels.CollectionChanged += this.Panels_CollectionChanged;
            }
        }

        private void Panels_CollectionChanged(object sender, System.Collections.Specialized.NotifyCollectionChangedEventArgs e)
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
            InstantVisualizationContainer containerPanel = this.DataContext as InstantVisualizationContainer;
            foreach (VisualizationPanel panel in containerPanel.Panels)
            {
                panel.Width = this.ActualWidth / containerPanel.Panels.Count;
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

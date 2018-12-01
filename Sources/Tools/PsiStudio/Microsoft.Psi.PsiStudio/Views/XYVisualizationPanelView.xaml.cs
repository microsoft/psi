// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.

namespace Microsoft.Psi.Visualization.Views
{
    using System;
    using System.Windows;
    using System.Windows.Controls;
    using System.Windows.Input;
    using System.Windows.Media;
    using System.Windows.Media.Imaging;
    using Microsoft.Psi.PsiStudio.Common;
    using Microsoft.Psi.Visualization.Helpers;
    using Microsoft.Psi.Visualization.VisualizationPanels;

    /// <summary>
    /// Interaction logic for XYVisualizationPanelView.xaml
    /// </summary>
    public partial class XYVisualizationPanelView : UserControl
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="XYVisualizationPanelView"/> class.
        /// </summary>
        public XYVisualizationPanelView()
        {
            this.InitializeComponent();
        }

        /// <summary>
        /// Gets ths visualization panel.
        /// </summary>
        protected XYVisualizationPanel VisualizationPanel => (XYVisualizationPanel)this.DataContext;

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

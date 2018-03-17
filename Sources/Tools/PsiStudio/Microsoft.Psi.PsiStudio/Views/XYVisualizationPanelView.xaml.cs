// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.

namespace Microsoft.Psi.Visualization.Views
{
    using System;
    using System.Windows;
    using System.Windows.Controls;
    using System.Windows.Input;
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

        private void Root_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            // Set the current panel on click
            if (!this.VisualizationPanel.IsCurrentPanel)
            {
                this.VisualizationPanel.Container.CurrentPanel = this.VisualizationPanel;
            }
        }

        private void Root_MouseWheel(object sender, MouseWheelEventArgs e)
        {
            if (Keyboard.IsKeyDown(Key.LeftCtrl) || Keyboard.IsKeyDown(Key.RightCtrl))
            {
                var factor = 1.0 + Math.Min(Math.Max(((double)e.Delta) / 200.0, -0.2), 0.2);
                this.VisualizationPanel.Configuration.Height = (float)(this.VisualizationPanel.Configuration.Height * factor);
                e.Handled = true;
            }
        }

        private void RemovePanel_Click(object sender, RoutedEventArgs e)
        {
            this.VisualizationPanel.Container.RemovePanel(this.VisualizationPanel);
        }

        private void Clear_Click(object sender, RoutedEventArgs e)
        {
            this.VisualizationPanel.Clear();
        }
    }
}

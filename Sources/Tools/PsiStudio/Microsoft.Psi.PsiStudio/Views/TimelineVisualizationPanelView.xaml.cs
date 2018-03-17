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
    /// Interaction logic for TimelineVisualizationPanelView.xaml
    /// </summary>
    public partial class TimelineVisualizationPanelView : UserControl
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="TimelineVisualizationPanelView"/> class.
        /// </summary>
        public TimelineVisualizationPanelView()
        {
            this.InitializeComponent();
            this.DataContextChanged += this.TimelineVisualizationPanelView_DataContextChanged;
        }

        /// <summary>
        /// Gets or sets the timeline visualization panel.
        /// </summary>
        protected TimelineVisualizationPanel VisualizationPanel { get; set; }

        /// <inheritdoc />
        protected override void OnRenderSizeChanged(SizeChangedInfo sizeInfo)
        {
            base.OnRenderSizeChanged(sizeInfo);
            if (sizeInfo.WidthChanged)
            {
                // Update panel Width property which some TimelineVisualizationObjects
                // need in order to perform data summarization based on the view width.
                // Not updating the Height property as it is bound to the panel view.
                this.VisualizationPanel.Configuration.Width = sizeInfo.NewSize.Width;
            }
        }

        private void Clear_Click(object sender, RoutedEventArgs e)
        {
            this.VisualizationPanel.Clear();
        }

        private DateTime GetTimeAtMousePointer(MouseEventArgs e)
        {
            Point point = e.GetPosition(this.Root);
            double percent = point.X / this.Root.ActualWidth;
            var viewRange = this.VisualizationPanel.Navigator.ViewRange;
            DateTime time = viewRange.StartTime + TimeSpan.FromTicks((long)((double)viewRange.Duration.Ticks * percent));
            return time;
        }

        private void RemovePanel_Click(object sender, RoutedEventArgs e)
        {
            this.VisualizationPanel.Container.RemovePanel(this.VisualizationPanel);
        }

        private void Root_ContextMenuOpening(object sender, ContextMenuEventArgs e)
        {
            if ((Keyboard.Modifiers & ModifierKeys.Shift) == ModifierKeys.Shift)
            {
                // eat context menu opening, when shift key is pressed (dropping end selection marker)
                e.Handled = true;
            }
        }

        private void Root_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            // Set the current panel on click
            if (!this.VisualizationPanel.IsCurrentPanel)
            {
                this.VisualizationPanel.Container.CurrentPanel = this.VisualizationPanel;
            }

            if (Keyboard.IsKeyDown(Key.LeftShift) || Keyboard.IsKeyDown(Key.RightShift))
            {
                DateTime time = this.GetTimeAtMousePointer(e);
                this.VisualizationPanel.Navigator.SelectionRange.SetRange(time, this.VisualizationPanel.Navigator.SelectionRange.EndTime);
                e.Handled = true;
            }
        }

        private void Root_MouseRightButtonDown(object sender, MouseButtonEventArgs e)
        {
            if (Keyboard.IsKeyDown(Key.LeftShift) || Keyboard.IsKeyDown(Key.RightShift))
            {
                DateTime time = this.GetTimeAtMousePointer(e);
                this.VisualizationPanel.Navigator.SelectionRange.SetRange(this.VisualizationPanel.Navigator.SelectionRange.StartTime, time);
                e.Handled = true;
            }
        }

        private void Root_MouseWheel(object sender, MouseWheelEventArgs e)
        {
            if (Keyboard.IsKeyDown(Key.LeftCtrl) || Keyboard.IsKeyDown(Key.RightCtrl))
            {
                var factor = 1.0 + Math.Min(Math.Max(((double)e.Delta) / 200.0, -0.2), 0.2);
                this.VisualizationPanel.Configuration.Height = (float)(this.VisualizationPanel.Configuration.Height * factor);
            }

            e.Handled = true;
        }

        private void ShowHideLegend_Click(object sender, RoutedEventArgs e)
        {
            this.VisualizationPanel.Configuration.ShowLegend = !this.VisualizationPanel.Configuration.ShowLegend;
        }

        private void TimelineVisualizationPanelView_DataContextChanged(object sender, DependencyPropertyChangedEventArgs e)
        {
            this.VisualizationPanel = e.NewValue as TimelineVisualizationPanel;
        }

        private void ZoomToSelection_Click(object sender, RoutedEventArgs e)
        {
            this.VisualizationPanel.Container.Navigator.ZoomToSelection();
        }

        private void ZoomToSessionExtents_Click(object sender, RoutedEventArgs e)
        {
            this.VisualizationPanel.Container.Navigator.ZoomToDataRange();
        }
    }
}

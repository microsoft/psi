// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.

namespace Microsoft.Psi.Visualization.Views
{
    using System;
    using System.Windows;
    using System.Windows.Controls;
    using System.Windows.Controls.Primitives;
    using System.Windows.Input;
    using System.Windows.Media;
    using Microsoft.Psi.PsiStudio;
    using Microsoft.Psi.Visualization.Navigation;
    using Microsoft.Psi.Visualization.VisualizationObjects;

    /// <summary>
    /// Interaction logic for NavigatorView.xaml.
    /// </summary>
    public partial class NavigatorView : UserControl
    {
        private ScaleTransform scaleTransform = new ScaleTransform();

        /// <summary>
        /// Initializes a new instance of the <see cref="NavigatorView"/> class.
        /// </summary>
        public NavigatorView()
        {
            this.InitializeComponent();
            this.DataContextChanged += this.NavigatorView_DataContextChanged;
        }

        /// <summary>
        /// Gets the navigator.
        /// </summary>
        public Navigator Navigator => this.DataContext as Navigator;

        /// <inheritdoc />
        protected override void OnRenderSizeChanged(SizeChangedInfo sizeInfo)
        {
            base.OnRenderSizeChanged(sizeInfo);
            this.PositionThumbs();
        }

        private DateTime GetTimeAtMousePointer(MouseEventArgs e)
        {
            Point point = e.GetPosition(this.Root);
            double percent = point.X / this.Root.ActualWidth;
            var viewRange = this.Navigator.ViewRange;
            DateTime time = viewRange.StartTime + TimeSpan.FromTicks((long)((double)viewRange.Duration.Ticks * percent));

            // If we're currently snapping to some Visualization Object, adjust the time to the timestamp of the nearest message
            DateTime? snappedTime = null;
            if (VisualizationContext.Instance.VisualizationContainer.SnapToVisualizationObject is IStreamVisualizationObject snapToVisualizationObject)
            {
                snappedTime = snapToVisualizationObject.GetSnappedTime(time);
            }

            return snappedTime ?? time;
        }

        private DateTime GetTimeFromPosition(Thumb sender)
        {
            double scaleScreenToData = this.Navigator.DataRange.Duration.TotalMilliseconds / this.Canvas.ActualWidth;
            DateTime dataStartTime = this.Navigator.DataRange.StartTime;
            var dateTime = dataStartTime + TimeSpan.FromMilliseconds(Canvas.GetLeft((Thumb)sender) * scaleScreenToData);
            return dateTime;
        }

        private void NavigatorView_DataContextChanged(object sender, DependencyPropertyChangedEventArgs e)
        {
            if (this.Navigator != null)
            {
                this.Navigator.DataRange.RangeChanged += this.RangeChanged;
                this.Navigator.SelectionRange.RangeChanged += this.RangeChanged;
                this.Navigator.ViewRange.RangeChanged += this.RangeChanged;
                this.Navigator.CursorChanged += this.Navigator_CursorChanged;
            }
        }

        private void RangeChanged(object sender, NavigatorTimeRangeChangedEventArgs e)
        {
            this.PositionThumbs();
        }

        private void Navigator_CursorChanged(object sender, NavigatorTimeChangedEventArgs e)
        {
            this.PositionThumbs();
        }

        private void PositionThumbs()
        {
            if (this.Navigator == null)
            {
                return;
            }

            double scaleDataToScreen = this.Canvas.ActualWidth / this.Navigator.DataRange.Duration.TotalMilliseconds;
            DateTime dataStartTime = this.Navigator.DataRange.StartTime;

            Canvas.SetLeft(this.ViewStartThumb, (this.Navigator.ViewRange.StartTime - dataStartTime).TotalMilliseconds * scaleDataToScreen);
            Canvas.SetLeft(this.ViewEndThumb, (this.Navigator.ViewRange.EndTime - dataStartTime).TotalMilliseconds * scaleDataToScreen);
            Canvas.SetLeft(this.ViewRangeThumb, Canvas.GetLeft(this.ViewStartThumb));
            var width = Canvas.GetLeft(this.ViewEndThumb) - Canvas.GetLeft(this.ViewStartThumb);
            this.ViewRangeThumb.Width = width <= 0 ? 1 : width;
        }

        private void Root_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            if (Keyboard.IsKeyDown(Key.LeftShift) || Keyboard.IsKeyDown(Key.RightShift))
            {
                DateTime time = this.GetTimeAtMousePointer(e);
                this.Navigator.SelectionRange.SetRange(time, this.Navigator.SelectionRange.EndTime);
                e.Handled = true;
            }
        }

        private void Root_MouseRightButtonDown(object sender, MouseButtonEventArgs e)
        {
            if (Keyboard.IsKeyDown(Key.LeftShift) || Keyboard.IsKeyDown(Key.RightShift))
            {
                DateTime time = this.GetTimeAtMousePointer(e);
                this.Navigator.SelectionRange.SetRange(this.Navigator.SelectionRange.StartTime, time);
                e.Handled = true;
            }
        }

        private void Thumb_OnDragDelta(object sender, DragDeltaEventArgs e)
        {
            Thumb thumb = (Thumb)sender;
            Canvas.SetLeft(thumb, Canvas.GetLeft(thumb) + e.HorizontalChange);
            this.UpdateNavigatorForThumbChange(thumb);
        }

        private void Thumb_OnDragCompleted(object sender, DragCompletedEventArgs e)
        {
            Thumb thumb = (Thumb)sender;
            this.UpdateNavigatorForThumbChange(thumb);
        }

        private void UpdateNavigatorForThumbChange(Thumb thumb)
        {
            DateTime time = this.GetTimeFromPosition(thumb);
            if (thumb == this.ViewRangeThumb)
            {
                this.Navigator.ViewRange.SetRange(time, time + this.Navigator.ViewRange.Duration);
            }
            else if (thumb == this.ViewStartThumb)
            {
                this.Navigator.ViewRange.SetRange(time, this.Navigator.ViewRange.EndTime);
            }
            else if (thumb == this.ViewEndThumb)
            {
                this.Navigator.ViewRange.SetRange(this.Navigator.ViewRange.StartTime, time);
            }
        }
    }
}

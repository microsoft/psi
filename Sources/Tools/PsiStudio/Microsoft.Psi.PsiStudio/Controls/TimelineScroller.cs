// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.

namespace Microsoft.Psi.Visualization.Controls
{
    using System;
    using System.Windows;
    using System.Windows.Controls;
    using System.Windows.Input;
    using Microsoft.Psi.Visualization.Navigation;

    /// <summary>
    /// Represents an element that scrolls time based on a timeline.
    /// </summary>
    public class TimelineScroller : Grid
    {
        /// <summary>
        /// Identifies the Navigator dependency property.
        /// </summary>
        public static readonly DependencyProperty NavigatorProperty =
            DependencyProperty.Register(
                "Navigator",
                typeof(Navigator),
                typeof(TimelineScroller));

        /// <summary>
        /// Gets or sets the navigator.
        /// </summary>
        public Navigator Navigator
        {
            get { return (Navigator)this.GetValue(NavigatorProperty); }
            set { this.SetValue(NavigatorProperty, value); }
        }

        /// <inheritdoc />
        protected override void OnMouseMove(MouseEventArgs e)
        {
            // Only move the cursor in playback mode - this avoids having the a battle between the live cursor and the mouse
            if (this.Navigator.NavigationMode == NavigationMode.Playback)
            {
                DateTime time = this.GetTimeAtMousePointer(e);
                this.Navigator.Cursor = time;
            }

            base.OnMouseMove(e);
        }

        /// <inheritdoc />
        protected override void OnMouseWheel(MouseWheelEventArgs e)
        {
            if (!Keyboard.IsKeyDown(Key.LeftCtrl) && !Keyboard.IsKeyDown(Key.RightCtrl))
            {
                this.Navigator.ZoomAroundCursor(1.0 - Math.Min(Math.Max(((double)e.Delta) / 200.0, -0.2), 0.2));
            }

            base.OnMouseWheel(e);
        }

        private DateTime GetTimeAtMousePointer(MouseEventArgs e)
        {
            Point point = e.GetPosition(this);
            double percent = point.X / this.ActualWidth;
            var viewRange = this.Navigator.ViewRange;
            DateTime time = viewRange.StartTime + TimeSpan.FromTicks((long)((double)viewRange.Duration.Ticks * percent));
            return time;
        }
    }
}
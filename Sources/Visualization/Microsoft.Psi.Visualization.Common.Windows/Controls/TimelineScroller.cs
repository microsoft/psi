// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.

namespace Microsoft.Psi.Visualization.Controls
{
    using System;
    using System.Windows;
    using System.Windows.Controls;
    using System.Windows.Input;
    using Microsoft.Psi.PsiStudio;
    using Microsoft.Psi.Visualization.Navigation;
    using Microsoft.Psi.Visualization.VisualizationObjects;

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
            // Only move the cursor if we're currently paused and if the cursor is currently following the mouse
            if (this.Navigator.CursorMode == CursorMode.Manual && this.Navigator.CursorFollowsMouse)
            {
                DateTime time = this.GetTimeAtMousePointer(e);
                DateTime? snappedTime = null;

                // If we're currently to snapping the cursor to some Visualization Object's messages, then
                // find the timestamp of the message that's temporally closest to the mouse pointer.
                if (VisualizationContext.Instance.VisualizationContainer.SnapToVisualizationObject is IStreamVisualizationObject streamVisualizationObject)
                {
                    snappedTime = streamVisualizationObject.GetSnappedTime(time);
                }

                if (snappedTime.HasValue)
                {
                    this.Navigator.Cursor = snappedTime.Value;
                }
                else
                {
                    this.Navigator.Cursor = time;
                }
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
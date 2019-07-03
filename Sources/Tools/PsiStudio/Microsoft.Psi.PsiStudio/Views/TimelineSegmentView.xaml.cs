// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.

namespace Microsoft.Psi.Visualization.Views
{
    using System.Collections.Generic;
    using System.Windows;
    using System.Windows.Controls;
    using System.Windows.Media;
    using System.Windows.Shapes;

    /// <summary>
    /// Interaction logic for TimelineSegmentView.xaml.
    /// </summary>
    public partial class TimelineSegmentView : UserControl
    {
        private List<LineGeometry> lines = new List<LineGeometry>();
        private Brush brush = new SolidColorBrush(Color.FromRgb(0x99, 0x99, 0x99));

        /// <summary>
        /// Initializes a new instance of the <see cref="TimelineSegmentView"/> class.
        /// </summary>
        public TimelineSegmentView()
        {
            this.InitializeComponent();
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="TimelineSegmentView"/> class.
        /// </summary>
        /// <param name="tickAlignment">Tick alignment.</param>
        /// <param name="numDivisions">Number of divisions.</param>
        /// <param name="label">Timeline segment lable.</param>
        public TimelineSegmentView(VerticalAlignment tickAlignment, int numDivisions, string label)
        {
            this.InitializeComponent();
            for (int i = 0; i < numDivisions; i++)
            {
                var tickHeight = i == 0 ? 20 : 5;
                double tickXPos = (this.ActualWidth / this.lines.Count) * i;
                Path path = new Path() { StrokeThickness = 1, Stroke = this.brush };
                LineGeometry line = new LineGeometry(new Point(tickXPos, 0), new Point(tickXPos, tickHeight));
                this.lines.Add(line);
                path.Data = line;
                path.VerticalAlignment = tickAlignment;
                this.Root.Children.Add(path);
            }

            this.label.Text = label;
            this.label.VerticalAlignment = tickAlignment;
        }

        /// <inheritdoc />
        protected override void OnRenderSizeChanged(SizeChangedInfo sizeInfo)
        {
            for (int i = 0; i < this.lines.Count; i++)
            {
                var tickHeight = i == 0 ? 20 : 5;
                double tickXPos = (this.ActualWidth / this.lines.Count) * i;
                this.lines[i].StartPoint = new Point(tickXPos, 0);
                this.lines[i].EndPoint = new Point(tickXPos, tickHeight);
            }
        }
    }
}

// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.
// Forked from https://github.com/microsoft/automatic-graph-layout/tree/master/GraphLayout/tools/WpfGraphControl

namespace Microsoft.Msagl.WpfGraphControl
{
    using System;
    using System.Collections.Generic;
    using System.Windows;
    using System.Windows.Controls;
    using System.Windows.Shapes;
    using Microsoft.Msagl.Drawing;

    /// <summary>
    /// Visual label.
    /// </summary>
    public class VLabel : IViewerObject, IInvalidatable
    {
        private bool markedForDragging = false;

        /// <summary>
        /// Initializes a new instance of the <see cref="VLabel"/> class.
        /// </summary>
        /// <param name="edge">Edge being labeled.</param>
        /// <param name="frameworkElement">Underlying framework element.</param>
        public VLabel(Edge edge, FrameworkElement frameworkElement)
        {
            this.FrameworkElement = frameworkElement;
            this.DrawingObject = edge.Label;
        }

        /// <inheritdoc/>
        public event EventHandler MarkedForDraggingEvent;

        /// <inheritdoc/>
        public event EventHandler UnmarkedForDraggingEvent;

        /// <summary>
        /// Gets underlying framework element.
        /// </summary>
        public FrameworkElement FrameworkElement { get; private set; }

        /// <summary>
        /// Gets underlying drawing object.
        /// </summary>
        public DrawingObject DrawingObject { get; private set; }

        /// <summary>
        /// Gets or sets a value indicating whether marked for dragging.
        /// </summary>
        public bool MarkedForDragging
        {
            get { return this.markedForDragging; }

            set
            {
                if (value != this.markedForDragging)
                {
                    this.markedForDragging = value;
                    if (value)
                    {
                        this.AttachmentLine = new Line
                        {
                            Stroke = System.Windows.Media.Brushes.Black,
                            StrokeDashArray = new System.Windows.Media.DoubleCollection(this.OffsetElems()),
                        }; // the line will have 0,0, 0,0 start and end so it would not be rendered

                        ((Canvas)this.FrameworkElement.Parent).Children.Add(this.AttachmentLine);

                        this.MarkedForDraggingEvent?.Invoke(this, EventArgs.Empty);
                    }
                    else
                    {
                        ((Canvas)this.FrameworkElement.Parent).Children.Remove(this.AttachmentLine);
                        this.AttachmentLine = null;

                        this.UnmarkedForDraggingEvent?.Invoke(this, EventArgs.Empty);
                    }
                }
            }
        }

        private Line AttachmentLine { get; set; }

        /// <summary>
        /// Invalidate rendered label.
        /// </summary>
        public void Invalidate()
        {
            var label = (Drawing.Label)this.DrawingObject;
            Common.PositionFrameworkElement(this.FrameworkElement, label.Center, 1);
            var geomLabel = label.GeometryLabel;
            if (this.AttachmentLine != null)
            {
                this.AttachmentLine.X1 = geomLabel.AttachmentSegmentStart.X;
                this.AttachmentLine.Y1 = geomLabel.AttachmentSegmentStart.Y;
                this.AttachmentLine.X2 = geomLabel.AttachmentSegmentEnd.X;
                this.AttachmentLine.Y2 = geomLabel.AttachmentSegmentEnd.Y;
            }
        }

        private IEnumerable<double> OffsetElems()
        {
            yield return 1;
            yield return 2;
        }
    }
}
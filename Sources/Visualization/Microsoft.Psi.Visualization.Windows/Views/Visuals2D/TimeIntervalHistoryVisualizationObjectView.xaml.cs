// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.

namespace Microsoft.Psi.Visualization.Views.Visuals2D
{
    using System.Collections.Generic;
    using System.ComponentModel;
    using Microsoft.Psi.Visualization.DataTypes;
    using Microsoft.Psi.Visualization.VisualizationObjects;

    /// <summary>
    /// Interaction logic for TimeIntervalHistoryVisualizationObjectView.xaml.
    /// </summary>
    public partial class TimeIntervalHistoryVisualizationObjectView : TimelineCanvasVisualizationObjectView<TimeIntervalHistoryVisualizationObject, TimeIntervalHistory>
    {
        private List<TimeIntervalHistoryVisualizationObjectViewItem> items = new List<TimeIntervalHistoryVisualizationObjectViewItem>();

        /// <summary>
        /// Initializes a new instance of the <see cref="TimeIntervalHistoryVisualizationObjectView"/> class.
        /// </summary>
        public TimeIntervalHistoryVisualizationObjectView()
        {
            this.InitializeComponent();
            this.Canvas = this._DynamicCanvas;
        }

        /// <inheritdoc/>
        protected override void OnVisualizationObjectPropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            base.OnVisualizationObjectPropertyChanged(sender, e);

            if (e.PropertyName == nameof(this.VisualizationObject.DisplayData))
            {
                this.OnDisplayDataChanged();
            }
        }

        /// <summary>
        /// Implements a response to display data changing.
        /// </summary>
        protected virtual void OnDisplayDataChanged()
        {
            this.Rerender();
        }

        /// <inheritdoc/>
        protected override void OnTransformsChanged()
        {
            this.Rerender();
        }

        private void Rerender()
        {
            for (int i = 0; i < this.VisualizationObject.DisplayData.Count; i++)
            {
                TimeIntervalHistoryVisualizationObjectViewItem item;
                if (i < this.items.Count)
                {
                    item = this.items[i];
                }
                else
                {
                    item = new TimeIntervalHistoryVisualizationObjectViewItem(this);
                    this.items.Add(item);
                }

                item.Update(this.VisualizationObject.DisplayData[i]);
            }

            // remove the remaining figures
            for (int i = this.VisualizationObject.DisplayData.Count; i < this.items.Count; i++)
            {
                var item = this.items[this.VisualizationObject.DisplayData.Count];
                item.RemoveFromCanvas();
                this.items.Remove(item);
            }
        }
    }
}

// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.

namespace Microsoft.Psi.Visualization.Views.Visuals2D
{
    using System;
    using System.Collections.Generic;
    using System.Collections.Specialized;
    using System.ComponentModel;
    using System.Linq;
    using System.Windows.Data;
    using System.Windows.Media;
    using System.Windows.Shapes;
    using Microsoft.Psi.Visualization;
    using Microsoft.Psi.Visualization.Data;
    using Microsoft.Psi.Visualization.Navigation;
    using Microsoft.Psi.Visualization.VisualizationObjects;

    /// <summary>
    /// Implements a visualization object view for a <see cref="PlotVisualizationObject{TData}"/>.
    /// </summary>
    /// <typeparam name="TPlotVisualizationObject">The type of the corresponding visualization object.</typeparam>
    /// <typeparam name="TData">The type of the data.</typeparam>
    /// <remarks>
    /// The class implements the <see cref="IPlotVisualizationObjectView{TKey, TData}"/> interface, and uses
    /// the <see cref="PlotVisualizationObjectViewHelper{TKey, TData}"/> helper class via aggregation to
    /// generate the necessary views.
    /// </remarks>
    public class PlotVisualizationObjectView<TPlotVisualizationObject, TData> :
        StreamIntervalVisualizationObjectTimelineCanvasView<TPlotVisualizationObject, TData>,
        IPlotVisualizationObjectView<int, TData>
        where TPlotVisualizationObject : PlotVisualizationObject<TData>, new()
    {
        private readonly PlotVisualizationObjectViewHelper<int, TData> helper;

        /// <summary>
        /// Initializes a new instance of the <see cref="PlotVisualizationObjectView{TTimelineVisualizationObject, TData}"/> class.
        /// </summary>
        public PlotVisualizationObjectView()
        {
            this.helper = new PlotVisualizationObjectViewHelper<int, TData>(this);
        }

        /// <summary>
        /// Gets the plot visualization object.
        /// </summary>
        public TPlotVisualizationObject PlotVisualizationObject => this.StreamVisualizationObject as TPlotVisualizationObject;

        /// <inheritdoc/>
        Transform IPlotVisualizationObjectView<int, TData>.TransformGroup => this.TransformGroup;

        /// <inheritdoc/>
        public InterpolationStyle InterpolationStyle => this.PlotVisualizationObject.InterpolationStyle;

        /// <inheritdoc/>
        public MarkerStyle MarkerStyle => this.PlotVisualizationObject.MarkerStyle;

        /// <inheritdoc/>
        public double MarkerSize => this.PlotVisualizationObject.MarkerSize;

        /// <inheritdoc/>
        public void CreateBindings(int seriesKey, Path linePath, Path markerPath, Path rangePath)
        {
            // Create binding for line color
            var binding = new Binding(nameof(this.PlotVisualizationObject.Color))
            {
                Source = this.PlotVisualizationObject,
                Converter = new Converters.ColorConverter(),
            };
            BindingOperations.SetBinding(linePath, Shape.StrokeProperty, binding);

            // Create binding for line width
            binding = new Binding(nameof(this.PlotVisualizationObject.LineWidth))
            {
                Source = this.PlotVisualizationObject,
            };
            BindingOperations.SetBinding(linePath, Shape.StrokeThicknessProperty, binding);

            // Create binding for markers stroke color
            binding = new Binding(nameof(this.PlotVisualizationObject.MarkerColor))
            {
                Source = this.PlotVisualizationObject,
                Converter = new Converters.ColorConverter(),
            };
            BindingOperations.SetBinding(markerPath, Shape.StrokeProperty, binding);

            // Create binding for markers fill color
            binding = new Binding(nameof(this.PlotVisualizationObject.MarkerColor))
            {
                Source = this.PlotVisualizationObject,
                Converter = new Converters.ColorConverter(),
            };
            BindingOperations.SetBinding(markerPath, Shape.FillProperty, binding);

            // Create binding for range color
            binding = new Binding(nameof(this.PlotVisualizationObject.RangeColor))
            {
                Source = this.PlotVisualizationObject,
                Converter = new Converters.ColorConverter(),
            };
            BindingOperations.SetBinding(rangePath, Shape.StrokeProperty, binding);

            // Create binding for range width
            binding = new Binding(nameof(this.PlotVisualizationObject.RangeWidth))
            {
                Source = this.PlotVisualizationObject,
            };
            BindingOperations.SetBinding(rangePath, Shape.StrokeThicknessProperty, binding);
        }

        /// <inheritdoc/>
        public IEnumerable<(DateTime OriginatingTime, TData Value, bool Available)> GetDataPoints(int seriesKey)
            => (this.PlotVisualizationObject.Data != null) ?
                this.PlotVisualizationObject.Data.Select(m => (m.OriginatingTime, m.Data, true)) :
                this.PlotVisualizationObject.SummaryData.Select(m => (m.OriginatingTime, m.Value, true));

        /// <inheritdoc/>
        public double GetNumericValue(TData data) => this.PlotVisualizationObject.GetNumericValue(data);

        /// <inheritdoc/>
        protected override void OnVisualizationObjectPropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            base.OnVisualizationObjectPropertyChanged(sender, e);

            if (e.PropertyName == nameof(this.PlotVisualizationObject.MarkerSize) ||
                e.PropertyName == nameof(this.PlotVisualizationObject.MarkerStyle) ||
                e.PropertyName == nameof(this.PlotVisualizationObject.MarkerColor))
            {
                this.UpdateView();
            }
            else if (e.PropertyName == nameof(this.PlotVisualizationObject.YAxis))
            {
                if (this.UpdateTransforms())
                {
                    this.UpdateView();
                }
            }
            else if (e.PropertyName == nameof(this.PlotVisualizationObject.InterpolationStyle))
            {
                if (this.PlotVisualizationObject.Data != null)
                {
                    this.OnDataCollectionChanged(this.PlotVisualizationObject.Data, new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Reset));
                }
                else if (this.PlotVisualizationObject.SummaryData != null)
                {
                    this.OnSummaryDataCollectionChanged(this.PlotVisualizationObject.SummaryData, new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Reset));
                }
            }
        }

        /// <inheritdoc/>
        protected override void OnNavigatorViewRangeChanged(object sender, NavigatorTimeRangeChangedEventArgs e)
        {
            if (this.Navigator.CursorMode == CursorMode.Live)
            {
                this.helper.RemoveSegments(s => s.EndTime < this.Navigator.ViewRange.StartTime);
            }

            base.OnNavigatorViewRangeChanged(sender, e);
        }

        /// <inheritdoc/>
        protected override void OnNavigatorDataRangeChanged(object sender, NavigatorTimeRangeChangedEventArgs e)
        {
            this.helper.RemoveSegments(s => s.EndTime < this.Navigator.DataRange.StartTime);

            base.OnNavigatorDataRangeChanged(sender, e);
        }

        /// <inheritdoc/>
        protected override void OnDataCollectionChanged(object sender, NotifyCollectionChangedEventArgs e)
        {
            if (e.Action == NotifyCollectionChangedAction.Add)
            {
                bool transformChanged = this.UpdateTransforms();
                foreach (var item in e.NewItems)
                {
                    Message<TData> value = (Message<TData>)item;
                    this.helper.AddPoint(0, value.OriginatingTime, this.PlotVisualizationObject.GetNumericValue(value.Data));
                }

                if (transformChanged)
                {
                    this.helper.ReRenderMarkers();
                }
            }
            else if (e.Action == NotifyCollectionChangedAction.Reset)
            {
                this.helper.Clear();
                if (this.PlotVisualizationObject.Data != null && this.PlotVisualizationObject.Data.Count > 0)
                {
                    this.UpdateTransforms();
                    foreach (var point in this.PlotVisualizationObject.Data)
                    {
                        this.helper.AddPoint(0, point.OriginatingTime, this.PlotVisualizationObject.GetNumericValue(point.Data));
                    }
                }
            }
            else
            {
                throw new NotImplementedException($"PlotVisualizationObjectView.OnDataCollectionChanged: Unexpected collectionChanged {e.Action} action.");
            }
        }

        /// <inheritdoc/>
        protected override void OnSummaryDataCollectionChanged(object sender, NotifyCollectionChangedEventArgs e)
        {
            if (e.Action == NotifyCollectionChangedAction.Add)
            {
                foreach (var item in e.NewItems)
                {
                    IntervalData<TData> range = (IntervalData<TData>)item;
                    this.helper.AddRange(
                        0,
                        range.OriginatingTime,
                        this.PlotVisualizationObject.GetNumericValue(range.Value),
                        this.PlotVisualizationObject.GetNumericValue(range.Minimum),
                        this.PlotVisualizationObject.GetNumericValue(range.Maximum));
                }
            }
            else if (e.Action == NotifyCollectionChangedAction.Reset)
            {
                this.helper.Clear();
                if (this.PlotVisualizationObject.SummaryData != null && this.PlotVisualizationObject.SummaryData.Count > 0)
                {
                    this.UpdateTransforms();
                    foreach (var range in this.PlotVisualizationObject.SummaryData)
                    {
                        this.helper.AddRange(
                            0,
                            range.OriginatingTime,
                            this.PlotVisualizationObject.GetNumericValue(range.Value),
                            this.PlotVisualizationObject.GetNumericValue(range.Minimum),
                            this.PlotVisualizationObject.GetNumericValue(range.Maximum));
                    }
                }
            }
            else if (e.Action == NotifyCollectionChangedAction.Replace)
            {
                // Currently, the only time we get hit with a replace is on the boundary intervals of a
                // range of new intervals being added, and those will be included in the Add notification,
                // so we handle them anyway. The old value being replaced should already have been removed
                // when its containing segment was removed.
            }
            else
            {
                throw new NotImplementedException($"PlotVisualizationObjectView.SummaryData_CollectionChanged: Unexpected collectionChanged {e.Action} action.");
            }
        }

        /// <inheritdoc/>
        protected override bool UpdateTransforms()
        {
            // Exit early if view is not parented
            if (this.PlotVisualizationObject.Panel == null)
            {
                return false;
            }

            var baseUpdate = base.UpdateTransforms();

            // now do the update on the Y scale
            double oldScaleY = this.ScaleTransform.ScaleY;
            double oldTranslateY = this.TranslateTransform.Y;

            double maxY = this.PlotVisualizationObject.YAxis.Maximum;
            double minY = this.PlotVisualizationObject.YAxis.Minimum;
            if (maxY == minY)
            {
                maxY += 1;
                minY -= 1;
            }

            var deltaY = maxY - minY;
            this.ScaleTransform.ScaleY = -this.PlotVisualizationObject.Panel.Height / deltaY;
            this.TranslateTransform.Y = -maxY;
            return baseUpdate || oldScaleY != this.ScaleTransform.ScaleY || oldTranslateY != this.TranslateTransform.Y;
        }

        /// <inheritdoc/>
        protected override void UpdateView()
        {
            this.helper.ReRenderMarkers();
        }
    }
}

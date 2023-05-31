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
    using Microsoft.Psi.Visualization.Data;
    using Microsoft.Psi.Visualization.Navigation;
    using Microsoft.Psi.Visualization.VisualizationObjects;

    /// <summary>
    /// Implements a visualization object view for a <see cref="PlotSeriesVisualizationObject{TKey, TData}"/>.
    /// </summary>
    /// <typeparam name="TPlotSeriesVisualizationObject">The type of the plot series visualization object.</typeparam>
    /// <typeparam name="TKey">The type of the series key.</typeparam>
    /// <typeparam name="TData">The type of the data.</typeparam>
    /// <remarks>
    /// The class implements the <see cref="IPlotVisualizationObjectView{TKey, TData}"/> interface, and uses
    /// the <see cref="PlotVisualizationObjectViewHelper{TKey, TData}"/> helper class via aggregation to
    /// generate the necessary views.
    /// </remarks>
    public class PlotSeriesVisualizationObjectView<TPlotSeriesVisualizationObject, TKey, TData> :
        StreamIntervalVisualizationObjectTimelineCanvasView<TPlotSeriesVisualizationObject, Dictionary<TKey, TData>>,
        IPlotVisualizationObjectView<TKey, TData>
        where TPlotSeriesVisualizationObject : PlotSeriesVisualizationObject<TKey, TData>, new()
    {
        private readonly PlotVisualizationObjectViewHelper<TKey, TData> helper;
        private readonly Dictionary<TKey, int> seriesKeyColorPaletteIndex = new ();

        /// <summary>
        /// Initializes a new instance of the <see cref="PlotSeriesVisualizationObjectView{TTimelineVisualizationObject, TKey, TData}"/> class.
        /// </summary>
        public PlotSeriesVisualizationObjectView()
        {
            this.helper = new PlotVisualizationObjectViewHelper<TKey, TData>(this);
        }

        /// <summary>
        /// Gets the plot series visualization object.
        /// </summary>
        public TPlotSeriesVisualizationObject PlotSeriesVisualizationObject
            => this.StreamVisualizationObject as TPlotSeriesVisualizationObject;

        /// <inheritdoc/>
        Transform IPlotVisualizationObjectView<TKey, TData>.TransformGroup => this.TransformGroup;

        /// <inheritdoc/>
        public InterpolationStyle InterpolationStyle => this.PlotSeriesVisualizationObject.InterpolationStyle;

        /// <inheritdoc/>
        public MarkerStyle MarkerStyle => this.PlotSeriesVisualizationObject.MarkerStyle;

        /// <inheritdoc/>
        public double MarkerSize => this.PlotSeriesVisualizationObject.MarkerSize;

        /// <inheritdoc/>
        public void CreateBindings(TKey seriesKey, Path linePath, Path markerPath, Path rangePath)
        {
            if (!this.seriesKeyColorPaletteIndex.ContainsKey(seriesKey))
            {
                this.seriesKeyColorPaletteIndex.Add(seriesKey, this.seriesKeyColorPaletteIndex.Count());
            }

            // Create binding for line color
            var binding = new Binding(nameof(this.PlotSeriesVisualizationObject.PaletteColors))
            {
                Source = this.PlotSeriesVisualizationObject,
                Converter = new Converters.ColorPaletteConverter(),
                ConverterParameter = this.seriesKeyColorPaletteIndex[seriesKey],
            };
            BindingOperations.SetBinding(linePath, Shape.StrokeProperty, binding);

            // Create binding for line width
            binding = new Binding(nameof(this.PlotSeriesVisualizationObject.LineWidth))
            {
                Source = this.PlotSeriesVisualizationObject,
            };
            BindingOperations.SetBinding(linePath, Shape.StrokeThicknessProperty, binding);

            // Create binding for marker stroke color
            binding = new Binding(nameof(this.PlotSeriesVisualizationObject.PaletteColors))
            {
                Source = this.PlotSeriesVisualizationObject,
                Converter = new Converters.ColorPaletteConverter(),
                ConverterParameter = this.seriesKeyColorPaletteIndex[seriesKey],
            };
            BindingOperations.SetBinding(markerPath, Shape.StrokeProperty, binding);

            // Create binding for marker fill color
            binding = new Binding(nameof(this.PlotSeriesVisualizationObject.PaletteColors))
            {
                Source = this.PlotSeriesVisualizationObject,
                Converter = new Converters.ColorPaletteConverter(),
                ConverterParameter = this.seriesKeyColorPaletteIndex[seriesKey],
            };
            BindingOperations.SetBinding(markerPath, Shape.FillProperty, binding);

            // Create binding for range color
            binding = new Binding(nameof(this.PlotSeriesVisualizationObject.PaletteColors))
            {
                Source = this.PlotSeriesVisualizationObject,
                Converter = new Converters.ColorPaletteConverter(),
                ConverterParameter = this.seriesKeyColorPaletteIndex[seriesKey],
            };
            BindingOperations.SetBinding(rangePath, Shape.StrokeProperty, binding);

            // Create binding for range width
            binding = new Binding(nameof(this.PlotSeriesVisualizationObject.RangeWidth))
            {
                Source = this.PlotSeriesVisualizationObject,
            };
            BindingOperations.SetBinding(rangePath, Shape.StrokeThicknessProperty, binding);
        }

        /// <inheritdoc/>
        public IEnumerable<(DateTime OriginatingTime, TData Value, bool Available)> GetDataPoints(TKey seriesKey)
            => (this.PlotSeriesVisualizationObject.Data != null) ?
                this.PlotSeriesVisualizationObject.Data.Select(m => (m.OriginatingTime, m.Data.ContainsKey(seriesKey) ? m.Data[seriesKey] : default, m.Data.ContainsKey(seriesKey))) :
                this.PlotSeriesVisualizationObject.SummaryData.Select(m => (m.OriginatingTime, m.Value.ContainsKey(seriesKey) ? m.Value[seriesKey] : default, m.Value.ContainsKey(seriesKey)));

        /// <inheritdoc/>
        public double GetNumericValue(TData data) => this.PlotSeriesVisualizationObject.GetNumericValue(data);

        /// <inheritdoc/>
        protected override void OnVisualizationObjectPropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            base.OnVisualizationObjectPropertyChanged(sender, e);

            if (e.PropertyName == nameof(this.PlotSeriesVisualizationObject.MarkerSize) ||
                e.PropertyName == nameof(this.PlotSeriesVisualizationObject.MarkerStyle))
            {
                this.UpdateView();
            }
            else if (e.PropertyName == nameof(this.PlotSeriesVisualizationObject.YAxis))
            {
                if (this.UpdateTransforms())
                {
                    this.UpdateView();
                }
            }
            else if (e.PropertyName == nameof(this.PlotSeriesVisualizationObject.InterpolationStyle))
            {
                if (this.PlotSeriesVisualizationObject.Data != null)
                {
                    this.OnDataCollectionChanged(this.PlotSeriesVisualizationObject.Data, new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Reset));
                }
                else if (this.PlotSeriesVisualizationObject.SummaryData != null)
                {
                    this.OnSummaryDataCollectionChanged(this.PlotSeriesVisualizationObject.SummaryData, new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Reset));
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
                    var message = (Message<Dictionary<TKey, TData>>)item;
                    if (message.Data != null)
                    {
                        foreach (var key in message.Data.Keys)
                        {
                            this.helper.AddPoint(key, message.OriginatingTime, this.PlotSeriesVisualizationObject.GetNumericValue(message.Data[key]));
                        }
                    }
                }

                if (transformChanged)
                {
                    this.helper.ReRenderMarkers();
                }
            }
            else if (e.Action == NotifyCollectionChangedAction.Reset)
            {
                this.helper.Clear();
                if (this.PlotSeriesVisualizationObject.Data != null && this.PlotSeriesVisualizationObject.Data.Count > 0)
                {
                    this.UpdateTransforms();
                    foreach (var message in this.PlotSeriesVisualizationObject.Data)
                    {
                        if (message.Data != null)
                        {
                            foreach (var key in message.Data.Keys)
                            {
                                this.helper.AddPoint(key, message.OriginatingTime, this.PlotSeriesVisualizationObject.GetNumericValue(message.Data[key]));
                            }
                        }
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
                    IntervalData<Dictionary<TKey, TData>> range = (IntervalData<Dictionary<TKey, TData>>)item;
                    foreach (var key in range.Value.Keys)
                    {
                        this.helper.AddRange(
                            key,
                            range.OriginatingTime,
                            this.PlotSeriesVisualizationObject.GetNumericValue(range.Value[key]),
                            this.PlotSeriesVisualizationObject.GetNumericValue(range.Minimum[key]),
                            this.PlotSeriesVisualizationObject.GetNumericValue(range.Maximum[key]));
                    }
                }
            }
            else if (e.Action == NotifyCollectionChangedAction.Reset)
            {
                this.helper.Clear();
                if (this.PlotSeriesVisualizationObject.SummaryData != null && this.PlotSeriesVisualizationObject.SummaryData.Count > 0)
                {
                    this.UpdateTransforms();
                    foreach (var range in this.PlotSeriesVisualizationObject.SummaryData)
                    {
                        foreach (var key in range.Value.Keys)
                        {
                            this.helper.AddRange(
                                key,
                                range.OriginatingTime,
                                this.PlotSeriesVisualizationObject.GetNumericValue(range.Value[key]),
                                this.PlotSeriesVisualizationObject.GetNumericValue(range.Minimum[key]),
                                this.PlotSeriesVisualizationObject.GetNumericValue(range.Maximum[key]));
                        }
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
            if (this.PlotSeriesVisualizationObject.Panel == null)
            {
                return false;
            }

            var baseUpdate = base.UpdateTransforms();

            // now do the update on the Y scale
            double oldScaleY = this.ScaleTransform.ScaleY;
            double oldTranslateY = this.TranslateTransform.Y;

            double maxY = this.PlotSeriesVisualizationObject.YAxis.Maximum;
            double minY = this.PlotSeriesVisualizationObject.YAxis.Minimum;
            if (maxY == minY)
            {
                maxY += 1;
                minY -= 1;
            }

            var deltaY = maxY - minY;
            this.ScaleTransform.ScaleY = -this.PlotSeriesVisualizationObject.Panel.Height / ((deltaY != 0) ? deltaY * 1.2 : 1.0);
            this.TranslateTransform.Y = -maxY - (deltaY * 0.1);
            return baseUpdate || oldScaleY != this.ScaleTransform.ScaleY || oldTranslateY != this.TranslateTransform.Y;
        }

        /// <inheritdoc/>
        protected override void UpdateView()
        {
            this.helper.ReRenderMarkers();
        }
    }
}

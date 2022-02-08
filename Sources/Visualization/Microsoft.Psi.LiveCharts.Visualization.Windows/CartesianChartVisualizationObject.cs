// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.

namespace Microsoft.Psi.Visualization.LiveCharts
{
    using System;
    using System.Collections.Generic;
    using System.ComponentModel;
    using System.Linq;
    using System.Runtime.Serialization;
    using System.Windows;
    using global::LiveCharts;
    using global::LiveCharts.Defaults;
    using global::LiveCharts.Wpf;
    using Microsoft.Psi.Visualization.Helpers;
    using Microsoft.Psi.Visualization.VisualizationObjects;
    using Microsoft.Psi.Visualization.VisualizationPanels;

    /// <summary>
    /// Enumeration that defines the type of cartesian series to use.
    /// </summary>
    public enum CartesianChartType
    {
        /// <summary>
        /// Line series.
        /// </summary>
        Line,

        /// <summary>
        /// Vertical line series.
        /// </summary>
        VerticalLine,

        /// <summary>
        /// Column series.
        /// </summary>
        Column,

        /// <summary>
        /// Row series.
        /// </summary>
        Row,

        /// <summary>
        /// Stacked area series.
        /// </summary>
        StackedArea,

        /// <summary>
        /// Vertical stacked area series.
        /// </summary>
        VerticalStackedArea,

        /// <summary>
        /// Stacked column series.
        /// </summary>
        StackedColumn,

        /// <summary>
        /// Stacked row series.
        /// </summary>
        StackedRow,
    }

    /// <summary>
    /// Implements a cartesian chart visualization object for series of a specified type which can be mapped to cartesian coordinates.
    /// </summary>
    /// <typeparam name="T">The underlying type of the objects to visualize.</typeparam>
    /// <remarks>
    /// <para>
    /// This is an instant visualization object operating over a dictionary. Each entry in the dictionary denotes a
    /// series. The key of the dictionary describes the name of the series, and the value of the dictionary entry contains the
    /// array of objects of type T to visualize. Upon construction, a cartesian mapper needs to be provided that maps every
    /// object of type T into a set of (x, y) coordinate.
    /// </para>
    /// <para>
    /// This instant visualization object can be used as a base class to define more specific cartesian chart visualization objects,
    /// for example <see cref="HistogramVisualizationObject"/>.
    /// </para></remarks>
    [VisualizationObject("Cartesian Chart")]
    [VisualizationPanelType(VisualizationPanelType.Canvas)]
    public class CartesianChartVisualizationObject<T> : StreamValueVisualizationObject<Dictionary<string, T[]>>
    {
        /// <summary>
        /// Exception message that describes encountering an unknown cartesian chart type.
        /// </summary>
        protected const string UnknownCartesianChartTypeMessage = "Unknown cartesian chart type.";

        private readonly Func<Dictionary<string, T[]>, string, int, (double, double)> cartesianMapper;
        private readonly Dictionary<string, int> seriesMapping = new Dictionary<string, int>();
        private SeriesCollection seriesCollection = new SeriesCollection();
        private Func<double, string> axisXLabelFormatter;
        private Func<double, string> axisYLabelFormatter;
        private string[] axisXLabels;
        private string[] axisYLabels;
        private string axisXTitle = "X-Axis";
        private string axisYTitle = "Y-Axis";
        private bool disableAnimations = true;
        private CartesianChartType cartesianChartType = CartesianChartType.Line;

        /// <summary>
        /// Initializes a new instance of the <see cref="CartesianChartVisualizationObject{T}"/> class.
        /// </summary>
        /// <param name="cartesianMapper">A mapping function that projects an object of type T into (x, y) coordinates.</param>
        /// <remarks>The cartesian mapper function will receive as input the entire dictionary corresponding to all the
        /// series, and the name of the series and index of the datapoint to be converted. It will need to return a tuple
        /// of doubles containing the x and y coordinates for the datapoint.</remarks>
        public CartesianChartVisualizationObject(
            Func<Dictionary<string, T[]>, string, int, (double, double)> cartesianMapper)
            : base()
        {
            this.cartesianMapper = cartesianMapper;
        }

        /// <inheritdoc />
        [IgnoreDataMember]
        public override DataTemplate DefaultViewTemplate => XamlHelper.CreateTemplate(this.GetType(), typeof(CartesianChartVisualizationObjectView));

        /// <summary>
        /// Gets or sets the series collection.
        /// </summary>
        [IgnoreDataMember]
        [Browsable(false)]
        public SeriesCollection SeriesCollection
        {
            get { return this.seriesCollection; }
            set { this.Set(nameof(this.SeriesCollection), ref this.seriesCollection, value); }
        }

        /// <summary>
        /// Gets or sets the X-axis labels formatter.
        /// </summary>
        [IgnoreDataMember]
        [Browsable(false)]
        public Func<double, string> AxisXLabelFormatter
        {
            get { return this.axisXLabelFormatter; }
            set { this.Set(nameof(this.AxisXLabelFormatter), ref this.axisXLabelFormatter, value); }
        }

        /// <summary>
        /// Gets or sets the Y-axis labels formatter.
        /// </summary>
        [IgnoreDataMember]
        [Browsable(false)]
        public Func<double, string> AxisYLabelFormatter
        {
            get { return this.axisYLabelFormatter; }
            set { this.Set(nameof(this.AxisYLabelFormatter), ref this.axisYLabelFormatter, value); }
        }

        /// <summary>
        /// Gets or sets the X-axis labels.
        /// </summary>
        [IgnoreDataMember]
        [Browsable(false)]
        public string[] AxisXLabels
        {
            get { return this.axisXLabels; }
            set { this.Set(nameof(this.AxisXLabels), ref this.axisXLabels, value); }
        }

        /// <summary>
        /// Gets or sets the Y-axis labels.
        /// </summary>
        [IgnoreDataMember]
        [Browsable(false)]
        public string[] AxisYLabels
        {
            get { return this.axisYLabels; }
            set { this.Set(nameof(this.AxisYLabels), ref this.axisYLabels, value); }
        }

        /// <summary>
        /// Gets or sets the X-axis title.
        /// </summary>
        [DataMember]
        [DisplayName("X Axis Title")]
        [Description("The title for the X axis.")]
        public string AxisXTitle
        {
            get { return this.axisXTitle; }
            set { this.Set(nameof(this.AxisXTitle), ref this.axisXTitle, value); }
        }

        /// <summary>
        /// Gets or sets the Y-axis title.
        /// </summary>
        [DataMember]
        [DisplayName("Y Axis Title")]
        [Description("The title for the Y axis.")]
        public string AxisYTitle
        {
            get { return this.axisYTitle; }
            set { this.Set(nameof(this.AxisYTitle), ref this.axisYTitle, value); }
        }

        /// <summary>
        /// Gets or sets a value indicating whether to disable animations.
        /// </summary>
        [DataMember]
        [DisplayName("Disable Animations")]
        [Description("If checked, animations are disabled.")]
        public bool DisableAnimations
        {
            get { return this.disableAnimations; }
            set { this.Set(nameof(this.DisableAnimations), ref this.disableAnimations, value); }
        }

        /// <summary>
        /// Gets or sets the type of cartesian series.
        /// </summary>
        [DataMember]
        [DisplayName("Chart type")]
        [Description("The type of the cartesian chart.")]
        public CartesianChartType CartesianChartType
        {
            get { return this.cartesianChartType; }
            set { this.Set(nameof(this.CartesianChartType), ref this.cartesianChartType, value); }
        }

        /// <inheritdoc/>
        protected override void OnPropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            Application.Current.Dispatcher.Invoke(() =>
            {
                if (e != null && e.PropertyName == nameof(this.CurrentData))
                {
                    if (this.CurrentData != null)
                    {
                        foreach (var kvp in this.CurrentData)
                        {
                            var series = this.seriesMapping.ContainsKey(kvp.Key) ?
                                this.SeriesCollection[this.seriesMapping[kvp.Key]] : this.CreateSeries(kvp.Key);

                            if (series.Values == null)
                            {
                                series.Values = this.ConstructChartValues(kvp.Key);
                            }
                            else if (kvp.Value == null)
                            {
                                series.Values.Clear();
                            }
                            else
                            {
                                var min = Math.Min(series.Values.Count, kvp.Value.Length);
                                for (int i = 0; i < min; i++)
                                {
                                    var observablePoint = (series.Values as ChartValues<ObservablePoint>)[i];
                                    (observablePoint.X, observablePoint.Y) = this.GetXYValues(kvp.Key, i);
                                }

                                if (series.Values.Count > kvp.Value.Length)
                                {
                                    while (series.Values.Count > kvp.Value.Length)
                                    {
                                        series.Values.RemoveAt(kvp.Value.Length);
                                    }
                                }
                                else if (series.Values.Count < kvp.Value.Length)
                                {
                                    for (int i = series.Values.Count; i < kvp.Value.Length; i++)
                                    {
                                        (var x, var y) = this.GetXYValues(kvp.Key, i);
                                        series.Values.Add(new ObservablePoint(x, y));
                                    }
                                }
                            }

                            if (!this.seriesMapping.ContainsKey(kvp.Key))
                            {
                                this.seriesMapping.Add(kvp.Key, this.SeriesCollection.Count);
                                this.SeriesCollection.Add(series);
                            }
                        }

                        // remove series that are not present in the data
                        var unfoundSeries = this.seriesMapping.Keys.Where(s => !this.CurrentData.ContainsKey(s)).ToArray();
                        var unfoundSeriesView = unfoundSeries.Select(s => this.SeriesCollection[this.seriesMapping[s]]).ToArray();

                        foreach (var series in unfoundSeries)
                        {
                            this.seriesMapping.Remove(series);
                        }

                        foreach (var seriesView in unfoundSeriesView)
                        {
                            this.SeriesCollection.Remove(seriesView);
                        }
                    }
                    else
                    {
                        if (this.SeriesCollection.Count > 0)
                        {
                            this.SeriesCollection = new SeriesCollection();
                            this.seriesMapping.Clear();
                        }
                    }
                }
                else if (e != null && e.PropertyName == nameof(this.CartesianChartType))
                {
                    if (this.CurrentData != null)
                    {
                        this.SeriesCollection = new SeriesCollection();
                        this.seriesMapping.Clear();

                        foreach (var kvp in this.CurrentData)
                        {
                            this.seriesMapping.Add(kvp.Key, this.SeriesCollection.Count);
                            this.SeriesCollection.Add(this.CreateSeries(kvp.Key));
                            this.SeriesCollection[this.seriesMapping[kvp.Key]].Values = this.ConstructChartValues(kvp.Key);
                        }
                    }
                }
            });

            base.OnPropertyChanged(sender, e);
        }

        private Series CreateSeries(string title) =>
            this.CartesianChartType switch
            {
                CartesianChartType.Line => new LineSeries() { Title = title },
                CartesianChartType.VerticalLine => new VerticalLineSeries() { Title = title },
                CartesianChartType.Column => new ColumnSeries() { Title = title },
                CartesianChartType.Row => new RowSeries() { Title = title },
                CartesianChartType.StackedArea => new StackedAreaSeries() { Title = title },
                CartesianChartType.VerticalStackedArea => new VerticalStackedAreaSeries() { Title = title },
                CartesianChartType.StackedColumn => new StackedColumnSeries() { Title = title },
                CartesianChartType.StackedRow => new StackedRowSeries() { Title = title },
                _ => throw new Exception(UnknownCartesianChartTypeMessage)
            };

        private ChartValues<ObservablePoint> ConstructChartValues(string seriesName)
        {
            var result = new ChartValues<ObservablePoint>();

            if (this.CurrentData[seriesName] != null)
            {
                for (int i = 0; i < this.CurrentData[seriesName].Length; i++)
                {
                    (var x, var y) = this.GetXYValues(seriesName, i);
                    result.Add(new ObservablePoint(x, y));
                }
            }

            return result;
        }

        private (double X, double Y) GetXYValues(string seriesName, int index)
        {
            (var x, var y) = this.cartesianMapper(this.CurrentData, seriesName, index);
            return this.CartesianChartType switch
            {
                CartesianChartType.Line => (x, y),
                CartesianChartType.VerticalLine => (y, x),
                CartesianChartType.Column => (x, y),
                CartesianChartType.Row => (y, x),
                CartesianChartType.StackedArea => (x, y),
                CartesianChartType.VerticalStackedArea => (y, x),
                CartesianChartType.StackedColumn => (x, y),
                CartesianChartType.StackedRow => (y, x),
                _ => throw new Exception(UnknownCartesianChartTypeMessage)
            };
        }
    }
}

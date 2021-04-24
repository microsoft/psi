// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.

namespace Microsoft.Psi.Visualization.VisualizationObjects
{
    using System;
    using System.Collections.Specialized;
    using System.ComponentModel;
    using System.Linq;
    using System.Runtime.Serialization;
    using System.Windows.Media;
    using Microsoft.Psi.Visualization.Collections;
    using Microsoft.Psi.Visualization.Data;
    using Microsoft.Psi.Visualization.Navigation;

    /// <summary>
    /// Provides a base, abstract class for stream visualization objects that show data from a stream interval.
    /// </summary>
    /// <typeparam name="TData">The type of the stream data.</typeparam>
    public abstract class StreamIntervalVisualizationObject<TData> : StreamVisualizationObject<TData>
    {
        private long samplingTicks;
        private string legendFormat = string.Empty;
        private TimeSpan viewDuration;
        private Guid summaryDataConsumerId = Guid.Empty;

        /// <summary>
        /// The data read from the stream.
        /// </summary>
        private ObservableKeyedCache<DateTime, Message<TData>>.ObservableKeyedView data;

        /// <summary>
        /// The interval data summarized from the stream data.
        /// </summary>
        private ObservableKeyedCache<DateTime, IntervalData<TData>>.ObservableKeyedView summaryData;

        /// <summary>
        /// Gets or sets the data view.
        /// </summary>
        [Browsable(false)]
        [IgnoreDataMember]
        public ObservableKeyedCache<DateTime, Message<TData>>.ObservableKeyedView Data
        {
            get => this.data;
            protected set
            {
                if (this.data != value)
                {
                    if (this.data != null)
                    {
                        this.data.DetailedCollectionChanged -= this.OnDataDetailedCollectionChanged;
                    }

                    this.Set(nameof(this.Data), ref this.data, value);

                    if (this.data != null)
                    {
                        this.data.DetailedCollectionChanged += this.OnDataDetailedCollectionChanged;
                        this.OnDataCollectionChanged(new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Reset));
                    }
                    else
                    {
                        this.SetCurrentValue(null);
                    }
                }
            }
        }

        /// <summary>
        /// Gets or sets the summary data view.
        /// </summary>
        [Browsable(false)]
        [IgnoreDataMember]
        public ObservableKeyedCache<DateTime, IntervalData<TData>>.ObservableKeyedView SummaryData
        {
            get => this.summaryData;
            protected set
            {
                if (this.summaryData != value)
                {
                    var oldValue = this.summaryData;
                    this.Set(nameof(this.SummaryData), ref this.summaryData, value);
                    this.OnSummaryDataChanged(oldValue, this.summaryData);
                }
            }
        }

        /// <summary>
        /// Gets the value of the color to use when displaying in the legend. By default, white.
        /// </summary>
        public virtual Color LegendColor => Colors.White;

        /// <summary>
        /// Gets the value to display in the live legend. By default a formatted version of the current value is returned.
        /// </summary>
        [IgnoreDataMember]
        [DisplayName("Legend Value")]
        [Description("The legend value.")]
        public virtual string LegendValue
        {
            get
            {
                if (this.CurrentValue.HasValue)
                {
                    var format = $"{{0:{this.LegendFormat}}}";
                    return string.Format(format, this.CurrentValue.Value.Data.ToString());
                }
                else
                {
                    return string.Empty;
                }
            }
        }

        /// <summary>
        /// Gets or sets the sampling ticks.
        /// </summary>
        [Browsable(false)]
        [DataMember]
        public long SamplingTicks
        {
            get => this.samplingTicks;
            set
            {
                if (value > 0)
                {
                    value = 1L << (int)Math.Log(value, 2);
                }

                this.Set(nameof(this.SamplingTicks), ref this.samplingTicks, value);
            }
        }

        /// <summary>
        /// Gets or sets a format specifier string used in displaying the live legend value.
        /// </summary>
        [DataMember]
        [DisplayName("Legend Format")]
        [Description("The formatting string for the legend.")]
        public string LegendFormat
        {
            get { return this.legendFormat; }

            set
            {
                this.RaisePropertyChanging(nameof(this.LegendValue));
                this.Set(nameof(this.LegendFormat), ref this.legendFormat, value);
                this.RaisePropertyChanged(nameof(this.LegendValue));
            }
        }

        /// <summary>
        /// Gets a value indicating whether the visualization object is using summarization.
        /// </summary>
        protected bool IsUsingSummarization => this.StreamBinding.SummarizerType != null;

        /// <inheritdoc/>
        protected override void OnCursorModeChanged(object sender, CursorModeChangedEventArgs cursorModeChangedEventArgs)
        {
            // If we changed from or to live mode, and we're currently bound to a datasource, then refresh the data or summaries
            if (this.IsBound && cursorModeChangedEventArgs.OriginalValue != cursorModeChangedEventArgs.NewValue)
            {
                if ((cursorModeChangedEventArgs.OriginalValue == CursorMode.Live) || (cursorModeChangedEventArgs.NewValue == CursorMode.Live))
                {
                    this.RefreshData();
                }
            }

            base.OnCursorModeChanged(sender, cursorModeChangedEventArgs);
        }

        /// <inheritdoc />
        protected override void OnStreamBound()
        {
            base.OnStreamBound();

            // Register as a summary data consumer
            if (this.IsUsingSummarization)
            {
                if (this.summaryDataConsumerId != Guid.Empty)
                {
                    throw new InvalidOperationException("An attempt was made to register as a summary data consumer while already having an existing summary data consumer id.");
                }

                this.summaryDataConsumerId = DataManager.Instance.RegisterSummaryDataConsumer(this.StreamSource);
            }

            this.Navigator.ViewRange.RangeChanged += this.OnViewRangeChanged;
            this.OnViewRangeChanged(
                this.Navigator.ViewRange,
                new NavigatorTimeRangeChangedEventArgs(this.Navigator.ViewRange.StartTime, this.Navigator.ViewRange.StartTime, this.Navigator.ViewRange.EndTime, this.Navigator.ViewRange.EndTime));
        }

        /// <inheritdoc />
        protected override void OnStreamUnbound()
        {
            base.OnStreamUnbound();

            // Unregister as a summary data consumer
            if (this.IsUsingSummarization)
            {
                if (this.summaryDataConsumerId == Guid.Empty)
                {
                    throw new InvalidOperationException("An attempt was made to unregister as a summary data consumer without having a valid summary data consumer id.");
                }

                DataManager.Instance.UnregisterSummaryDataConsumer(this.summaryDataConsumerId);
                this.summaryDataConsumerId = Guid.Empty;
            }

            this.Navigator.ViewRange.RangeChanged -= this.OnViewRangeChanged;

            this.Data = null;
            this.SummaryData = null;
        }

        /// <inheritdoc/>
        protected override void OnPropertyChanging(object sender, PropertyChangingEventArgs e)
        {
            base.OnPropertyChanging(sender, e);

            if (e.PropertyName == nameof(this.CurrentValue))
            {
                this.RaisePropertyChanging(nameof(this.LegendValue));
            }
        }

        /// <inheritdoc/>
        protected override void OnPropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName == nameof(this.CurrentValue))
            {
                this.RaisePropertyChanged(nameof(this.LegendValue));
            }

            base.OnPropertyChanged(sender, e);
        }

        /// <summary>
        /// Invoked when the <see cref="StreamIntervalVisualizationObject{TData}.SummaryData"/> property changes.
        /// </summary>
        /// <param name="oldValue">The old summary data value.</param>
        /// <param name="newValue">The new summary data value.</param>
        protected virtual void OnSummaryDataChanged(
            ObservableKeyedCache<DateTime, IntervalData<TData>>.ObservableKeyedView oldValue,
            ObservableKeyedCache<DateTime, IntervalData<TData>>.ObservableKeyedView newValue)
        {
            if (oldValue != null)
            {
                oldValue.DetailedCollectionChanged -= this.OnSummaryDataDetailedCollectionChanged;
            }

            if (newValue != null)
            {
                newValue.DetailedCollectionChanged += this.OnSummaryDataDetailedCollectionChanged;
            }

            this.OnSummaryDataCollectionChanged(new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Reset));
        }

        /// <summary>
        /// Called when data collection contents have changed.
        /// </summary>
        /// <param name="e">Collection changed event arguments.</param>
        protected virtual void OnDataCollectionChanged(NotifyCollectionChangedEventArgs e)
        {
            // see if we are still active
            if (this.Container == null)
            {
                return;
            }

            if (this.Navigator.CursorMode == CursorMode.Live)
            {
                var last = this.Data.LastOrDefault();
                if (last != default)
                {
                    this.SetCurrentValue(last);
                }
            }
        }

        /// <summary>
        /// Invoked when the <see cref="StreamIntervalVisualizationObject{TData}.SummaryData"/> collection changes.
        /// </summary>
        /// <param name="e">Collection changed event arguments.</param>
        protected virtual void OnSummaryDataCollectionChanged(NotifyCollectionChangedEventArgs e)
        {
            // see if we are still active
            if (this.Container == null)
            {
                return;
            }

            if (this.Navigator.IsCursorModePlayback)
            {
                IntervalData<TData> last = this.SummaryData.LastOrDefault();
                if (last != default)
                {
                    this.SetCurrentValue(Message.Create(last.Value, last.OriginatingTime, last.EndTime, 0, 0));
                }
            }
        }

        /// <inheritdoc />
        protected override void OnCursorChanged(object sender, NavigatorTimeChangedEventArgs e)
        {
            DateTime currentTime = e.NewTime;

            if (this.SummaryData != null)
            {
                int index = this.GetIndexForTime(currentTime, this.SummaryData?.Count ?? 0, (idx) => this.SummaryData[idx].OriginatingTime);
                if (index != -1)
                {
                    var interval = this.SummaryData[index];
                    this.SetCurrentValue(Message.Create(interval.Value, interval.OriginatingTime, interval.EndTime, 0, 0));
                }
                else
                {
                    this.SetCurrentValue(null);
                }
            }
            else
            {
                int index = this.GetIndexForTime(currentTime, this.Data?.Count ?? 0, (idx) => this.Data[idx].OriginatingTime);
                if (index != -1)
                {
                    this.SetCurrentValue(this.Data[index]);
                }
                else
                {
                    this.SetCurrentValue(null);
                }
            }

            base.OnCursorChanged(sender, e);
        }

        /// <inheritdoc/>
        protected override void OnPanelPropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName == nameof(this.Panel.Width))
            {
                if (this.StreamBinding?.SummarizerType != null)
                {
                    this.SamplingTicks = (long)(this.viewDuration.Ticks / this.Panel.Width);
                    this.RefreshData();
                }
            }

            base.OnPanelPropertyChanged(sender, e);
        }

        private void OnViewRangeChanged(object sender, NavigatorTimeRangeChangedEventArgs e)
        {
            this.RefreshData();
        }

        private void RefreshData()
        {
            // Check that we're actually bound to a store
            if (this.IsBound)
            {
                if (this.Navigator.CursorMode == CursorMode.Live)
                {
                    if (this.viewDuration != this.Navigator.ViewRange.Duration)
                    {
                        this.viewDuration = this.Navigator.ViewRange.Duration;

                        if (this.IsUsingSummarization)
                        {
                            // If performing summarization, recompute the sampling tick interval (i.e. summarization interval)
                            // whenever the view range duration has changed.
                            this.SamplingTicks = (long)(this.viewDuration.Ticks / this.Panel.Width);
                            this.SummaryData = DataManager.Instance.ReadSummary<TData>(
                                this.StreamSource,
                                TimeSpan.FromTicks(this.SamplingTicks),
                                last => last - this.viewDuration);
                        }
                        else
                        {
                            // Not summarizing, so read data directly from the stream
                            this.Data = DataManager.Instance.ReadStream<TData>(this.StreamSource, last => last - this.viewDuration);
                        }
                    }
                }
                else
                {
                    this.viewDuration = this.Navigator.ViewRange.Duration;

                    if (this.IsUsingSummarization)
                    {
                        // If performing summarization, recompute the sampling tick interval (i.e. summarization interval)
                        // whenever the view range duration has changed.
                        this.SamplingTicks = (long)(this.viewDuration.Ticks / this.Panel.Width);

                        var startTime = this.Navigator.ViewRange.StartTime;
                        var endTime = this.Navigator.ViewRange.EndTime;

                        // Attempt to read a little extra data outside the view range so that the end
                        // points appear to connect to the next/previous values. This is flawed as we
                        // are just guessing how much to extend the time interval by. What we really
                        // need is for the DataManager to give us everything in the requested time
                        // interval plus the next/previous data point just outside the interval.
                        var extra = TimeSpan.FromMilliseconds(100);

                        this.SummaryData = DataManager.Instance.ReadSummary<TData>(
                            this.StreamSource,
                            startTime > DateTime.MinValue + extra ? startTime - extra : startTime,
                            endTime < DateTime.MaxValue - extra ? endTime + extra : endTime,
                            TimeSpan.FromTicks(this.SamplingTicks));
                    }
                    else
                    {
                        // Not summarizing, so read data directly from the stream - note that end time is exclusive, so adding one tick to ensure any message at EndTime is included
                        this.Data = DataManager.Instance.ReadStream<TData>(
                            this.StreamSource,
                            this.Navigator.ViewRange.StartTime,
                            this.Navigator.ViewRange.EndTime.AddTicks(1));
                    }
                }
            }
            else
            {
                this.Data = null;
                this.SummaryData = null;
            }
        }

        private void OnDataDetailedCollectionChanged(object sender, NotifyCollectionChangedEventArgs e)
        {
            this.OnDataCollectionChanged(e);
        }

        private void OnSummaryDataDetailedCollectionChanged(object sender, NotifyCollectionChangedEventArgs e)
        {
            this.OnSummaryDataCollectionChanged(e);
        }
    }
}

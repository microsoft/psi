// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.

namespace Microsoft.Psi.Visualization.VisualizationObjects
{
    using System;
    using System.Collections.Specialized;
    using System.ComponentModel;
    using System.Linq;
    using System.Runtime.Serialization;
    using Microsoft.Psi.Visualization.Collections;
    using Microsoft.Psi.Visualization.Config;
    using Microsoft.Psi.Visualization.Data;
    using Microsoft.Psi.Visualization.Navigation;

    /// <summary>
    /// Represents a timeline visualization object.
    /// </summary>
    /// <typeparam name="TData">The type of the timeline visualization.</typeparam>
    /// <typeparam name="TConfig">The type of the timeline visualization object configuration.</typeparam>
    [DataContract(Namespace = "http://www.microsoft.com/psi")]
    public abstract class TimelineVisualizationObject<TData, TConfig> : StreamVisualizationObject<TData, TConfig>
        where TConfig : TimelineVisualizationObjectConfiguration, new()
    {
        private TimeSpan viewDuration;

        /// <summary>
        /// The interval data summarized from the stream data.
        /// </summary>
        private ObservableKeyedCache<DateTime, IntervalData<TData>>.ObservableKeyedView summaryData;

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

        /// <inheritdoc />
        protected override void OnCloseStream()
        {
            ((NavigatorRange)this.Navigator.ViewRange).RangeChanged -= this.OnViewRangeChanged;
            base.OnCloseStream();
        }

        /// <inheritdoc />
        protected override void OnConnect()
        {
            base.OnConnect();
            this.Panel.PropertyChanged += this.ParentPanel_PropertyChanged;
        }

        /// <inheritdoc />
        protected override void OnDisconnect()
        {
            base.OnDisconnect();
            this.Panel.PropertyChanged -= this.ParentPanel_PropertyChanged;
            this.Navigator.ViewRange.RangeChanged -= this.OnViewRangeChanged;
        }

        /// <inheritdoc />
        protected override void OnOpenStream()
        {
            this.Navigator.ViewRange.RangeChanged += this.OnViewRangeChanged;
            this.OnViewRangeChanged(
                this.Navigator.ViewRange,
                new NavigatorTimeRangeChangedEventArgs(this.Navigator.ViewRange.StartTime, this.Navigator.ViewRange.StartTime, this.Navigator.ViewRange.EndTime, this.Navigator.ViewRange.EndTime));
        }

        /// <summary>
        /// Invoked when the <see cref="TimelineVisualizationObject{TData, TConfig}.SummaryData"/> property changes.
        /// </summary>
        /// <param name="oldValue">The old summary data value.</param>
        /// <param name="newValue">The new summary data value.</param>
        protected virtual void OnSummaryDataChanged(
            ObservableKeyedCache<DateTime, IntervalData<TData>>.ObservableKeyedView oldValue,
            ObservableKeyedCache<DateTime, IntervalData<TData>>.ObservableKeyedView newValue)
        {
            if (oldValue != null)
            {
                oldValue.DetailedCollectionChanged -= this.SummaryData_CollectionChanged;
            }

            if (newValue != null)
            {
                newValue.DetailedCollectionChanged += this.SummaryData_CollectionChanged;
            }

            this.OnSummaryDataCollectionChanged(new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Reset));
        }

        /// <summary>
        /// Invoked when the <see cref="TimelineVisualizationObject{TData, TConfig}.SummaryData"/> collection changes.
        /// </summary>
        /// <param name="e">Collection changed event arguments.</param>
        protected virtual void OnSummaryDataCollectionChanged(NotifyCollectionChangedEventArgs e)
        {
            // see if we are still active
            if (this.Container == null)
            {
                return;
            }

            if (this.Navigator.NavigationMode == NavigationMode.Live)
            {
                var last = this.SummaryData.LastOrDefault();
                if (last != default(IntervalData<TData>))
                {
                    this.CurrentValue = Message.Create(last.Value, last.OriginatingTime, last.EndTime, 0, 0);
                    this.Navigator.UpdateLiveExtents(last.OriginatingTime);
                }
            }
        }

        /// <inheritdoc />
        protected override void SetCurrentValue(DateTime currentTime)
        {
            if (this.SummaryData != null)
            {
                int index = this.GetIndexForTime(currentTime, this.SummaryData?.Count ?? 0, (idx) => this.SummaryData[idx].OriginatingTime);
                if (index != -1)
                {
                    var interval = this.SummaryData[index];
                    this.CurrentValue = Message.Create(interval.Value, interval.OriginatingTime, interval.EndTime, 0, 0);
                }
                else
                {
                    base.SetCurrentValue(currentTime);
                }
            }
            else
            {
                int index = this.GetIndexForTime(currentTime, this.Data?.Count ?? 0, (idx) => this.Data[idx].OriginatingTime);
                if (index != -1)
                {
                    this.CurrentValue = this.Data[index];
                }
                else
                {
                    base.SetCurrentValue(currentTime);
                }
            }
        }

        private void OnViewRangeChanged(object sender, NavigatorTimeRangeChangedEventArgs e)
        {
            if (this.Navigator.NavigationMode == NavigationMode.Live)
            {
                if (this.viewDuration != this.Navigator.ViewRange.Duration)
                {
                    this.viewDuration = this.Navigator.ViewRange.Duration;

                    if (this.Configuration.StreamBinding.SummarizerType != null)
                    {
                        // If performing summarization, recompute the sampling tick interval (i.e. summarization interval)
                        // whenever the view range duration has changed.
                        this.Configuration.SamplingTicks = (long)(this.viewDuration.Ticks / this.Panel.Width);
                        this.RefreshSummaryData();
                    }
                    else
                    {
                        // Not summarizing, so read data directly from the stream
                        this.Data = DataManager.Instance.ReadStream<TData>(this.Configuration.StreamBinding, last => last - this.viewDuration);
                    }
                }
            }
            else
            {
                this.viewDuration = this.Navigator.ViewRange.Duration;

                if (this.Configuration.StreamBinding.SummarizerType != null)
                {
                    // If performing summarization, recompute the sampling tick interval (i.e. summarization interval)
                    // whenever the view range duration has changed.
                    this.Configuration.SamplingTicks = (long)(this.viewDuration.Ticks / this.Panel.Width);
                    this.RefreshSummaryData();
                }
                else
                {
                    // Not summarizing, so read data directly from the stream
                    this.Data = DataManager.Instance.ReadStream<TData>(this.Configuration.StreamBinding, this.Navigator.DataRange.StartTime, this.Navigator.DataRange.EndTime);
                }
            }
        }

        private void ParentPanel_PropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName == nameof(this.Panel.Width))
            {
                if (this.Configuration.StreamBinding?.SummarizerType != null)
                {
                    this.Configuration.SamplingTicks = (long)(this.viewDuration.Ticks / this.Panel.Width);
                    this.RefreshSummaryData();
                }
            }
        }

        private void RefreshSummaryData()
        {
            if (this.Navigator.NavigationMode == NavigationMode.Live)
            {
                this.SummaryData = DataManager.Instance.ReadSummary<TData>(
                   this.Configuration.StreamBinding,
                   TimeSpan.FromTicks(this.Configuration.SamplingTicks),
                   last => last - this.viewDuration);
            }
            else
            {
                TimeSpan extra = TimeSpan.FromMilliseconds(100);
                this.SummaryData = DataManager.Instance.ReadSummary<TData>(
                    this.Configuration.StreamBinding,
                    this.Navigator.ViewRange.StartTime - extra,
                    this.Navigator.ViewRange.EndTime + extra,
                    TimeSpan.FromTicks(this.Configuration.SamplingTicks));
            }
        }

        private void SummaryData_CollectionChanged(object sender, NotifyCollectionChangedEventArgs e)
        {
            this.OnSummaryDataCollectionChanged(e);
        }
    }
}

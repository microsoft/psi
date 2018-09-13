// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.

namespace Microsoft.Psi.Visualization.VisualizationObjects
{
    using System;
    using System.Collections.Generic;
    using System.Collections.Specialized;
    using System.ComponentModel;
    using System.Linq;
    using System.Runtime.InteropServices;
    using System.Runtime.Serialization;
    using Microsoft.Psi;
    using Microsoft.Psi.Visualization.Collections;
    using Microsoft.Psi.Visualization.Common;
    using Microsoft.Psi.Visualization.Config;
    using Microsoft.Psi.Visualization.Data;
    using Microsoft.Psi.Visualization.Datasets;
    using Microsoft.Psi.Visualization.Navigation;
    using Microsoft.Psi.Visualization.Server;
    using Newtonsoft.Json;

    /// <summary>
    /// Represents a stream visualization object.
    /// </summary>
    /// <typeparam name="TData">The type of the stream.</typeparam>
    /// <typeparam name="TConfig">The configuration of the visualizer.</typeparam>
    [DataContract(Namespace = "http://www.microsoft.com/psi")]
    [ClassInterface(ClassInterfaceType.None)]
    [Guid(Guids.RemoteStreamVisualizationObjectCLSIDString)]
    [ComVisible(false)]
    public abstract class StreamVisualizationObject<TData, TConfig> : VisualizationObject<TConfig>, IStreamVisualizationObject, IRemoteStreamVisualizationObject
        where TConfig : StreamVisualizationObjectConfiguration, new()
    {
        /// <summary>
        /// Flag inidcating whether type paramamter T is Shared{} or not.
        /// </summary>
        private readonly bool isShared = typeof(TData).IsGenericType && typeof(TData).GetGenericTypeDefinition() == typeof(Shared<>);

        /// <summary>
        /// The current (based on navigation cursor) value of the stream.
        /// </summary>
        private Message<TData>? currentValue;

        /// <summary>
        /// Gets or sets the epsilon around the cursor for which we show the instant visualization
        /// </summary>
        private RelativeTimeInterval cursorEpsilon;

        /// <summary>
        /// The data read from the stream.
        /// </summary>
        private ObservableKeyedCache<DateTime, Message<TData>>.ObservableKeyedView data;

        /// <summary>
        /// Gets or sets the current value.
        /// </summary>
        [Browsable(false)]
        [IgnoreDataMember]
        public Message<TData>? CurrentValue
        {
            get => this.currentValue;
            protected set
            {
                if (this.currentValue != value)
                {
                    this.RaisePropertyChanging(nameof(this.CurrentValue));

                    if (this.isShared)
                    {
                        value.DeepClone(ref this.currentValue);
                    }
                    else
                    {
                        this.currentValue = value;
                    }

                    this.RaisePropertyChanged(nameof(this.CurrentValue));
                }
            }
        }

        /// <summary>
        /// Gets the cursor epsilon.
        /// </summary>
        [Browsable(false)]
        [IgnoreDataMember]
        public RelativeTimeInterval CursorEpsilon
        {
            get => this.cursorEpsilon;
            internal set
            {
                this.Set(nameof(this.CursorEpsilon), ref this.cursorEpsilon, value);
                if (this.Navigator != null)
                {
                    this.SetCurrentValue(this.Navigator.Cursor); // force a reevaluation of the current value whenever cursor epsilon changes
                }
            }
        }

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
                    var oldValue = this.data;
                    this.Set(nameof(this.Data), ref this.data, value);
                    this.OnDataChanged(oldValue, this.data);
                }
            }
        }

        /// <summary>
        /// Gets how to interpolate values at times that are between messages
        /// </summary>
        protected virtual InterpolationStyle InterpolationStyle
        {
            get
            {
                return InterpolationStyle.Direct;
            }
        }

        /// <inheritdoc />
        public void CloseStream()
        {
            if (this.Configuration.StreamBinding != null)
            {
                this.OnCloseStream();
                this.Configuration.StreamBinding = null;
            }
        }

        /// <summary>
        /// Opens a stream, closing the underlying stream if neccesary.
        /// </summary>
        /// <param name="streamBinding">The stream to open and visualize.</param>
        public void OpenStream(StreamBinding streamBinding)
        {
            if (this.Panel == null)
            {
                throw new ApplicationException("You must set the parent on a StreamVisualizationObject before calling OpenStream.");
            }

            this.CloseStream();
            this.Configuration.StreamBinding = streamBinding;
            this.OnOpenStream();
        }

        /// <inheritdoc />
        void IRemoteStreamVisualizationObject.OpenStream(string jsonStreamBinding)
        {
            StreamBinding streamBinding = JsonConvert.DeserializeObject<StreamBinding>(jsonStreamBinding);
            this.OpenStream(streamBinding);
        }

        /// <inheritdoc />
        public void UpdateStoreBindings(IEnumerable<PartitionViewModel> partitions)
        {
            PartitionViewModel partition = partitions.FirstOrDefault(p => p.Name == this.Configuration.StreamBinding.PartitionName);
            if (partition != null)
            {
                var streamBinding = new StreamBinding(this.Configuration.StreamBinding, partition.StoreName, partition.StorePath);
                this.OpenStream(streamBinding);
            }
        }

        /// <summary>
        /// Finds the index that is either exactly at currentTime, or closest to currentTime +- the CursorEpsilon.
        /// Uses binary search to find exact match or location where match should be.
        /// </summary>
        /// <param name="currentTime">Time to search for.</param>
        /// <param name="count">Number of entries to search within.</param>
        /// <param name="timeAtIndex">Function that returns and index given a time.</param>
        /// <returns>Best matching index or -1 if no qualifying match was found.</returns>
        protected int GetIndexForTime(DateTime currentTime, int count, Func<int, DateTime> timeAtIndex)
        {
            if (count == 0)
            {
                return -1;
            }

            // do a binary search and return if exact match
            int lo = 0;
            int hi = count - 1;
            while ((lo != hi - 1) && (lo != hi))
            {
                var val = (lo + hi) / 2;
                if (timeAtIndex(val) < currentTime)
                {
                    lo = val;
                }
                else if (timeAtIndex(val) > currentTime)
                {
                    hi = val;
                }
                else
                {
                    return val;
                }
            }

            // if no exact match, lo and hi indicate ticks that
            // are right before and right after the time we're looking for.
            // If we're using Step interpolation, then we should return
            // lo, otherwise we should return whichever value is closest
            if (this.InterpolationStyle == InterpolationStyle.Step)
            {
                return lo;
            }

            var interval = currentTime + this.CursorEpsilon;
            if (lo == hi - 1)
            {
                // if the "hi" tick is closer
                if ((timeAtIndex(hi) - currentTime) < (currentTime - timeAtIndex(lo)))
                {
                    if (interval.PointIsWithin(timeAtIndex(hi)))
                    {
                        return hi;
                    }
                    else if (interval.PointIsWithin(timeAtIndex(lo)))
                    {
                        return lo;
                    }
                    else
                    {
                        return -1;
                    }
                }

                // if the lo tick is closer
                else
                {
                    if (interval.PointIsWithin(timeAtIndex(lo)))
                    {
                        return lo;
                    }
                    else if (interval.PointIsWithin(timeAtIndex(hi)))
                    {
                        return hi;
                    }
                    else
                    {
                        return -1;
                    }
                }
            }
            else
            {
                if (interval.PointIsWithin(timeAtIndex(lo)))
                {
                    return lo;
                }
                else
                {
                    return -1;
                }
            }
        }

        /// <inheritdoc />
        protected override void InitNew()
        {
            base.InitNew();
            this.CursorEpsilon = new RelativeTimeInterval(-TimeSpan.FromMilliseconds(this.Configuration.CursorEpsilonMs), TimeSpan.FromMilliseconds(this.Configuration.CursorEpsilonMs));
        }

        /// <summary>
        /// Called when underlying stream is closed.
        /// </summary>
        protected virtual void OnCloseStream()
        {
            this.Data = null;
        }

        /// <inheritdoc />
        protected override void OnConfigurationChanged()
        {
            base.OnConfigurationChanged();
            this.OnConfigurationPropertyChanged(nameof(StreamVisualizationObjectConfiguration.CursorEpsilonMs));
        }

        /// <inheritdoc />
        protected override void OnConfigurationPropertyChanged(string propertyName)
        {
            base.OnConfigurationPropertyChanged(propertyName);
            if (propertyName == nameof(StreamVisualizationObjectConfiguration.CursorEpsilonMs))
            {
                this.CursorEpsilon = new RelativeTimeInterval(-TimeSpan.FromMilliseconds(this.Configuration.CursorEpsilonMs), TimeSpan.FromMilliseconds(this.Configuration.CursorEpsilonMs));
            }
        }

        /// <inheritdoc />
        protected override void OnCursorChanged(object sender, NavigatorTimeChangedEventArgs e)
        {
            this.SetCurrentValue(e.NewTime);
        }

        /// <summary>
        /// Called when data collection property has changed.
        /// </summary>
        /// <param name="oldValue">Old data collection.</param>
        /// <param name="newValue">New data collection.</param>
        protected virtual void OnDataChanged(ObservableKeyedCache<DateTime, Message<TData>>.ObservableKeyedView oldValue, ObservableKeyedCache<DateTime, Message<TData>>.ObservableKeyedView newValue)
        {
            if (oldValue != null)
            {
                oldValue.DetailedCollectionChanged -= this.OnDataDetailedCollectionChanged;
            }

            if (newValue != null)
            {
                newValue.DetailedCollectionChanged += this.OnDataDetailedCollectionChanged;
                this.OnDataCollectionChanged(new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Reset));
            }
            else
            {
                this.CurrentValue = null;
            }
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

            if (this.Navigator.NavigationMode == NavigationMode.Live)
            {
                var last = this.Data.LastOrDefault();
                if (last != default(Message<TData>))
                {
                    this.CurrentValue = last;
                    this.Navigator.UpdateLiveExtents(last.OriginatingTime);
                }
            }
        }

        /// <inheritdoc />
        protected override void OnDisconnect()
        {
            this.CloseStream();
            base.OnDisconnect();
        }

        /// <summary>
        /// Called when underlying stream is opened.
        /// </summary>
        protected virtual void OnOpenStream()
        {
        }

        /// <summary>
        /// Set the current value to the value at the the indicated time.
        /// </summary>
        /// <param name="currentTime">Time to set value with.</param>
        protected virtual void SetCurrentValue(DateTime currentTime)
        {
            this.CurrentValue = null;
        }

        private void OnDataDetailedCollectionChanged(object sender, NotifyCollectionChangedEventArgs e)
        {
            this.OnDataCollectionChanged(e);
        }
    }
}

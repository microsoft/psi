// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.

namespace Microsoft.Psi.Visualization.VisualizationObjects
{
    using System;
    using System.Collections.Specialized;
    using System.ComponentModel;
    using System.Linq;
    using System.Runtime.Serialization;
    using GalaSoft.MvvmLight.Command;
    using Microsoft.Psi;
    using Microsoft.Psi.Data;
    using Microsoft.Psi.Visualization.Collections;
    using Microsoft.Psi.Visualization.Common;
    using Microsoft.Psi.Visualization.Config;
    using Microsoft.Psi.Visualization.Data;
    using Microsoft.Psi.Visualization.Navigation;

    /// <summary>
    /// Represents a stream visualization object.
    /// </summary>
    /// <typeparam name="TData">The type of the stream.</typeparam>
    /// <typeparam name="TConfig">The configuration of the visualizer.</typeparam>
    public abstract class StreamVisualizationObject<TData, TConfig> : VisualizationObject<TConfig>, IStreamVisualizationObject
        where TConfig : StreamVisualizationObjectConfiguration, new()
    {
        /// <summary>
        /// Flag indicating whether type paramamter T is Shared{} or not.
        /// </summary>
        private readonly bool isShared = typeof(TData).IsGenericType && typeof(TData).GetGenericTypeDefinition() == typeof(Shared<>);

        /// <summary>
        /// The current (based on navigation cursor) value of the stream.
        /// </summary>
        private Message<TData>? currentValue;

        /// <summary>
        /// Gets or sets the epsilon around the cursor for which we show the instant visualization.
        /// </summary>
        private RelativeTimeInterval cursorEpsilon;

        /// <summary>
        /// The data read from the stream.
        /// </summary>
        private ObservableKeyedCache<DateTime, Message<TData>>.ObservableKeyedView data;

        /// <summary>
        /// Indicates whether the store that is the source of data for this visualization object is a live store.
        /// </summary>
        private bool isLive = false;

        /// <summary>
        /// The snap to stream command.
        /// </summary>
        private RelayCommand snapToStreamCommand;

        /// <summary>
        /// The zoom to stream command.
        /// </summary>
        private RelayCommand zoomToStreamCommand;

        /// <inheritdoc/>
        [Browsable(false)]
        [IgnoreDataMember]
        public override bool CanSnapToStream => true;

        /// <summary>
        /// Gets the snap to stream command.
        /// </summary>
        [Browsable(false)]
        [IgnoreDataMember]
        public RelayCommand SnapToStreamCommand
        {
            get
            {
                if (this.snapToStreamCommand == null)
                {
                    this.snapToStreamCommand = new RelayCommand(
                        () =>
                        {
                            // If this is already the visualization object being snapped to, then
                            // reset snap to stream, otherwise set it to this visualization object.
                            // If another object was previously snapped, then ask it to unsnap itself
                            // so that the correct property changed event gets raised.
                            if (this.Container.SnapToVisualizationObject == null)
                            {
                                this.SnapToStream(true);
                            }
                            else if (this.Container.SnapToVisualizationObject == this)
                            {
                                this.SnapToStream(false);
                            }
                            else
                            {
                                this.Container.SnapToVisualizationObject.SnapToStream(false);
                                this.SnapToStream(true);
                            }
                        });
                }

                return this.snapToStreamCommand;
            }
        }

        /// <summary>
        /// Gets the zoom to stream command.
        /// </summary>
        [Browsable(false)]
        [IgnoreDataMember]
        public RelayCommand ZoomToStreamCommand
        {
            get
            {
                if (this.zoomToStreamCommand == null)
                {
                    this.zoomToStreamCommand = new RelayCommand(
                        () =>
                        {
                            this.Container.Navigator.Zoom(this.StreamBinding.StreamMetadata.FirstMessageOriginatingTime, this.StreamBinding.StreamMetadata.LastMessageOriginatingTime);
                        });
                }

                return this.zoomToStreamCommand;
            }
        }

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
                        if (value != null)
                        {
                            value.DeepClone(ref this.currentValue);
                        }
                        else
                        {
                            // If the new value is null, we need to ensure that the current value is properly
                            // disposed so that the shared object is released and potentially recycled.
                            ((IDisposable)this.currentValue.Value.Data)?.Dispose();
                            this.currentValue = null;
                        }
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
                        this.CurrentValue = null;
                    }
                }
            }
        }

        /// <inheritdoc/>
        public StreamBinding StreamBinding => this.Configuration.StreamBinding;

        /// <summary>
        /// Gets a value indicating whether the visualization object is currenty bound to a datasource.
        /// </summary>
        [Browsable(true)]
        [IgnoreDataMember]
        public bool IsBound => this.Configuration.StreamBinding.IsBound;

        /// <inheritdoc/>
        [Browsable(false)]
        [IgnoreDataMember]
        public override string IconSource
        {
            get
            {
                if (!this.Configuration.StreamBinding.IsBound)
                {
                    return IconSourcePath.StreamUnbound;
                }
                else if (this.IsSnappedToStream)
                {
                    return this.IsLive ? IconSourcePath.SnapToStreamLive : IconSourcePath.SnapToStream;
                }
                else
                {
                    return this.IsLive ? IconSourcePath.StreamLive : IconSourcePath.Stream;
                }
            }
        }

        /// <inheritdoc/>
        [Browsable(false)]
        [IgnoreDataMember]
        public override bool IsSnappedToStream => this.Container.SnapToVisualizationObject == this;

        /// <summary>
        /// Gets the text to display in the snap to stream menu item.
        /// </summary>
        public string SnapToStreamCommandText => this.IsSnappedToStream ? "Unsnap From Stream" : "Snap To Stream";

        /// <inheritdoc />
        public bool IsLive
        {
            get => this.isLive;

            set
            {
                if (this.isLive != value)
                {
                    this.RaisePropertyChanging(nameof(this.IconSource));
                    this.isLive = value;
                    this.RaisePropertyChanged(nameof(this.IconSource));
                }
            }
        }

        /// <summary>
        /// Gets how to interpolate values at times that are between messages.
        /// </summary>
        protected virtual InterpolationStyle InterpolationStyle
        {
            get
            {
                return InterpolationStyle.Direct;
            }
        }

        /// <inheritdoc/>
        [Browsable(false)]
        public override void SnapToStream(bool snapToStream)
        {
            this.RaisePropertyChanging(nameof(this.IsSnappedToStream));
            this.RaisePropertyChanging(nameof(this.IconSource));
            this.RaisePropertyChanging(nameof(this.SnapToStreamCommandText));
            this.Container.SnapToVisualizationObject = snapToStream ? this : null;
            this.RaisePropertyChanged(nameof(this.IsSnappedToStream));
            this.RaisePropertyChanged(nameof(this.IconSource));
            this.RaisePropertyChanged(nameof(this.SnapToStreamCommandText));
        }

        /// <inheritdoc />
        public void UpdateStreamBinding(Session session)
        {
            // Attempt to rebind to the underlying dataset
            StreamBindingResult bindingResult = this.Configuration.StreamBinding.Update(session);

            // If nothing's changed, then we're done
            if (bindingResult == StreamBindingResult.BindingUnchanged)
            {
                return;
            }

            this.RaisePropertyChanging(nameof(this.IconSource));
            this.RaisePropertyChanging(nameof(this.CanSnapToStream));

            // Notify that we're no longer bound to the previous data source
            this.OnStreamUnbound();

            // Notify that we're now bound to a new data source
            if (bindingResult == StreamBindingResult.BoundToNewSource)
            {
                this.OnStreamBound();
            }

            this.RaisePropertyChanged(nameof(this.IconSource));
            this.RaisePropertyChanged(nameof(this.CanSnapToStream));
        }

        /// <inheritdoc/>
        public virtual DateTime? GetSnappedTime(DateTime time)
        {
            return this.GetTimeOfNearestMessage(time, this.Data?.Count ?? 0, (idx) => this.Data[idx].OriginatingTime);
        }

        /// <summary>
        /// Returns the timestamp of the Message that's closest to currentTime.  Used by the "Snap To Stream" functionality.
        /// </summary>
        /// <param name="currentTime">The time underneath the mouse cursor.</param>
        /// <param name="count">Number of entries to search within.</param>
        /// <param name="timeAtIndex">Function that returns an index given a time.</param>
        /// <returns>The timestamp of the message that's temporally closest to currentTime.</returns>
        public DateTime? GetTimeOfNearestMessage(DateTime currentTime, int count, Func<int, DateTime> timeAtIndex)
        {
            int index = this.GetIndexForTime(currentTime, count, timeAtIndex, InterpolationStyle.Direct);
            if (index >= 0)
            {
                return timeAtIndex(index);
            }

            return null;
        }

        /// <summary>
        /// Finds the index that is either exactly at currentTime, or closest to currentTime +- the CursorEpsilon.
        /// Uses binary search to find exact match or location where match should be.
        /// </summary>
        /// <param name="currentTime">Time to search for.</param>
        /// <param name="count">Number of entries to search within.</param>
        /// <param name="timeAtIndex">Function that returns an index given a time.</param>
        /// <returns>Best matching index or -1 if no qualifying match was found.</returns>
        protected int GetIndexForTime(DateTime currentTime, int count, Func<int, DateTime> timeAtIndex)
        {
            return this.GetIndexForTime(currentTime, count, timeAtIndex, this.InterpolationStyle);
        }

        /// <inheritdoc/>
        protected override void OnConfigurationChanged()
        {
            this.CursorEpsilon = new RelativeTimeInterval(-TimeSpan.FromMilliseconds(this.Configuration.CursorEpsilonMs), TimeSpan.FromMilliseconds(this.Configuration.CursorEpsilonMs));
            base.OnConfigurationChanged();
        }

        /// <inheritdoc />
        protected override void OnConfigurationPropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName == nameof(StreamVisualizationObjectConfiguration.CursorEpsilonMs))
            {
                this.CursorEpsilon = new RelativeTimeInterval(-TimeSpan.FromMilliseconds(this.Configuration.CursorEpsilonMs), TimeSpan.FromMilliseconds(this.Configuration.CursorEpsilonMs));
            }

            base.OnConfigurationPropertyChanged(sender, e);
        }

        /// <inheritdoc />
        protected override void OnCursorChanged(object sender, NavigatorTimeChangedEventArgs e)
        {
            this.SetCurrentValue(e.NewTime);
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
                if (last != default(Message<TData>))
                {
                    this.CurrentValue = last;
                }
            }
        }

        /// <inheritdoc />
        protected override void OnRemoveFromPanel()
        {
            this.OnStreamUnbound();
            base.OnRemoveFromPanel();
        }

        /// <summary>
        /// Called when the stream visualization object is bound to a data source.
        /// </summary>
        protected virtual void OnStreamBound()
        {
        }

        /// <summary>
        /// Called when the stream visualization object is unbound from a data source.
        /// </summary>
        protected virtual void OnStreamUnbound()
        {
            this.Data = null;
        }

        /// <summary>
        /// Set the current value to the value at the the indicated time.
        /// </summary>
        /// <param name="currentTime">Time to set value with.</param>
        protected virtual void SetCurrentValue(DateTime currentTime)
        {
            this.CurrentValue = null;
        }

        private int GetIndexForTime(DateTime currentTime, int count, Func<int, DateTime> timeAtIndex, InterpolationStyle interpolationStyle)
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
            if (interpolationStyle == InterpolationStyle.Step)
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

        private void OnDataDetailedCollectionChanged(object sender, NotifyCollectionChangedEventArgs e)
        {
            this.OnDataCollectionChanged(e);
        }
    }
}

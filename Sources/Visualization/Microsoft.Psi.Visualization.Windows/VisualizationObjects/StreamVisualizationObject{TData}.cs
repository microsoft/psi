// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.

namespace Microsoft.Psi.Visualization.VisualizationObjects
{
    using System;
    using System.Collections.Specialized;
    using System.ComponentModel;
    using System.Linq;
    using System.Runtime.Serialization;
    using System.Windows;
    using GalaSoft.MvvmLight.Command;
    using Microsoft.Psi;
    using Microsoft.Psi.PsiStudio.TypeSpec;
    using Microsoft.Psi.Visualization;
    using Microsoft.Psi.Visualization.Collections;
    using Microsoft.Psi.Visualization.Data;
    using Microsoft.Psi.Visualization.Helpers;
    using Microsoft.Psi.Visualization.Navigation;
    using Microsoft.Psi.Visualization.ViewModels;
    using Microsoft.Psi.Visualization.Windows;

    /// <summary>
    /// Represents a stream visualization object.
    /// </summary>
    /// <typeparam name="TData">The type of the stream.</typeparam>
    public abstract class StreamVisualizationObject<TData> : VisualizationObject, IStreamVisualizationObject
    {
        /// <summary>
        /// The stream being visualized.
        /// </summary>
        private StreamBinding streamBinding;

        /// <summary>
        /// The source for the stream data, or null if the visualization object is not currently bound to a source.
        /// </summary>
        private StreamSource streamSource = null;

        /// <summary>
        /// The current (based on navigation cursor) value of the stream.
        /// </summary>
        private Message<TData>? currentValue;

        /// <summary>
        /// The data read from the stream.
        /// </summary>
        private ObservableKeyedCache<DateTime, Message<TData>>.ObservableKeyedView data;

        /// <summary>
        /// The snap to stream command.
        /// </summary>
        private RelayCommand toggleSnapToStreamCommand;

        /// <summary>
        /// The zoom to stream command.
        /// </summary>
        private RelayCommand zoomToStreamCommand;

        /// <summary>
        /// Initializes a new instance of the <see cref="StreamVisualizationObject{TData}"/> class.
        /// </summary>
        public StreamVisualizationObject()
        {
            this.IsShared = typeof(TData).IsGenericType && typeof(TData).GetGenericTypeDefinition() == typeof(Shared<>);
        }

        /// <inheritdoc/>
        [Browsable(false)]
        [IgnoreDataMember]
        public override bool CanSnapToStream => this.IsBound;

        /// <summary>
        /// Gets the snap to stream command.
        /// </summary>
        [Browsable(false)]
        [IgnoreDataMember]
        public RelayCommand ToggleSnapToStreamCommand
        {
            get
            {
                if (this.toggleSnapToStreamCommand == null)
                {
                    this.toggleSnapToStreamCommand = new RelayCommand(() => this.ToggleSnapToStream());
                }

                return this.toggleSnapToStreamCommand;
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
                        () => this.Container.Navigator.Zoom(this.StreamSource.StreamMetadata.FirstMessageOriginatingTime, this.StreamSource.StreamMetadata.LastMessageOriginatingTime),
                        () => this.StreamSource != null);
                }

                return this.zoomToStreamCommand;
            }
        }

        /// <summary>
        /// Gets the current value.
        /// </summary>
        [Browsable(false)]
        [IgnoreDataMember]
        public Message<TData>? CurrentValue => this.currentValue;

        /// <summary>
        /// Gets the current data.
        /// </summary>
        [Browsable(false)]
        [IgnoreDataMember]
        public TData CurrentData => this.currentValue.HasValue ? this.currentValue.Value.Data : default;

        /// <summary>
        /// Gets the originating time of the current data.
        /// </summary>
        [Browsable(false)]
        [IgnoreDataMember]
        public DateTime CurrentOriginatingTime => this.currentValue.HasValue ? this.currentValue.Value.OriginatingTime : default;

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
        /// Gets or sets the stream binding.
        /// </summary>
        [Browsable(false)]
        [DataMember]
        public StreamBinding StreamBinding
        {
            get { return this.streamBinding; }
            set { this.Set(nameof(this.StreamBinding), ref this.streamBinding, value); }
        }

        /// <inheritdoc/>
        [Browsable(false)]
        [IgnoreDataMember]
        public StreamSource StreamSource
        {
            get => this.streamSource;

            private set
            {
                this.RaisePropertyChanging(nameof(this.StreamSource));
                this.RaisePropertyChanging(nameof(this.IconSource));
                this.streamSource = value;
                this.RaisePropertyChanged(nameof(this.StreamSource));
                this.RaisePropertyChanged(nameof(this.IconSource));
            }
        }

        /// <summary>
        /// Gets the adapter type.
        /// </summary>
        [Browsable(false)]
        [IgnoreDataMember]
        public Type StreamAdapterType => this.StreamBinding?.StreamAdapterType;

        /// <summary>
        /// Gets the adapter type name (used by property browser).
        /// </summary>
        [Browsable(true)]
        [DisplayName("Stream Adapter")]
        [Description("The stream adapter used by the visualizer.")]
        [IgnoreDataMember]
        public string StreamAdapterDisplayName => TypeSpec.Simplify(this.StreamBinding?.StreamAdapterType?.FullName);

        /// <summary>
        /// Gets the summarizer type.
        /// </summary>
        [Browsable(false)]
        [IgnoreDataMember]
        public Type SummarizerType => this.StreamBinding?.SummarizerType;

        /// <summary>
        /// Gets the summarizer type name (used by property browser).
        /// </summary>
        [Browsable(true)]
        [DisplayName("Summarizer")]
        [IgnoreDataMember]
        public string SummarizerTypeDisplayName => TypeSpec.Simplify(this.StreamBinding?.SummarizerType?.FullName);

        /// <inheritdoc/>
        [Browsable(false)]
        [IgnoreDataMember]
        public virtual bool RequiresSupplementalMetadata => false;

        /// <summary>
        /// Gets a value indicating whether the visualization object is currently bound to a datasource.
        /// </summary>
        [Browsable(false)]
        [IgnoreDataMember]
        public bool IsBound => this.StreamSource != null;

        /// <inheritdoc/>
        public override bool ShowZoomToStreamMenuItem => this.IsBound;

        /// <inheritdoc/>
        [Browsable(false)]
        [IgnoreDataMember]
        public TimeInterval StreamExtents => this.IsBound ? new TimeInterval(this.StreamSource.StreamMetadata.FirstMessageOriginatingTime, this.StreamSource.StreamMetadata.LastMessageOriginatingTime) : TimeInterval.Empty;

        /// <inheritdoc/>
        [Browsable(false)]
        [IgnoreDataMember]
        public override string IconSource
        {
            get
            {
                if (!this.IsBound)
                {
                    if (this.StreamBinding.IsStreamMemberBinding)
                    {
                        // Stream member unbound
                        return IconSourcePath.StreamMemberUnbound;
                    }
                    else
                    {
                        // Stream unbound
                        return IconSourcePath.StreamUnbound;
                    }
                }
                else if (this.IsSnappedToStream)
                {
                    if (this.StreamBinding.IsStreamMemberBinding)
                    {
                        // Snap to stream member
                        return this.IsLive ? IconSourcePath.StreamMemberSnapLive : IconSourcePath.StreamMemberSnap;
                    }
                    else
                    {
                        // Snap to stream
                        return this.IsLive ? IconSourcePath.SnapToStreamLive : IconSourcePath.SnapToStream;
                    }
                }
                else if (this.StreamBinding.IsStreamMemberBinding)
                {
                    // Stream member
                    return this.IsLive ? IconSourcePath.StreamMemberLive : IconSourcePath.StreamMember;
                }
                else
                {
                    // Stream
                    return this.IsLive ? IconSourcePath.StreamLive : IconSourcePath.Stream;
                }
            }
        }

        /// <inheritdoc/>
        [Browsable(false)]
        [IgnoreDataMember]
        public override bool IsSnappedToStream => this.Container != null ? this.Container.SnapToVisualizationObject == this : false;

        /// <summary>
        /// Gets the text to display in the snap to stream menu item.
        /// </summary>
        [Browsable(false)]
        [IgnoreDataMember]
        public string ToggleSnapToStreamCommandText => this.IsSnappedToStream ? "Unsnap From Stream" : "Snap To Stream";

        /// <inheritdoc />
        [Browsable(false)]
        [IgnoreDataMember]
        public bool IsLive => this.StreamSource != null && this.StreamSource.IsLive;

        /// <summary>
        /// Gets a value indicating whether type parameter T is Shared{} or not.
        /// </summary>
        protected bool IsShared { get; private set; }

        /// <summary>
        /// Sets the current value for the visualization object.
        /// </summary>
        /// <param name="value">The value to use as the new current value.</param>
        /// <param name="incrementSharedRefCount">True if the reference count of shared data should be automatically incremented, otherwise false.</param>
        public void SetCurrentValue(Message<TData>? value, bool incrementSharedRefCount = true)
        {
            if (this.currentValue != value)
            {
                this.RaisePropertyChanging(nameof(this.CurrentValue));
                this.RaisePropertyChanging(nameof(this.CurrentData));
                this.RaisePropertyChanging(nameof(this.CurrentOriginatingTime));

                // If we're handling shared objects, decrement the reference count of the current value
                if (this.IsShared && this.currentValue.HasValue)
                {
                    ((IDisposable)this.currentValue.Value.Data)?.Dispose();
                }

                // If we're handling shared objects, increment the reference count if requested.
                // NOTE: InstantVisualzationObject will pass false for incrementSharedRefCount
                // because it already increased the reference count itself before calling us.
                if (this.IsShared && incrementSharedRefCount)
                {
                    this.currentValue = value.DeepClone();
                }
                else
                {
                    this.currentValue = value;
                }

                this.RaisePropertyChanged(nameof(this.CurrentValue));
                this.RaisePropertyChanged(nameof(this.CurrentData));
                this.RaisePropertyChanged(nameof(this.CurrentOriginatingTime));
            }
        }

        /// <inheritdoc/>
        public override void ToggleSnapToStream()
        {
            this.RaisePropertyChanging(nameof(this.IsSnappedToStream));
            this.RaisePropertyChanging(nameof(this.IconSource));
            this.RaisePropertyChanging(nameof(this.ToggleSnapToStreamCommandText));

            // If this is already the visualization object being snapped to, then
            // reset snap to stream, otherwise set it to this visualization object.
            // If another object was previously snapped, then ask it to unsnap itself
            // so that the correct property changed event gets raised.
            if (this.Container.SnapToVisualizationObject == null)
            {
                this.Container.SnapToVisualizationObject = this;
            }
            else if (this.Container.SnapToVisualizationObject == this)
            {
                this.Container.SnapToVisualizationObject = null;
            }
            else
            {
                this.Container.SnapToVisualizationObject.ToggleSnapToStream();
                this.Container.SnapToVisualizationObject = this;
            }

            this.RaisePropertyChanged(nameof(this.IsSnappedToStream));
            this.RaisePropertyChanged(nameof(this.IconSource));
            this.RaisePropertyChanged(nameof(this.ToggleSnapToStreamCommandText));
        }

        /// <inheritdoc />
        public void UpdateStreamSource(SessionViewModel activeSession)
        {
            // Attempt to rebind to a source
            StreamSource newStreamSource = activeSession != null ? activeSession.GetStreamSource(this.streamBinding) : null;

            // If we're still bound to the same source, then we're done.
            if (this.StreamSource == newStreamSource)
            {
                return;
            }

            // Unhook event handlers from current stream source
            if (this.StreamSource != null)
            {
                this.StreamSource.PropertyChanging -= this.StreamSource_PropertyChanging;
                this.StreamSource.PropertyChanged -= this.StreamSource_PropertyChanged;
            }

            // Hook event handlers to new stream source
            if (newStreamSource != null)
            {
                newStreamSource.PropertyChanging += this.StreamSource_PropertyChanging;
                newStreamSource.PropertyChanged += this.StreamSource_PropertyChanged;
            }

            this.RaisePropertyChanging(nameof(this.IconSource));
            this.RaisePropertyChanging(nameof(this.CanSnapToStream));

            // Notify if we're becoming unbound from a stream source
            if (this.StreamSource != null)
            {
                this.StreamSource = null;
                this.OnStreamUnbound();
            }

            // Notify if we're now bound to a new stream source
            if (newStreamSource != null)
            {
                this.StreamSource = newStreamSource;
                this.OnStreamBound();
            }

            this.RaisePropertyChanged(nameof(this.IconSource));
            this.RaisePropertyChanged(nameof(this.CanSnapToStream));
        }

        /// <inheritdoc/>
        public virtual DateTime? GetSnappedTime(DateTime time, SnappingBehavior snappingBehavior = SnappingBehavior.Nearest)
        {
            // TODO
            return this.GetTimeOfNearestMessage(time, this.Data?.Count ?? 0, (idx) => this.Data[idx].OriginatingTime, snappingBehavior);
        }

        /// <summary>
        /// Returns the timestamp of the Message that's closest to currentTime.  Used by the "Snap To Stream" functionality.
        /// </summary>
        /// <param name="currentTime">The time underneath the mouse cursor.</param>
        /// <param name="count">Number of entries to search within.</param>
        /// <param name="timeAtIndex">Function that returns an index given a time.</param>
        /// <param name="snappingBehavior">Timeline snapping behavior.</param>
        /// <returns>The timestamp of the message that's temporally closest to currentTime.</returns>
        protected DateTime? GetTimeOfNearestMessage(DateTime currentTime, int count, Func<int, DateTime> timeAtIndex, SnappingBehavior snappingBehavior)
        {
            int index = IndexHelper.GetIndexForTime(currentTime, count, timeAtIndex, snappingBehavior);
            if (index >= 0)
            {
                return timeAtIndex(index);
            }

            return null;
        }

        /// <summary>
        /// Finds the index that is either exactly at currentTime, or closest to currentTime.
        /// Uses binary search to find exact match or location where match should be.
        /// </summary>
        /// <param name="currentTime">Time to search for.</param>
        /// <param name="count">Number of entries to search within.</param>
        /// <param name="timeAtIndex">Function that returns an index given a time.</param>
        /// <returns>Best matching index or -1 if no qualifying match was found.</returns>
        protected virtual int GetIndexForTime(DateTime currentTime, int count, Func<int, DateTime> timeAtIndex)
        {
            return IndexHelper.GetIndexForTime(currentTime, count, timeAtIndex);
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
                    this.SetCurrentValue(last);
                }
            }
        }

        /// <inheritdoc/>
        protected override void OnAddToPanel()
        {
            // Listen for stream read errors
            DataManager.Instance.StreamReadError += this.OnStreamReadError;

            base.OnAddToPanel();
        }

        /// <inheritdoc />
        protected override void OnRemoveFromPanel()
        {
            // Stop listening for stream read errors
            DataManager.Instance.StreamReadError -= this.OnStreamReadError;

            // Unbind the visualization object from any source
            this.UpdateStreamSource(null);

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

            // If this is the stream currently being snapped to, disable snap to stream.
            if (this.IsSnappedToStream)
            {
                this.ToggleSnapToStream();
            }
        }

        private void OnDataDetailedCollectionChanged(object sender, NotifyCollectionChangedEventArgs e)
        {
            this.OnDataCollectionChanged(e);
        }

        private void StreamSource_PropertyChanging(object sender, PropertyChangingEventArgs e)
        {
            if (e.PropertyName == nameof(this.StreamSource.IsLive))
            {
                this.RaisePropertyChanging(nameof(this.IconSource));
            }
        }

        private void StreamSource_PropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName == nameof(this.StreamSource.IsLive))
            {
                this.RaisePropertyChanged(nameof(this.IconSource));
            }
        }

        private void OnStreamReadError(object sender, StreamReadErrorEventArgs e)
        {
            // Check if the error is related to the stream that this visualization object currently references
            if ((this.StreamSource != null) &&
                (this.StreamSource.StoreName == e.StoreName) &&
                (this.StreamSource.StorePath == e.StorePath) &&
                (this.StreamSource.StreamName == e.StreamName))
            {
                Application.Current.Dispatcher.BeginInvoke((Action)(() =>
                {
                    // Mark this visualization object as unbound since we can't read any messages.
                    this.UpdateStreamSource(null);

                    // Display an error message to the user.
                    string errorMessage = $"The format of the messages in the stream {e.StreamName} in store {e.StoreName} have changed and are unable to be deserialized, see error below:{Environment.NewLine}{Environment.NewLine}{e.Exception.Message}";
                    new MessageBoxWindow(Application.Current.MainWindow, "Stream Type Mismatch", errorMessage, "Close", null).ShowDialog();
                }));
            }
        }
    }
}

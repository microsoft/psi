// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.

namespace Microsoft.Psi.Visualization.Navigation
{
    using System;
    using System.Runtime.InteropServices;
    using System.Runtime.Serialization;
    using System.Windows.Threading;
    using Microsoft.Psi.Visualization.Server;

    /// <summary>
    /// Class implements the time Navigator view model
    /// </summary>
    [DataContract(Namespace = "http://www.microsoft.com/psi")]
    [ClassInterface(ClassInterfaceType.None)]
    [Guid(Guids.RemoteNavigatorCLSIDString)]
    [ComVisible(false)]
    public partial class Navigator : ReferenceCountedObject, IRemoteNavigator
    {
        private DateTime cursor;
        private NavigatorRange dataRange;
        private NavigationMode navigationMode;
        private NavigatorRange selectionRange;
        private NavigatorRange viewRange;

        /// <summary>
        /// The padding (in percentage) when performing a zoom to selection. The resulting view
        /// will be larger than the selection by this percentage
        /// </summary>
        private double zoomToSelectionPadding;

        // The various Timing displays
        private bool showAbsoluteTiming = false;
        private bool showTimingRelativeToSessionStart = false;
        private bool showTimingRelativeToSelectionStart = false;

        private DispatcherTimer playTimer;

        /// <summary>
        /// Initializes a new instance of the <see cref="Navigator"/> class.
        /// </summary>
        public Navigator()
        {
            this.selectionRange = new NavigatorRange();
            this.dataRange = new NavigatorRange();
            this.viewRange = new NavigatorRange();
            this.cursor = DateTime.MinValue;
            this.navigationMode = NavigationMode.Playback;
            this.zoomToSelectionPadding = 0.1;

            this.dataRange.SetRange(DateTime.MinValue, TimeSpan.FromSeconds(60));

            this.SelectionRange.RangeChanged += this.SelectionRange_RangeChanged;
        }

        /// <summary>
        /// Occurs when the navigation mode changes.
        /// </summary>
        public event NavigatorModeChangedHandler NavigationModeChanged;

        /// <summary>
        /// Occurs when the cursor changes.
        /// </summary>
        public event NavigatorTimeChangedHandler CursorChanged;

        /// <inheritdoc />
        [DataMember]
        public DateTime Cursor
        {
            get => this.cursor;
            set
            {
                if (this.cursor != value)
                {
                    this.RaisePropertyChanging(nameof(this.Cursor));
                    this.RaisePropertyChanging(nameof(this.CursorRelativeToSessionStartFormatted));
                    this.RaisePropertyChanging(nameof(this.CursorRelativeToSelectionStartFormatted));
                    var original = this.cursor;
                    this.cursor = value;
                    this.CursorChanged?.Invoke(this, new NavigatorTimeChangedEventArgs(original, value));
                    this.RaisePropertyChanged(nameof(this.Cursor));
                    this.RaisePropertyChanged(nameof(this.CursorRelativeToSessionStartFormatted));
                    this.RaisePropertyChanged(nameof(this.CursorRelativeToSelectionStartFormatted));
                }
            }
        }

        /// <summary>
        /// Gets the cursor position relative to the Session Start time
        /// </summary>
        public string CursorRelativeToSessionStartFormatted => this.FormatTimespan(this.Cursor - this.DataRange.StartTime);

        /// <summary>
        /// Gets the cursor position relative to the Selection Start time
        /// </summary>
        public string CursorRelativeToSelectionStartFormatted => this.FormatTimespan(this.Cursor - this.SelectionRange.StartTime);

        /// <summary>
        /// Gets the data range.
        /// </summary>
        [DataMember]
        public NavigatorRange DataRange => this.dataRange;

        /// <summary>
        /// Gets the Session End time relative to the Selection Start time
        /// </summary>
        public string SessionEndRelativeToSelectionStartFormatted => this.FormatTimespan(this.DataRange.EndTime - this.SelectionRange.StartTime);

        /// <summary>
        /// Gets a value indicating whether the navigator has a finite range.
        /// </summary>
        public bool HasFiniteRange => this.selectionRange.IsFinite && this.viewRange.IsFinite && this.dataRange.IsFinite;

        /// <summary>
        /// Gets or sets the navigation mode.
        /// </summary>
        [DataMember]
        public NavigationMode NavigationMode
        {
            get => this.navigationMode;
            set
            {
                if (this.navigationMode != value)
                {
                    this.RaisePropertyChanging(nameof(this.NavigationMode));
                    var original = this.navigationMode;
                    this.navigationMode = value;
                    this.NavigationModeChanged?.Invoke(this, new NavigatorModeChangedEventArgs(original, value));
                    this.RaisePropertyChanged(nameof(this.NavigationMode));
                }
            }
        }

        /// <summary>
        /// Gets the selection range.
        /// </summary>
        [DataMember]
        public NavigatorRange SelectionRange => this.selectionRange;

        /// <summary>
        /// Gets the offset of the Section Start marker from the start of the Session
        /// </summary>
        public string SelectionStartRelativeToSessionStartFormatted => this.FormatTimespan(this.SelectionRange.StartTime - this.DataRange.StartTime);

        /// <summary>
        /// Gets the offset of the Section End marker from the start of the Session
        /// </summary>
        public string SelectionEndRelativeToSessionStartFormatted => this.FormatTimespan(this.SelectionRange.EndTime - this.DataRange.StartTime);

        /// <summary>
        /// Gets the view range.
        /// </summary>
        [DataMember]
        public NavigatorRange ViewRange => this.viewRange;

        /// <inheritdoc />
        [DataMember]
        public double ZoomToSelectionPadding
        {
            get => this.zoomToSelectionPadding;
            set => this.Set(nameof(this.ZoomToSelectionPadding), ref this.zoomToSelectionPadding, value);
        }

        /// <inheritdoc />
        IRemoteNavigatorRange IRemoteNavigator.DataRange => this.DataRange;

        /// <inheritdoc />
        IRemoteNavigatorRange IRemoteNavigator.SelectionRange => this.SelectionRange;

        /// <inheritdoc />
        IRemoteNavigatorRange IRemoteNavigator.ViewRange => this.ViewRange;

        /// <inheritdoc />
        RemoteNavigationMode IRemoteNavigator.NavigationMode
        {
            get => (this.NavigationMode == NavigationMode.Live) ? RemoteNavigationMode.Live : RemoteNavigationMode.Playback;
            set => this.NavigationMode = (value == RemoteNavigationMode.Live) ? NavigationMode.Live : NavigationMode.Playback;
        }

        /// <summary>
        /// Gets a value indicating whether the Timing Header is visible
        /// </summary>
        public bool ShowTimingHeader => this.ShowAbsoluteTiming || this.ShowTimingRelativeToSessionStart || this.ShowTimingRelativeToSelectionStart;

        /// <summary>
        /// Gets or sets a value indicating whether Absolute Timing is displayed
        /// </summary>
        public bool ShowAbsoluteTiming
        {
            get => this.showAbsoluteTiming;

            set
            {
                this.RaisePropertyChanging(nameof(this.ShowTimingHeader));
                this.Set(nameof(this.ShowAbsoluteTiming), ref this.showAbsoluteTiming, value);
                this.RaisePropertyChanged(nameof(this.ShowTimingHeader));
            }
        }

        /// <summary>
        /// Gets or sets a value indicating whether Timing Relative to the Session Start is displayed
        /// </summary>
        public bool ShowTimingRelativeToSessionStart
        {
            get => this.showTimingRelativeToSessionStart;

            set
            {
                this.RaisePropertyChanging(nameof(this.ShowTimingHeader));
                this.Set(nameof(this.ShowTimingRelativeToSessionStart), ref this.showTimingRelativeToSessionStart, value);
                this.RaisePropertyChanged(nameof(this.ShowTimingHeader));
            }
        }

        /// <summary>
        /// Gets or sets a value indicating whether Timing Relative to the Selection Start is displayed
        /// </summary>
        public bool ShowTimingRelativeToSelectionStart
        {
            get => this.showTimingRelativeToSelectionStart;

            set
            {
                this.RaisePropertyChanging(nameof(this.ShowTimingHeader));
                this.Set(nameof(this.ShowTimingRelativeToSelectionStart), ref this.showTimingRelativeToSelectionStart, value);
                this.RaisePropertyChanged(nameof(this.ShowTimingHeader));
            }
        }

        /// <summary>
        /// Animates the navigator curor based on indicated speed.
        /// </summary>
        /// <param name="speed">The speed to animate the cursor. Default is 1.0.</param>
        public void Play(double speed = 1.0)
        {
            if (this.NavigationMode != NavigationMode.Playback)
            {
                throw new NotSupportedException("Play is only supported in Playback mode.");
            }

            if (this.playTimer != null)
            {
                return;
            }

            var startTime = this.selectionRange.StartTime;
            var playStartTime = DateTime.Now;
            var newCursor = this.selectionRange.StartTime;
            this.playTimer = new DispatcherTimer(
                TimeSpan.FromMilliseconds(10),
                DispatcherPriority.Background,
                (s, e) =>
                {
                    if (newCursor < this.selectionRange.EndTime)
                    {
                        newCursor = startTime + TimeSpan.FromTicks((long)((DateTime.Now - playStartTime).Ticks * speed));
                        this.Cursor = newCursor;
                        if (newCursor > this.viewRange.EndTime)
                        {
                            this.viewRange.SetRange(newCursor, this.viewRange.Duration);
                        }
                    }
                    else
                    {
                        this.StopPlaying();
                    }
                },
                Dispatcher.CurrentDispatcher);
            this.playTimer.Start();
        }

        /// <summary>
        /// Stop annimating the navigator cursor.
        /// </summary>
        public void StopPlaying()
        {
            if (this.playTimer != null)
            {
                this.playTimer.Stop();
                this.playTimer = null;
            }
        }

        /// <summary>
        /// Zoom to a particular location
        /// </summary>
        /// <param name="start">The start of the time interval to zoom to.</param>
        /// <param name="end">The end of the time interval to zoom to.</param>
        public void Zoom(DateTime start, DateTime end)
        {
            this.viewRange.SetRange(start, end);
        }

        /// <summary>
        /// Zoom by a ratio.
        /// </summary>
        /// <param name="ratio">The ratio to zoom at.</param>
        public void ZoomAroundCenter(double ratio)
        {
            DateTime viewCenter = this.viewRange.StartTime + TimeSpan.FromTicks(this.viewRange.Duration.Ticks / 2);
            TimeSpan halfViewDuration = TimeSpan.FromTicks((long)(this.viewRange.Duration.Ticks * ratio * 0.5));
            this.viewRange.SetRange(viewCenter - halfViewDuration, viewCenter + halfViewDuration);
        }

        /// <summary>
        /// Zoom to a certain view duration
        /// </summary>
        /// <param name="viewDuration">The timespan to zoom to.</param>
        public void ZoomAroundCenter(TimeSpan viewDuration)
        {
            DateTime viewCenter = this.viewRange.StartTime + TimeSpan.FromTicks(this.viewRange.Duration.Ticks / 2);
            TimeSpan halfViewDuration = TimeSpan.FromTicks((long)(viewDuration.Ticks * 0.5));
            this.viewRange.SetRange(viewCenter - halfViewDuration, viewCenter + halfViewDuration);
        }

        /// <summary>
        /// Zooms to cursor.
        /// </summary>
        /// <param name="ratio">The ratio to zoom at.</param>
        public void ZoomAroundCursor(double ratio)
        {
            TimeSpan beforeDuration = TimeSpan.FromTicks((long)((this.Cursor.Ticks - this.viewRange.StartTime.Ticks) * ratio));
            TimeSpan afterDuration = TimeSpan.FromTicks((long)((this.viewRange.EndTime.Ticks - this.Cursor.Ticks) * ratio));
            this.viewRange.SetRange(this.Cursor - beforeDuration, this.Cursor + afterDuration);
        }

        /// <summary>
        /// Zoom to cursor.
        /// </summary>
        /// <param name="viewDuration">The duration of the time interval to zoom to.</param>
        public void ZoomAroundCursor(TimeSpan viewDuration)
        {
            TimeSpan halfViewDuration = TimeSpan.FromTicks((long)(viewDuration.Ticks * 0.5));
            this.viewRange.SetRange(this.Cursor - halfViewDuration, this.Cursor + halfViewDuration);
        }

        /// <summary>
        /// Zoom in
        /// </summary>
        public void ZoomIn()
        {
            this.ZoomAroundCenter(1 / 3.0);
        }

        /// <summary>
        /// Zoom out
        /// </summary>
        public void ZoomOut()
        {
            this.ZoomAroundCenter(3.0);
        }

        /// <summary>
        /// Zooms out to the maximum extent of data
        /// </summary>
        public void ZoomToDataRange()
        {
            this.viewRange.SetRange(this.dataRange.StartTime, this.dataRange.EndTime);
        }

        /// <summary>
        /// Zooms to selection
        /// </summary>
        public void ZoomToSelection()
        {
            TimeSpan padding = TimeSpan.FromTicks((long)(this.selectionRange.Duration.Ticks * this.zoomToSelectionPadding * 0.5));
            this.viewRange.SetRange(this.selectionRange.StartTime - padding, this.selectionRange.EndTime + padding);
        }

        /// <summary>
        /// Updates the live mode extents based on the current time.
        /// </summary>
        /// <param name="currentTime">The current time.</param>
        internal void UpdateLiveExtents(DateTime currentTime)
        {
            if (this.NavigationMode != NavigationMode.Live)
            {
                return;
            }

            if (currentTime > this.Cursor)
            {
                this.Cursor = currentTime;
            }

            // check if we need to scroll
            if (currentTime > this.viewRange.EndTime)
            {
                this.viewRange.SetRange(currentTime - this.viewRange.Duration, currentTime);
            }

            // check if we need to move the data range
            if (currentTime > this.dataRange.EndTime)
            {
                this.dataRange.SetRange(this.dataRange.StartTime, currentTime);
            }
        }

        private string FormatTimespan(TimeSpan timespan)
        {
            return timespan.ToString(timespan < TimeSpan.Zero ? "\\-hh\\:mm\\:ss\\.ffff" : "hh\\:mm\\:ss\\.ffff");
        }

        private void SelectionRange_RangeChanged(object sender, NavigatorTimeRangeChangedEventArgs e)
        {
            this.RaisePropertyChanged(nameof(this.SelectionStartRelativeToSessionStartFormatted));
            this.RaisePropertyChanged(nameof(this.SelectionEndRelativeToSessionStartFormatted));
            this.RaisePropertyChanged(nameof(this.SessionEndRelativeToSelectionStartFormatted));
        }
    }
}
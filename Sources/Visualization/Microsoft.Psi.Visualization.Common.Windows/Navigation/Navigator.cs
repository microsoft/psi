// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.

namespace Microsoft.Psi.Visualization.Navigation
{
    using System;
    using System.Runtime.Serialization;
    using System.Windows.Threading;

    using Microsoft.Psi.Audio;
    using Microsoft.Psi.Visualization.Base;
    using Microsoft.Psi.Visualization.Data;

    /// <summary>
    /// Class implements the time Navigator view model
    /// </summary>
    public partial class Navigator : ObservableObject
    {
        private const int DefaultLiveModeViewportWidthSeconds = 30;

        // The position of the cursor in the timeline window when the timeline
        // is playing, expressed as a percentage of the viewport width
        private double liveCursorViewportPosition = 0.8d;

        private DateTime cursor;
        private NavigatorRange dataRange;
        private NavigatorRange selectionRange;
        private NavigatorRange viewRange;
        private CursorMode cursorMode;

        private Pipeline audioPlaybackPipeline = null;

        /// <summary>
        /// The current stream providing audio playback (if any)
        /// </summary>
        private StreamBinding audioPlaybackStream = null;

        // The time offset of the Cursor when in Live mode from the left hand edge of the view window
        private TimeSpan liveCursorOffsetFromViewRangeStart;

        /// <summary>
        /// The padding (in percentage) when performing a zoom to selection. The resulting view
        /// will be larger than the selection by this percentage
        /// </summary>
        private double zoomToSelectionPadding;

        // The various Timing displays
        private bool showAbsoluteTiming = false;
        private bool showTimingRelativeToSessionStart = false;
        private bool showTimingRelativeToSelectionStart = false;

        // The playback speed
        private double playSpeed = 1.0d;

        private DispatcherTimer playTimer = null;
        private int playTimerTickIntervalMs = 10;

        // The current clock time (in ticks) of the last time the play timer fired
        private DateTime lastPlayTimerTickTime;

        // Repeats playback in a loop if true, otherwise stops playback at the selection end marker
        private bool repeatPlayback = false;

        /// <summary>
        /// Initializes a new instance of the <see cref="Navigator"/> class.
        /// </summary>
        public Navigator()
        {
            DateTime now = DateTime.UtcNow;

            this.selectionRange = new NavigatorRange(now.AddSeconds(-60), now);
            this.dataRange = new NavigatorRange(now.AddSeconds(-60), now);
            this.viewRange = new NavigatorRange(now.AddSeconds(-60), now);
            this.cursor = now.AddSeconds(-60);
            this.zoomToSelectionPadding = 0.1;

            this.SelectionRange.RangeChanged += this.SelectionRange_RangeChanged;
            this.viewRange.PropertyChanged += this.ViewRange_PropertyChanged;
        }

        /// <summary>
        /// Occurs when the cursor mode changes
        /// </summary>
        public event CursorModeChangedHandler CursorModeChanged;

        /// <summary>
        /// Occurs when the cursor changes.
        /// </summary>
        public event NavigatorTimeChangedHandler CursorChanged;

        /// <summary>
        /// Gets or the cursor mode
        /// </summary>
        public CursorMode CursorMode
        {
            get => this.cursorMode;

            private set
            {
                if (this.cursorMode != value)
                {
                    CursorMode oldCursorMode = this.cursorMode;

                    this.RaisePropertyChanging(nameof(this.CursorMode));
                    this.RaisePropertyChanging(nameof(this.IsCursorModePlayback));
                    this.RaisePropertyChanging(nameof(this.IsCursorModeLive));
                    this.RaisePropertyChanging(nameof(this.DisplaySelectionMarkers));
                    this.cursorMode = value;
                    this.RaisePropertyChanged(nameof(this.CursorMode));
                    this.RaisePropertyChanged(nameof(this.IsCursorModePlayback));
                    this.RaisePropertyChanged(nameof(this.IsCursorModeLive));
                    this.RaisePropertyChanged(nameof(this.DisplaySelectionMarkers));

                    this.CursorModeChanged?.Invoke(this, new CursorModeChangedEventArgs(oldCursorMode, this.cursorMode));
                }
            }
        }

        /// <summary>
        /// Gets or sets the cursor position
        /// </summary>
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
        /// Gets or sets the play speed
        /// </summary>
        public double PlaySpeed
        {
            get => this.playSpeed;

            set
            {
                if ((value >= 1) && (value <= 11))
                {
                    this.RaisePropertyChanging(nameof(this.PlaySpeed));
                    this.playSpeed = value;
                    this.RaisePropertyChanged(nameof(this.PlaySpeed));
                    this.UpdateAudioPlayback();
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
        /// Gets a value indicating whether we're currently playing back data
        /// </summary>
        public bool IsCursorModePlayback => this.CursorMode == CursorMode.Playback;

        /// <summary>
        /// Gets a value indicating whether the navigator is currently tracking a live partition
        /// </summary>
        public bool IsCursorModeLive => this.CursorMode == CursorMode.Live;

        /// <summary>
        /// Gets a value indicating whether the start and end selection markers should be displayed in the timeline view
        /// </summary>
        public bool DisplaySelectionMarkers => this.CursorMode != CursorMode.Live;

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

        /// <summary>
        /// Gets or sets the zoom range selection padding.
        /// </summary>
        [DataMember]
        public double ZoomToSelectionPadding
        {
            get => this.zoomToSelectionPadding;
            set => this.Set(nameof(this.ZoomToSelectionPadding), ref this.zoomToSelectionPadding, value);
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
        /// Gets or sets the audio stream to be played during playback
        /// </summary>
        public StreamBinding AudioPlaybackStream
        {
            get => this.audioPlaybackStream;

            set
            {
                this.audioPlaybackStream = value;
                this.UpdateAudioPlayback();
                this.RaisePropertyChanged(nameof(this.AudioPlaybackStream));
            }
        }

        /// <summary>
        /// Gets or sets a value indicating whether playback repeats
        /// </summary>
        public bool RepeatPlayback
        {
            get => this.repeatPlayback;

            set
            {
                this.Set(nameof(this.RepeatPlayback), ref this.repeatPlayback, value);
            }
        }

        /// <summary>
        /// Moves the cursor to the start of the selection
        /// </summary>
        public void MoveToSelectionStart()
        {
            if (this.CursorMode != CursorMode.Live)
            {
                this.Cursor = this.SelectionRange.StartTime;
                this.EnsureCursorVisible();
            }
        }

        /// <summary>
        /// Moves the cursor to the end of the selection
        /// </summary>
        public void MoveToSelectionEnd()
        {
            if (this.CursorMode != CursorMode.Live)
            {
                this.Cursor = this.SelectionRange.EndTime;
                this.EnsureCursorVisible();
            }
        }

        /// <summary>
        /// Sets the cursor mode
        /// </summary>
        /// <param name="cursorMode">The cursor mode to set</param>
        public void SetCursorMode(CursorMode cursorMode)
        {
            switch (cursorMode)
            {
                // Switch into Manual mode
                case CursorMode.Manual:
                    switch (this.CursorMode)
                    {
                        case CursorMode.Playback:
                        case CursorMode.Live:
                            this.StopPlayback();
                            break;
                    }

                    break;

                // Switch into Playback mode
                case CursorMode.Playback:
                    switch (this.CursorMode)
                    {
                        case CursorMode.Manual:
                        case CursorMode.Live:
                            this.StartPlayback();
                            break;
                    }

                    break;

                // Switch into Live mode
                case CursorMode.Live:
                    switch (this.CursorMode)
                    {
                        case CursorMode.Manual:
                            this.EnterLiveMode();
                            break;

                        case CursorMode.Playback:
                            this.StopPlayback();
                            this.EnterLiveMode();
                            break;
                    }

                    break;

                default:
                    throw new ApplicationException(string.Format("Navigator.SetCursorMode() - Don't know how to handle CursorMode.{0}", cursorMode));
            }
        }

        /// <summary>
        /// Updates the viewport in response to live messages
        /// </summary>
        /// <param name="datetime">The new message time to set the cursor at</param>
        public void NotifyLiveMessageReceived(DateTime datetime)
        {
            // Check if the new event is newer than the current data range
            if (datetime > this.DataRange.EndTime)
            {
                // Update the data range
                this.DataRange.SetRange(this.DataRange.StartTime, datetime);

                // If we're in Live mode, scrub the viewport
                if (this.CursorMode == CursorMode.Live)
                {
                    this.ViewRange.SetRange(this.DataRange.EndTime - this.liveCursorOffsetFromViewRangeStart, this.ViewRange.Duration);

                    // Set the cursor
                    this.Cursor = datetime;
                }
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
        /// Animates the navigator curor based on indicated speed.
        /// </summary>
        private void StartPlayback()
        {
            // Create the play timer if it doesn't exist yet
            if (this.playTimer == null)
            {
                this.playTimer = new DispatcherTimer(TimeSpan.FromMilliseconds(this.playTimerTickIntervalMs), DispatcherPriority.Background, new EventHandler(this.OnPlaytimerTick), Dispatcher.CurrentDispatcher);
            }

            // Move the cursor to the start of the selection
            this.Cursor = this.SelectionRange.StartTime;

            // Make sure that the cursor is visible
            this.EnsureCursorVisible();

            // Update the cursor mode
            this.CursorMode = CursorMode.Playback;

            // Start the play timer running
            this.lastPlayTimerTickTime = DateTime.UtcNow;
            this.playTimer.Start();

            // Start audio playback
            this.UpdateAudioPlayback();
        }

        /// <summary>
        /// Stop animating the navigator cursor.
        /// </summary>
        private void StopPlayback()
        {
            // Pause the play timer
            if (this.playTimer != null)
            {
                this.playTimer.Stop();
            }

            // Update the cursor mode
            this.CursorMode = CursorMode.Manual;

            // Stop audio playback
            this.UpdateAudioPlayback();
        }

        private void UpdateAudioPlayback()
        {
            if (this.audioPlaybackPipeline != null)
            {
                this.audioPlaybackPipeline.Dispose();
                this.audioPlaybackPipeline = null;
            }

            // If we're in playback mode, and the playback speed is 1x, and we have a bound audio stream to play back, then create the audio player
            if ((this.CursorMode == CursorMode.Playback) && (this.audioPlaybackStream != null) && this.audioPlaybackStream.IsBound && (this.playSpeed == 1.0d))
            {
                // Create the playback pipeline
                this.audioPlaybackPipeline = Pipeline.Create("AudioPlayer");

                // Get the audio stream to play
                var importer = Store.Open(this.audioPlaybackPipeline, this.audioPlaybackStream.StoreName, this.audioPlaybackStream.StorePath);
                var stream = importer.OpenStream<AudioBuffer>(this.audioPlaybackStream.StreamName);

                // Create the audio player
                var audioPlayer = new AudioPlayer(this.audioPlaybackPipeline, new AudioPlayerConfiguration());
                stream.PipeTo(audioPlayer.In);

                // Start playing back the audio
                this.audioPlaybackPipeline.RunAsync(new ReplayDescriptor(this.Cursor, this.SelectionRange.EndTime));
            }
        }

        /// <summary>
        /// Sets the cursor mode to live
        /// </summary>
        private void EnterLiveMode()
        {
            // Update the cursor mode
            this.CursorMode = CursorMode.Live;

            // Set the view range to just the last 30 seconds of data
            if (this.DataRange.IsFinite)
            {
                this.ViewRange.SetRange(this.DataRange.EndTime.AddSeconds(-DefaultLiveModeViewportWidthSeconds) - this.liveCursorOffsetFromViewRangeStart, new TimeSpan(0, 0, DefaultLiveModeViewportWidthSeconds));
            }
        }

        private void OnPlaytimerTick(object sender, EventArgs e)
        {
            // Calculate the new cursor position
            DateTime now = DateTime.UtcNow;
            this.Cursor = this.Cursor.AddTicks((long)((now - this.lastPlayTimerTickTime).Ticks * this.playSpeed));

            // Check if we've hit the end of the selection
            if (this.Cursor >= this.SelectionRange.EndTime)
            {
                // If Repeat is enabled, then move the cursor back to the
                // selection start marker, otherwise stop the play timer
                if (this.RepeatPlayback)
                {
                    this.Cursor = this.SelectionRange.StartTime;
                    this.UpdateAudioPlayback();
                }
                else
                {
                    this.Cursor = this.SelectionRange.EndTime;
                    this.playTimer.Stop();
                    this.CursorMode = CursorMode.Manual;
                }
            }

            // Make sure the cursor is visible in the view window
            if (this.Cursor > this.ViewRange.EndTime)
            {
                this.ViewRange.SetRange(this.Cursor, this.ViewRange.Duration);
            }

            this.lastPlayTimerTickTime = now;
        }

        private void EnsureCursorVisible()
        {
            // Make sure the cursor is visible in the view window.  If it isn't, scrub the timeline
            // so that the cursor is at the 20th percentile position in the view window.
            if ((this.Cursor < this.ViewRange.StartTime) || (this.Cursor > this.ViewRange.EndTime))
            {
                this.ViewRange.SetRange(this.Cursor.AddTicks((long)(this.ViewRange.Duration.Ticks * -0.2d)), this.ViewRange.Duration);
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

        private void ViewRange_PropertyChanged(object sender, System.ComponentModel.PropertyChangedEventArgs e)
        {
            // If the duration of the View Range has changed, then we need to recalculate
            // the time offset of the cursor from the start time of the view range
            if (e.PropertyName == nameof(NavigatorRange.Duration))
            {
                this.liveCursorOffsetFromViewRangeStart = new TimeSpan((long)(this.viewRange.Duration.Ticks * this.liveCursorViewportPosition));
            }
        }
    }
}
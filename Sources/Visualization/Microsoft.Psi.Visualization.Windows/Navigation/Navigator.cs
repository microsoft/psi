// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.

namespace Microsoft.Psi.Visualization.Navigation
{
    using System;
    using System.Collections.Generic;
    using System.ComponentModel;
    using System.Linq;
    using System.Runtime.Serialization;
    using System.Windows;
    using System.Windows.Threading;
    using GalaSoft.MvvmLight.CommandWpf;
    using Microsoft.Psi.Audio;
    using Microsoft.Psi.Data;
    using Microsoft.Psi.Visualization.Data;
    using Microsoft.Psi.Visualization.Helpers;
    using Microsoft.Psi.Visualization.VisualizationObjects;

    /// <summary>
    /// Class implements the time Navigator view model.
    /// </summary>
    public partial class Navigator : ObservableObject
    {
        private const int DefaultLiveModeViewportWidthSeconds = 30;

        // The position of the cursor in the timeline window when the timeline
        // is playing, expressed as a percentage of the viewport width
        private readonly double liveCursorViewportPosition = 0.8d;

        private readonly NavigatorRange dataRange;
        private readonly NavigatorRange selectionRange;
        private readonly NavigatorRange viewRange;

        // The dictionary of audio playback sources, keyed by audio visualization objects.  The audio
        // of all currently bound audio sources in this collection will be played during playback.
        private readonly Dictionary<AudioVisualizationObject, StreamSource> audioPlaybackSources = new ();

        private readonly int playTimerTickIntervalMs = 10;

        private DateTime cursor;
        private CursorMode cursorMode;

        private Pipeline audioPlaybackPipeline = null;

        // The time offset of the Cursor when in Live mode from the left hand edge of the view window
        private TimeSpan liveCursorOffsetFromViewRangeStart;

        /// <summary>
        /// The padding (in percentage) when performing a zoom to selection. The resulting view
        /// will be larger than the selection by this percentage.
        /// </summary>
        private double zoomToSelectionPadding;

        // The various Timing displays
        private bool showAbsoluteTiming = false;
        private bool showTimingRelativeToSessionStart = false;
        private bool showTimingRelativeToSelectionStart = false;

        // The playback speed
        private double playSpeed = 1.0d;

        private DispatcherTimer playTimer = null;

        // The current clock time (in ticks) of the last time the play timer fired
        private DateTime lastPlayTimerTickTime;

        // Repeats playback in a loop if true, otherwise stops playback at the selection end marker
        private bool repeatPlayback = false;

        // True if the timeline cursor should follow the mouse cursor when in manual navigation mode, otherwise false
        private bool cursorFollowsMouse = true;

        private RelayCommand<string> copyToClipboardCommand;

        /// <summary>
        /// Initializes a new instance of the <see cref="Navigator"/> class.
        /// </summary>
        public Navigator()
        {
            DateTime now = DateTime.UtcNow;

            this.selectionRange = new NavigatorRange();
            this.dataRange = new NavigatorRange(now.AddSeconds(-60), now);
            this.viewRange = new NavigatorRange(now.AddSeconds(-60), now);
            this.cursor = now.AddSeconds(-60);
            this.zoomToSelectionPadding = 0.1;

            this.selectionRange.RangeChanged += this.OnSelectionRangeChanged;
            this.viewRange.PropertyChanged += this.OnViewRangePropertyChanged;
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
        /// Gets the command for copying a string to clipboard.
        /// </summary>
        [Browsable(false)]
        [IgnoreDataMember]
        public RelayCommand<string> CopyToClipboardCommand
            => this.copyToClipboardCommand ??= new RelayCommand<string>(text => Clipboard.SetText(text));

        /// <summary>
        /// Gets or the cursor mode.
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
                    this.RaisePropertyChanging(nameof(this.ShowSelectionStart));
                    this.RaisePropertyChanging(nameof(this.ShowSelectionEnd));
                    this.RaisePropertyChanging(nameof(this.ShowSelectionRegion));
                    this.cursorMode = value;
                    this.RaisePropertyChanged(nameof(this.CursorMode));
                    this.RaisePropertyChanged(nameof(this.IsCursorModePlayback));
                    this.RaisePropertyChanged(nameof(this.IsCursorModeLive));
                    this.RaisePropertyChanged(nameof(this.ShowSelectionStart));
                    this.RaisePropertyChanged(nameof(this.ShowSelectionEnd));
                    this.RaisePropertyChanged(nameof(this.ShowSelectionRegion));

                    this.CursorModeChanged?.Invoke(this, new CursorModeChangedEventArgs(oldCursorMode, this.cursorMode));
                }
            }
        }

        /// <summary>
        /// Gets or sets the cursor position.
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
                    DataManager.Instance.ReadAndPublishStreamValue(value);
                    this.RaisePropertyChanged(nameof(this.Cursor));
                    this.RaisePropertyChanged(nameof(this.CursorRelativeToSessionStartFormatted));
                    this.RaisePropertyChanged(nameof(this.CursorRelativeToSelectionStartFormatted));
                }
            }
        }

        /// <summary>
        /// Gets or sets a value indicating whether the timeline cursor follows the mouse cursor when in manual navigation mode.
        /// </summary>
        [IgnoreDataMember]
        public bool CursorFollowsMouse
        {
            get { return this.cursorFollowsMouse; }

            set
            {
                this.RaisePropertyChanging(nameof(this.CursorFollowsMouse));
                this.cursorFollowsMouse = value;
                this.RaisePropertyChanged(nameof(this.CursorFollowsMouse));
            }
        }

        /// <summary>
        /// Gets or sets the play speed.
        /// </summary>
        public double PlaySpeed
        {
            get => this.playSpeed;

            set
            {
                if (value > 0)
                {
                    this.RaisePropertyChanging(nameof(this.PlaySpeed));
                    this.playSpeed = value;
                    this.RaisePropertyChanged(nameof(this.PlaySpeed));
                    this.UpdateAudioPlayback();
                }
            }
        }

        /// <summary>
        /// Gets the cursor position relative to the session start time.
        /// </summary>
        public string CursorRelativeToSessionStartFormatted =>
            this.DataRange.StartTime != DateTime.MinValue ?
                this.FormatTimespan(this.Cursor - this.DataRange.StartTime) : string.Empty;

        /// <summary>
        /// Gets the cursor position relative to the selection start time.
        /// </summary>
        public string CursorRelativeToSelectionStartFormatted =>
            this.SelectionRange.StartTime != DateTime.MinValue ?
            this.FormatTimespan(this.Cursor - this.SelectionRange.StartTime) : string.Empty;

        /// <summary>
        /// Gets the selection start relative to the selection start time.
        /// </summary>
        public string SelectionStartRelativeToSelectionStartFormatted =>
            this.SelectionRange.StartTime != DateTime.MinValue ?
            this.FormatTimespan(TimeSpan.Zero) : string.Empty;

        /// <summary>
        /// Gets the selection end relative to the selection start time.
        /// </summary>
        public string SelectionEndRelativeToSelectionStartFormatted =>
            this.SelectionRange.StartTime != DateTime.MinValue && this.SelectionRange.EndTime != DateTime.MaxValue ?
            this.FormatTimespan(this.SelectionRange.EndTime - this.SelectionRange.StartTime) : string.Empty;

        /// <summary>
        /// Gets the data range.
        /// </summary>
        [DataMember]
        public NavigatorRange DataRange => this.dataRange;

        /// <summary>
        /// Gets the Session End time relative to the Selection Start time.
        /// </summary>
        public string SessionEndRelativeToSelectionStartFormatted =>
            this.SelectionRange.StartTime != DateTime.MinValue ?
            this.FormatTimespan(this.DataRange.EndTime - this.SelectionRange.StartTime) : string.Empty;

        /// <summary>
        /// Gets a value indicating whether the navigator has a finite range.
        /// </summary>
        public bool HasFiniteRange => this.selectionRange.IsFinite && this.viewRange.IsFinite && this.dataRange.IsFinite;

        /// <summary>
        /// Gets a value indicating whether we're currently playing back data.
        /// </summary>
        public bool IsCursorModePlayback => this.CursorMode == CursorMode.Playback;

        /// <summary>
        /// Gets a value indicating whether the navigator is currently tracking a live partition.
        /// </summary>
        public bool IsCursorModeLive => this.CursorMode == CursorMode.Live;

        /// <summary>
        /// Gets the selection range.
        /// </summary>
        [DataMember]
        public NavigatorRange SelectionRange => this.selectionRange;

        /// <summary>
        /// Gets a value indicating whether the selection start should be displayed in the timeline view.
        /// </summary>
        public bool ShowSelectionStart => this.SelectionRange.StartTime != DateTime.MinValue && this.CursorMode != CursorMode.Live;

        /// <summary>
        /// Gets a value indicating whether the selection end should be displayed in the timeline view.
        /// </summary>
        public bool ShowSelectionEnd => this.SelectionRange.EndTime != DateTime.MaxValue && this.CursorMode != CursorMode.Live;

        /// <summary>
        /// Gets a value indicating whether the selection region should be displayed in the timeline view.
        /// </summary>
        public bool ShowSelectionRegion => this.SelectionRange.IsFinite && this.CursorMode != CursorMode.Live;

        /// <summary>
        /// Gets selection start as a formatted string.
        /// </summary>
        public string SelectionStartFormatted => DateTimeHelper.FormatDateTime(this.SelectionRange.StartTime, false);

        /// <summary>
        /// Gets selection end as a formatted string.
        /// </summary>
        public string SelectionEndFormatted => DateTimeHelper.FormatDateTime(this.SelectionRange.EndTime, false);

        /// <summary>
        /// Gets the offset of the section start from the start of the ession.
        /// </summary>
        public string SelectionStartRelativeToSessionStartFormatted =>
            this.SelectionRange.StartTime != DateTime.MinValue ?
            this.FormatTimespan(this.SelectionRange.StartTime - this.DataRange.StartTime) : string.Empty;

        /// <summary>
        /// Gets the offset of the section end from the start of the session.
        /// </summary>
        public string SelectionEndRelativeToSessionStartFormatted =>
            this.SelectionRange.EndTime != DateTime.MaxValue ?
            this.FormatTimespan(this.SelectionRange.EndTime - this.DataRange.StartTime) : string.Empty;

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
        /// Gets a value indicating whether the Timing Header is visible.
        /// </summary>
        public bool ShowTimingHeader => this.ShowAbsoluteTiming || this.ShowTimingRelativeToSessionStart || this.ShowTimingRelativeToSelectionStart;

        /// <summary>
        /// Gets or sets a value indicating whether Absolute Timing is displayed.
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
        /// Gets or sets a value indicating whether Timing Relative to the Session Start is displayed.
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
        /// Gets or sets a value indicating whether Timing Relative to the Selection Start is displayed.
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
        /// Gets or sets a value indicating whether playback repeats.
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
        /// Initializes a navigator with the properties of an existing navigator.
        /// </summary>
        /// <param name="navigator">The existing navigator instance whose properties should be copied.</param>
        public void CopyFrom(Navigator navigator)
        {
            this.dataRange.Set(navigator.DataRange.AsTimeInterval);
            this.viewRange.Set(navigator.ViewRange.AsTimeInterval);
            this.selectionRange.Set(navigator.SelectionRange.AsTimeInterval);
            this.cursor = navigator.Cursor;
            this.showAbsoluteTiming = navigator.ShowAbsoluteTiming;
            this.showTimingRelativeToSessionStart = navigator.ShowTimingRelativeToSessionStart;
            this.showTimingRelativeToSelectionStart = navigator.ShowTimingRelativeToSelectionStart;
            this.cursorFollowsMouse = navigator.CursorFollowsMouse;
        }

        /// <summary>
        /// Moves the cursor to the given datetime.
        /// </summary>
        /// <param name="dateTime">Time to which to move cursor.</param>
        public void MoveCursorTo(DateTime dateTime)
        {
            if (this.CursorMode != CursorMode.Live)
            {
                this.Cursor = dateTime;
                this.EnsureCursorVisible();
            }
        }

        /// <summary>
        /// Gets a value indicating whether the cursor can be programmatically moved.
        /// </summary>
        /// <returns>True if the cursor can be programatically moved, false otherwise.</returns>
        public bool CanMoveCursor() => this.CursorMode != CursorMode.Live;

        /// <summary>
        /// Moves the cursor to the start of the selection.
        /// </summary>
        public void MoveCursorToSelectionStart() => this.MoveCursorTo(this.SelectionRange.StartTime);

        /// <summary>
        /// Gets a value indicating whether we can move the cursor to the start of the selection.
        /// </summary>
        /// <returns>True if the cursor can move to the start of the selection, false otherwise.</returns>
        public bool CanMoveCursorToSelectionStart() => this.CanMoveCursor() && this.SelectionRange.StartTime != DateTime.MinValue;

        /// <summary>
        /// Moves the cursor to the end of the selection.
        /// </summary>
        public void MoveCursorToSelectionEnd() => this.MoveCursorTo(this.SelectionRange.EndTime);

        /// <summary>
        /// Gets a value indicating whether we can move the cursor to the end of the selection.
        /// </summary>
        /// <returns>True if the cursor can move to the end of the selection, false otherwise.</returns>
        public bool CanMoveCursorToSelectionEnd() => this.CanMoveCursor() && this.SelectionRange.EndTime != DateTime.MaxValue;

        /// <summary>
        /// Adds an audio source to play during playback.
        /// </summary>
        /// <param name="audioVisualizationObject">The audio visualization object whose audio should be played during playback if it is currently bound to a source.</param>
        /// <param name="streamSource">Stream source of audio playback stream to add.</param>
        public void AddOrUpdateAudioPlaybackSource(AudioVisualizationObject audioVisualizationObject, StreamSource streamSource)
        {
            this.audioPlaybackSources[audioVisualizationObject] = streamSource;
            this.UpdateAudioPlayback();
            ////this.RaisePropertyChanged(nameof(this.AudioPlaybackStreams));
        }

        /// <summary>
        /// Removes an audio source to play during playback.
        /// </summary>
        /// <param name="audioVisualizationObject">The audio visualization object whose audio should no longer be played during playback.</param>
        public void RemoveAudioPlaybackSource(AudioVisualizationObject audioVisualizationObject)
        {
            this.audioPlaybackSources.Remove(audioVisualizationObject);
            this.UpdateAudioPlayback();
        }

        /// <summary>
        /// Gets a value indicating whether an audio visualization object's audio stream is currently being played back when bound to a source.
        /// </summary>
        /// <param name="audioVisualizationObject">An audio visualization object.</param>
        /// <returns>True if the visualization object's audio will be played during playback when it is bound to a source.</returns>
        public bool IsAudioPlaybackVisualizationObject(AudioVisualizationObject audioVisualizationObject) => this.audioPlaybackSources.ContainsKey(audioVisualizationObject);

        /// <summary>
        /// Sets the cursor mode.
        /// </summary>
        /// <param name="cursorMode">The cursor mode to set.</param>
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
        /// Updates the viewport in response to live messages.
        /// </summary>
        /// <param name="datetime">The new message time to set the cursor at.</param>
        public void NotifyLiveMessageReceived(DateTime datetime)
        {
            // Check if the new event is newer than the current data range
            if (datetime > this.DataRange.EndTime)
            {
                // Update the data range
                this.DataRange.Set(this.DataRange.StartTime, datetime);

                // If we're in Live mode, scrub the viewport
                if (this.CursorMode == CursorMode.Live)
                {
                    this.ViewRange.Set(this.DataRange.EndTime - this.liveCursorOffsetFromViewRangeStart, this.ViewRange.Duration);

                    // Set the cursor
                    this.Cursor = datetime;
                }
            }
        }

        /// <summary>
        /// Zoom to a particular location.
        /// </summary>
        /// <param name="start">The start of the time interval to zoom to.</param>
        /// <param name="end">The end of the time interval to zoom to.</param>
        public void Zoom(DateTime start, DateTime end)
        {
            this.viewRange.Set(start, end);
        }

        /// <summary>
        /// Zoom by a ratio.
        /// </summary>
        /// <param name="ratio">The ratio to zoom at.</param>
        public void ZoomAroundCenter(double ratio)
        {
            DateTime viewCenter = this.viewRange.StartTime + TimeSpan.FromTicks(this.viewRange.Duration.Ticks / 2);
            TimeSpan halfViewDuration = TimeSpan.FromTicks((long)(this.viewRange.Duration.Ticks * ratio * 0.5));
            this.viewRange.Set(viewCenter - halfViewDuration, viewCenter + halfViewDuration);
        }

        /// <summary>
        /// Zoom to a certain view duration.
        /// </summary>
        /// <param name="viewDuration">The timespan to zoom to.</param>
        public void ZoomAroundCenter(TimeSpan viewDuration)
        {
            DateTime viewCenter = this.viewRange.StartTime + TimeSpan.FromTicks(this.viewRange.Duration.Ticks / 2);
            TimeSpan halfViewDuration = TimeSpan.FromTicks((long)(viewDuration.Ticks * 0.5));
            this.viewRange.Set(viewCenter - halfViewDuration, viewCenter + halfViewDuration);
        }

        /// <summary>
        /// Zooms to cursor.
        /// </summary>
        /// <param name="ratio">The ratio to zoom at.</param>
        public void ZoomAroundCursor(double ratio)
        {
            TimeSpan beforeDuration = TimeSpan.FromTicks((long)((this.Cursor.Ticks - this.viewRange.StartTime.Ticks) * ratio));
            TimeSpan afterDuration = TimeSpan.FromTicks((long)((this.viewRange.EndTime.Ticks - this.Cursor.Ticks) * ratio));
            this.viewRange.Set(this.Cursor - beforeDuration, this.Cursor + afterDuration);
        }

        /// <summary>
        /// Zoom to cursor.
        /// </summary>
        /// <param name="viewDuration">The duration of the time interval to zoom to.</param>
        public void ZoomAroundCursor(TimeSpan viewDuration)
        {
            TimeSpan halfViewDuration = TimeSpan.FromTicks((long)(viewDuration.Ticks * 0.5));
            this.viewRange.Set(this.Cursor - halfViewDuration, this.Cursor + halfViewDuration);
        }

        /// <summary>
        /// Zoom in.
        /// </summary>
        public void ZoomIn()
        {
            this.ZoomAroundCenter(1 / 3.0);
        }

        /// <summary>
        /// Zoom out.
        /// </summary>
        public void ZoomOut()
        {
            this.ZoomAroundCenter(3.0);
        }

        /// <summary>
        /// Zooms out to the maximum extent of data.
        /// </summary>
        public void ZoomToDataRange()
        {
            this.viewRange.Set(this.dataRange.StartTime, this.dataRange.EndTime);
        }

        /// <summary>
        /// Zooms to selection.
        /// </summary>
        public void ZoomToSelection()
        {
            TimeSpan padding = TimeSpan.FromTicks((long)(this.selectionRange.Duration.Ticks * this.zoomToSelectionPadding * 0.5));
            this.viewRange.Set(this.selectionRange.StartTime - padding, this.selectionRange.EndTime + padding);
        }

        /// <summary>
        /// Move selection left.
        /// </summary>
        public void MoveSelectionLeft()
        {
            var selectionDuration = this.selectionRange.Duration;
            var selectionStartTime = new DateTime(Math.Max((this.selectionRange.StartTime - selectionDuration).Ticks, this.dataRange.StartTime.Ticks));
            var selectionEndTime = selectionStartTime + selectionDuration;
            this.selectionRange.Set(selectionStartTime, selectionEndTime);

            this.ShiftViewRangeToAccomodateSelection();
        }

        /// <summary>
        /// Gets a value indicating whether the selection can be moved left.
        /// </summary>
        /// <returns>True if the selection can be moved left.</returns>
        public bool CanMoveSelectionLeft()
            => this.CursorMode != CursorMode.Live && this.SelectionRange.IsFinite && this.SelectionRange.StartTime > this.DataRange.StartTime;

        /// <summary>
        /// Move selection right.
        /// </summary>
        public void MoveSelectionRight()
        {
            var selectionDuration = this.selectionRange.Duration;
            var selectionEndTime = new DateTime(Math.Min((this.selectionRange.EndTime + selectionDuration).Ticks, this.dataRange.EndTime.Ticks));
            var selectionStartTime = selectionEndTime - selectionDuration;
            this.selectionRange.Set(selectionStartTime, selectionEndTime);

            this.ShiftViewRangeToAccomodateSelection();
        }

        /// <summary>
        /// Gets a value indicating whether the selection can be moved right.
        /// </summary>
        /// <returns>True if the selection can be moved right.</returns>
        public bool CanMoveSelectionRight()
            => this.CursorMode != CursorMode.Live && this.SelectionRange.IsFinite && this.SelectionRange.EndTime < this.DataRange.EndTime;

        /// <summary>
        /// Shift the view range to capture the selection.
        /// </summary>
        public void ShiftViewRangeToAccomodateSelection()
        {
            var viewRangeDuration = this.viewRange.Duration;

            // If the view range is shorter than the selection
            if (viewRangeDuration <= this.selectionRange.Duration)
            {
                // adjust the desired view range duration to be more than the selection range
                viewRangeDuration = TimeSpan.FromTicks((int)(this.selectionRange.Duration.Ticks * 1.1));
            }

            // If the selection range is after the view range
            if (this.selectionRange.EndTime > this.viewRange.EndTime)
            {
                var padding = TimeSpan.FromTicks((int)(viewRangeDuration.Ticks * 0.05));
                var viewRangeEnd = new DateTime(Math.Min((this.selectionRange.StartTime - padding + viewRangeDuration).Ticks, this.dataRange.EndTime.Ticks));
                var viewRangeStart = viewRangeEnd - viewRangeDuration;
                this.viewRange.Set(viewRangeStart, viewRangeEnd);
            }
            else if (this.selectionRange.StartTime < this.viewRange.StartTime)
            {
                var padding = TimeSpan.FromTicks((int)(viewRangeDuration.Ticks * 0.05));
                var viewRangeStart = new DateTime(Math.Max((this.selectionRange.EndTime + padding - viewRangeDuration).Ticks, this.dataRange.StartTime.Ticks));
                var viewRangeEnd = viewRangeStart + viewRangeDuration;
                this.viewRange.Set(viewRangeStart, viewRangeEnd);
            }
        }

        /// <summary>
        /// Gets a value indicating whether the navigator can zoom to selection.
        /// </summary>
        /// <returns>True if the navigator can zoom to selection, false otherwise.</returns>
        public bool CanZoomToSelection() =>
            this.CursorMode != CursorMode.Live && this.SelectionRange.IsFinite;

        /// <summary>
        /// Clears the selection.
        /// </summary>
        public void ClearSelection()
        {
            this.selectionRange.Set(DateTime.MinValue, DateTime.MaxValue);
        }

        /// <summary>
        /// Gets a value indicating whether the navigator can clear the selection.
        /// </summary>
        /// <returns>True if the navigator can clear the selection, false otherwise.</returns>
        public bool CanClearSelection() =>
            this.CursorMode != CursorMode.Live && (this.SelectionRange.StartTime != DateTime.MinValue || this.SelectionRange.EndTime != DateTime.MaxValue);

        /// <summary>
        /// Animates the navigator cursor based on indicated speed.
        /// </summary>
        private void StartPlayback()
        {
            // Create the play timer if it doesn't exist yet
            if (this.playTimer == null)
            {
                this.playTimer = new DispatcherTimer(TimeSpan.FromMilliseconds(this.playTimerTickIntervalMs), DispatcherPriority.Background, new EventHandler(this.OnPlaytimerTick), Dispatcher.CurrentDispatcher);
            }

            // Move the cursor to the start of the selection, if one is available or from the start of the data if not.
            this.Cursor = this.SelectionRange.StartTime != DateTime.MinValue ? this.SelectionRange.StartTime : this.DataRange.StartTime;

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

            // If we're in playback mode, and the playback speed is 1x, and we have any bound audio streams to play back, then create the audio player
            if ((this.CursorMode == CursorMode.Playback) && (this.playSpeed == 1.0d) && this.audioPlaybackSources.Any(s => s.Value != null))
            {
                // Create the playback pipeline
                this.audioPlaybackPipeline = Pipeline.Create("AudioPlayer");

                foreach (StreamSource streamSource in this.audioPlaybackSources.Values)
                {
                    if (streamSource != null)
                    {
                        // Get the audio stream to play
                        var reader = StreamReader.Create(streamSource.StoreName, streamSource.StorePath, streamSource.StreamReaderType);
                        var importer = new Importer(this.audioPlaybackPipeline, reader, false);
                        var stream = importer.OpenStream<AudioBuffer>(streamSource.StreamName);

                        // Create the audio player
                        var audioPlayer = new AudioPlayer(this.audioPlaybackPipeline, new AudioPlayerConfiguration());
                        stream.PipeTo(audioPlayer.In);
                    }
                }

                // Start playing back the audio
                var endTime = this.SelectionRange.EndTime != DateTime.MaxValue ? this.SelectionRange.EndTime : this.DataRange.EndTime;
                this.audioPlaybackPipeline.RunAsync(new ReplayDescriptor(this.Cursor, endTime));
            }
        }

        /// <summary>
        /// Sets the cursor mode to live.
        /// </summary>
        private void EnterLiveMode()
        {
            // Update the cursor mode
            this.CursorMode = CursorMode.Live;

            // Set the view range to just the last 30 seconds of data
            if (this.DataRange.IsFinite)
            {
                this.ViewRange.Set(this.DataRange.EndTime.AddSeconds(-DefaultLiveModeViewportWidthSeconds) - this.liveCursorOffsetFromViewRangeStart, new TimeSpan(0, 0, DefaultLiveModeViewportWidthSeconds));
            }
        }

        private void OnPlaytimerTick(object sender, EventArgs e)
        {
            // Calculate the new cursor position
            DateTime now = DateTime.UtcNow;
            this.Cursor = this.Cursor.AddTicks((long)((now - this.lastPlayTimerTickTime).Ticks * this.playSpeed));

            // Check if we've hit the end of the selection (or of the data)
            var endTime = this.SelectionRange.EndTime != DateTime.MaxValue ? this.SelectionRange.EndTime : this.DataRange.EndTime;
            if (this.Cursor >= endTime)
            {
                // If Repeat is enabled, then move the cursor back to the
                // selection start marker, otherwise stop the play timer
                if (this.RepeatPlayback)
                {
                    this.Cursor = this.SelectionRange.StartTime != DateTime.MinValue ? this.SelectionRange.StartTime : this.DataRange.StartTime;
                    this.EnsureCursorVisible();
                    this.UpdateAudioPlayback();
                }
                else
                {
                    this.Cursor = endTime;
                    this.playTimer.Stop();
                    this.CursorMode = CursorMode.Manual;
                }
            }

            // Make sure the cursor is visible in the view window
            if (this.Cursor > this.ViewRange.EndTime)
            {
                this.ViewRange.Set(this.Cursor, this.ViewRange.Duration);
            }

            this.lastPlayTimerTickTime = now;
        }

        private void EnsureCursorVisible()
        {
            // Make sure the cursor is visible in the view window.  If it isn't, scrub the timeline
            // so that the cursor is at the 20th percentile position in the view window.
            if ((this.Cursor < this.ViewRange.StartTime) || (this.Cursor > this.ViewRange.EndTime))
            {
                this.ViewRange.Set(this.Cursor.AddTicks((long)(this.ViewRange.Duration.Ticks * -0.2d)), this.ViewRange.Duration);
            }
        }

        private string FormatTimespan(TimeSpan timespan)
        {
            return timespan.ToString(timespan < TimeSpan.Zero ? "\\-hh\\:mm\\:ss\\.ffff" : "hh\\:mm\\:ss\\.ffff");
        }

        private void OnSelectionRangeChanged(object sender, NavigatorTimeRangeChangedEventArgs e)
        {
            this.RaisePropertyChanged(nameof(this.ShowSelectionStart));
            this.RaisePropertyChanged(nameof(this.ShowSelectionEnd));
            this.RaisePropertyChanged(nameof(this.ShowSelectionRegion));
            this.RaisePropertyChanged(nameof(this.SelectionStartFormatted));
            this.RaisePropertyChanged(nameof(this.SelectionEndFormatted));
            this.RaisePropertyChanged(nameof(this.SelectionStartRelativeToSessionStartFormatted));
            this.RaisePropertyChanged(nameof(this.SelectionEndRelativeToSessionStartFormatted));
            this.RaisePropertyChanged(nameof(this.SessionEndRelativeToSelectionStartFormatted));
            this.RaisePropertyChanged(nameof(this.SelectionStartRelativeToSelectionStartFormatted));
            this.RaisePropertyChanged(nameof(this.CursorRelativeToSelectionStartFormatted));
            this.RaisePropertyChanged(nameof(this.SelectionEndRelativeToSelectionStartFormatted));
        }

        private void OnViewRangePropertyChanged(object sender, System.ComponentModel.PropertyChangedEventArgs e)
        {
            // If the duration of the View Range has changed, then we need to recalculate
            // the time offset of the cursor from the start time of the view range
            if (e.PropertyName == nameof(NavigatorRange.Duration))
            {
                this.liveCursorOffsetFromViewRangeStart = new TimeSpan((long)(this.viewRange.Duration.Ticks * this.liveCursorViewportPosition));
            }

            if (e.PropertyName == nameof(NavigatorRange.StartTime) || e.PropertyName == nameof(NavigatorRange.EndTime))
            {
                DataManager.Instance.SetStreamValueProvidersCacheInterval(this.ViewRange.AsTimeInterval);
            }
        }
    }
}
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.

namespace Microsoft.Psi.Visualization.VisualizationObjects
{
    using System;
    using System.Collections.Generic;
    using System.Collections.ObjectModel;
    using System.Collections.Specialized;
    using System.ComponentModel;
    using System.Linq;
    using System.Runtime.Serialization;
    using System.Text;
    using System.Windows;
    using System.Windows.Controls;
    using System.Windows.Input;
    using System.Windows.Media;
    using GalaSoft.MvvmLight.CommandWpf;
    using Microsoft.Psi.Data.Annotations;
    using Microsoft.Psi.Visualization;
    using Microsoft.Psi.Visualization.Controls;
    using Microsoft.Psi.Visualization.Data;
    using Microsoft.Psi.Visualization.Helpers;
    using Microsoft.Psi.Visualization.Views.Visuals2D;
    using Microsoft.Psi.Visualization.VisualizationPanels;
    using Microsoft.Psi.Visualization.Windows;

    /// <summary>
    /// Defines which tracks to show.
    /// </summary>
    public enum ShowTracks
    {
        /// <summary>
        /// Shows all tracks.
        /// </summary>
        All,

        /// <summary>
        /// Shows all tracks with events in view.
        /// </summary>
        WithEventsInView,

        /// <summary>
        /// Shows all selected tracks.
        /// </summary>
        Selected,
    }

    /// <summary>
    /// Implements a visualization object for time interval annotations.
    /// </summary>
    [VisualizationObject("Time Interval Annotations")]
    [VisualizationPanelType(VisualizationPanelType.Timeline)]
    public class TimeIntervalAnnotationVisualizationObject : StreamIntervalVisualizationObject<TimeIntervalAnnotationSet>
    {
        private const string ErrorStreamNotBound = "The visualization object is not currently bound to a stream.";
        private const string ErrorEditingDisabled = "Annotation add/delete is currently disabled in the visualization object properties.";
        private const string ErrorSelectionMarkersUnset = "Both the start and end selection markers must be set.\r\n\r\nYou can set the start and end selection markers with SHIFT + Left mouse button and SHIFT + Right mouse button.";
        private const string ErrorOverlappingAnnotations = "Time interval annotations may not overlap.";

        private readonly Dictionary<string, int> trackIndex = new ();
        private int trackUnderMouseIndex = 0;
        private int newTrackId = 0;

        private bool allowEditAnnotationValue = true;
        private bool allowEditAnnotationBoundaries = true;
        private bool allowAddOrDeleteAnnotation = true;

        private double padding = 0;
        private double lineWidth = 2;
        private double fontSize = 10;
        private string legendValue = string.Empty;
        private ShowTracks showTracks = ShowTracks.All;
        private ObservableCollection<string> showTracksSelection = null;

        private RelayCommand<MouseButtonEventArgs> mouseLeftButtonDownCommand;
        private RelayCommand<MouseButtonEventArgs> mouseRightButtonDownCommand;
        private RelayCommand<MouseEventArgs> mouseMoveCommand;
        private RelayCommand<MouseButtonEventArgs> mouseLeftButtonUpCommand;
        private RelayCommand<MouseButtonEventArgs> mouseDoubleClickCommand;

        private TimeIntervalAnnotationDisplayData selectedDisplayDataItem = null;

        private TimeIntervalAnnotationDragInfo annotationDragInfo = null;

        /// <summary>
        /// Event that fires when an annotation value should be edited in the view.
        /// </summary>
        public event EventHandler<TimeIntervalAnnotationEditEventArgs> TimeIntervalAnnotationEdit;

        /// <summary>
        /// Event that fires when a time interval annotation drag operation is in progress.
        /// </summary>
        public event EventHandler TimeIntervalAnnotationDrag;

        private enum AnnotationEdge
        {
            None,
            Left,
            Right,
        }

        /// <summary>
        /// Gets the number of attributes in the schema.
        /// </summary>
        [Browsable(false)]
        [IgnoreDataMember]
        public int AttributeCount { get; private set; }

        /// <summary>
        /// Gets or sets the line width.
        /// </summary>
        [DataMember]
        [DisplayName("Line Width")]
        [Description("The line width for annotation events.")]
        public double LineWidth
        {
            get { return this.lineWidth; }
            set { this.Set(nameof(this.LineWidth), ref this.lineWidth, value); }
        }

        /// <summary>
        /// Gets or sets the padding.
        /// </summary>
        [DataMember]
        [DisplayName("Padding")]
        [Description("The vertical padding (in pixels) for annotation inside tracks.")]
        public double Padding
        {
            get { return this.padding; }
            set { this.Set(nameof(this.Padding), ref this.padding, value); }
        }

        /// <summary>
        /// Gets or sets the font size.
        /// </summary>
        [DataMember]
        [DisplayName("Font Size")]
        [Description("The font size.")]
        public double FontSize
        {
            get { return this.fontSize; }
            set { this.Set(nameof(this.FontSize), ref this.fontSize, value); }
        }

        /// <summary>
        /// Gets or sets a value indicating whether annotation values can be changed.
        /// </summary>
        [DataMember]
        [DisplayName("Allow Edit Annotation Values")]
        [Description("Specifies whether the annotation values can be edited.")]
        public bool AllowEditAnnotationValue
        {
            get { return this.allowEditAnnotationValue; }
            set { this.Set(nameof(this.AllowEditAnnotationValue), ref this.allowEditAnnotationValue, value); }
        }

        /// <summary>
        /// Gets or sets a value indicating whether annotation edges can be dragged.
        /// </summary>
        [DataMember]
        [DisplayName("Allow Edit Annotation Boundaries")]
        [Description("Specifies whether annotation boundaries can be edited.")]
        public bool AllowEditAnnotationBoundaries
        {
            get { return this.allowEditAnnotationBoundaries; }
            set { this.Set(nameof(this.AllowEditAnnotationBoundaries), ref this.allowEditAnnotationBoundaries, value); }
        }

        /// <summary>
        /// Gets or sets a value indicating whether annotations can be added or deleted.
        /// </summary>
        [DataMember]
        [DisplayName("Allow Create/Delete annotations")]
        [Description("Specifies whether annotations can be created/deleted.")]
        public bool AllowAddOrDeleteAnnotation
        {
            get { return this.allowAddOrDeleteAnnotation; }
            set { this.Set(nameof(this.AllowAddOrDeleteAnnotation), ref this.allowAddOrDeleteAnnotation, value); }
        }

        /// <summary>
        /// Gets or sets a value indicating which tracks to show.
        /// </summary>
        [DataMember]
        [DisplayName("Show Tracks")]
        [Description("Specifies which tracks to show.")]
        public ShowTracks ShowTracks
        {
            get { return this.showTracks; }
            set { this.Set(nameof(this.ShowTracks), ref this.showTracks, value); }
        }

        /// <summary>
        /// Gets or sets the set of tracks to display when Show Tracks is set to Selected.
        /// </summary>
        [DataMember]
        [DisplayName("Show Tracks Selection")]
        [Description("Set of tracks to display when Show Tracks is set to Selected.")]
        public ObservableCollection<string> ShowTracksSelection
        {
            get { return this.showTracksSelection; }

            set
            {
                if (this.showTracksSelection != null)
                {
                    this.showTracksSelection.CollectionChanged -= this.OnSelectedTracksCollectionChanged;
                }

                this.Set(nameof(this.showTracksSelection), ref this.showTracksSelection, value);

                if (this.showTracksSelection != null)
                {
                    this.showTracksSelection.CollectionChanged += this.OnSelectedTracksCollectionChanged;
                }
            }
        }

        /// <inheritdoc/>
        [IgnoreDataMember]
        public override Color LegendColor => Colors.White;

        /// <summary>
        /// Gets the annotation schema.
        /// </summary>
        [Browsable(false)]
        [IgnoreDataMember]
        public AnnotationSchema AnnotationSchema { get; private set; }

        /// <summary>
        /// Gets the data to be displayed in the control.
        /// </summary>
        [Browsable(false)]
        [IgnoreDataMember]
        public List<TimeIntervalAnnotationDisplayData> DisplayData { get; private set; } = new List<TimeIntervalAnnotationDisplayData>();

        /// <summary>
        /// Gets the current set of tracks.
        /// </summary>
        [Browsable(false)]
        [IgnoreDataMember]
        public IEnumerable<string> Tracks => this.trackIndex.Keys;

        /// <summary>
        /// Gets the current number of tracks.
        /// </summary>
        [Browsable(false)]
        [IgnoreDataMember]
        public int TrackCount => this.trackIndex.Count;

        /// <summary>
        /// Gets or sets the index of the track under the mouse.
        /// </summary>
        [Browsable(false)]
        [IgnoreDataMember]
        public int TrackUnderMouseIndex
        {
            get { return this.trackUnderMouseIndex; }
            set { this.Set(nameof(this.TrackUnderMouseIndex), ref this.trackUnderMouseIndex, value); }
        }

        /// <inheritdoc/>
        public override string LegendValue => this.legendValue;

        /// <inheritdoc/>
        public override bool RequiresSupplementalMetadata => true;

        /// <inheritdoc/>
        public override string IconSource => (this.StreamSource != null) ? IconSourcePath.Annotation : IconSourcePath.AnnotationUnbound;

        /// <summary>
        /// Gets the mouse left button down command.
        /// </summary>
        [Browsable(false)]
        public RelayCommand<MouseButtonEventArgs> MouseLeftButtonDownCommand
            => this.mouseLeftButtonDownCommand ??= new RelayCommand<MouseButtonEventArgs>(e => this.DoMouseLeftButtonDown(e));

        /// <summary>
        /// Gets the mouse right button down command.
        /// </summary>
        [Browsable(false)]
        public RelayCommand<MouseButtonEventArgs> MouseRightButtonDownCommand
            => this.mouseRightButtonDownCommand ??= new RelayCommand<MouseButtonEventArgs>(e => this.DoMouseRightButtonDown(e));

        /// <summary>
        /// Gets the mouse move command.
        /// </summary>
        [Browsable(false)]
        public RelayCommand<MouseEventArgs> MouseMoveCommand
            => this.mouseMoveCommand ??= new RelayCommand<MouseEventArgs>(e => this.DoMouseMove(e));

        /// <summary>
        /// Gets the mouse left button up command.
        /// </summary>
        [Browsable(false)]
        public RelayCommand<MouseButtonEventArgs> MouseLeftButtonUpCommand
            => this.mouseLeftButtonUpCommand ??= new RelayCommand<MouseButtonEventArgs>(e => this.DoMouseLeftButtonUp(e));

        /// <summary>
        /// Gets the mouse double-click command.
        /// </summary>
        [Browsable(false)]
        public RelayCommand<MouseButtonEventArgs> MouseDoubleClickCommand
            => this.mouseDoubleClickCommand ??= new RelayCommand<MouseButtonEventArgs>(e => this.DoMouseDoubleClick(e));

        /// <inheritdoc />
        [Browsable(false)]
        [IgnoreDataMember]
        public override DataTemplate DefaultViewTemplate => XamlHelper.CreateTemplate(this.GetType(), typeof(TimeIntervalAnnotationVisualizationObjectView));

        /// <summary>
        /// Gets the timeline visualization panel.
        /// </summary>
        [Browsable(false)]
        [IgnoreDataMember]
        public TimelineVisualizationPanel TimelineVisualizationPanel => this.Panel as TimelineVisualizationPanel;

        /// <inheritdoc/>
        public override List<ContextMenuItemInfo> ContextMenuItemsInfo()
        {
            var items = new List<ContextMenuItemInfo>();

            // Get the track under the cursor
            var trackUnderCursor = this.GetTrackByIndex(this.TrackUnderMouseIndex);

            // Add the add annotation (on current track) context menu item
            var newAnnotationOnCurrentTrackCommand = this.GetCreateAnnotationOnTrackCommand(trackUnderCursor);
            items.Add(
                new ContextMenuItemInfo(
                    IconSourcePath.Annotation,
                    "New Annotation",
                    newAnnotationOnCurrentTrackCommand,
                    isEnabled: newAnnotationOnCurrentTrackCommand != null));

            // Add the add annotation on new track context menu item
            var newAnnotationOnNewTrackCommand = this.GetNewAnnotationOnNewTrackCommand();
            items.Add(
                new ContextMenuItemInfo(
                    null,
                    "New Annotation on New Track",
                    newAnnotationOnNewTrackCommand,
                    isEnabled: newAnnotationOnNewTrackCommand != null));

            // Add the set annotation time interval to selection boundaries menu item
            var setAnnotationTimeIntervalToSelectionCommand = this.GetSetAnnotationTimeIntervalToSelectionCommand(this.Navigator.Cursor, trackUnderCursor);
            items.Add(
                new ContextMenuItemInfo(
                    IconSourcePath.SetAnnotationToSelection,
                    "Adjust Annotation to Selection Boundaries",
                    setAnnotationTimeIntervalToSelectionCommand,
                    isEnabled: setAnnotationTimeIntervalToSelectionCommand != null));

            // Add the delete annotation context menu item
            var deleteCommand = this.GetDeleteAnnotationOnTrackCommand(this.Navigator.Cursor, trackUnderCursor);
            items.Add(
                new ContextMenuItemInfo(
                    null,
                    "Delete Annotation",
                    deleteCommand,
                    isEnabled: deleteCommand != null));

            // Add the delete annotation context menu item
            var deleteAllAnnotationsOnTrackCommand = this.GetDeleteAllAnnotationsOnTrackCommand(trackUnderCursor);
            items.Add(
                new ContextMenuItemInfo(
                    null,
                    $"Delete All Annotations on Curent Track ({trackUnderCursor})",
                    deleteAllAnnotationsOnTrackCommand,
                    isEnabled: deleteAllAnnotationsOnTrackCommand != null));

            // Add the delete annotation context menu item
            var renameCurrentTrackCommand = this.GetRenameTrackCommand(trackUnderCursor);
            items.Add(
                new ContextMenuItemInfo(
                    null,
                    $"Rename Curent Track ({trackUnderCursor})",
                    renameCurrentTrackCommand,
                    isEnabled: renameCurrentTrackCommand != null));

            // Add the command to show all tracks
            var showAllTracksCommand = this.GetShowAllTracksCommand();
            if (showAllTracksCommand != null)
            {
                items.Add(new ContextMenuItemInfo(null, $"Show All Tracks", showAllTracksCommand));
            }

            // Add the command to show only tracks with events in view
            var showOnlyTracksWithEventsInViewCommand = this.GetShowOnlyTracksWithEventsInViewCommand();
            if (showOnlyTracksWithEventsInViewCommand != null)
            {
                items.Add(new ContextMenuItemInfo(null, $"Show Only Tracks with Events in View", showOnlyTracksWithEventsInViewCommand));
            }

            // Add a separator
            items.Add(null);

            // Add the base visualization object commands
            items.AddRange(base.ContextMenuItemsInfo());

            return items;
        }

        /// <summary>
        /// Update the specified annotation set message.
        /// </summary>
        /// <param name="annotationSetMessage">The annotation set message to update.</param>
        public void UpdateAnnotationSetMessage(Message<TimeIntervalAnnotationSet> annotationSetMessage)
        {
            // Create the replace update
            var updates = new StreamUpdate<TimeIntervalAnnotationSet>[] { new (StreamUpdateType.Replace, annotationSetMessage) };

            // Send it to the cache
            DataManager.Instance.UpdateStream(this.StreamSource, updates);
        }

        /// <summary>
        /// Gets the command for adding a new annotation on a specified track.
        /// </summary>
        /// <param name="track">The track to add the annotation on.</param>
        /// <returns>The command for adding a new annotation to the specified track.</returns>
        public ICommand GetCreateAnnotationOnTrackCommand(string track)
        {
            if (track == null)
            {
                return null;
            }

            // All of the following must be true to allow an annotation to be added:
            //
            // 1) We must be bound to a source
            // 2) Add/Delete annotations must be enabled.
            // 3) Both selection markers must be set.
            // 4) There must be no annotations between the selection markers.
            //
            // If one of these conditions does not hold, display an error message
            var selectionTimeInterval = this.Navigator.SelectionRange.AsTimeInterval;

            if (!this.IsBound)
            {
                return this.CreateEditAnnotationErrorCommand(ErrorStreamNotBound);
            }

            if (!this.AllowAddOrDeleteAnnotation)
            {
                return this.CreateEditAnnotationErrorCommand(ErrorEditingDisabled);
            }

            if ((selectionTimeInterval.Left <= DateTime.MinValue) || (selectionTimeInterval.Right >= DateTime.MaxValue))
            {
                return this.CreateEditAnnotationErrorCommand(ErrorSelectionMarkersUnset);
            }

            if (this.Data != null && this.Data.Select(m => m.Data).GetAnnotationTimeIntervalOverlappingWith(track, selectionTimeInterval, out var _))
            {
                return this.CreateEditAnnotationErrorCommand(ErrorOverlappingAnnotations);
            }

            return new PsiCommand(() => this.CreateAnnotation(selectionTimeInterval, track));
        }

        /// <summary>
        /// Gets the command for adding a specified annotation.
        /// </summary>
        /// <param name="annotation">The annotation to add.</param>
        /// <returns>The command for adding an annotation to the specified track.</returns>
        public ICommand GetAddAnnotationCommand(TimeIntervalAnnotation annotation)
        {
            if (annotation == null)
            {
                return null;
            }

            // All of the following must be true to allow an annotation to be added:
            //
            // 1) We must be bound to a source
            // 2) Add/Delete annotations must be enabled.
            // 3) Both selection markers must be set.
            // 4) There must be no annotations between the selection markers.
            //
            // If one of these conditions does not hold, display an error message
            if (!this.IsBound)
            {
                return this.CreateEditAnnotationErrorCommand(ErrorStreamNotBound);
            }

            if (!this.AllowAddOrDeleteAnnotation)
            {
                return this.CreateEditAnnotationErrorCommand(ErrorEditingDisabled);
            }

            if ((annotation.Interval.Left <= DateTime.MinValue) || (annotation.Interval.Right >= DateTime.MaxValue))
            {
                return this.CreateEditAnnotationErrorCommand(ErrorSelectionMarkersUnset);
            }

            if (this.Data != null && this.Data.Select(m => m.Data).GetAnnotationTimeIntervalOverlappingWith(annotation.Track, annotation.Interval, out var _))
            {
                return this.CreateEditAnnotationErrorCommand(ErrorOverlappingAnnotations);
            }

            return new PsiCommand(() => this.AddAnnotation(annotation));
        }

        /// <summary>
        /// Gets the command for adding an annotation to a new track.
        /// </summary>
        /// <returns>The command for adding an annotation to a new track.</returns>
        public ICommand GetNewAnnotationOnNewTrackCommand()
        {
            // All of the following must be true to allow an annotation to be added:
            //
            // 1) We must be bound to a source
            // 2) Add/Delete annotations must be enabled.
            // 3) Both selection markers must be set.
            //
            // If one of these conditions does not hold, display an error message
            var selectionTimeInterval = this.Container.Navigator.SelectionRange.AsTimeInterval;

            if (!this.IsBound)
            {
                return this.CreateEditAnnotationErrorCommand(ErrorStreamNotBound);
            }

            if (!this.AllowAddOrDeleteAnnotation)
            {
                return this.CreateEditAnnotationErrorCommand(ErrorEditingDisabled);
            }

            if ((selectionTimeInterval.Left <= DateTime.MinValue) || (selectionTimeInterval.Right >= DateTime.MaxValue))
            {
                return this.CreateEditAnnotationErrorCommand(ErrorSelectionMarkersUnset);
            }

            return new PsiCommand(() => this.CreateAnnotation(selectionTimeInterval, null));
        }

        /// <summary>
        /// Gets the command for setting the annotation time interval to the selection boundaries.
        /// </summary>
        /// <param name="cursorTime">The cursor time.</param>
        /// <param name="track">The track name.</param>
        /// <returns>The command for setting the annotation time interval to the selection boundaries.</returns>
        public ICommand GetSetAnnotationTimeIntervalToSelectionCommand(DateTime cursorTime, string track)
{
            if (this.Navigator.SelectionRange.StartTime == DateTime.MinValue || this.Navigator.SelectionRange.EndTime == DateTime.MaxValue)
            {
                return null;
            }

            // All of the following must be true to edit the annotation boundaries:
            //
            // 1) We must be bound to a source
            // 2) EditAnnotationBoundaries annotations must be enabled.
            // 3) The mouse cursor must be above an existing annotation.
            // 4) There must be no other annotation on the specified track between
            //    the selection markers.
            var selectionTimeInterval = this.Container.Navigator.SelectionRange.AsTimeInterval;

            if (!this.IsBound)
            {
                return this.CreateEditAnnotationErrorCommand(ErrorStreamNotBound);
            }

            if (!this.AllowEditAnnotationBoundaries)
            {
                return this.CreateEditAnnotationErrorCommand(ErrorEditingDisabled);
            }

            // Get the annotation under the cursor on the specified track
            var annotation = this.Data?.Select(m => m.Data).GetTimeIntervalAnnotationAtTime(cursorTime, track);

            if (annotation == null)
            {
                return null;
            }

            // Check if there is a different annotation overlapping on the same track
            if (this.Data != null && this.Data.Select(m => m.Data).GetAnnotationTimeIntervalOverlappingWith(track, selectionTimeInterval, out var intersectingTimeInterval))
            {
                if ((intersectingTimeInterval.Left != annotation.Interval.Left) ||
                    (intersectingTimeInterval.Right != annotation.Interval.Right))
                {
                    return this.CreateEditAnnotationErrorCommand(ErrorOverlappingAnnotations);
                }
            }

            return new PsiCommand(() => this.EditAnnotationTimeInterval(annotation, this.Navigator.SelectionRange.AsTimeInterval));
        }

        /// <summary>
        /// Gets the command for deleting the annotation on a specified track.
        /// </summary>
        /// <param name="cursorTime">The cursor time.</param>
        /// <param name="track">The track name.</param>
        /// <returns>The command for deleting the annotation on a specified track.</returns>
        public ICommand GetDeleteAnnotationOnTrackCommand(DateTime cursorTime, string track)
        {
            if (track == null)
            {
                return null;
            }

            // All of the following must be true to delete an annotation:
            //
            // 1) We must be bound to a source
            // 2) Add/Delete annotations must be enabled.
            // 3) The mouse cursor must be above an existing annotation.
            var selectionTimeInterval = this.Container.Navigator.SelectionRange.AsTimeInterval;

            if (!this.IsBound)
            {
                return this.CreateEditAnnotationErrorCommand(ErrorStreamNotBound);
            }

            if (!this.AllowAddOrDeleteAnnotation)
            {
                return this.CreateEditAnnotationErrorCommand(ErrorEditingDisabled);
            }

            // Get the index of the annotation under the cursor on the specified track
            var annotation = this.Data?.Select(m => m.Data).GetTimeIntervalAnnotationAtTime(cursorTime, track);
            return (annotation != null) ? new PsiCommand(() => this.DeleteAnnotation(annotation)) : null;
        }

        /// <summary>
        /// Gets the command for deleting all annotation on a specified track.
        /// </summary>
        /// <param name="track">The track name.</param>
        /// <returns>The command for deleting all annotations on a specified track.</returns>
        public ICommand GetDeleteAllAnnotationsOnTrackCommand(string track)
        {
            if (this.Data.Any(m => m.Data.Tracks.Contains(track)))
            {
                return new PsiCommand(() => this.DeleteAllAnnotationsOnTrack(track));
            }
            else
            {
                return null;
            }
        }

        /// <summary>
        /// Gets the command for renaming a specified track.
        /// </summary>
        /// <param name="track">The track name.</param>
        /// <returns>The command for renaming a specified track.</returns>
        public ICommand GetRenameTrackCommand(string track)
            => new PsiCommand(() => this.RenameTrack(track));

        /// <summary>
        /// Gets the command to show all tracks.
        /// </summary>
        /// <returns>The command to show all tracks.</returns>
        public ICommand GetShowAllTracksCommand()
            => this.ShowTracks != ShowTracks.All ? new PsiCommand(() => this.ShowTracks = ShowTracks.All) : null;

        /// <summary>
        /// Gets the command to show only tracks with events in view.
        /// </summary>
        /// <returns>The command to show tracks with events in view.</returns>
        public ICommand GetShowOnlyTracksWithEventsInViewCommand()
            => this.ShowTracks != ShowTracks.WithEventsInView ? new PsiCommand(() => this.ShowTracks = ShowTracks.WithEventsInView) : null;

        /// <summary>
        /// Gets the name of a track with the specified index.
        /// </summary>
        /// <param name="index">The track index.</param>
        /// <returns>The name of the track with the specified index.</returns>
        public string GetTrackByIndex(int index) => this.trackIndex.FirstOrDefault(kvp => kvp.Value == index).Key;

        /// <inheritdoc/>
        protected override (DateTime StartTime, DateTime EndTime) GetTimeInterval() => (this.Navigator.DataRange.StartTime, this.Navigator.DataRange.EndTime);

        /// <inheritdoc/>
        protected override void OnStreamBound()
        {
            base.OnStreamBound();
            this.AnnotationSchema = DataManager.Instance.GetSupplementalMetadata<AnnotationSchema>(this.StreamSource);

            if (this.AnnotationSchema == null)
            {
                throw new Exception("Cannot find annotation schema.");
            }

            this.UpdateDisplayData();
            this.GenerateLegendValue();
        }

        /// <inheritdoc/>
        protected override void OnStreamUnbound()
        {
            base.OnStreamUnbound();
            this.AnnotationSchema = null;
        }

        /// <inheritdoc />
        protected override void OnDataCollectionChanged(NotifyCollectionChangedEventArgs e)
        {
            this.UpdateDisplayData();
            base.OnDataCollectionChanged(e);
        }

        /// <inheritdoc/>
        protected override void OnPropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            base.OnPropertyChanged(sender, e);

            if (e.PropertyName == nameof(this.ShowTracks))
            {
                this.UpdateDisplayData();
            }
        }

        /// <inheritdoc/>
        protected override void OnViewRangeChanged(object sender, Navigation.NavigatorTimeRangeChangedEventArgs e)
        {
            base.OnViewRangeChanged(sender, e);

            if (this.ShowTracks == ShowTracks.WithEventsInView)
            {
                this.UpdateDisplayData();
            }
        }

        private PsiCommand CreateEditAnnotationErrorCommand(string errorMessage)
            => new (() => new MessageBoxWindow(Application.Current.MainWindow, "Error Editing Annotation", errorMessage, "Close", null).ShowDialog());

        /// <summary>
        /// Creates a new annotation on a specified track.
        /// </summary>
        /// <param name="timeInterval">The time interval for the annotation.</param>
        /// <param name="track">The track for the annotation. If null, a new track is generated.</param>
        private void CreateAnnotation(TimeInterval timeInterval, string track)
        {
            // Create a new track if one is not specified
            track ??= this.CreateNewTrack();

            // Create the annotation using the annotation schema
            var annotation = this.AnnotationSchema.CreateDefaultTimeIntervalAnnotation(timeInterval, track);

            // Now find if we need to alter an existing annotation set, or create a new one
            var existingAnnotationSetMessage = this.Data.FirstOrDefault(m => m.OriginatingTime == annotation.Interval.Right);
            if (existingAnnotationSetMessage != default)
            {
                existingAnnotationSetMessage.Data.AddAnnotation(annotation);
                var streamUpdate = new StreamUpdate<TimeIntervalAnnotationSet>[] { new (StreamUpdateType.Replace, existingAnnotationSetMessage) };
                DataManager.Instance.UpdateStream(this.StreamSource, streamUpdate);
            }
            else
            {
                // Create a message for the annotation
                var annotationSet = new TimeIntervalAnnotationSet(annotation);
                var newAnnotationSetMessage = new Message<TimeIntervalAnnotationSet>(annotationSet, annotation.Interval.Right, annotation.Interval.Right, 0, 0);
                var streamUpdate = new StreamUpdate<TimeIntervalAnnotationSet>[] { new (StreamUpdateType.Add, newAnnotationSetMessage) };
                DataManager.Instance.UpdateStream(this.StreamSource, streamUpdate);
            }

            // Update the data
            this.UpdateDisplayData();

            // Display the properties of the new annotation
            this.SelectAnnotation(annotation);
        }

        /// <summary>
        /// Adds a specified annotation.
        /// </summary>
        /// <param name="annotation">The annotation to add.</param>
        private void AddAnnotation(TimeIntervalAnnotation annotation)
        {
            // Now find if we need to alter an existing annotation set, or create a new one
            var existingAnnotationSetMessage = this.Data.FirstOrDefault(m => m.OriginatingTime == annotation.Interval.Right);
            if (existingAnnotationSetMessage != default)
            {
                existingAnnotationSetMessage.Data.AddAnnotation(annotation);
                var streamUpdate = new StreamUpdate<TimeIntervalAnnotationSet>[] { new (StreamUpdateType.Replace, existingAnnotationSetMessage) };
                DataManager.Instance.UpdateStream(this.StreamSource, streamUpdate);
            }
            else
            {
                // Create a message for the annotation
                var annotationSet = new TimeIntervalAnnotationSet(annotation);
                var newAnnotationSetMessage = new Message<TimeIntervalAnnotationSet>(annotationSet, annotation.Interval.Right, annotation.Interval.Right, 0, 0);
                var streamUpdate = new StreamUpdate<TimeIntervalAnnotationSet>[] { new (StreamUpdateType.Add, newAnnotationSetMessage) };
                DataManager.Instance.UpdateStream(this.StreamSource, streamUpdate);
            }

            // Update the data
            this.UpdateDisplayData();

            // Display the properties of the new annotation
            this.SelectAnnotation(annotation);
        }

        /// <summary>
        /// Deletes an existing annotation.
        /// </summary>
        /// <param name="annotation">The annotation to delete.</param>
        private void DeleteAnnotation(TimeIntervalAnnotation annotation)
        {
            // Find the corresponding annotation set message
            var annotationSetMessage = this.FindTimeIntervalAnnotationSetMessageContaining(annotation);

            // If the annotation is currently selected, then deselect it
            if (this.selectedDisplayDataItem != null && this.selectedDisplayDataItem.Annotation == annotation)
            {
                this.SelectAnnotation(null);
            }

            // Create the list of stream updates
            var updates = new List<StreamUpdate<TimeIntervalAnnotationSet>>();

            // If this is the last annotation in the set
            if (annotationSetMessage.Data.Tracks.Count() == 1)
            {
                // then delete
                updates.Add(new StreamUpdate<TimeIntervalAnnotationSet>(StreamUpdateType.Delete, annotationSetMessage));
            }
            else
            {
                // otherwise remove the annotation from the annotation set
                annotationSetMessage.Data.RemoveAnnotation(annotation.Track);

                // and update the annotation set
                updates.Add(new StreamUpdate<TimeIntervalAnnotationSet>(StreamUpdateType.Replace, annotationSetMessage));
            }

            // Update the stream
            DataManager.Instance.UpdateStream(this.StreamSource, updates);

            // Update the view
            this.UpdateDisplayData();
        }

        /// <summary>
        /// Edits the boundaries of an existing annotation.
        /// </summary>
        /// <param name="annotation">The annotation to edit.</param>
        /// <param name="timeInterval">The new time interval for the annotation.</param>
        private void EditAnnotationTimeInterval(TimeIntervalAnnotation annotation, TimeInterval timeInterval)
        {
            // Find the corresponding annotation set message
            var annotationSetMessage = this.FindTimeIntervalAnnotationSetMessageContaining(annotation);

            // If the annotation is currently selected, then deselect it
            if (this.selectedDisplayDataItem != null && this.selectedDisplayDataItem.Annotation == annotation)
            {
                this.SelectAnnotation(null);
            }

            // Create the list of stream updates
            var updates = new List<StreamUpdate<TimeIntervalAnnotationSet>>();

            // Set the time interval
            annotation.Interval = timeInterval.DeepClone();

            // and update the annotation set
            updates.Add(new StreamUpdate<TimeIntervalAnnotationSet>(StreamUpdateType.Replace, annotationSetMessage));

            // Update the stream
            DataManager.Instance.UpdateStream(this.StreamSource, updates);

            // Update the view
            this.UpdateDisplayData();
        }

        /// <summary>
        /// Delete all annotations on a specified track.
        /// </summary>
        /// <param name="track">The name of the track.</param>
        private void DeleteAllAnnotationsOnTrack(string track)
        {
            // If the currently selected annotation is on the specified track, then deselect it
            if (this.selectedDisplayDataItem != null && this.selectedDisplayDataItem.Annotation.Track == track)
            {
                this.SelectAnnotation(null);
            }

            // Create the list of stream updates
            var updates = new List<StreamUpdate<TimeIntervalAnnotationSet>>();

            // Go through all the annotation sets
            foreach (var annotationSet in this.Data)
            {
                if (annotationSet.Data.Tracks.Contains(track))
                {
                    // If there is a single annotation in the set
                    if (annotationSet.Data.Tracks.Count() == 1)
                    {
                        // Then delete the whole annotation set
                        updates.Add(new (StreamUpdateType.Delete, annotationSet));
                    }
                    else
                    {
                        // O/w remove the annotation from the set
                        annotationSet.Data.RemoveAnnotation(track);
                        updates.Add(new (StreamUpdateType.Replace, annotationSet));
                    }
                }
            }

            // Update the stream
            DataManager.Instance.UpdateStream(this.StreamSource, updates);

            // Update the view
            this.UpdateDisplayData();
        }

        /// <summary>
        /// Rename a specified track.
        /// </summary>
        /// <param name="track">The name of the track.</param>
        private void RenameTrack(string track)
        {
            var dlg = new GetParameterWindow(Application.Current.MainWindow, "Rename Track", "New Track Name", track);
            if (dlg.ShowDialog() == true)
            {
                var newTrackName = dlg.ParameterValue;

                // If the currently selected annotation is on the specified track, then deselect it
                if (this.selectedDisplayDataItem != null && this.selectedDisplayDataItem.Annotation.Track == track)
                {
                    this.SelectAnnotation(null);
                }

                // Create the list of stream updates
                var updates = new List<StreamUpdate<TimeIntervalAnnotationSet>>();

                // Go through all the annotation sets
                foreach (var annotationSet in this.Data)
                {
                    if (annotationSet.Data.Tracks.Contains(track))
                    {
                        annotationSet.Data.AddAnnotation(new TimeIntervalAnnotation(
                            annotationSet.Data[track].Interval,
                            newTrackName,
                            annotationSet.Data[track].AttributeValues));

                        annotationSet.Data.RemoveAnnotation(track);

                        updates.Add(new (StreamUpdateType.Replace, annotationSet));
                    }
                }

                // Update the stream
                DataManager.Instance.UpdateStream(this.StreamSource, updates);

                // Update the view
                this.UpdateDisplayData();
            }
        }

        private void UpdateDisplayData()
        {
            // Rebuild the data
            this.RaisePropertyChanging(nameof(this.DisplayData));
            this.DisplayData.Clear();

            if (this.IsBound && this.Data != null)
            {
                // Get the view interval
                var viewInterval = new TimeInterval(this.Navigator.ViewRange.StartTime, this.Navigator.ViewRange.EndTime);

                // Then recompute the track index
                this.trackIndex.Clear();

                // Go through the messages and figure out all the tracks and which ones have events in view
                var tracks = new Dictionary<string, bool>();
                foreach (var annotationsMessage in this.Data)
                {
                    foreach (var track in annotationsMessage.Data.Tracks)
                    {
                        var eventInView = annotationsMessage.Data[track].Interval.IntersectsWith(viewInterval);
                        if (!tracks.ContainsKey(track))
                        {
                            tracks.Add(track, eventInView);
                        }
                        else if (eventInView)
                        {
                            tracks[track] = true;
                        }
                    }
                }

                // If we are showing tracks with events in view
                if (this.ShowTracks == ShowTracks.WithEventsInView)
                {
                    // Sort the tracks that have events in view alphabetically
                    this.ShowTracksSelection = new ObservableCollection<string>(tracks.Keys.Where(t => tracks[t]).OrderBy(x => x).ToList());
                }
                else if (this.ShowTracks == ShowTracks.All)
                {
                    // O/w if we are showing all tracks sort the tracks alphabetically
                    this.ShowTracksSelection = new ObservableCollection<string>(tracks.Keys.OrderBy(x => x).ToList());
                }

                // Now build the tracks index from the selected tracks
                foreach (var track in this.ShowTracksSelection)
                {
                    this.trackIndex.Add(track, this.trackIndex.Count);
                }
            }

            // If no tracks exist, initialize a new track
            if (this.trackIndex.Count == 0)
            {
                this.CreateNewTrack();
            }

            // Now compute the display data
            if (this.IsBound && this.Data != null && this.AnnotationSchema != null)
            {
                // Get the view interval
                var viewInterval = new TimeInterval(this.Navigator.ViewRange.StartTime, this.Navigator.ViewRange.EndTime);

                // Finally, reconstruct the items to be displayed
                foreach (var annotationsMessage in this.Data)
                {
                    foreach (var track in annotationsMessage.Data.Tracks)
                    {
                        if (this.ShowTracks == ShowTracks.All ||
                            (this.ShowTracks == ShowTracks.WithEventsInView && annotationsMessage.Data[track].Interval.IntersectsWith(viewInterval)) ||
                            (this.ShowTracks == ShowTracks.Selected && this.trackIndex.ContainsKey(track)))
                        {
                            this.DisplayData.Add(new TimeIntervalAnnotationDisplayData(this, annotationsMessage, track, this.trackIndex[track], this.AnnotationSchema));
                        }
                    }
                }
            }

            this.RaisePropertyChanged(nameof(this.DisplayData));
        }

        private void DoMouseDoubleClick(MouseButtonEventArgs e)
        {
            System.Diagnostics.Debug.WriteLine("DoMouseDoubleClick");

            // First figure out the canvas and get the track under the mouse cursor.
            var canvas = this.FindCanvas(e.Source);
            var trackUnderMouse = this.GetTrackUnderMouse(e, canvas);

            // Get the time at the mouse cursor
            var cursorTime = (this.Panel as TimelineVisualizationPanel).GetTimeAtMousePointer(e, false);

            // Get the annotation (if any) that straddles this time
            var annotation = this.Data?.Select(m => m.Data).GetTimeIntervalAnnotationAtTime(cursorTime, trackUnderMouse);
            if (annotation != null)
            {
                // Set the navigator selection to the bounds of the annotation
                this.Navigator.SelectionRange.Set(annotation.Interval);

                // If the shift key was down, then also zoom to the annotation (with 10% empty space to the left and right)
                if (Keyboard.IsKeyDown(Key.LeftShift) || Keyboard.IsKeyDown(Key.RightShift))
                {
                    double bufferSeconds = annotation.Interval.Span.TotalSeconds * 0.1d;
                    this.Navigator.ViewRange.Set(annotation.Interval.Left.AddSeconds(-bufferSeconds), annotation.Interval.Right.AddSeconds(bufferSeconds));
                }
            }
        }

        private void DoMouseLeftButtonDown(MouseButtonEventArgs e)
        {
            // First figure out the canvas and get the track under the mouse cursor.
            var canvas = this.FindCanvas(e.Source);
            var trackUnderMouse = this.GetTrackUnderMouse(e, canvas);

            // Get the time at the mouse cursor
            var cursorTime = this.TimelineVisualizationPanel.GetTimeAtMousePointer(e, false);

            // Get the track index to be edited based on the mouse position, and also the attributeIndex
            var timelineScroller = this.TimelineVisualizationPanel.GetTimelineScroller(e.Source);

            var annotationSetMessage = default(Message<TimeIntervalAnnotationSet>);
            var annotationSetMessageIndex = -1;
            var annotationEdge = AnnotationEdge.None;

            // Get the annotation (if any) that straddles this time
            var annotation = this.Data?.Select(m => m.Data).GetTimeIntervalAnnotationAtTime(cursorTime, trackUnderMouse);

            if (annotation != null && trackUnderMouse != null)
            {
                // Set the corresponding annotation set message
                annotationSetMessage = this.FindTimeIntervalAnnotationSetMessageContaining(annotation);

                // Set the corresponding annotation set message index
                annotationSetMessageIndex = this.Data.IndexOf(annotationSetMessage);

                // Check if the mouse is over an edge of the annotation
                annotationEdge = this.GetMouseOverAnnotationEdge(cursorTime, annotation, timelineScroller);
            }

            // If the shift key is down, the user is dropping the start selection marker. If there is no VO currently being snapped
            // to and the mouse is over an annotation edge, then manually set the selection marker right on the edge. Otherwise
            // let the event bubble up to the timeline visualization panel which will set the selection marker in the usual fashion.
            if (Keyboard.IsKeyDown(Key.LeftShift) || Keyboard.IsKeyDown(Key.RightShift))
            {
                if ((this.Container.SnapToVisualizationObject == null) && (annotationEdge != AnnotationEdge.None))
                {
                    var selectionMarkerTime = annotationEdge == AnnotationEdge.Left ? annotation.Interval.Left : annotation.Interval.Right;
                    this.Navigator.SelectionRange.Set(selectionMarkerTime, this.Navigator.SelectionRange.EndTime >= selectionMarkerTime ? this.Navigator.SelectionRange.EndTime : DateTime.MaxValue);
                    e.Handled = true;
                }
                else
                {
                    return;
                }
            }

            // If we're over an annotation
            if (annotation != default)
            {
                if (annotationEdge == AnnotationEdge.None)
                {
                    // We're over an annotation, but not an annotation edge, display the annotation's properties
                    this.SelectAnnotation(annotation);

                    // Begin annotation edit if it's enabled
                    if (this.AllowEditAnnotationValue)
                    {
                        // Compute the attribute index
                        var point = e.GetPosition(timelineScroller);
                        var trackActualHeight = timelineScroller.ActualHeight / this.TrackCount;
                        var attributeActualHeight = trackActualHeight / this.AttributeCount;
                        var attributeIndex = (int)((point.Y - this.trackIndex[trackUnderMouse] * trackActualHeight) / attributeActualHeight);

                        // Find the display data object corresponding to the annotation and fire an edit event to the view
                        var displayData = this.DisplayData.FirstOrDefault(d => d.Annotation.Track == trackUnderMouse && d.Annotation.Interval.Right == annotation.Interval.Right);
                        this.TimeIntervalAnnotationEdit?.Invoke(this, new TimeIntervalAnnotationEditEventArgs(displayData, attributeIndex));
                    }
                }

                // Check if we're over an edge and annotation drag is enabled.
                if (annotationEdge != AnnotationEdge.None && this.AllowEditAnnotationBoundaries)
                {
                    // Get the previous and next annotation sets containing an annotation for the current track (if any)
                    // and check if they abut the annotation whose edge we're going to drag
                    Message<TimeIntervalAnnotationSet> previousAnnotationSet = this.Data.Take(annotationSetMessageIndex).LastOrDefault(a => a.Data.ContainsTrack(annotation.Track));
                    Message<TimeIntervalAnnotationSet>? previousAnnotationSetNullable = previousAnnotationSet != default ? previousAnnotationSet : default;
                    bool previousAnnotationSetFound = previousAnnotationSet != default;
                    bool previousAnnotationAbuts = previousAnnotationSetFound && previousAnnotationSet.Data[annotation.Track].Interval.Right == annotation.Interval.Left;

                    Message<TimeIntervalAnnotationSet> nextAnnotationSet = this.Data.Skip(annotationSetMessageIndex + 1).FirstOrDefault(a => a.Data.ContainsTrack(annotation.Track));
                    Message<TimeIntervalAnnotationSet>? nextAnnotationSetNullable = nextAnnotationSet != default ? nextAnnotationSet : default;
                    bool nextAnnotationSetFound = nextAnnotationSet != default;
                    bool nextAnnotationAbuts = nextAnnotationSetFound && nextAnnotationSet.Data[annotation.Track].Interval.Left == annotation.Interval.Right;

                    // If the ALT key is down, then we will not try to move the annotation that abuts this one
                    bool moveNeighborAnnotation = !Keyboard.IsKeyDown(Key.LeftAlt) && !Keyboard.IsKeyDown(Key.RightAlt);

                    // Work out the minimum and maximum times we can drag the annotation's edge to
                    var minTime = default(DateTime);
                    var maxTime = default(DateTime);
                    if (annotationEdge == AnnotationEdge.Left)
                    {
                        maxTime = annotation.Interval.Right;
                        if (!previousAnnotationSetFound)
                        {
                            minTime = this.Navigator.ViewRange.StartTime;
                        }
                        else if (previousAnnotationAbuts && moveNeighborAnnotation)
                        {
                            minTime = previousAnnotationSet.Data[annotation.Track].Interval.Left;
                        }
                        else
                        {
                            minTime = previousAnnotationSet.Data[annotation.Track].Interval.Right;
                        }
                    }
                    else
                    {
                        minTime = annotation.Interval.Left;
                        if (!nextAnnotationSetFound)
                        {
                            maxTime = this.Navigator.ViewRange.EndTime;
                        }
                        else if (nextAnnotationAbuts && moveNeighborAnnotation)
                        {
                            maxTime = nextAnnotationSet.Data[annotation.Track].Interval.Right;
                        }
                        else
                        {
                            maxTime = nextAnnotationSet.Data[annotation.Track].Interval.Left;
                        }
                    }

                    // Create the drag data that specifies which annotation(s) to drag, and the minimum and maximum time we can drag to
                    if (annotationEdge == AnnotationEdge.Right)
                    {
                        this.annotationDragInfo = new TimeIntervalAnnotationDragInfo(
                            annotation.Track,
                            annotationSetMessage,
                            moveNeighborAnnotation && nextAnnotationAbuts ? nextAnnotationSetNullable : null,
                            minTime,
                            maxTime);
                    }
                    else
                    {
                        this.annotationDragInfo = new TimeIntervalAnnotationDragInfo(
                            annotation.Track,
                            moveNeighborAnnotation && previousAnnotationAbuts ? previousAnnotationSetNullable : null,
                            annotationSetMessage,
                            minTime,
                            maxTime);
                    }

                    // Signal that we are doing a drag operation
                    this.TimeIntervalAnnotationDrag?.Invoke(this, default);
                }
            }
            else
            {
                // We're not over any annotation, cancel any current edit operation in the view and display the VO's properties
                this.TimeIntervalAnnotationEdit?.Invoke(this, new TimeIntervalAnnotationEditEventArgs(null, 0));
                this.SelectAnnotation(null);
            }
        }

        private void DoMouseRightButtonDown(MouseButtonEventArgs e)
        {
            // First figure out the canvas and get the track under the mouse cursor.
            var canvas = this.FindCanvas(e.Source);
            var trackUnderMouse = this.GetTrackUnderMouse(e, canvas);

            // Get the time at the mouse cursor
            DateTime cursorTime = this.TimelineVisualizationPanel.GetTimeAtMousePointer(e, false);

            var annotationEdge = AnnotationEdge.None;

            // Get the annotation (if any) that straddles this time
            var annotation = this.Data?.Select(m => m.Data).GetTimeIntervalAnnotationAtTime(cursorTime, trackUnderMouse);
            if (annotation != null && trackUnderMouse != null)
            {
                // Check if the mouse is over an edge of the annotation
                annotationEdge = this.GetMouseOverAnnotationEdge(cursorTime, annotation, this.TimelineVisualizationPanel.GetTimelineScroller(e.Source));
            }

            // If the shift key is down, the user is dropping the end selection marker. If there is no VO currently being snapped
            // to and the mouse is over an annotation edge, then manually set the selection marker right on the edge. Otherwise
            // let the event bubble up to the timeline visualization panel which will set the selection marker in the usual fashion.
            if (Keyboard.IsKeyDown(Key.LeftShift) || Keyboard.IsKeyDown(Key.RightShift))
            {
                if ((this.Container.SnapToVisualizationObject == null) && (annotationEdge != AnnotationEdge.None))
                {
                    var selectionMarkerTime = annotationEdge == AnnotationEdge.Left ? annotation.Interval.Left : annotation.Interval.Right;
                    this.Navigator.SelectionRange.Set(this.Navigator.SelectionRange.StartTime <= selectionMarkerTime ? this.Navigator.SelectionRange.StartTime : DateTime.MinValue, selectionMarkerTime);
                    e.Handled = true;
                }
            }
        }

        private void DoMouseMove(MouseEventArgs e)
        {
            // First figure out the canvas and get the track under the mouse cursor.
            var canvas = this.FindCanvas(e.Source);
            var trackUnderMouse = this.GetTrackUnderMouse(e, canvas);

            if (this.annotationDragInfo != null)
            {
                this.DragAnnotationEdge(e);
                e.Handled = true;
            }
            else
            {
                // Get the time at the mouse cursor
                var timeAtMousePointer = this.TimelineVisualizationPanel.GetTimeAtMousePointer(e, false);

                // Get the item (if any) that straddles this time
                if (this.AllowEditAnnotationBoundaries)
                {
                    // Get the item (if any) that straddles this time
                    var annotation = this.Data?.Select(m => m.Data).GetTimeIntervalAnnotationAtTime(timeAtMousePointer, trackUnderMouse);
                    if (annotation != null && trackUnderMouse != null)
                    {
                        // Check if the mouse is over an edge of the annotation
                        var annotationEdge = this.GetMouseOverAnnotationEdge(timeAtMousePointer, annotation, this.TimelineVisualizationPanel.GetTimelineScroller(e.Source));

                        if (annotationEdge != AnnotationEdge.None)
                        {
                            canvas.Cursor = Cursors.SizeWE;
                        }
                        else
                        {
                            canvas.Cursor = Cursors.Arrow;
                        }
                    }
                    else
                    {
                        canvas.Cursor = Cursors.Arrow;
                    }
                }
            }
        }

        private void DoMouseLeftButtonUp(MouseEventArgs e)
        {
            if (this.annotationDragInfo != null)
            {
                if (this.annotationDragInfo.LeftAnnotationSetMessage.HasValue)
                {
                    this.UpdateAnnotationSetMessageTime(this.annotationDragInfo.LeftAnnotationSetMessage.Value, this.annotationDragInfo.Track);
                }

                if (this.annotationDragInfo.RightAnnotationSetMessage.HasValue)
                {
                    this.UpdateAnnotationSetMessageTime(this.annotationDragInfo.RightAnnotationSetMessage.Value, this.annotationDragInfo.Track);
                }

                this.annotationDragInfo = null;
            }
        }

        private string GetTrackUnderMouse(MouseEventArgs e, Canvas canvas)
        {
            this.TrackUnderMouseIndex = (int)(e.GetPosition(canvas).Y * this.trackIndex.Count / canvas.ActualHeight);
            return this.GetTrackByIndex(this.TrackUnderMouseIndex);
        }

        private void SelectAnnotation(TimeIntervalAnnotation annotation)
        {
            var displayData = this.DisplayData.FirstOrDefault(d => d.Annotation.Interval.Right == annotation?.Interval.Right);
            if (displayData != this.selectedDisplayDataItem)
            {
                if (this.selectedDisplayDataItem != null)
                {
                    this.selectedDisplayDataItem.IsSelected = false;
                    VisualizationContext.Instance.DisplayObjectProperties(null);
                }

                if (displayData != null)
                {
                    displayData.IsSelected = true;
                    VisualizationContext.Instance.DisplayObjectProperties(displayData);
                }
                else
                {
                    VisualizationContext.Instance.DisplayObjectProperties(this);
                }

                this.selectedDisplayDataItem = displayData;
            }
        }

        /// <summary>
        /// Updates the time of an annotation set message to match the values contained in the specified
        /// annotation after dragging the annotation's start or end edge.
        /// </summary>
        /// <param name="annotationSetMessage">The annotation set message to update.</param>
        /// <param name="track">The track name for the annotation whose time has changed.</param>
        private void UpdateAnnotationSetMessageTime(Message<TimeIntervalAnnotationSet> annotationSetMessage, string track)
        {
            if (!annotationSetMessage.Data.ContainsTrack(track))
            {
                throw new ArgumentException("Annotation set message does not contain specified track.");
            }

            // NOTE: We can't just do an update to the existing message because the message's OriginationTime is the collection
            // key, and we're changing that value.  So we need to delete the existing item and add the updated one
            var updates = new List<StreamUpdate<TimeIntervalAnnotationSet>>();

            // if the new end time is not different from the message time, we simply replace the message
            if (annotationSetMessage.OriginatingTime == annotationSetMessage.Data[track].Interval.Right)
            {
                updates.Add(new (StreamUpdateType.Replace, annotationSetMessage));
            }
            else
            {
                // O/w we need to move this annotation.

                // First figure out if there is an existing annotation set at the new end time
                var newEndTime = annotationSetMessage.Data[track].Interval.Right;
                var existingAnnotationSetMessage = this.Data.FirstOrDefault(m => m.OriginatingTime == newEndTime);
                if (existingAnnotationSetMessage != default)
                {
                    existingAnnotationSetMessage.Data.AddAnnotation(annotationSetMessage.Data[track]);
                    updates.Add(new (StreamUpdateType.Replace, existingAnnotationSetMessage));
                }
                else
                {
                    // Create the new annotation set
                    var newAnnotationSet = new TimeIntervalAnnotationSet(
                        new TimeIntervalAnnotation(
                            annotationSetMessage.Data[track].Interval,
                            track,
                            annotationSetMessage.Data[track].AttributeValues));

                    // Create an annotation set message for the annotation
                    var newAnnotationSetMessage = new Message<TimeIntervalAnnotationSet>(newAnnotationSet, newEndTime, newEndTime, 0, 0);

                    updates.Add(new StreamUpdate<TimeIntervalAnnotationSet>(StreamUpdateType.Add, newAnnotationSetMessage));
                }

                // If this is the only annotation from the current message
                if (annotationSetMessage.Data.Tracks.Count() == 1)
                {
                    updates.Add(new (StreamUpdateType.Delete, annotationSetMessage));
                }
                else
                {
                    // O/w remove the annotation from the message
                    annotationSetMessage.Data.RemoveAnnotation(track);

                    // And update
                    updates.Add(new StreamUpdate<TimeIntervalAnnotationSet>(StreamUpdateType.Replace, annotationSetMessage));
                }
            }

            // Update the stream
            DataManager.Instance.UpdateStream(this.StreamSource, updates);
        }

        private AnnotationEdge GetMouseOverAnnotationEdge(DateTime cursorTime, TimeIntervalAnnotation annotation, TimelineScroller timelineScroller)
        {
            // Work out what time interval is expressed in 3 pixels at the current zoom
            double percent = 5.0d / timelineScroller.ActualWidth;
            var viewRange = this.Navigator.ViewRange;
            TimeSpan hitTargetWidth = TimeSpan.FromTicks((long)((double)viewRange.Duration.Ticks * percent));

            // Check if the mouse cursor is within 3 pixels of the left or right edge of the annoation
            if (Math.Abs((annotation.Interval.Left - cursorTime).Ticks) <= hitTargetWidth.Ticks)
            {
                return AnnotationEdge.Left;
            }

            if (Math.Abs((annotation.Interval.Right - cursorTime).Ticks) <= hitTargetWidth.Ticks)
            {
                return AnnotationEdge.Right;
            }

            return AnnotationEdge.None;
        }

        private void DragAnnotationEdge(MouseEventArgs e)
        {
            // Get the time at the mouse cursor
            var timeAtMousePointer = this.TimelineVisualizationPanel.GetTimeAtMousePointer(e, true);

            // Make sure we stay within bounds
            if (timeAtMousePointer < this.annotationDragInfo.MinimumTime)
            {
                timeAtMousePointer = this.annotationDragInfo.MinimumTime;
            }

            if (timeAtMousePointer > this.annotationDragInfo.MaximumTime)
            {
                timeAtMousePointer = this.annotationDragInfo.MaximumTime;
            }

            // Set the new positions of the ends of the annotation(s)
            if (this.annotationDragInfo.LeftAnnotation != null)
            {
                this.annotationDragInfo.LeftAnnotation.Interval = new TimeInterval(this.annotationDragInfo.LeftAnnotation.Interval.Left, timeAtMousePointer);
            }

            if (this.annotationDragInfo.RightAnnotation != null)
            {
                this.annotationDragInfo.RightAnnotation.Interval = new TimeInterval(timeAtMousePointer, this.annotationDragInfo.RightAnnotation.Interval.Right);
            }

            this.UpdateDisplayData();

            this.Navigator.Cursor = timeAtMousePointer;
        }

        private Canvas FindCanvas(object sourceElement)
        {
            // Walk up the visual tree until we either find the
            // Timeline Scroller or fall off the top of the tree
            var target = sourceElement as DependencyObject;
            while (target != null && target is not Canvas && target is not TimeIntervalAnnotationVisualizationObjectView)
            {
                target = VisualTreeHelper.GetParent(target);
            }

            return target is Canvas ? target as Canvas : (target as TimeIntervalAnnotationVisualizationObjectView).Canvas;
        }

        private void GenerateLegendValue()
        {
            this.RaisePropertyChanging(nameof(this.AttributeCount));

            // For now the legend value is simmply a list of all the attribute names
            var legend = new StringBuilder();
            var attributeNames = this.AnnotationSchema.AttributeSchemas.Select(d => d.Name).ToList();
            foreach (string attributeName in attributeNames)
            {
                if (legend.Length > 0)
                {
                    legend.AppendLine();
                }

                legend.Append(attributeName);
            }

            this.legendValue = legend.ToString();
            this.AttributeCount = attributeNames.Count();

            this.RaisePropertyChanged(nameof(this.AttributeCount));
        }

        private string CreateNewTrack()
        {
            while (this.trackIndex.ContainsKey($"Track:{this.newTrackId}"))
            {
                this.newTrackId++;
            }

            var track = $"Track:{this.newTrackId}";
            this.trackIndex.Add(track, this.trackIndex.Count);

            return track;
        }

        private Message<TimeIntervalAnnotationSet> FindTimeIntervalAnnotationSetMessageContaining(TimeIntervalAnnotation annotation)
            => this.Data.First(asm => asm.Data.ContainsTrack(annotation.Track) && asm.Data[annotation.Track] == annotation);

        private void OnSelectedTracksCollectionChanged(object sender, NotifyCollectionChangedEventArgs e)
        {
            // If the selected tracks collection has changed, switch the show tracks mode to selected
            this.ShowTracks = ShowTracks.Selected;

            // An update the data
            this.UpdateDisplayData();
        }
    }
}
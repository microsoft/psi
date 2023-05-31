// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.

namespace Microsoft.Psi.Visualization
{
    /// <summary>
    /// The paths to Icon Sources.
    /// </summary>
    public static class IconSourcePath
    {
        /// <summary>
        /// Gets the prefix to append to icon filenames to create a packed uri.
        /// </summary>
        public const string IconPrefix = @"pack://application:,,,/Microsoft.Psi.Visualization.Windows;component/Icons/";

        /// <summary>
        /// Annotation.
        /// </summary>
        public const string Annotation = IconPrefix + "annotation.png";

        /// <summary>
        /// Annotation.
        /// </summary>
        public const string AnnotationUnbound = IconPrefix + "annotation-unbound.png";

        /// <summary>
        /// Sets the annotation to the selection boundaries.
        /// </summary>
        public const string SetAnnotationToSelection = IconPrefix + "set-annotation-to-selection.png";

        /// <summary>
        /// Partition.
        /// </summary>
        public const string Partition = IconPrefix + "partition.png";

        /// <summary>
        /// Add partition.
        /// </summary>
        public const string PartitionAdd = IconPrefix + "partition-add.png";

        /// <summary>
        /// Add multiple partitions.
        /// </summary>
        public const string PartitionAddMultiple = IconPrefix + "partition-add-multiple.png";

        /// <summary>
        /// Create partition.
        /// </summary>
        public const string PartitionCreate = IconPrefix + "partition-create.png";

        /// <summary>
        /// Create partition.
        /// </summary>
        public const string PartitionCrop = IconPrefix + "partition-crop.png";

        /// <summary>
        /// Export partition.
        /// </summary>
        public const string PartitionExport = IconPrefix + "partition-export.png";

        /// <summary>
        /// Remove partition.
        /// </summary>
        public const string PartitionRemove = IconPrefix + "partition-remove.png";

        /// <summary>
        /// Live Partition.
        /// </summary>
        public const string PartitionLive = IconPrefix + "partition-live.png";

        /// <summary>
        /// Invalid Partition.
        /// </summary>
        public const string PartitionInvalid = IconPrefix + "partition-invalid.png";

        /// <summary>
        /// Remove session.
        /// </summary>
        public const string SessionRemove = IconPrefix + "session-remove.png";

        /// <summary>
        /// Create session.
        /// </summary>
        public const string SessionCreate = IconPrefix + "session-create.png";

        /// <summary>
        /// Add session from store.
        /// </summary>
        public const string SessionAddFromStore = IconPrefix + "session-from-store.png";

        /// <summary>
        /// Add session from folder.
        /// </summary>
        public const string SessionAddFromFolder = IconPrefix + "session-from-folder.png";

        /// <summary>
        /// Add multiple sessions from folder.
        /// </summary>
        public const string MultipleSessionsAddFromFolder = IconPrefix + "multiple-sessions-from-folder.png";

        /// <summary>
        /// Close dataset.
        /// </summary>
        public const string DatasetClose = IconPrefix + "dataset-remove.png";

        /// <summary>
        /// Stream.
        /// </summary>
        public const string Stream = IconPrefix + "stream.png";

        /// <summary>
        /// Stream in a panel.
        /// </summary>
        public const string StreamInPanel = IconPrefix + "panel-stream.png";

        /// <summary>
        /// Stream live.
        /// </summary>
        public const string StreamLive = IconPrefix + "stream-live.png";

        /// <summary>
        /// Stream not bound to a datasource.
        /// </summary>
        public const string StreamUnbound = IconPrefix + "stream-unbound.png";

        /// <summary>
        /// Audio.
        /// </summary>
        public const string StreamAudio = IconPrefix + "stream-audio.png";

        /// <summary>
        /// Audio live.
        /// </summary>
        public const string StreamAudioLive = IconPrefix + "stream-audio-live.png";

        /// <summary>
        /// Audio muted.
        /// </summary>
        public const string StreamAudioMuted = IconPrefix + "stream-audio-muted.png";

        /// <summary>
        /// Audio muted live.
        /// </summary>
        public const string StreamAudioMutedLive = IconPrefix + "stream-audio-muted-live.png";

        /// <summary>
        /// A stream member.
        /// </summary>
        public const string DerivedStream = IconPrefix + "stream-member.png";

        /// <summary>
        /// A live stream member.
        /// </summary>
        public const string DerivedStreamLive = IconPrefix + "stream-member-live.png";

        /// <summary>
        /// A stream member.
        /// </summary>
        public const string DerivedStreamSnap = IconPrefix + "stream-member-snap.png";

        /// <summary>
        /// A stream member.
        /// </summary>
        public const string DerivedStreamSnapLive = IconPrefix + "stream-member-snap-live.png";

        /// <summary>
        /// A stream member.
        /// </summary>
        public const string DerivedStreamUnbound = IconPrefix + "stream-member-unbound.png";

        /// <summary>
        /// A stream member.
        /// </summary>
        public const string StreamMemberUnboundLive = IconPrefix + "stream-member-unbound-live.png";

        /// <summary>
        /// Snap to stream.
        /// </summary>
        public const string SnapToStream = IconPrefix + "stream-snap.png";

        /// <summary>
        /// Snap to stream live.
        /// </summary>
        public const string SnapToStreamLive = IconPrefix + "stream-snap-live.png";

        /// <summary>
        /// A group icon.
        /// </summary>
        public const string Group = IconPrefix + "group.png";

        /// <summary>
        /// A group icon.
        /// </summary>
        public const string GroupLive = IconPrefix + "group-live.png";

        /// <summary>
        /// A stream and group icon.
        /// </summary>
        public const string StreamGroup = IconPrefix + "stream-group.png";

        /// <summary>
        /// A live stream and group icon.
        /// </summary>
        public const string StreamGroupLive = IconPrefix + "stream-group-live.png";

        /// <summary>
        /// Latency Visualization.
        /// </summary>
        public const string Latency = IconPrefix + "latency.png";

        /// <summary>
        /// Latency Visualization in a panel.
        /// </summary>
        public const string LatencyInPanel = IconPrefix + "panel-latency.png";

        /// <summary>
        /// Messages Visualization.
        /// </summary>
        public const string Messages = IconPrefix + "messages.png";

        /// <summary>
        /// Messages Visualization in a panel.
        /// </summary>
        public const string MessagesInPanel = IconPrefix + "panel-messages.png";

        /// <summary>
        /// One cell instant container.
        /// </summary>
        public const string InstantContainerOneCell = IconPrefix + "panel-matrix-1.png";

        /// <summary>
        /// Two cell instant container.
        /// </summary>
        public const string InstantContainerTwoCell = IconPrefix + "panel-matrix-2.png";

        /// <summary>
        /// Three cell instant container.
        /// </summary>
        public const string InstantContainerThreeCell = IconPrefix + "panel-matrix-3.png";

        /// <summary>
        /// Add instant container cell to the left.
        /// </summary>
        public const string InstantContainerAddCellLeft = IconPrefix + "panel-matrix-add-cell-left.png";

        /// <summary>
        /// Add instant container cell to the right.
        /// </summary>
        public const string InstantContainerAddCellRight = IconPrefix + "panel-matrix-add-cell-right.png";

        /// <summary>
        /// Remove instant container cell.
        /// </summary>
        public const string InstantContainerRemoveCell = IconPrefix + "panel-matrix-remove-cell.png";

        /// <summary>
        /// Remove a panel.
        /// </summary>
        public const string RemovePanel = IconPrefix + "panel-remove.png";

        /// <summary>
        /// Clear a panel.
        /// </summary>
        public const string ClearPanel = IconPrefix + "panel-clear.png";

        /// <summary>
        /// Legend.
        /// </summary>
        public const string Legend = IconPrefix + "legend.png";

        /// <summary>
        /// Zoom to stream extents.
        /// </summary>
        public const string ZoomToStream = IconPrefix + "zoom-to-stream.png";

        /// <summary>
        /// Zoom to selection.
        /// </summary>
        public const string ZoomToSelection = IconPrefix + "zoom-to-selection.png";

        /// <summary>
        /// Move selection left.
        /// </summary>
        public const string MoveSelectionLeft = IconPrefix + "move-selection-left.png";

        /// <summary>
        /// Move selection right.
        /// </summary>
        public const string MoveSelectionRight = IconPrefix + "move-selection-right.png";

        /// <summary>
        /// Clear selection.
        /// </summary>
        public const string ClearSelection = IconPrefix + "selection-remove.png";

        /// <summary>
        /// Zoom to stream extents.
        /// </summary>
        public const string ZoomToSession = IconPrefix + "zoom-to-session.png";

        /// <summary>
        /// The live button when live mode is enabled.
        /// </summary>
        public const string LiveButtonOn = IconPrefix + "live-button-on.png";

        /// <summary>
        /// The live button when live mode is disabled.
        /// </summary>
        public const string LiveButtonOff = IconPrefix + "live-button-off.png";

        /// <summary>
        /// Diagnostics.
        /// </summary>
        public const string Diagnostics = IconPrefix + "diagnostics.png";

        /// <summary>
        /// Live Diagnostics.
        /// </summary>
        public const string DiagnosticsLive = IconPrefix + "diagnostics-live.png";

        /// <summary>
        /// A blank icon.
        /// </summary>
        public const string Blank = IconPrefix + "blank.png";

        /// <summary>
        /// Expand all nodes.
        /// </summary>
        public const string ExpandAllNodes = IconPrefix + "expand-all.png";

        /// <summary>
        /// Collapse all nodes.
        /// </summary>
        public const string CollapseAllNodes = IconPrefix + "collapse-all.png";

        /// <summary>
        /// Checkmark for menu items.
        /// </summary>
        public const string Checkmark = IconPrefix + "checkmark.png";

        /// <summary>
        /// Toggle visibility.
        /// </summary>
        public const string ToggleVisibility = IconPrefix + "stream-show-hide.png";

        /// <summary>
        /// Go to time button.
        /// </summary>
        public const string GoToTime = IconPrefix + "go-to-time.png";
    }
}

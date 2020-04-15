// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.

namespace Microsoft.Psi.Visualization.Common
{
    /// <summary>
    /// The paths to Icon Sources.
    /// </summary>
    public static class IconSourcePath
    {
        /// <summary>
        /// Gets the prefix to append to icon filenames to create a packed uri.
        /// </summary>
        public const string IconPrefix = @"pack://application:,,,/Microsoft.Psi.Visualization.Common.Windows;component/Icons/";

        /// <summary>
        /// Partition.
        /// </summary>
        public const string Partition = IconPrefix + "partition.png";

        /// <summary>
        /// Live Partition.
        /// </summary>
        public const string PartitionLive = IconPrefix + "partition-live.png";

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
        /// Zoom to stream extents.
        /// </summary>
        public const string ZoomToSelection = IconPrefix + "zoom-to-selection.png";

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
    }
}

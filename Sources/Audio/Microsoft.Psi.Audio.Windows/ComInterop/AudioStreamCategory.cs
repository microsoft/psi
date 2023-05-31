// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.

namespace Microsoft.Psi.Audio.ComInterop
{
    /// <summary>
    /// Audio stream categories enumeration (defined in AudioSessionTypes.h).
    /// </summary>
    internal enum AudioStreamCategory
    {
        /// <summary>
        /// All other streams (default).
        /// </summary>
        Other = 0,

        /// <summary>
        /// (deprecated for Win10) Music, Streaming audio
        /// </summary>
        ForegroundOnlyMedia = 1,

        /// <summary>
        /// (deprecated for Win10) Video with audio
        /// </summary>
        BackgroundCapableMedia = 2,

        /// <summary>
        /// VOIP, chat, phone call
        /// </summary>
        Communications = 3,

        /// <summary>
        /// Alarm, Ring tones
        /// </summary>
        Alerts = 4,

        /// <summary>
        /// Sound effects, clicks, dings
        /// </summary>
        SoundEffects = 5,

        /// <summary>
        /// Game sound effects
        /// </summary>
        GameEffects = 6,

        /// <summary>
        /// Background audio for games
        /// </summary>
        GameMedia = 7,

        /// <summary>
        /// In game player chat
        /// </summary>
        GameChat = 8,

        /// <summary>
        /// Speech recognition
        /// </summary>
        Speech = 9,

        /// <summary>
        /// Video with audio
        /// </summary>
        Movie = 10,

        /// <summary>
        /// Music, Streaming audio
        /// </summary>
        Media = 11,
    }
}
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the Microsoft Research license.

namespace Sigma
{
    /// <summary>
    /// Represents the configuration for the <see cref="GemUserInterface"/>.
    /// </summary>
    public class GemUserInterfaceConfiguration
    {
        /// <summary>
        /// Gets the default gem size (m).
        /// </summary>
        public static float DefaultGemSize => 0.008f;

        /// <summary>
        /// Gets or sets the thickness (m) of editing handle.
        /// </summary>
        public float GraspDistance { get; set; } = 0.05f;
    }
}
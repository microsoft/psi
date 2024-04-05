// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the Microsoft Research license.

namespace Microsoft.Psi.MixedReality.Applications
{
    using System.Collections.Generic;

    /// <summary>
    /// Represents the client app configuration.
    /// </summary>
    public class ClientAppConfiguration
    {
        /// <summary>
        /// Gets or sets the name of the configuration.
        /// </summary>
        public string Name { get; set; } = "Default";

        /// <summary>
        /// Gets or sets the compute server names used by the client app.
        /// </summary>
        public List<string> ComputeServerNames { get; set; } = new List<string> { };

        /// <summary>
        /// Gets or sets a value indicating whether to automatically start this configuration.
        /// </summary>
        public bool AutoStart { get; set; } = false;

        /// <summary>
        /// Gets or sets a value indicating whether to ignore the server heartbeat.
        /// </summary>
        public bool IgnoreServerHeartbeat { get; set; } = false;

        /// <summary>
        /// Gets or sets the world spatial anchor id.
        /// </summary>
        public string WorldSpatialAnchorId { get; set; }
    }
}

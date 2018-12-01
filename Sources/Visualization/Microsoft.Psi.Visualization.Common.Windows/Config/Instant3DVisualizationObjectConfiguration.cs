// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.

namespace Microsoft.Psi.Visualization.Config
{
    using System.Runtime.Serialization;

    /// <summary>
    /// Represents an instant 3D visualization object configuration.
    /// </summary>
    [DataContract(Namespace = "http://www.microsoft.com/psi")]
    public class Instant3DVisualizationObjectConfiguration : InstantVisualizationObjectConfiguration
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="Instant3DVisualizationObjectConfiguration"/> class.
        /// </summary>
        public Instant3DVisualizationObjectConfiguration()
        {
        }
    }
}

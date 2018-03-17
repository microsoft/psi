// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.

namespace Microsoft.Psi.Visualization.Config
{
    using System.Runtime.Serialization;

    /// <summary>
    /// Represents a string with duration visualization object configuration.
    /// </summary>
    [DataContract(Namespace = "http://www.microsoft.com/psi")]
    public class StringWithDurationVisualizationObjectConfiguration : TimelineVisualizationObjectConfiguration
    {
    }
}

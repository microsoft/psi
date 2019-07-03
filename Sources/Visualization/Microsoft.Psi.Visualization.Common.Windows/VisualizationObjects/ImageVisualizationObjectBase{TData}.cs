// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.

namespace Microsoft.Psi.Visualization.VisualizationObjects
{
    using System.Runtime.Serialization;
    using Microsoft.Psi.Visualization.Config;

    /// <summary>
    /// Represents an image visualization object.
    /// </summary>
    /// <typeparam name="TData">The type of the image visualzation object.</typeparam>
    [DataContract(Namespace = "http://www.microsoft.com/psi")]
    public abstract class ImageVisualizationObjectBase<TData> : InstantVisualizationObject<TData, ImageVisualizationObjectBaseConfiguration>
    {
    }
}

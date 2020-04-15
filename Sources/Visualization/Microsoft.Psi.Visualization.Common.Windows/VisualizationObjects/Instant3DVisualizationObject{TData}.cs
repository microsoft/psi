// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.

namespace Microsoft.Psi.Visualization.VisualizationObjects
{
    using System.ComponentModel;
    using System.Runtime.Serialization;
    using System.Windows;
    using System.Windows.Media.Media3D;
    using Microsoft.Psi.Visualization.Serialization;
    using Microsoft.Psi.Visualization.VisualizationPanels;
    using Newtonsoft.Json.Serialization;

    /// <summary>
    /// Represents an instant 3D visualization object.
    /// </summary>
    /// <typeparam name="TData">The type of the instant 3D visualization.</typeparam>
    [VisualizationPanelType(VisualizationPanelType.XYZ)]
    public abstract class Instant3DVisualizationObject<TData> : InstantVisualizationObject<TData>, I3DVisualizationObject
    {
        private IContractResolver contractResolver = new Instant3DVisualizationObjectContractResolver();

        /// <inheritdoc />
        [Browsable(false)]
        [IgnoreDataMember]
        public override DataTemplate DefaultViewTemplate => null;

        /// <inheritdoc />
        [Browsable(false)]
        [IgnoreDataMember]
        public virtual Visual3D Visual3D { get; protected set; }

        /// <inheritdoc />
        protected override IContractResolver ContractResolver => this.contractResolver;
    }
}

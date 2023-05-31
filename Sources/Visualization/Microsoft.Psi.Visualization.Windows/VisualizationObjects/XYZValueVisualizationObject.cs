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
    /// Provides an abstract base class for stream value visualization objects that show three dimensional data.
    /// </summary>
    /// <typeparam name="TData">The type of the stream values to visualize.</typeparam>
    [VisualizationPanelType(VisualizationPanelType.XYZ)]
    public abstract class XYZValueVisualizationObject<TData> : StreamValueVisualizationObject<TData>, I3DVisualizationObject
    {
        private readonly IContractResolver contractResolver = new XYZValueVisualizationObjectContractResolver();

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

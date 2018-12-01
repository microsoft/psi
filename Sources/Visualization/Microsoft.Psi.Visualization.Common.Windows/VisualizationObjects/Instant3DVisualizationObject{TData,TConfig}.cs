// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.

namespace Microsoft.Psi.Visualization.VisualizationObjects
{
    using System.ComponentModel;
    using System.Runtime.Serialization;
    using System.Windows;
    using System.Windows.Media.Media3D;
    using Microsoft.Psi.Visualization.Config;
    using Microsoft.Psi.Visualization.Serialization;
    using Newtonsoft.Json.Serialization;

    /// <summary>
    /// Represents an instant 3D visualization object.
    /// </summary>
    /// <typeparam name="TData">The type of the instant 3D visualization.</typeparam>
    /// <typeparam name="TConfig">The type of the instant 3D visualiztion object configuration.</typeparam>
    [DataContract(Namespace = "http://www.microsoft.com/psi")]
    public abstract class Instant3DVisualizationObject<TData, TConfig> : InstantVisualizationObject<TData, TConfig>, I3DVisualizationObject
        where TConfig : Instant3DVisualizationObjectConfiguration, new()
    {
        private IContractResolver contractResolver = new Instant3DVisualizationObjectContractResolver();

        /// <inheritdoc />
        [Browsable(false)]
        [IgnoreDataMember]
        public override DataTemplate DefaultViewTemplate => null;

        /// <inheritdoc />
        public virtual Visual3D Visual3D { get; protected set; }

        /// <inheritdoc />
        protected override IContractResolver ContractResolver => this.contractResolver;
    }
}

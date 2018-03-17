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

        /// <summary>
        /// Gets or sets the local offset string.
        /// </summary>
        public string LocalOffsetString
        {
            get => string.Format($"{this.Configuration.LocalOffset.X}, {this.Configuration.LocalOffset.Y}, {this.Configuration.LocalOffset.Z}");
            set
            {
                double x = 0, y = 0, z = 0;
                string[] values = value.Split(',');
                if (values.Length == 3 && double.TryParse(values[0], out x) && double.TryParse(values[1], out y) && double.TryParse(values[2], out z))
                {
                    this.Configuration.LocalOffset = new MathNet.Spatial.Euclidean.Vector3D(x, y, z);
                }

                this.RaisePropertyChanged(nameof(this.LocalOffsetString));
            }
        }

        /// <summary>
        /// Gets or sets the local scale string.
        /// </summary>
        public string LocalScaleString
        {
            get => string.Format($"{this.Configuration.LocalScale.X}, {this.Configuration.LocalScale.Y}, {this.Configuration.LocalScale.Z}");
            set
            {
                double x = 1, y = 1, z = 1;
                string[] values = value.Split(',');
                if (values.Length == 3 && double.TryParse(values[0], out x) && double.TryParse(values[1], out y) && double.TryParse(values[2], out z))
                {
                    this.Configuration.LocalScale = new MathNet.Spatial.Euclidean.Vector3D(x, y, z);
                }

                this.RaisePropertyChanged(nameof(this.LocalScaleString));
            }
        }

        /// <summary>
        /// Gets or sets the local rotation string.
        /// </summary>
        public string LocalRotationString
        {
            get => string.Format($"{this.Configuration.LocalRotation.X}, {this.Configuration.LocalRotation.Y}, {this.Configuration.LocalRotation.Z}");
            set
            {
                double x = 0, y = 0, z = 0;
                string[] values = value.Split(',');
                if (values.Length == 3 && double.TryParse(values[0], out x) && double.TryParse(values[1], out y) && double.TryParse(values[2], out z))
                {
                    this.Configuration.LocalRotation = new MathNet.Spatial.Euclidean.Vector3D(x, y, z);
                }

                this.RaisePropertyChanged(nameof(this.LocalRotationString));
            }
        }

        /// <inheritdoc />
        protected override IContractResolver ContractResolver => this.contractResolver;
    }
}

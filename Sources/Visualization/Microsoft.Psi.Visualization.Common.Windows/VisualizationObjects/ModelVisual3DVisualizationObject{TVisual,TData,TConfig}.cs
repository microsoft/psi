// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.

namespace Microsoft.Psi.Visualization.VisualizationObjects
{
    using System.ComponentModel;
    using System.Runtime.Serialization;
    using System.Windows.Media.Media3D;
    using Microsoft.Psi.Visualization.Config;
    using Microsoft.Psi.Visualization.Views.Visuals3D;

    /// <summary>
    /// Represents a visualization object for a ModelVisual3D view.
    /// </summary>
    /// <typeparam name="TVisual">The type of visual in this object.</typeparam>
    /// <typeparam name="TData">The underlying data being visualized.</typeparam>
    /// <typeparam name="TConfig">The configuration associated with TData.</typeparam>
    [DataContract(Namespace = "http://www.microsoft.com/psi")]
    public class ModelVisual3DVisualizationObject<TVisual, TData, TConfig> : Instant3DVisualizationObject<TData, TConfig>
        where TVisual : ModelVisual3D, IView3D<TData, TConfig>, new()
        where TConfig : Instant3DVisualizationObjectConfiguration, new()
    {
        /// <inheritdoc/>
        protected override void InitNew()
        {
            this.Visual3D = new TVisual();
            base.InitNew();
        }

        /// <inheritdoc/>
        protected override void OnConfigurationPropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            (this.Visual3D as IView3D<TData, TConfig>).UpdateConfiguration(this.Configuration);
            base.OnConfigurationPropertyChanged(sender, e);
        }

        /// <inheritdoc/>
        protected override void OnPropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            var currentValue = this.CurrentValue.GetValueOrDefault();
            if (currentValue == null)
            {
                (this.Visual3D as IView3D<TData, TConfig>).ClearAll();
            }
            else
            {
                (this.Visual3D as IView3D<TData, TConfig>).UpdateData(currentValue.Data, currentValue.OriginatingTime);
            }

            base.OnPropertyChanged(sender, e);
        }
    }
}

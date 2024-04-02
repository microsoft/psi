// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.

namespace Microsoft.Psi.Visualization.VisualizationObjects
{
    using System.ComponentModel;
    using System.Runtime.Serialization;
    using System.Windows;
    using System.Windows.Media.Media3D;
    using Microsoft.Psi.Visualization.VisualizationPanels;

    /// <summary>
    /// Provides an abstract base class for stream interval visualization objects that show three dimensional data.
    /// </summary>
    /// <typeparam name="TData">The type of the stream values to visualize.</typeparam>
    [VisualizationPanelType(VisualizationPanelType.XYZ)]
    public abstract class XYZIntervalVisualizationObject<TData> : StreamIntervalVisualizationObject<TData>, I3DVisualizationObject
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="XYZIntervalVisualizationObject{TData}"/> class.
        /// </summary>
        public XYZIntervalVisualizationObject()
            : base()
        {
            this.VisualizationInterval = VisualizationInterval.CursorEpsilon;
        }

        /// <inheritdoc />
        [Browsable(false)]
        [IgnoreDataMember]
        public override DataTemplate DefaultViewTemplate => null;

        /// <inheritdoc />
        [Browsable(false)]
        [IgnoreDataMember]
        public virtual Visual3D Visual3D { get; protected set; }
    }
}

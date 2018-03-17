// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.

namespace Microsoft.Psi.Visualization.VisualizationObjects
{
    using System.Collections.Generic;
    using System.Runtime.Serialization;
    using MathNet.Spatial.Euclidean;
    using Microsoft.Psi.Serialization;
    using Microsoft.Psi.Visualization.Config;
    using Microsoft.Psi.Visualization.Views.Visuals3D;

    /// <summary>
    /// Represents a scatter coordinate systems visualization object.
    /// </summary>
    [DataContract(Namespace = "http://www.microsoft.com/psi")]
    public class ScatterCoordinateSystemsVisualizationObject : Instant3DVisualizationObject<List<CoordinateSystem>, ScatterCoordinateSystemsVisualizationObjectConfiguration>
    {
        /// <summary>
        /// Initializes static members of the <see cref="ScatterCoordinateSystemsVisualizationObject"/> class.
        /// </summary>
        static ScatterCoordinateSystemsVisualizationObject()
        {
            // This is b/c we don't know yet how to load the right polymorphic type b/c we don't have the assembly
            KnownSerializers.Default.Register<MathNet.Numerics.LinearAlgebra.Storage.DenseColumnMajorMatrixStorage<double>>(null);
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="ScatterCoordinateSystemsVisualizationObject"/> class.
        /// </summary>
        public ScatterCoordinateSystemsVisualizationObject()
        {
            this.Visual3D = new ScatterCoordinateSystemVisual(this);
        }
    }
}

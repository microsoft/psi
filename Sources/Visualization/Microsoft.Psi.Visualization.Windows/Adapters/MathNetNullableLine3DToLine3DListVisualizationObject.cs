// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.

namespace Microsoft.Psi.Visualization.Adapters
{
    using System;
    using System.Collections.Generic;
    using MathNet.Spatial.Euclidean;
    using Microsoft.Psi.Visualization.Data;

    /// <summary>
    /// Used to adapt streams of <see cref="Nullable{Line3D}"/> into a <see cref="List{Line3D}"/>.
    /// </summary>
    [StreamAdapter]
    public class MathNetNullableLine3DToLine3DListVisualizationObject : StreamAdapter<Line3D?, List<Line3D>>
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="MathNetNullableLine3DToLine3DListVisualizationObject"/> class.
        /// </summary>
        public MathNetNullableLine3DToLine3DListVisualizationObject()
            : base(Adapter)
        {
        }

        private static List<Line3D> Adapter(Line3D? value, Envelope env)
        {
            var list = new List<Line3D>();
            if (value.HasValue)
            {
                list.Add(value.Value);
            }

            return list;
        }
    }
}
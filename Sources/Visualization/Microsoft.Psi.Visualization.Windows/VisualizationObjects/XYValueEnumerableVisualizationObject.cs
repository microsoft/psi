// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.

namespace Microsoft.Psi.Visualization.VisualizationObjects
{
    using System.Collections.Generic;

    /// <summary>
    /// Provides an abstract base class for stream value visualization objects that show enumerations
    /// of two dimensional, cartesian data.
    /// </summary>
    /// <typeparam name="TItem">The type of data item.</typeparam>
    /// <typeparam name="TEnumerable">The type of the enumeration.</typeparam>
    public abstract class XYValueEnumerableVisualizationObject<TItem, TEnumerable> : XYValueVisualizationObject<TEnumerable>
        where TEnumerable : IEnumerable<TItem>
    {
    }
}

// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.

namespace Microsoft.Psi.Visualization.DataTypes
{
    using System.Collections.Generic;

    /// <summary>
    /// Represents an indexed history of time intervals.
    /// </summary>
    public class TimeIntervalHistory : Dictionary<string, List<(TimeInterval, string, System.Drawing.Color?)>>
    {
    }
}

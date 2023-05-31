// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.

namespace Microsoft.Psi.Visualization
{
    using System;

    /// <summary>
    /// Represents an object that provides a Y value range.
    /// </summary>
    public interface IYValueRangeProvider
    {
        /// <summary>
        /// Fires when the Y value range has changed.
        /// </summary>
        event EventHandler<EventArgs> YValueRangeChanged;

        /// <summary>
        /// Gets the Y value range.
        /// </summary>
        ValueRange<double> YValueRange { get; }
    }
}

// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.

namespace Microsoft.Psi
{
    /// <summary>
    /// Represents a bounded/unbounded, inclusive/exclusive interval endpoint value.
    /// </summary>
    /// <typeparam name="TPoint">Type of point value.</typeparam>
    public interface IIntervalEndpoint<TPoint>
    {
        /// <summary>
        /// Gets a value indicating whether the endpoint is bounded.
        /// </summary>
        bool Bounded { get; }

        /// <summary>
        /// Gets a value indicating whether the endpoint is inclusive.
        /// </summary>
        bool Inclusive { get; }

        /// <summary>
        /// Gets the point value.
        /// </summary>
        TPoint Point { get; }
    }
}

// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.

namespace Microsoft.Psi
{
    /// <summary>
    /// Defines a greedy stream interpolator with the same input and output type.
    /// </summary>
    /// <typeparam name="T">The type of the input messages.</typeparam>
    /// <remarks>Greedy interpolators produce results based on what is available on the secondary
    /// stream at the moment the primary message arrives. As such, they depend on the wall-clock
    /// time of message arrival, and hence are not guaranteed to produce reproducible results.</remarks>
    public abstract class GreedyInterpolator<T> : GreedyInterpolator<T, T>
    {
    }
}
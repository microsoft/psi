// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.

namespace Microsoft.Psi
{
    /// <summary>
    /// Defines a reproducible stream interpolator.
    /// </summary>
    /// <typeparam name="TIn">The type of the input messages.</typeparam>
    /// <typeparam name="TResult">The type of the interpolation result.</typeparam>
    /// <remarks>Reproducible interpolators produce results that do not depend on the wall-clock time of
    /// message arrival on a stream, i.e., they are based on originating times of messages. As a result,
    /// these interpolators might introduce extra delays as they might have to wait for enough messages on the
    /// secondary stream to proove that the interpolation result is correct, irrespective of any other messages
    /// that might arrive later.</remarks>
    public abstract class ReproducibleInterpolator<TIn, TResult> : Interpolator<TIn, TResult>
    {
    }
}

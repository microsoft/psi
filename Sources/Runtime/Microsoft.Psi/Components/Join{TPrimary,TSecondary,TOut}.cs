// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.

namespace Microsoft.Psi.Components
{
    using System;
    using System.Collections.Generic;

    /// <summary>
    /// Component that joins multiple streams using a reproducible interpolator.
    /// </summary>
    /// <typeparam name="TPrimary">The type the messages on the primary stream.</typeparam>
    /// <typeparam name="TSecondary">The type messages on the secondary stream.</typeparam>
    /// <typeparam name="TOut">The type of output message.</typeparam>
    public class Join<TPrimary, TSecondary, TOut> : Fuse<TPrimary, TSecondary, TSecondary, TOut>
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="Join{TPrimary, TSecondary, TOut}"/> class.
        /// </summary>
        /// <param name="pipeline">The pipeline to add the component to.</param>
        /// <param name="interpolator">Reproducible interpolator to use when joining the streams.</param>
        /// <param name="outputCreator">Mapping function from message pair to output.</param>
        /// <param name="secondaryCount">Number of secondary streams.</param>
        /// <param name="secondarySelector">Selector function mapping primary messages to secondary stream indices.</param>
        /// <param name="name">An optional name for the component.</param>
        public Join(
            Pipeline pipeline,
            ReproducibleInterpolator<TSecondary> interpolator,
            Func<TPrimary, TSecondary[], TOut> outputCreator,
            int secondaryCount = 1,
            Func<TPrimary, IEnumerable<int>> secondarySelector = null,
            string name = null)
            : base(pipeline, interpolator, outputCreator, secondaryCount, secondarySelector, name ?? $"Join({interpolator})")
        {
        }
    }
}
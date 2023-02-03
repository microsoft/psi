// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.

namespace Microsoft.Psi.Visualization.Adapters
{
    using System;
    using Microsoft.Psi.Visualization.Data;

    /// <summary>
    /// Implements a stream adapter that chains two different stream adapters.
    /// </summary>
    /// <typeparam name="TSource">The type of messages in the source stream.</typeparam>
    /// <typeparam name="TIntermediate">The type of the messages produced by the first adapter.</typeparam>
    /// <typeparam name="TDestination">The type of the messages produced by the second adapter.</typeparam>
    /// <typeparam name="TFirstAdapter">The type of the first adapter.</typeparam>
    /// <typeparam name="TSecondAdapter">The type of the second adapter.</typeparam>
    public class ChainedStreamAdapter<TSource, TIntermediate, TDestination, TFirstAdapter, TSecondAdapter> : StreamAdapter<TSource, TDestination>
        where TFirstAdapter : StreamAdapter<TSource, TIntermediate>
        where TSecondAdapter : StreamAdapter<TIntermediate, TDestination>
    {
        private readonly TFirstAdapter firstAdapter;
        private readonly TSecondAdapter secondAdapter;

        /// <summary>
        /// Initializes a new instance of the <see cref="ChainedStreamAdapter{TSource, TIntermediate, TDestination, TIntermediateAdapter, TDestinationAdapter}"/> class.
        /// </summary>
        /// <param name="firstAdapterParameters">The parameters for the first adapter.</param>
        /// <param name="secondAdapterParameters">The parameters for the second adapter.</param>
        public ChainedStreamAdapter(object[] firstAdapterParameters, object[] secondAdapterParameters)
        {
            this.firstAdapter = Activator.CreateInstance(typeof(TFirstAdapter), firstAdapterParameters) as TFirstAdapter;
            this.secondAdapter = Activator.CreateInstance(typeof(TSecondAdapter), secondAdapterParameters) as TSecondAdapter;
        }

        /// <inheritdoc/>
        public override TDestination GetAdaptedValue(TSource source, Envelope envelope)
        {
            var intermediate = this.firstAdapter.GetAdaptedValue(source, envelope);
            var result = this.secondAdapter.GetAdaptedValue(intermediate, envelope);
            this.firstAdapter.Dispose(intermediate);
            return result;
        }

        /// <inheritdoc/>
        public override void Dispose(TDestination destination)
            => this.secondAdapter.Dispose(destination);
    }
}

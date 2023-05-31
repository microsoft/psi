// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.

namespace Microsoft.Psi.Components
{
    /// <summary>
    /// Interface representing a component that is both a consumer and producer of messages.
    /// </summary>
    /// <typeparam name="TIn">Type of input stream messages.</typeparam>
    /// <typeparam name="TOut">Type of output stream messages.</typeparam>
    public interface IConsumerProducer<TIn, TOut> : IConsumer<TIn>, IProducer<TOut>
    {
    }
}

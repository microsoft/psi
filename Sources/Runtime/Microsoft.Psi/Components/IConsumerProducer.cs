// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.

namespace Microsoft.Psi.Components
{
    public interface IConsumerProducer<TIn, TOut> : IConsumer<TIn>, IProducer<TOut>
    {
    }
}

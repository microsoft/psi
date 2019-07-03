// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.

namespace Microsoft.Psi
{
    /// <summary>
    /// Components that implement this interface are simple, single input consumers.
    /// </summary>
    /// <typeparam name="TIn">The type of message input.</typeparam>
    public interface IConsumer<TIn>
    {
        /// <summary>
        /// Gets the input we receive messages on.
        /// </summary>
        Receiver<TIn> In { get; }
    }
}
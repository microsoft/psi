// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.

namespace Microsoft.Psi
{
    using System;

    /// <summary>
    /// Enables message passing between components.
    /// </summary>
    public interface IReceiver : IDisposable
    {
        /// <summary>
        /// Gets receiver owner object.
        /// </summary>
        object Owner { get; }

        /// <summary>
        /// Gets receiver source emitter.
        /// </summary>
        IEmitter Source { get; }
    }
}

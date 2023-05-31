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
        /// Gets receiver ID.
        /// </summary>
        int Id { get; }

        /// <summary>
        /// Gets receiver name.
        /// </summary>
        string Name { get; }

        /// <summary>
        /// Gets receiver type.
        /// </summary>
        Type Type { get; }

        /// <summary>
        /// Gets receiver owner object.
        /// </summary>
        object Owner { get; }

        /// <summary>
        /// Gets receiver source emitter.
        /// </summary>
        IEmitter Source { get; }

        /// <summary>
        /// Gets the envelope of the last message received by this receiver.
        /// </summary>
        Envelope LastEnvelope { get; }
    }
}

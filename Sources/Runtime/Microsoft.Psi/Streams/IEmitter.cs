// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.

namespace Microsoft.Psi
{
    using System;

    /// <summary>
    /// Enables message passing between components.
    /// </summary>
    public interface IEmitter
    {
        /// <summary>
        /// Gets emitter ID.
        /// </summary>
        int Id { get; }

        /// <summary>
        /// Gets emitter name.
        /// </summary>
        string Name { get; }

        /// <summary>
        /// Gets emitter type.
        /// </summary>
        Type Type { get; }

        /// <summary>
        /// Gets emitter owner object.
        /// </summary>
        object Owner { get; }

        /// <summary>
        /// Gets pipeline to which emitter belongs.
        /// </summary>
        Pipeline Pipeline { get; }

        /// <summary>
        /// Gets the envelope of the last message posted on this emitter.
        /// </summary>
        Envelope LastEnvelope { get; }

        /// <summary>
        /// Gets a value indicating whether emitter has subscribers.
        /// </summary>
        bool HasSubscribers { get; }

        /// <summary>
        /// Close emitter, causing all receivers to be unsubscribed.
        /// </summary>
        /// <param name="originatingTime">The originating time at which the emitter closes.</param>
        void Close(DateTime originatingTime);

        /// <summary>
        /// Enables debug visualization for this stream.
        /// </summary>
        /// <param name="debugName">An optional name to use in the visualization window.</param>
        /// <returns>The debug name of the stream, either as provided or the generated one if one was not specified.</returns>
        string DebugView(string debugName = null);
    }
}

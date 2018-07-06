// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.

namespace Microsoft.Psi.Components
{
    using System;

    /// <summary>
    /// Interface indicating that a component is a finite "source" of messages; generators, etc.
    /// Components that implement this interface should advise the pipeline when they are done posting messages.
    /// Reactive components that generate messages only in response to incoming messages should not implement this interface.
    /// A pipeline containing source components will shut down only once all sources have completed (or earlier if explicitly stopped/disposed).
    /// </summary>
    public interface IFiniteSourceComponent : ISourceComponent
    {
        /// <summary>
        /// Called once all the subscriptions are established.
        /// </summary>
        /// <param name="onCompleted">Delegate to call when the component finished generating messages</param>
        void Initialize(Action onCompleted);
    }
}

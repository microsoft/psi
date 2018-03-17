// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.

namespace Microsoft.Psi.Components
{
    using System;
    using Microsoft.Psi;

    /// <summary>
    /// Allows source components to bootstrap their message generation (e.g. start their own thread, or post a seed message).
    /// Components that implement this interface are required to advise the pipeline when they are done publishing messages.
    /// Reactive components that generate messages only in response to incoming messages don't need to implement this interface.
    /// </summary>
    public interface IStartable
    {
        /// <summary>
        /// Called once all the subscriptions are established.
        /// </summary>
        /// <param name="onCompleted">Delegate to call when the component finished generating messages</param>
        /// <param name="descriptor">If set, describes the playback constraints</param>
        void Start(Action onCompleted, ReplayDescriptor descriptor);

        /// <summary>
        /// Called when the pipeline is shutting down. The component is expected to stop generating new messages once this method completes.
        /// However, the component might still receive new messages (if it subscribed to other components) after this call and is expected to handle them.
        /// </summary>
        void Stop();
    }
}

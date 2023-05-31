// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.

namespace Microsoft.Psi.Components
{
    using System;

    /// <summary>
    /// Interface indicating that a component is a "source" of messages (e.g. sensor inputs, generators, etc.), or otherwise
    /// posts messages from outside a receiver (e.g. from an internal thread or an event handler that is not in response to
    /// incoming messages). Components that implement this interface should advise the pipeline when they are done posting
    /// messages via the <c>notifyCompletionTime</c> action delegate that is supplied when the pipeline calls the
    /// <see cref="Start"/> method. This delegate may be saved and invoked later once the pipeline completion time is known,
    /// or in the case of components that do not have the concept of "completion" (i.e. infinite sources), it may be invoked
    /// immediately with a completion time of <see cref="DateTime.MaxValue"/>. Reactive components that generate messages only
    /// in response to incoming messages should not implement this interface. A pipeline containing source components will
    /// shut down only once all sources have notified the pipeline of completion (or earlier if explicitly stopped/disposed).
    /// </summary>
    public interface ISourceComponent
    {
        /// <summary>
        /// Called by the pipeline to start the component once all the subscriptions are established.
        /// </summary>
        /// <param name="notifyCompletionTime">
        /// Delegate to call to notify the pipeline at what time the component will complete. Finite source components
        /// should invoke this delegate, passing in the time at which the component completes after which it will post
        /// no further messages. If this time is not yet known, the component may store a reference to the
        /// <paramref name="notifyCompletionTime"/> delegate and call it once the completion time is known. Source
        /// components that do not have the concept of "completion" must invoke <paramref name="notifyCompletionTime"/>
        /// with a completion time of <see cref="DateTime.MaxValue"/>. Similarly, reactive components that implement
        /// the <see cref="ISourceComponent"/> due to the fact that they post messages from outside of a receiver method
        /// should also notify with a completion time of <see cref="DateTime.MaxValue"/>.
        /// </param>
        void Start(Action<DateTime> notifyCompletionTime);

        /// <summary>
        /// Called by the pipeline when shutting down. The component should stop generating new messages once this
        /// method completes. However, the component might still receive new messages (if it is subscribed to other
        /// components) after this call and is expected to handle them.
        /// </summary>
        /// <param name="finalOriginatingTime">
        /// The last originating time of any message which may be posted, after which the component should stop
        /// posting non-reactive source messages.
        /// </param>
        /// <param name="notifyCompleted">
        /// Delegate to call to notify the pipeline that the component has completed posting non-reactive source
        /// messages. This delegate should be called once the component has posted its last non-reactive source
        /// message, but only up to (and possibly including) <paramref name="finalOriginatingTime"/>.
        /// </param>
        void Stop(DateTime finalOriginatingTime, Action notifyCompleted);
    }
}

// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.

namespace Microsoft.Psi
{
    /// <summary>
    /// Defines specifiers for global delivery policies.
    /// </summary>
    public enum DeliveryPolicySpec
    {
        /// <summary>
        /// Specifies the <see cref="DeliveryPolicy.Unlimited"/> delivery policy.
        /// </summary>
        Unlimited,

        /// <summary>
        /// Specifies the <see cref="DeliveryPolicy.LatestMessage"/> delivery policy.
        /// </summary>
        LatestMessage,

        /// <summary>
        /// Specifies the <see cref="DeliveryPolicy.Throttle"/> delivery policy.
        /// </summary>
        Throttle,

        /// <summary>
        /// Specifies the <see cref="DeliveryPolicy.SynchronousOrThrottle"/> delivery policy.
        /// </summary>
        SynchronousOrThrottle,
    }
}

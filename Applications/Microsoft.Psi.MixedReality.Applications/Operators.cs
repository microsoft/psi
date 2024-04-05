// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the Microsoft Research license.

namespace Microsoft.Psi.MixedReality.Applications
{
    using System.Linq;
    using MathNet.Spatial.Euclidean;
    using Microsoft.Psi;
    using Microsoft.Psi.Spatial.Euclidean;

    /// <summary>
    /// Implements helper stream operators.
    /// </summary>
    public static class Operators
    {
        /// <summary>
        /// Compute the head velocity.
        /// </summary>
        /// <param name="source">The stream containing the head coordinate system.</param>
        /// <param name="deliveryPolicy">An optional delivery policy.</param>
        /// <param name="name">An optional name of the stream operator.</param>
        /// <returns>A stream containing the head velocity.</returns>
        public static IProducer<CoordinateSystemVelocity3D> GetHeadVelocity(
            this IProducer<CoordinateSystem> source,
            DeliveryPolicy<CoordinateSystem> deliveryPolicy = null,
            string name = nameof(GetHeadVelocity))
            => source.Window(
                -1,
                1,
                values =>
                {
                    var origin = values.First().Data;
                    var destination = values.Last().Data;
                    if (origin != null && destination != null)
                    {
                        return new CoordinateSystemVelocity3D(origin, destination, values.Last().OriginatingTime - values.First().OriginatingTime);
                    }
                    else
                    {
                        return default;
                    }
                },
                deliveryPolicy ?? DeliveryPolicy.SynchronousOrThrottle,
                name);
    }
}

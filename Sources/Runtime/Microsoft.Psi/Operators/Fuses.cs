// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.

namespace Microsoft.Psi
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using Microsoft.Psi.Components;

    /// <summary>
    /// Extension methods that simplify operator usage.
    /// </summary>
    public static partial class Operators
    {
        #region Scalar fuse operators

        /// <summary>
        /// Fuse with values from a secondary stream based on a specified interpolator.
        /// </summary>
        /// <typeparam name="TPrimary">Type of primary messages.</typeparam>
        /// <typeparam name="TSecondary">Type of secondary messages.</typeparam>
        /// <typeparam name="TInterpolation">Type of the interpolation result.</typeparam>
        /// <typeparam name="TOut">Type of output messages.</typeparam>
        /// <param name="primary">Primary stream.</param>
        /// <param name="secondary">Secondary stream.</param>
        /// <param name="interpolator">Interpolator to use when fusing the streams.</param>
        /// <param name="outputCreator">Function mapping the primary and secondary messages to an output message type.</param>
        /// <param name="primaryDeliveryPolicy">An optional delivery policy for the primary stream.</param>
        /// <param name="secondaryDeliveryPolicy">An optional delivery policy for the secondary stream(s).</param>
        /// <returns>Stream of fused values.</returns>
        public static IProducer<TOut> Fuse<TPrimary, TSecondary, TInterpolation, TOut>(
            this IProducer<TPrimary> primary,
            IProducer<TSecondary> secondary,
            Interpolator<TSecondary, TInterpolation> interpolator,
            Func<TPrimary, TInterpolation, TOut> outputCreator,
            DeliveryPolicy<TPrimary> primaryDeliveryPolicy = null,
            DeliveryPolicy<TSecondary> secondaryDeliveryPolicy = null)
        {
            return Fuse(
                primary,
                new[] { secondary },
                interpolator,
                (m, secondaryArray) => outputCreator(m, secondaryArray[0]),
                primaryDeliveryPolicy,
                secondaryDeliveryPolicy);
        }

        /// <summary>
        /// Fuse with values from a secondary stream based on a specified interpolator.
        /// </summary>
        /// <typeparam name="TPrimary">Type of primary messages.</typeparam>
        /// <typeparam name="TSecondary">Type of secondary messages.</typeparam>
        /// <typeparam name="TInterpolation">Type of the interpolation result.</typeparam>
        /// <param name="primary">Primary stream.</param>
        /// <param name="secondary">Secondary stream.</param>
        /// <param name="interpolator">Interpolator to use when fusing the streams.</param>
        /// <param name="primaryDeliveryPolicy">An optional delivery policy for the primary stream.</param>
        /// <param name="secondaryDeliveryPolicy">An optional delivery policy for the secondary stream(s).</param>
        /// <returns>Stream of fused values.</returns>
        public static IProducer<(TPrimary, TInterpolation)> Fuse<TPrimary, TSecondary, TInterpolation>(
            this IProducer<TPrimary> primary,
            IProducer<TSecondary> secondary,
            Interpolator<TSecondary, TInterpolation> interpolator,
            DeliveryPolicy<TPrimary> primaryDeliveryPolicy = null,
            DeliveryPolicy<TSecondary> secondaryDeliveryPolicy = null)
        {
            return Fuse(primary, secondary, interpolator, ValueTuple.Create, primaryDeliveryPolicy, secondaryDeliveryPolicy);
        }

        #endregion Scalar fuse operators

        #region Tuple-flattening scalar fuse operators

        /// <summary>
        /// Fuse with values from a secondary stream based on a specified interpolator.
        /// </summary>
        /// <typeparam name="TPrimaryItem1">Type of item 1 of primary messages.</typeparam>
        /// <typeparam name="TPrimaryItem2">Type of item 2 of primary messages.</typeparam>
        /// <typeparam name="TSecondary">Type of secondary messages.</typeparam>
        /// <typeparam name="TInterpolation">Type of the interpolation result.</typeparam>
        /// <param name="primary">Primary stream of tuples (arity 2).</param>
        /// <param name="secondary">Secondary stream.</param>
        /// <param name="interpolator">Interpolator to use when fusing the streams.</param>
        /// <param name="primaryDeliveryPolicy">An optional delivery policy for the primary stream.</param>
        /// <param name="secondaryDeliveryPolicy">An optional delivery policy for the secondary stream(s).</param>
        /// <returns>Stream of fused tuple values flattened to arity 3.</returns>
        public static IProducer<(TPrimaryItem1, TPrimaryItem2, TInterpolation)> Fuse<TPrimaryItem1, TPrimaryItem2, TSecondary, TInterpolation>(
            this IProducer<(TPrimaryItem1, TPrimaryItem2)> primary,
            IProducer<TSecondary> secondary,
            Interpolator<TSecondary, TInterpolation> interpolator,
            DeliveryPolicy<(TPrimaryItem1, TPrimaryItem2)> primaryDeliveryPolicy = null,
            DeliveryPolicy<TSecondary> secondaryDeliveryPolicy = null)
        {
            return Fuse(
                primary,
                secondary,
                interpolator,
                (p, s) => (p.Item1, p.Item2, s),
                primaryDeliveryPolicy,
                secondaryDeliveryPolicy);
        }

        /// <summary>
        /// Fuse with values from a secondary stream based on a specified interpolator.
        /// </summary>
        /// <typeparam name="TPrimaryItem1">Type of item 1 of primary messages.</typeparam>
        /// <typeparam name="TPrimaryItem2">Type of item 2 of primary messages.</typeparam>
        /// <typeparam name="TPrimaryItem3">Type of item 3 of primary messages.</typeparam>
        /// <typeparam name="TSecondary">Type of secondary messages.</typeparam>
        /// <typeparam name="TInterpolation">Type of the interpolation result.</typeparam>
        /// <param name="primary">Primary stream of tuples (arity 3).</param>
        /// <param name="secondary">Secondary stream.</param>
        /// <param name="interpolator">Interpolator to use when fusing the streams.</param>
        /// <param name="primaryDeliveryPolicy">An optional delivery policy for the primary stream.</param>
        /// <param name="secondaryDeliveryPolicy">An optional delivery policy for the secondary stream(s).</param>
        /// <returns>Stream of fused tuple values flattened to arity 4.</returns>
        public static IProducer<(TPrimaryItem1, TPrimaryItem2, TPrimaryItem3, TInterpolation)> Fuse<TPrimaryItem1, TPrimaryItem2, TPrimaryItem3, TSecondary, TInterpolation>(
            this IProducer<(TPrimaryItem1, TPrimaryItem2, TPrimaryItem3)> primary,
            IProducer<TSecondary> secondary,
            Interpolator<TSecondary, TInterpolation> interpolator,
            DeliveryPolicy<(TPrimaryItem1, TPrimaryItem2, TPrimaryItem3)> primaryDeliveryPolicy = null,
            DeliveryPolicy<TSecondary> secondaryDeliveryPolicy = null)
        {
            return Fuse(
                primary,
                secondary,
                interpolator,
                (p, s) => (p.Item1, p.Item2, p.Item3, s),
                primaryDeliveryPolicy,
                secondaryDeliveryPolicy);
        }

        /// <summary>
        /// Fuse with values from a secondary stream based on a specified interpolator.
        /// </summary>
        /// <typeparam name="TPrimaryItem1">Type of item 1 of primary messages.</typeparam>
        /// <typeparam name="TPrimaryItem2">Type of item 2 of primary messages.</typeparam>
        /// <typeparam name="TPrimaryItem3">Type of item 3 of primary messages.</typeparam>
        /// <typeparam name="TPrimaryItem4">Type of item 4 of primary messages.</typeparam>
        /// <typeparam name="TSecondary">Type of secondary messages.</typeparam>
        /// <typeparam name="TInterpolation">Type of the interpolation result.</typeparam>
        /// <param name="primary">Primary stream of tuples (arity 4).</param>
        /// <param name="secondary">Secondary stream.</param>
        /// <param name="interpolator">Interpolator to use when fusing the streams.</param>
        /// <param name="primaryDeliveryPolicy">An optional delivery policy for the primary stream.</param>
        /// <param name="secondaryDeliveryPolicy">An optional delivery policy for the secondary stream(s).</param>
        /// <returns>Stream of fused tuple values flattened to arity 5.</returns>
        public static IProducer<(TPrimaryItem1, TPrimaryItem2, TPrimaryItem3, TPrimaryItem4, TInterpolation)> Fuse<TPrimaryItem1, TPrimaryItem2, TPrimaryItem3, TPrimaryItem4, TSecondary, TInterpolation>(
            this IProducer<(TPrimaryItem1, TPrimaryItem2, TPrimaryItem3, TPrimaryItem4)> primary,
            IProducer<TSecondary> secondary,
            Interpolator<TSecondary, TInterpolation> interpolator,
            DeliveryPolicy<(TPrimaryItem1, TPrimaryItem2, TPrimaryItem3, TPrimaryItem4)> primaryDeliveryPolicy = null,
            DeliveryPolicy<TSecondary> secondaryDeliveryPolicy = null)
        {
            return Fuse(
                primary,
                secondary,
                interpolator,
                (p, s) => (p.Item1, p.Item2, p.Item3, p.Item4, s),
                primaryDeliveryPolicy,
                secondaryDeliveryPolicy);
        }

        /// <summary>
        /// Fuse with values from a secondary stream based on a specified interpolator.
        /// </summary>
        /// <typeparam name="TPrimaryItem1">Type of item 1 of primary messages.</typeparam>
        /// <typeparam name="TPrimaryItem2">Type of item 2 of primary messages.</typeparam>
        /// <typeparam name="TPrimaryItem3">Type of item 3 of primary messages.</typeparam>
        /// <typeparam name="TPrimaryItem4">Type of item 4 of primary messages.</typeparam>
        /// <typeparam name="TPrimaryItem5">Type of item 5 of primary messages.</typeparam>
        /// <typeparam name="TSecondary">Type of secondary messages.</typeparam>
        /// <typeparam name="TInterpolation">Type of the interpolation result.</typeparam>
        /// <param name="primary">Primary stream of tuples (arity 5).</param>
        /// <param name="secondary">Secondary stream.</param>
        /// <param name="interpolator">Interpolator to use when fusing the streams.</param>
        /// <param name="primaryDeliveryPolicy">An optional delivery policy for the primary stream.</param>
        /// <param name="secondaryDeliveryPolicy">An optional delivery policy for the secondary stream(s).</param>
        /// <returns>Stream of fused tuple values flattened to arity 6.</returns>
        public static IProducer<(TPrimaryItem1, TPrimaryItem2, TPrimaryItem3, TPrimaryItem4, TPrimaryItem5, TInterpolation)> Fuse<TPrimaryItem1, TPrimaryItem2, TPrimaryItem3, TPrimaryItem4, TPrimaryItem5, TSecondary, TInterpolation>(
            this IProducer<(TPrimaryItem1, TPrimaryItem2, TPrimaryItem3, TPrimaryItem4, TPrimaryItem5)> primary,
            IProducer<TSecondary> secondary,
            Interpolator<TSecondary, TInterpolation> interpolator,
            DeliveryPolicy<(TPrimaryItem1, TPrimaryItem2, TPrimaryItem3, TPrimaryItem4, TPrimaryItem5)> primaryDeliveryPolicy = null,
            DeliveryPolicy<TSecondary> secondaryDeliveryPolicy = null)
        {
            return Fuse(
                primary,
                secondary,
                interpolator,
                (p, s) => (p.Item1, p.Item2, p.Item3, p.Item4, p.Item5, s),
                primaryDeliveryPolicy,
                secondaryDeliveryPolicy);
        }

        /// <summary>
        /// Fuse with values from a secondary stream based on a specified interpolator.
        /// </summary>
        /// <typeparam name="TPrimaryItem1">Type of item 1 of primary messages.</typeparam>
        /// <typeparam name="TPrimaryItem2">Type of item 2 of primary messages.</typeparam>
        /// <typeparam name="TPrimaryItem3">Type of item 3 of primary messages.</typeparam>
        /// <typeparam name="TPrimaryItem4">Type of item 4 of primary messages.</typeparam>
        /// <typeparam name="TPrimaryItem5">Type of item 5 of primary messages.</typeparam>
        /// <typeparam name="TPrimaryItem6">Type of item 6 of primary messages.</typeparam>
        /// <typeparam name="TSecondary">Type of secondary messages.</typeparam>
        /// <typeparam name="TInterpolation">Type of the interpolation result.</typeparam>
        /// <param name="primary">Primary stream of tuples (arity 6).</param>
        /// <param name="secondary">Secondary stream.</param>
        /// <param name="interpolator">Interpolator to use when fusing the streams.</param>
        /// <param name="primaryDeliveryPolicy">An optional delivery policy for the primary stream.</param>
        /// <param name="secondaryDeliveryPolicy">An optional delivery policy for the secondary stream(s).</param>
        /// <returns>Stream of fused tuple values flattened to arity 7.</returns>
        public static IProducer<(TPrimaryItem1, TPrimaryItem2, TPrimaryItem3, TPrimaryItem4, TPrimaryItem5, TPrimaryItem6, TInterpolation)> Fuse<TPrimaryItem1, TPrimaryItem2, TPrimaryItem3, TPrimaryItem4, TPrimaryItem5, TPrimaryItem6, TSecondary, TInterpolation>(
            this IProducer<(TPrimaryItem1, TPrimaryItem2, TPrimaryItem3, TPrimaryItem4, TPrimaryItem5, TPrimaryItem6)> primary,
            IProducer<TSecondary> secondary,
            Interpolator<TSecondary, TInterpolation> interpolator,
            DeliveryPolicy<(TPrimaryItem1, TPrimaryItem2, TPrimaryItem3, TPrimaryItem4, TPrimaryItem5, TPrimaryItem6)> primaryDeliveryPolicy = null,
            DeliveryPolicy<TSecondary> secondaryDeliveryPolicy = null)
        {
            return Fuse(
                primary,
                secondary,
                interpolator,
                (p, s) => (p.Item1, p.Item2, p.Item3, p.Item4, p.Item5, p.Item6, s),
                primaryDeliveryPolicy,
                secondaryDeliveryPolicy);
        }

        #endregion Tuple-flattening scalar fuse operators

        #region Reverse tuple-flattening scalar fuse operators

        /// <summary>
        /// Fuse with values from a secondary stream based on a specified interpolator.
        /// </summary>
        /// <typeparam name="TPrimary">Type of primary messages.</typeparam>
        /// <typeparam name="TSecondaryItem1">Type of item 1 of secondary messages.</typeparam>
        /// <typeparam name="TSecondaryItem2">Type of item 2 of secondary messages.</typeparam>
        /// <param name="primary">Primary stream.</param>
        /// <param name="secondary">Secondary stream of tuples (arity 2).</param>
        /// <param name="interpolator">Interpolator to use when fusing the streams.</param>
        /// <param name="primaryDeliveryPolicy">An optional delivery policy for the primary stream.</param>
        /// <param name="secondaryDeliveryPolicy">An optional delivery policy for the secondary stream(s).</param>
        /// <returns>Stream of fused tuple values flattened to arity 3.</returns>
        public static IProducer<(TPrimary, TSecondaryItem1, TSecondaryItem2)> Fuse<TPrimary, TSecondaryItem1, TSecondaryItem2>(
            this IProducer<TPrimary> primary,
            IProducer<(TSecondaryItem1, TSecondaryItem2)> secondary,
            Interpolator<(TSecondaryItem1, TSecondaryItem2), (TSecondaryItem1, TSecondaryItem2)> interpolator,
            DeliveryPolicy<TPrimary> primaryDeliveryPolicy = null,
            DeliveryPolicy<(TSecondaryItem1, TSecondaryItem2)> secondaryDeliveryPolicy = null)
        {
            return Fuse<TPrimary, (TSecondaryItem1, TSecondaryItem2), (TSecondaryItem1, TSecondaryItem2), (TPrimary, TSecondaryItem1, TSecondaryItem2)>(
                primary,
                secondary,
                interpolator,
                (p, s) => (p, s.Item1, s.Item2),
                primaryDeliveryPolicy,
                secondaryDeliveryPolicy);
        }

        /// <summary>
        /// Fuse with values from a secondary stream based on a specified interpolator.
        /// </summary>
        /// <typeparam name="TPrimary">Type of primary messages.</typeparam>
        /// <typeparam name="TSecondaryItem1">Type of item 1 of secondary messages.</typeparam>
        /// <typeparam name="TSecondaryItem2">Type of item 2 of secondary messages.</typeparam>
        /// <typeparam name="TSecondaryItem3">Type of item 3 of secondary messages.</typeparam>
        /// <param name="primary">Primary stream.</param>
        /// <param name="secondary">Secondary stream of tuples (arity 3).</param>
        /// <param name="interpolator">Interpolator to use when fusing the streams.</param>
        /// <param name="primaryDeliveryPolicy">An optional delivery policy for the primary stream.</param>
        /// <param name="secondaryDeliveryPolicy">An optional delivery policy for the secondary stream(s).</param>
        /// <returns>Stream of fused tuple values flattened to arity 4.</returns>
        public static IProducer<(TPrimary, TSecondaryItem1, TSecondaryItem2, TSecondaryItem3)> Fuse<TPrimary, TSecondaryItem1, TSecondaryItem2, TSecondaryItem3>(
            this IProducer<TPrimary> primary,
            IProducer<(TSecondaryItem1, TSecondaryItem2, TSecondaryItem3)> secondary,
            Interpolator<(TSecondaryItem1, TSecondaryItem2, TSecondaryItem3), (TSecondaryItem1, TSecondaryItem2, TSecondaryItem3)> interpolator,
            DeliveryPolicy<TPrimary> primaryDeliveryPolicy = null,
            DeliveryPolicy<(TSecondaryItem1, TSecondaryItem2, TSecondaryItem3)> secondaryDeliveryPolicy = null)
        {
            return Fuse(
                primary,
                secondary,
                interpolator,
                (p, s) => (p, s.Item1, s.Item2, s.Item3),
                primaryDeliveryPolicy,
                secondaryDeliveryPolicy);
        }

        /// <summary>
        /// Fuse with values from a secondary stream based on a specified interpolator.
        /// </summary>
        /// <typeparam name="TPrimary">Type of primary messages.</typeparam>
        /// <typeparam name="TSecondaryItem1">Type of item 1 of secondary messages.</typeparam>
        /// <typeparam name="TSecondaryItem2">Type of item 2 of secondary messages.</typeparam>
        /// <typeparam name="TSecondaryItem3">Type of item 3 of secondary messages.</typeparam>
        /// <typeparam name="TSecondaryItem4">Type of item 4 of secondary messages.</typeparam>
        /// <param name="primary">Primary stream.</param>
        /// <param name="secondary">Secondary stream of tuples (arity 4).</param>
        /// <param name="interpolator">Interpolator to use when fusing the streams.</param>
        /// <param name="primaryDeliveryPolicy">An optional delivery policy for the primary stream.</param>
        /// <param name="secondaryDeliveryPolicy">An optional delivery policy for the secondary stream(s).</param>
        /// <returns>Stream of fused tuple values flattened to arity 5.</returns>
        public static IProducer<(TPrimary, TSecondaryItem1, TSecondaryItem2, TSecondaryItem3, TSecondaryItem4)> Fuse<TPrimary, TSecondaryItem1, TSecondaryItem2, TSecondaryItem3, TSecondaryItem4>(
            this IProducer<TPrimary> primary,
            IProducer<(TSecondaryItem1, TSecondaryItem2, TSecondaryItem3, TSecondaryItem4)> secondary,
            Interpolator<(TSecondaryItem1, TSecondaryItem2, TSecondaryItem3, TSecondaryItem4), (TSecondaryItem1, TSecondaryItem2, TSecondaryItem3, TSecondaryItem4)> interpolator,
            DeliveryPolicy<TPrimary> primaryDeliveryPolicy = null,
            DeliveryPolicy<(TSecondaryItem1, TSecondaryItem2, TSecondaryItem3, TSecondaryItem4)> secondaryDeliveryPolicy = null)
        {
            return Fuse(
                primary,
                secondary,
                interpolator,
                (p, s) => (p, s.Item1, s.Item2, s.Item3, s.Item4),
                primaryDeliveryPolicy,
                secondaryDeliveryPolicy);
        }

        /// <summary>
        /// Fuse with values from a secondary stream based on a specified interpolator.
        /// </summary>
        /// <typeparam name="TPrimary">Type of primary messages.</typeparam>
        /// <typeparam name="TSecondaryItem1">Type of item 1 of secondary messages.</typeparam>
        /// <typeparam name="TSecondaryItem2">Type of item 2 of secondary messages.</typeparam>
        /// <typeparam name="TSecondaryItem3">Type of item 3 of secondary messages.</typeparam>
        /// <typeparam name="TSecondaryItem4">Type of item 4 of secondary messages.</typeparam>
        /// <typeparam name="TSecondaryItem5">Type of item 5 of secondary messages.</typeparam>
        /// <param name="primary">Primary stream.</param>
        /// <param name="secondary">Secondary stream of tuples (arity 5).</param>
        /// <param name="interpolator">Interpolator to use when fusing the streams.</param>
        /// <param name="primaryDeliveryPolicy">An optional delivery policy for the primary stream.</param>
        /// <param name="secondaryDeliveryPolicy">An optional delivery policy for the secondary stream(s).</param>
        /// <returns>Stream of fused tuple values flattened to arity 6.</returns>
        public static IProducer<(TPrimary, TSecondaryItem1, TSecondaryItem2, TSecondaryItem3, TSecondaryItem4, TSecondaryItem5)> Fuse<TPrimary, TSecondaryItem1, TSecondaryItem2, TSecondaryItem3, TSecondaryItem4, TSecondaryItem5>(
            this IProducer<TPrimary> primary,
            IProducer<(TSecondaryItem1, TSecondaryItem2, TSecondaryItem3, TSecondaryItem4, TSecondaryItem5)> secondary,
            Interpolator<(TSecondaryItem1, TSecondaryItem2, TSecondaryItem3, TSecondaryItem4, TSecondaryItem5), (TSecondaryItem1, TSecondaryItem2, TSecondaryItem3, TSecondaryItem4, TSecondaryItem5)> interpolator,
            DeliveryPolicy<TPrimary> primaryDeliveryPolicy = null,
            DeliveryPolicy<(TSecondaryItem1, TSecondaryItem2, TSecondaryItem3, TSecondaryItem4, TSecondaryItem5)> secondaryDeliveryPolicy = null)
        {
            return Fuse(
                primary,
                secondary,
                interpolator,
                (p, s) => (p, s.Item1, s.Item2, s.Item3, s.Item4, s.Item5),
                primaryDeliveryPolicy,
                secondaryDeliveryPolicy);
        }

        /// <summary>
        /// Fuse with values from a secondary stream based on a specified interpolator.
        /// </summary>
        /// <typeparam name="TPrimary">Type of primary messages.</typeparam>
        /// <typeparam name="TSecondaryItem1">Type of item 1 of secondary messages.</typeparam>
        /// <typeparam name="TSecondaryItem2">Type of item 2 of secondary messages.</typeparam>
        /// <typeparam name="TSecondaryItem3">Type of item 3 of secondary messages.</typeparam>
        /// <typeparam name="TSecondaryItem4">Type of item 4 of secondary messages.</typeparam>
        /// <typeparam name="TSecondaryItem5">Type of item 5 of secondary messages.</typeparam>
        /// <typeparam name="TSecondaryItem6">Type of item 6 of secondary messages.</typeparam>
        /// <param name="primary">Primary stream.</param>
        /// <param name="secondary">Secondary stream of tuples (arity 6).</param>
        /// <param name="interpolator">Interpolator to use when fusing the streams.</param>
        /// <param name="primaryDeliveryPolicy">An optional delivery policy for the primary stream.</param>
        /// <param name="secondaryDeliveryPolicy">An optional delivery policy for the secondary stream(s).</param>
        /// <returns>Stream of fused tuple values flattened to arity 7.</returns>
        public static IProducer<(TPrimary, TSecondaryItem1, TSecondaryItem2, TSecondaryItem3, TSecondaryItem4, TSecondaryItem5, TSecondaryItem6)> Fuse<TPrimary, TSecondaryItem1, TSecondaryItem2, TSecondaryItem3, TSecondaryItem4, TSecondaryItem5, TSecondaryItem6>(
            this IProducer<TPrimary> primary,
            IProducer<(TSecondaryItem1, TSecondaryItem2, TSecondaryItem3, TSecondaryItem4, TSecondaryItem5, TSecondaryItem6)> secondary,
            Interpolator<(TSecondaryItem1, TSecondaryItem2, TSecondaryItem3, TSecondaryItem4, TSecondaryItem5, TSecondaryItem6), (TSecondaryItem1, TSecondaryItem2, TSecondaryItem3, TSecondaryItem4, TSecondaryItem5, TSecondaryItem6)> interpolator,
            DeliveryPolicy<TPrimary> primaryDeliveryPolicy = null,
            DeliveryPolicy<(TSecondaryItem1, TSecondaryItem2, TSecondaryItem3, TSecondaryItem4, TSecondaryItem5, TSecondaryItem6)> secondaryDeliveryPolicy = null)
        {
            return Fuse(
                primary,
                secondary,
                interpolator,
                (p, s) => (p, s.Item1, s.Item2, s.Item3, s.Item4, s.Item5, s.Item6),
                primaryDeliveryPolicy,
                secondaryDeliveryPolicy);
        }

        #endregion Reverse tuple-flattening scalar fuse operators

        #region Vector fuse operators

        /// <summary>
        /// Fuses a primary stream with an enumeration of secondary streams based on a specified interpolator.
        /// </summary>
        /// <typeparam name="TPrimary">Type of primary stream messages.</typeparam>
        /// <typeparam name="TSecondary">Type of secondary stream messages.</typeparam>
        /// <typeparam name="TInterpolation">Type of the interpolation result.</typeparam>
        /// <typeparam name="TOut">Type of output stream messages.</typeparam>
        /// <param name="primary">Primary stream.</param>
        /// <param name="secondaries">Enumeration of secondary streams.</param>
        /// <param name="interpolator">Interpolator to use when fusing the streams.</param>
        /// <param name="outputCreator">Mapping function from primary and secondary messages to output.</param>
        /// <param name="primaryDeliveryPolicy">An optional delivery policy for the primary stream.</param>
        /// <param name="secondariesDeliveryPolicy">An optional delivery policy for the secondary stream(s).</param>
        /// <returns>Output stream.</returns>
        public static IProducer<TOut> Fuse<TPrimary, TSecondary, TInterpolation, TOut>(
            this IProducer<TPrimary> primary,
            IEnumerable<IProducer<TSecondary>> secondaries,
            Interpolator<TSecondary, TInterpolation> interpolator,
            Func<TPrimary, TInterpolation[], TOut> outputCreator,
            DeliveryPolicy<TPrimary> primaryDeliveryPolicy = null,
            DeliveryPolicy<TSecondary> secondariesDeliveryPolicy = null)
        {
            var fuse = new Fuse<TPrimary, TSecondary, TInterpolation, TOut>(
                primary.Out.Pipeline,
                interpolator,
                outputCreator,
                secondaries.Count(),
                null);

            primary.PipeTo(fuse.InPrimary, primaryDeliveryPolicy);

            var i = 0;
            foreach (var input in secondaries)
            {
                input.PipeTo(fuse.InSecondaries[i++], secondariesDeliveryPolicy);
            }

            return fuse;
        }

        /// <summary>
        /// Fuses an enumeration of streams into a vector stream, based on a specified interpolator.
        /// </summary>
        /// <typeparam name="TIn">Type of input stream messages.</typeparam>
        /// <param name="inputs">Collection of input streams.</param>
        /// <param name="interpolator">Interpolator to use when fusing the streams.</param>
        /// <param name="deliveryPolicy">An optional delivery policy to use for the streams.</param>
        /// <returns>Output stream.</returns>
        public static IProducer<TIn[]> Fuse<TIn>(
            this IEnumerable<IProducer<TIn>> inputs,
            Interpolator<TIn, TIn> interpolator,
            DeliveryPolicy<TIn> deliveryPolicy = null)
        {
            var count = inputs.Count();
            if (count > 1)
            {
                var buffer = new TIn[count];
                return Fuse(
                    inputs.First(),
                    inputs.Skip(1),
                    interpolator,
                    (m, secondaryArray) =>
                    {
                        buffer[0] = m;
                        Array.Copy(secondaryArray, 0, buffer, 1, count - 1);
                        return buffer;
                    },
                    deliveryPolicy,
                    deliveryPolicy);
            }
            else if (count == 1)
            {
                return inputs.First().Select(x => new[] { x }, deliveryPolicy);
            }
            else
            {
                throw new ArgumentException("Vector fuse with empty inputs collection.");
            }
        }

        #endregion Vector fuse operators
    }
}
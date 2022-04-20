// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.

namespace Microsoft.Psi
{
    using System;
    using Microsoft.Psi.Components;

    /// <summary>
    /// Extension methods that simplify operator usage.
    /// </summary>
    public static partial class Operators
    {
        #region Scalar pairs

        /// <summary>
        /// Pair with currently available value from a secondary stream.
        /// </summary>
        /// <typeparam name="TPrimary">Type of primary messages.</typeparam>
        /// <typeparam name="TSecondary">Type of secondary messages.</typeparam>
        /// <typeparam name="TOut">Type of output messages.</typeparam>
        /// <param name="primary">Primary stream.</param>
        /// <param name="secondary">Secondary stream.</param>
        /// <param name="outputCreator">Mapping function from primary/secondary pairs to output type.</param>
        /// <param name="initialValue">An initial value to be used until the first secondary message is received.</param>
        /// <param name="primaryDeliveryPolicy">An optional delivery policy for the primary stream.</param>
        /// <param name="secondaryDeliveryPolicy">An optional delivery policy for the secondary stream(s).</param>
        /// <param name="name">An optional name for the stream operator.</param>
        /// <returns>Stream of output values.</returns>
        public static IProducer<TOut> Pair<TPrimary, TSecondary, TOut>(
            this IProducer<TPrimary> primary,
            IProducer<TSecondary> secondary,
            Func<TPrimary, TSecondary, TOut> outputCreator,
            TSecondary initialValue,
            DeliveryPolicy<TPrimary> primaryDeliveryPolicy = null,
            DeliveryPolicy<TSecondary> secondaryDeliveryPolicy = null,
            string name = nameof(Pair))
        {
            return Pair(
                new Pair<TPrimary, TSecondary, TOut>(primary.Out.Pipeline, outputCreator, initialValue, name),
                primary,
                secondary,
                primaryDeliveryPolicy,
                secondaryDeliveryPolicy);
        }

        /// <summary>
        /// Pair with currently available value from a secondary stream.
        /// </summary>
        /// <typeparam name="TPrimary">Type of primary messages.</typeparam>
        /// <typeparam name="TSecondary">Type of secondary messages.</typeparam>
        /// <typeparam name="TOut">Type of output messages.</typeparam>
        /// <param name="primary">Primary stream.</param>
        /// <param name="secondary">Secondary stream.</param>
        /// <param name="outputCreator">Mapping function from primary/secondary pairs to output type.</param>
        /// <param name="primaryDeliveryPolicy">An optional delivery policy for the primary stream.</param>
        /// <param name="secondaryDeliveryPolicy">An optional delivery policy for the secondary stream(s).</param>
        /// <param name="name">An optional name for the stream operator.</param>
        /// <remarks>Primary messages will be dropped until the first secondary message is received (no `initialValue` provided).</remarks>
        /// <returns>Stream of output values.</returns>
        public static IProducer<TOut> Pair<TPrimary, TSecondary, TOut>(
            this IProducer<TPrimary> primary,
            IProducer<TSecondary> secondary,
            Func<TPrimary, TSecondary, TOut> outputCreator,
            DeliveryPolicy<TPrimary> primaryDeliveryPolicy = null,
            DeliveryPolicy<TSecondary> secondaryDeliveryPolicy = null,
            string name = nameof(Pair))
        {
            return Pair(
                new Pair<TPrimary, TSecondary, TOut>(primary.Out.Pipeline, outputCreator, name),
                primary,
                secondary,
                primaryDeliveryPolicy,
                secondaryDeliveryPolicy);
        }

        /// <summary>
        /// Pair with currently available value from a secondary stream.
        /// </summary>
        /// <typeparam name="TPrimary">Type of primary messages.</typeparam>
        /// <typeparam name="TSecondary">Type of secondary messages.</typeparam>
        /// <param name="primary">Primary stream.</param>
        /// <param name="secondary">Secondary stream.</param>
        /// <param name="initialValue">An initial value to be used until the first secondary message is received.</param>
        /// <param name="primaryDeliveryPolicy">An optional delivery policy for the primary stream.</param>
        /// <param name="secondaryDeliveryPolicy">An optional delivery policy for the secondary stream(s).</param>
        /// <param name="name">An optional name for the stream operator.</param>
        /// <returns>Stream of output tuples.</returns>
        public static IProducer<(TPrimary, TSecondary)> Pair<TPrimary, TSecondary>(
            this IProducer<TPrimary> primary,
            IProducer<TSecondary> secondary,
            TSecondary initialValue,
            DeliveryPolicy<TPrimary> primaryDeliveryPolicy = null,
            DeliveryPolicy<TSecondary> secondaryDeliveryPolicy = null,
            string name = nameof(Pair))
        {
            return Pair(
                new Pair<TPrimary, TSecondary, (TPrimary, TSecondary)>(primary.Out.Pipeline, ValueTuple.Create, initialValue, name),
                primary,
                secondary,
                primaryDeliveryPolicy,
                secondaryDeliveryPolicy);
        }

        /// <summary>
        /// Pair with currently available value from a secondary stream.
        /// </summary>
        /// <typeparam name="TPrimary">Type of primary messages.</typeparam>
        /// <typeparam name="TSecondary">Type of secondary messages.</typeparam>
        /// <param name="primary">Primary stream.</param>
        /// <param name="secondary">Secondary stream.</param>
        /// <param name="primaryDeliveryPolicy">An optional delivery policy for the primary stream.</param>
        /// <param name="secondaryDeliveryPolicy">An optional delivery policy for the secondary stream(s).</param>
        /// <param name="name">An optional name for the stream operator.</param>
        /// <remarks>Primary messages will be dropped until the first secondary message is received (no `initialValue` provided).</remarks>
        /// <returns>Stream of output tuples.</returns>
        public static IProducer<(TPrimary, TSecondary)> Pair<TPrimary, TSecondary>(
            this IProducer<TPrimary> primary,
            IProducer<TSecondary> secondary,
            DeliveryPolicy<TPrimary> primaryDeliveryPolicy = null,
            DeliveryPolicy<TSecondary> secondaryDeliveryPolicy = null,
            string name = nameof(Pair))
        {
            return Pair(
                new Pair<TPrimary, TSecondary, (TPrimary, TSecondary)>(primary.Out.Pipeline, ValueTuple.Create, name),
                primary,
                secondary,
                primaryDeliveryPolicy,
                secondaryDeliveryPolicy);
        }

        #endregion Scalar pairs

        #region Tuple-flattening scalar pairs

        /// <summary>
        /// Pair with currently available value from a secondary stream.
        /// </summary>
        /// <typeparam name="TPrimaryItem1">Type of item 1 of primary messages.</typeparam>
        /// <typeparam name="TPrimaryItem2">Type of item 2 of primary messages.</typeparam>
        /// <typeparam name="TSecondary">Type of secondary messages.</typeparam>
        /// <param name="primary">Primary stream of tuples (arity 2).</param>
        /// <param name="secondary">Secondary stream.</param>
        /// <param name="initialValue">An initial value to be used until the first secondary message is received.</param>
        /// <param name="primaryDeliveryPolicy">An optional delivery policy for the primary stream.</param>
        /// <param name="secondaryDeliveryPolicy">An optional delivery policy for the secondary stream(s).</param>
        /// <param name="name">An optional name for the stream operator.</param>
        /// <returns>Stream of output tuples flattened to arity 3.</returns>
        public static IProducer<(TPrimaryItem1, TPrimaryItem2, TSecondary)> Pair<TPrimaryItem1, TPrimaryItem2, TSecondary>(
            this IProducer<(TPrimaryItem1, TPrimaryItem2)> primary,
            IProducer<TSecondary> secondary,
            TSecondary initialValue,
            DeliveryPolicy<(TPrimaryItem1, TPrimaryItem2)> primaryDeliveryPolicy = null,
            DeliveryPolicy<TSecondary> secondaryDeliveryPolicy = null,
            string name = nameof(Pair))
        {
            return Pair(primary, secondary, (p, s) => (p.Item1, p.Item2, s), initialValue, primaryDeliveryPolicy, secondaryDeliveryPolicy, name);
        }

        /// <summary>
        /// Pair with currently available value from a secondary stream.
        /// </summary>
        /// <typeparam name="TPrimaryItem1">Type of item 1 of primary messages.</typeparam>
        /// <typeparam name="TPrimaryItem2">Type of item 2 of primary messages.</typeparam>
        /// <typeparam name="TSecondary">Type of secondary messages.</typeparam>
        /// <param name="primary">Primary stream of tuples (arity 2).</param>
        /// <param name="secondary">Secondary stream.</param>
        /// <param name="primaryDeliveryPolicy">An optional delivery policy for the primary stream.</param>
        /// <param name="secondaryDeliveryPolicy">An optional delivery policy for the secondary stream(s).</param>
        /// <param name="name">An optional name for the stream operator.</param>
        /// <returns>Stream of output tuples flattened to arity 3.</returns>
        public static IProducer<(TPrimaryItem1, TPrimaryItem2, TSecondary)> Pair<TPrimaryItem1, TPrimaryItem2, TSecondary>(
            this IProducer<(TPrimaryItem1, TPrimaryItem2)> primary,
            IProducer<TSecondary> secondary,
            DeliveryPolicy<(TPrimaryItem1, TPrimaryItem2)> primaryDeliveryPolicy = null,
            DeliveryPolicy<TSecondary> secondaryDeliveryPolicy = null,
            string name = nameof(Pair))
        {
            return Pair(primary, secondary, (p, s) => (p.Item1, p.Item2, s), primaryDeliveryPolicy, secondaryDeliveryPolicy, name);
        }

        /// <summary>
        /// Pair with currently available value from a secondary stream.
        /// </summary>
        /// <typeparam name="TPrimaryItem1">Type of item 1 of primary messages.</typeparam>
        /// <typeparam name="TPrimaryItem2">Type of item 2 of primary messages.</typeparam>
        /// <typeparam name="TPrimaryItem3">Type of item 3 of primary messages.</typeparam>
        /// <typeparam name="TSecondary">Type of secondary messages.</typeparam>
        /// <param name="primary">Primary stream of tuples (arity 3).</param>
        /// <param name="secondary">Secondary stream.</param>
        /// <param name="initialValue">An initial value to be used until the first secondary message is received.</param>
        /// <param name="primaryDeliveryPolicy">An optional delivery policy for the primary stream.</param>
        /// <param name="secondaryDeliveryPolicy">An optional delivery policy for the secondary stream(s).</param>
        /// <param name="name">An optional name for the stream operator.</param>
        /// <returns>Stream of output tuples flattened to arity 4.</returns>
        public static IProducer<(TPrimaryItem1, TPrimaryItem2, TPrimaryItem3, TSecondary)> Pair<TPrimaryItem1, TPrimaryItem2, TPrimaryItem3, TSecondary>(
            this IProducer<(TPrimaryItem1, TPrimaryItem2, TPrimaryItem3)> primary,
            IProducer<TSecondary> secondary,
            TSecondary initialValue,
            DeliveryPolicy<(TPrimaryItem1, TPrimaryItem2, TPrimaryItem3)> primaryDeliveryPolicy = null,
            DeliveryPolicy<TSecondary> secondaryDeliveryPolicy = null,
            string name = nameof(Pair))
        {
            return Pair(primary, secondary, (p, s) => (p.Item1, p.Item2, p.Item3, s), initialValue, primaryDeliveryPolicy, secondaryDeliveryPolicy, name);
        }

        /// <summary>
        /// Pair with currently available value from a secondary stream.
        /// </summary>
        /// <typeparam name="TPrimaryItem1">Type of item 1 of primary messages.</typeparam>
        /// <typeparam name="TPrimaryItem2">Type of item 2 of primary messages.</typeparam>
        /// <typeparam name="TPrimaryItem3">Type of item 3 of primary messages.</typeparam>
        /// <typeparam name="TSecondary">Type of secondary messages.</typeparam>
        /// <param name="primary">Primary stream of tuples (arity 3).</param>
        /// <param name="secondary">Secondary stream.</param>
        /// <param name="primaryDeliveryPolicy">An optional delivery policy for the primary stream.</param>
        /// <param name="secondaryDeliveryPolicy">An optional delivery policy for the secondary stream(s).</param>
        /// <param name="name">An optional name for the stream operator.</param>
        /// <returns>Stream of output tuples flattened to arity 4.</returns>
        public static IProducer<(TPrimaryItem1, TPrimaryItem2, TPrimaryItem3, TSecondary)> Pair<TPrimaryItem1, TPrimaryItem2, TPrimaryItem3, TSecondary>(
            this IProducer<(TPrimaryItem1, TPrimaryItem2, TPrimaryItem3)> primary,
            IProducer<TSecondary> secondary,
            DeliveryPolicy<(TPrimaryItem1, TPrimaryItem2, TPrimaryItem3)> primaryDeliveryPolicy = null,
            DeliveryPolicy<TSecondary> secondaryDeliveryPolicy = null,
            string name = nameof(Pair))
        {
            return Pair(primary, secondary, (p, s) => (p.Item1, p.Item2, p.Item3, s), primaryDeliveryPolicy, secondaryDeliveryPolicy, name);
        }

        /// <summary>
        /// Pair with currently available value from a secondary stream.
        /// </summary>
        /// <typeparam name="TPrimaryItem1">Type of item 1 of primary messages.</typeparam>
        /// <typeparam name="TPrimaryItem2">Type of item 2 of primary messages.</typeparam>
        /// <typeparam name="TPrimaryItem3">Type of item 3 of primary messages.</typeparam>
        /// <typeparam name="TPrimaryItem4">Type of item 4 of primary messages.</typeparam>
        /// <typeparam name="TSecondary">Type of secondary messages.</typeparam>
        /// <param name="primary">Primary stream of tuples (arity 4).</param>
        /// <param name="secondary">Secondary stream.</param>
        /// <param name="initialValue">An initial value to be used until the first secondary message is received.</param>
        /// <param name="primaryDeliveryPolicy">An optional delivery policy for the primary stream.</param>
        /// <param name="secondaryDeliveryPolicy">An optional delivery policy for the secondary stream(s).</param>
        /// <param name="name">An optional name for the stream operator.</param>
        /// <returns>Stream of output tuples flattened to arity 5.</returns>
        public static IProducer<(TPrimaryItem1, TPrimaryItem2, TPrimaryItem3, TPrimaryItem4, TSecondary)> Pair<TPrimaryItem1, TPrimaryItem2, TPrimaryItem3, TPrimaryItem4, TSecondary>(
            this IProducer<(TPrimaryItem1, TPrimaryItem2, TPrimaryItem3, TPrimaryItem4)> primary,
            IProducer<TSecondary> secondary,
            TSecondary initialValue,
            DeliveryPolicy<(TPrimaryItem1, TPrimaryItem2, TPrimaryItem3, TPrimaryItem4)> primaryDeliveryPolicy = null,
            DeliveryPolicy<TSecondary> secondaryDeliveryPolicy = null,
            string name = nameof(Pair))
        {
            return Pair(primary, secondary, (p, s) => (p.Item1, p.Item2, p.Item3, p.Item4, s), initialValue, primaryDeliveryPolicy, secondaryDeliveryPolicy, name);
        }

        /// <summary>
        /// Pair with currently available value from a secondary stream.
        /// </summary>
        /// <typeparam name="TPrimaryItem1">Type of item 1 of primary messages.</typeparam>
        /// <typeparam name="TPrimaryItem2">Type of item 2 of primary messages.</typeparam>
        /// <typeparam name="TPrimaryItem3">Type of item 3 of primary messages.</typeparam>
        /// <typeparam name="TPrimaryItem4">Type of item 4 of primary messages.</typeparam>
        /// <typeparam name="TSecondary">Type of secondary messages.</typeparam>
        /// <param name="primary">Primary stream of tuples (arity 4).</param>
        /// <param name="secondary">Secondary stream.</param>
        /// <param name="primaryDeliveryPolicy">An optional delivery policy for the primary stream.</param>
        /// <param name="secondaryDeliveryPolicy">An optional delivery policy for the secondary stream(s).</param>
        /// <param name="name">An optional name for the stream operator.</param>
        /// <returns>Stream of output tuples flattened to arity 5.</returns>
        public static IProducer<(TPrimaryItem1, TPrimaryItem2, TPrimaryItem3, TPrimaryItem4, TSecondary)> Pair<TPrimaryItem1, TPrimaryItem2, TPrimaryItem3, TPrimaryItem4, TSecondary>(
            this IProducer<(TPrimaryItem1, TPrimaryItem2, TPrimaryItem3, TPrimaryItem4)> primary,
            IProducer<TSecondary> secondary,
            DeliveryPolicy<(TPrimaryItem1, TPrimaryItem2, TPrimaryItem3, TPrimaryItem4)> primaryDeliveryPolicy = null,
            DeliveryPolicy<TSecondary> secondaryDeliveryPolicy = null,
            string name = nameof(Pair))
        {
            return Pair(primary, secondary, (p, s) => (p.Item1, p.Item2, p.Item3, p.Item4, s), primaryDeliveryPolicy, secondaryDeliveryPolicy, name);
        }

        /// <summary>
        /// Pair with currently available value from a secondary stream.
        /// </summary>
        /// <typeparam name="TPrimaryItem1">Type of item 1 of primary messages.</typeparam>
        /// <typeparam name="TPrimaryItem2">Type of item 2 of primary messages.</typeparam>
        /// <typeparam name="TPrimaryItem3">Type of item 3 of primary messages.</typeparam>
        /// <typeparam name="TPrimaryItem4">Type of item 4 of primary messages.</typeparam>
        /// <typeparam name="TPrimaryItem5">Type of item 5 of primary messages.</typeparam>
        /// <typeparam name="TSecondary">Type of secondary messages.</typeparam>
        /// <param name="primary">Primary stream of tuples (arity 5).</param>
        /// <param name="secondary">Secondary stream.</param>
        /// <param name="initialValue">An initial value to be used until the first secondary message is received.</param>
        /// <param name="primaryDeliveryPolicy">An optional delivery policy for the primary stream.</param>
        /// <param name="secondaryDeliveryPolicy">An optional delivery policy for the secondary stream(s).</param>
        /// <param name="name">An optional name for the stream operator.</param>
        /// <returns>Stream of output tuples flattened to arity 6.</returns>
        public static IProducer<(TPrimaryItem1, TPrimaryItem2, TPrimaryItem3, TPrimaryItem4, TPrimaryItem5, TSecondary)> Pair<TPrimaryItem1, TPrimaryItem2, TPrimaryItem3, TPrimaryItem4, TPrimaryItem5, TSecondary>(
            this IProducer<(TPrimaryItem1, TPrimaryItem2, TPrimaryItem3, TPrimaryItem4, TPrimaryItem5)> primary,
            IProducer<TSecondary> secondary,
            TSecondary initialValue,
            DeliveryPolicy<(TPrimaryItem1, TPrimaryItem2, TPrimaryItem3, TPrimaryItem4, TPrimaryItem5)> primaryDeliveryPolicy = null,
            DeliveryPolicy<TSecondary> secondaryDeliveryPolicy = null,
            string name = nameof(Pair))
        {
            return Pair(primary, secondary, (p, s) => (p.Item1, p.Item2, p.Item3, p.Item4, p.Item5, s), initialValue, primaryDeliveryPolicy, secondaryDeliveryPolicy, name);
        }

        /// <summary>
        /// Pair with currently available value from a secondary stream.
        /// </summary>
        /// <typeparam name="TPrimaryItem1">Type of item 1 of primary messages.</typeparam>
        /// <typeparam name="TPrimaryItem2">Type of item 2 of primary messages.</typeparam>
        /// <typeparam name="TPrimaryItem3">Type of item 3 of primary messages.</typeparam>
        /// <typeparam name="TPrimaryItem4">Type of item 4 of primary messages.</typeparam>
        /// <typeparam name="TPrimaryItem5">Type of item 5 of primary messages.</typeparam>
        /// <typeparam name="TSecondary">Type of secondary messages.</typeparam>
        /// <param name="primary">Primary stream of tuples (arity 5).</param>
        /// <param name="secondary">Secondary stream.</param>
        /// <param name="primaryDeliveryPolicy">An optional delivery policy for the primary stream.</param>
        /// <param name="secondaryDeliveryPolicy">An optional delivery policy for the secondary stream(s).</param>
        /// <param name="name">An optional name for the stream operator.</param>
        /// <returns>Stream of output tuples flattened to arity 6.</returns>
        public static IProducer<(TPrimaryItem1, TPrimaryItem2, TPrimaryItem3, TPrimaryItem4, TPrimaryItem5, TSecondary)> Pair<TPrimaryItem1, TPrimaryItem2, TPrimaryItem3, TPrimaryItem4, TPrimaryItem5, TSecondary>(
            this IProducer<(TPrimaryItem1, TPrimaryItem2, TPrimaryItem3, TPrimaryItem4, TPrimaryItem5)> primary,
            IProducer<TSecondary> secondary,
            DeliveryPolicy<(TPrimaryItem1, TPrimaryItem2, TPrimaryItem3, TPrimaryItem4, TPrimaryItem5)> primaryDeliveryPolicy = null,
            DeliveryPolicy<TSecondary> secondaryDeliveryPolicy = null,
            string name = nameof(Pair))
        {
            return Pair(primary, secondary, (p, s) => (p.Item1, p.Item2, p.Item3, p.Item4, p.Item5, s), primaryDeliveryPolicy, secondaryDeliveryPolicy, name);
        }

        /// <summary>
        /// Pair with currently available value from a secondary stream.
        /// </summary>
        /// <typeparam name="TPrimaryItem1">Type of item 1 of primary messages.</typeparam>
        /// <typeparam name="TPrimaryItem2">Type of item 2 of primary messages.</typeparam>
        /// <typeparam name="TPrimaryItem3">Type of item 3 of primary messages.</typeparam>
        /// <typeparam name="TPrimaryItem4">Type of item 4 of primary messages.</typeparam>
        /// <typeparam name="TPrimaryItem5">Type of item 5 of primary messages.</typeparam>
        /// <typeparam name="TPrimaryItem6">Type of item 6 of primary messages.</typeparam>
        /// <typeparam name="TSecondary">Type of secondary messages.</typeparam>
        /// <param name="primary">Primary stream of tuples (arity 6).</param>
        /// <param name="secondary">Secondary stream.</param>
        /// <param name="initialValue">An initial value to be used until the first secondary message is received.</param>
        /// <param name="primaryDeliveryPolicy">An optional delivery policy for the primary stream.</param>
        /// <param name="secondaryDeliveryPolicy">An optional delivery policy for the secondary stream(s).</param>
        /// <param name="name">An optional name for the stream operator.</param>
        /// <returns>Stream of output tuples flattened to arity 7.</returns>
        public static IProducer<(TPrimaryItem1, TPrimaryItem2, TPrimaryItem3, TPrimaryItem4, TPrimaryItem5, TPrimaryItem6, TSecondary)> Pair<TPrimaryItem1, TPrimaryItem2, TPrimaryItem3, TPrimaryItem4, TPrimaryItem5, TPrimaryItem6, TSecondary>(
            this IProducer<(TPrimaryItem1, TPrimaryItem2, TPrimaryItem3, TPrimaryItem4, TPrimaryItem5, TPrimaryItem6)> primary,
            IProducer<TSecondary> secondary,
            TSecondary initialValue,
            DeliveryPolicy<(TPrimaryItem1, TPrimaryItem2, TPrimaryItem3, TPrimaryItem4, TPrimaryItem5, TPrimaryItem6)> primaryDeliveryPolicy = null,
            DeliveryPolicy<TSecondary> secondaryDeliveryPolicy = null,
            string name = nameof(Pair))
        {
            return Pair(primary, secondary, (p, s) => (p.Item1, p.Item2, p.Item3, p.Item4, p.Item5, p.Item6, s), initialValue, primaryDeliveryPolicy, secondaryDeliveryPolicy, name);
        }

        /// <summary>
        /// Pair with currently available value from a secondary stream.
        /// </summary>
        /// <typeparam name="TPrimaryItem1">Type of item 1 of primary messages.</typeparam>
        /// <typeparam name="TPrimaryItem2">Type of item 2 of primary messages.</typeparam>
        /// <typeparam name="TPrimaryItem3">Type of item 3 of primary messages.</typeparam>
        /// <typeparam name="TPrimaryItem4">Type of item 4 of primary messages.</typeparam>
        /// <typeparam name="TPrimaryItem5">Type of item 5 of primary messages.</typeparam>
        /// <typeparam name="TPrimaryItem6">Type of item 6 of primary messages.</typeparam>
        /// <typeparam name="TSecondary">Type of secondary messages.</typeparam>
        /// <param name="primary">Primary stream of tuples (arity 6).</param>
        /// <param name="secondary">Secondary stream.</param>
        /// <param name="primaryDeliveryPolicy">An optional delivery policy for the primary stream.</param>
        /// <param name="secondaryDeliveryPolicy">An optional delivery policy for the secondary stream(s).</param>
        /// <param name="name">An optional name for the stream operator.</param>
        /// <returns>Stream of output tuples flattened to arity 7.</returns>
        public static IProducer<(TPrimaryItem1, TPrimaryItem2, TPrimaryItem3, TPrimaryItem4, TPrimaryItem5, TPrimaryItem6, TSecondary)> Pair<TPrimaryItem1, TPrimaryItem2, TPrimaryItem3, TPrimaryItem4, TPrimaryItem5, TPrimaryItem6, TSecondary>(
            this IProducer<(TPrimaryItem1, TPrimaryItem2, TPrimaryItem3, TPrimaryItem4, TPrimaryItem5, TPrimaryItem6)> primary,
            IProducer<TSecondary> secondary,
            DeliveryPolicy<(TPrimaryItem1, TPrimaryItem2, TPrimaryItem3, TPrimaryItem4, TPrimaryItem5, TPrimaryItem6)> primaryDeliveryPolicy = null,
            DeliveryPolicy<TSecondary> secondaryDeliveryPolicy = null,
            string name = nameof(Pair))
        {
            return Pair(primary, secondary, (p, s) => (p.Item1, p.Item2, p.Item3, p.Item4, p.Item5, p.Item6, s), primaryDeliveryPolicy, secondaryDeliveryPolicy, name);
        }

        #endregion Tuple-flattening scalar pairs

        #region Reverse tuple-flattening scalar pairs

        /// <summary>
        /// Pair with currently available value from a secondary stream.
        /// </summary>
        /// <typeparam name="TPrimary">Type of primary messages.</typeparam>
        /// <typeparam name="TSecondaryItem1">Type of item 1 of secondary messages.</typeparam>
        /// <typeparam name="TSecondaryItem2">Type of item 2 of secondary messages.</typeparam>
        /// <param name="primary">Primary stream.</param>
        /// <param name="secondary">Secondary stream of tuples (arity 2).</param>
        /// <param name="initialValue">An initial value to be used until the first secondary message is received.</param>
        /// <param name="primaryDeliveryPolicy">An optional delivery policy for the primary stream.</param>
        /// <param name="secondaryDeliveryPolicy">An optional delivery policy for the secondary stream(s).</param>
        /// <param name="name">An optional name for the stream operator.</param>
        /// <returns>Stream of output tuples flattened to arity 3.</returns>
        public static IProducer<(TPrimary, TSecondaryItem1, TSecondaryItem2)> Pair<TPrimary, TSecondaryItem1, TSecondaryItem2>(
            this IProducer<TPrimary> primary,
            IProducer<(TSecondaryItem1, TSecondaryItem2)> secondary,
            (TSecondaryItem1, TSecondaryItem2) initialValue,
            DeliveryPolicy<TPrimary> primaryDeliveryPolicy = null,
            DeliveryPolicy<(TSecondaryItem1, TSecondaryItem2)> secondaryDeliveryPolicy = null,
            string name = nameof(Pair))
        {
            return Pair(primary, secondary, (p, s) => (p, s.Item1, s.Item2), initialValue, primaryDeliveryPolicy, secondaryDeliveryPolicy, name);
        }

        /// <summary>
        /// Pair with currently available value from a secondary stream.
        /// </summary>
        /// <typeparam name="TPrimary">Type of primary messages.</typeparam>
        /// <typeparam name="TSecondaryItem1">Type of item 1 of secondary messages.</typeparam>
        /// <typeparam name="TSecondaryItem2">Type of item 2 of secondary messages.</typeparam>
        /// <param name="primary">Primary stream.</param>
        /// <param name="secondary">Secondary stream of tuples (arity 2).</param>
        /// <param name="primaryDeliveryPolicy">An optional delivery policy for the primary stream.</param>
        /// <param name="secondaryDeliveryPolicy">An optional delivery policy for the secondary stream(s).</param>
        /// <param name="name">An optional name for the stream operator.</param>
        /// <returns>Stream of output tuples flattened to arity 3.</returns>
        public static IProducer<(TPrimary, TSecondaryItem1, TSecondaryItem2)> Pair<TPrimary, TSecondaryItem1, TSecondaryItem2>(
            this IProducer<TPrimary> primary,
            IProducer<(TSecondaryItem1, TSecondaryItem2)> secondary,
            DeliveryPolicy<TPrimary> primaryDeliveryPolicy = null,
            DeliveryPolicy<(TSecondaryItem1, TSecondaryItem2)> secondaryDeliveryPolicy = null,
            string name = nameof(Pair))
        {
            return Pair(primary, secondary, (p, s) => (p, s.Item1, s.Item2), primaryDeliveryPolicy, secondaryDeliveryPolicy, name);
        }

        /// <summary>
        /// Pair with currently available value from a secondary stream.
        /// </summary>
        /// <typeparam name="TPrimary">Type of primary messages.</typeparam>
        /// <typeparam name="TSecondaryItem1">Type of item 1 of secondary messages.</typeparam>
        /// <typeparam name="TSecondaryItem2">Type of item 2 of secondary messages.</typeparam>
        /// <typeparam name="TSecondaryItem3">Type of item 3 of secondary messages.</typeparam>
        /// <param name="primary">Primary stream.</param>
        /// <param name="secondary">Secondary stream of tuples (arity 3).</param>
        /// <param name="initialValue">An initial value to be used until the first secondary message is received.</param>
        /// <param name="primaryDeliveryPolicy">An optional delivery policy for the primary stream.</param>
        /// <param name="secondaryDeliveryPolicy">An optional delivery policy for the secondary stream(s).</param>
        /// <param name="name">An optional name for the stream operator.</param>
        /// <returns>Stream of output tuples flattened to arity 4.</returns>
        public static IProducer<(TPrimary, TSecondaryItem1, TSecondaryItem2, TSecondaryItem3)> Pair<TPrimary, TSecondaryItem1, TSecondaryItem2, TSecondaryItem3>(
            this IProducer<TPrimary> primary,
            IProducer<(TSecondaryItem1, TSecondaryItem2, TSecondaryItem3)> secondary,
            (TSecondaryItem1, TSecondaryItem2, TSecondaryItem3) initialValue,
            DeliveryPolicy<TPrimary> primaryDeliveryPolicy = null,
            DeliveryPolicy<(TSecondaryItem1, TSecondaryItem2, TSecondaryItem3)> secondaryDeliveryPolicy = null,
            string name = nameof(Pair))
        {
            return Pair(primary, secondary, (p, s) => (p, s.Item1, s.Item2, s.Item3), initialValue, primaryDeliveryPolicy, secondaryDeliveryPolicy, name);
        }

        /// <summary>
        /// Pair with currently available value from a secondary stream.
        /// </summary>
        /// <typeparam name="TPrimary">Type of primary messages.</typeparam>
        /// <typeparam name="TSecondaryItem1">Type of item 1 of secondary messages.</typeparam>
        /// <typeparam name="TSecondaryItem2">Type of item 2 of secondary messages.</typeparam>
        /// <typeparam name="TSecondaryItem3">Type of item 3 of secondary messages.</typeparam>
        /// <param name="primary">Primary stream.</param>
        /// <param name="secondary">Secondary stream of tuples (arity 3).</param>
        /// <param name="primaryDeliveryPolicy">An optional delivery policy for the primary stream.</param>
        /// <param name="secondaryDeliveryPolicy">An optional delivery policy for the secondary stream(s).</param>
        /// <param name="name">An optional name for the stream operator.</param>
        /// <returns>Stream of output tuples flattened to arity 4.</returns>
        public static IProducer<(TPrimary, TSecondaryItem1, TSecondaryItem2, TSecondaryItem3)> Pair<TPrimary, TSecondaryItem1, TSecondaryItem2, TSecondaryItem3>(
            this IProducer<TPrimary> primary,
            IProducer<(TSecondaryItem1, TSecondaryItem2, TSecondaryItem3)> secondary,
            DeliveryPolicy<TPrimary> primaryDeliveryPolicy = null,
            DeliveryPolicy<(TSecondaryItem1, TSecondaryItem2, TSecondaryItem3)> secondaryDeliveryPolicy = null,
            string name = nameof(Pair))
        {
            return Pair(primary, secondary, (p, s) => (p, s.Item1, s.Item2, s.Item3), primaryDeliveryPolicy, secondaryDeliveryPolicy, name);
        }

        /// <summary>
        /// Pair with currently available value from a secondary stream.
        /// </summary>
        /// <typeparam name="TPrimary">Type of primary messages.</typeparam>
        /// <typeparam name="TSecondaryItem1">Type of item 1 of secondary messages.</typeparam>
        /// <typeparam name="TSecondaryItem2">Type of item 2 of secondary messages.</typeparam>
        /// <typeparam name="TSecondaryItem3">Type of item 3 of secondary messages.</typeparam>
        /// <typeparam name="TSecondaryItem4">Type of item 4 of secondary messages.</typeparam>
        /// <param name="primary">Primary stream.</param>
        /// <param name="secondary">Secondary stream of tuples (arity 4).</param>
        /// <param name="initialValue">An initial value to be used until the first secondary message is received.</param>
        /// <param name="primaryDeliveryPolicy">An optional delivery policy for the primary stream.</param>
        /// <param name="secondaryDeliveryPolicy">An optional delivery policy for the secondary stream(s).</param>
        /// <param name="name">An optional name for the stream operator.</param>
        /// <returns>Stream of output tuples flattened to arity 5.</returns>
        public static IProducer<(TPrimary, TSecondaryItem1, TSecondaryItem2, TSecondaryItem3, TSecondaryItem4)> Pair<TPrimary, TSecondaryItem1, TSecondaryItem2, TSecondaryItem3, TSecondaryItem4>(
            this IProducer<TPrimary> primary,
            IProducer<(TSecondaryItem1, TSecondaryItem2, TSecondaryItem3, TSecondaryItem4)> secondary,
            (TSecondaryItem1, TSecondaryItem2, TSecondaryItem3, TSecondaryItem4) initialValue,
            DeliveryPolicy<TPrimary> primaryDeliveryPolicy = null,
            DeliveryPolicy<(TSecondaryItem1, TSecondaryItem2, TSecondaryItem3, TSecondaryItem4)> secondaryDeliveryPolicy = null,
            string name = nameof(Pair))
        {
            return Pair(primary, secondary, (p, s) => (p, s.Item1, s.Item2, s.Item3, s.Item4), initialValue, primaryDeliveryPolicy, secondaryDeliveryPolicy, name);
        }

        /// <summary>
        /// Pair with currently available value from a secondary stream.
        /// </summary>
        /// <typeparam name="TPrimary">Type of primary messages.</typeparam>
        /// <typeparam name="TSecondaryItem1">Type of item 1 of secondary messages.</typeparam>
        /// <typeparam name="TSecondaryItem2">Type of item 2 of secondary messages.</typeparam>
        /// <typeparam name="TSecondaryItem3">Type of item 3 of secondary messages.</typeparam>
        /// <typeparam name="TSecondaryItem4">Type of item 4 of secondary messages.</typeparam>
        /// <param name="primary">Primary stream.</param>
        /// <param name="secondary">Secondary stream of tuples (arity 4).</param>
        /// <param name="primaryDeliveryPolicy">An optional delivery policy for the primary stream.</param>
        /// <param name="secondaryDeliveryPolicy">An optional delivery policy for the secondary stream(s).</param>
        /// <param name="name">An optional name for the stream operator.</param>
        /// <returns>Stream of output tuples flattened to arity 5.</returns>
        public static IProducer<(TPrimary, TSecondaryItem1, TSecondaryItem2, TSecondaryItem3, TSecondaryItem4)> Pair<TPrimary, TSecondaryItem1, TSecondaryItem2, TSecondaryItem3, TSecondaryItem4>(
            this IProducer<TPrimary> primary,
            IProducer<(TSecondaryItem1, TSecondaryItem2, TSecondaryItem3, TSecondaryItem4)> secondary,
            DeliveryPolicy<TPrimary> primaryDeliveryPolicy = null,
            DeliveryPolicy<(TSecondaryItem1, TSecondaryItem2, TSecondaryItem3, TSecondaryItem4)> secondaryDeliveryPolicy = null,
            string name = nameof(Pair))
        {
            return Pair(primary, secondary, (p, s) => (p, s.Item1, s.Item2, s.Item3, s.Item4), primaryDeliveryPolicy, secondaryDeliveryPolicy, name);
        }

        /// <summary>
        /// Pair with currently available value from a secondary stream.
        /// </summary>
        /// <typeparam name="TPrimary">Type of primary messages.</typeparam>
        /// <typeparam name="TSecondaryItem1">Type of item 1 of secondary messages.</typeparam>
        /// <typeparam name="TSecondaryItem2">Type of item 2 of secondary messages.</typeparam>
        /// <typeparam name="TSecondaryItem3">Type of item 3 of secondary messages.</typeparam>
        /// <typeparam name="TSecondaryItem4">Type of item 4 of secondary messages.</typeparam>
        /// <typeparam name="TSecondaryItem5">Type of item 5 of secondary messages.</typeparam>
        /// <param name="primary">Primary stream.</param>
        /// <param name="secondary">Secondary stream of tuples (arity 5).</param>
        /// <param name="initialValue">An initial value to be used until the first secondary message is received.</param>
        /// <param name="primaryDeliveryPolicy">An optional delivery policy for the primary stream.</param>
        /// <param name="secondaryDeliveryPolicy">An optional delivery policy for the secondary stream(s).</param>
        /// <param name="name">An optional name for the stream operator.</param>
        /// <returns>Stream of output tuples flattened to arity 6.</returns>
        public static IProducer<(TPrimary, TSecondaryItem1, TSecondaryItem2, TSecondaryItem3, TSecondaryItem4, TSecondaryItem5)> Pair<TPrimary, TSecondaryItem1, TSecondaryItem2, TSecondaryItem3, TSecondaryItem4, TSecondaryItem5>(
            this IProducer<TPrimary> primary,
            IProducer<(TSecondaryItem1, TSecondaryItem2, TSecondaryItem3, TSecondaryItem4, TSecondaryItem5)> secondary,
            (TSecondaryItem1, TSecondaryItem2, TSecondaryItem3, TSecondaryItem4, TSecondaryItem5) initialValue,
            DeliveryPolicy<TPrimary> primaryDeliveryPolicy = null,
            DeliveryPolicy<(TSecondaryItem1, TSecondaryItem2, TSecondaryItem3, TSecondaryItem4, TSecondaryItem5)> secondaryDeliveryPolicy = null,
            string name = nameof(Pair))
        {
            return Pair(primary, secondary, (p, s) => (p, s.Item1, s.Item2, s.Item3, s.Item4, s.Item5), initialValue, primaryDeliveryPolicy, secondaryDeliveryPolicy, name);
        }

        /// <summary>
        /// Pair with currently available value from a secondary stream.
        /// </summary>
        /// <typeparam name="TPrimary">Type of primary messages.</typeparam>
        /// <typeparam name="TSecondaryItem1">Type of item 1 of secondary messages.</typeparam>
        /// <typeparam name="TSecondaryItem2">Type of item 2 of secondary messages.</typeparam>
        /// <typeparam name="TSecondaryItem3">Type of item 3 of secondary messages.</typeparam>
        /// <typeparam name="TSecondaryItem4">Type of item 4 of secondary messages.</typeparam>
        /// <typeparam name="TSecondaryItem5">Type of item 5 of secondary messages.</typeparam>
        /// <param name="primary">Primary stream.</param>
        /// <param name="secondary">Secondary stream of tuples (arity 5).</param>
        /// <param name="primaryDeliveryPolicy">An optional delivery policy for the primary stream.</param>
        /// <param name="secondaryDeliveryPolicy">An optional delivery policy for the secondary stream(s).</param>
        /// <param name="name">An optional name for the stream operator.</param>
        /// <returns>Stream of output tuples flattened to arity 6.</returns>
        public static IProducer<(TPrimary, TSecondaryItem1, TSecondaryItem2, TSecondaryItem3, TSecondaryItem4, TSecondaryItem5)> Pair<TPrimary, TSecondaryItem1, TSecondaryItem2, TSecondaryItem3, TSecondaryItem4, TSecondaryItem5>(
            this IProducer<TPrimary> primary,
            IProducer<(TSecondaryItem1, TSecondaryItem2, TSecondaryItem3, TSecondaryItem4, TSecondaryItem5)> secondary,
            DeliveryPolicy<TPrimary> primaryDeliveryPolicy = null,
            DeliveryPolicy<(TSecondaryItem1, TSecondaryItem2, TSecondaryItem3, TSecondaryItem4, TSecondaryItem5)> secondaryDeliveryPolicy = null,
            string name = nameof(Pair))
        {
            return Pair(primary, secondary, (p, s) => (p, s.Item1, s.Item2, s.Item3, s.Item4, s.Item5), primaryDeliveryPolicy, secondaryDeliveryPolicy, name);
        }

        /// <summary>
        /// Pair with currently available value from a secondary stream.
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
        /// <param name="initialValue">An initial value to be used until the first secondary message is received.</param>
        /// <param name="primaryDeliveryPolicy">An optional delivery policy for the primary stream.</param>
        /// <param name="secondaryDeliveryPolicy">An optional delivery policy for the secondary stream(s).</param>
        /// <param name="name">An optional name for the stream operator.</param>
        /// <returns>Stream of output tuples flattened to arity 7.</returns>
        public static IProducer<(TPrimary, TSecondaryItem1, TSecondaryItem2, TSecondaryItem3, TSecondaryItem4, TSecondaryItem5, TSecondaryItem6)> Pair<TPrimary, TSecondaryItem1, TSecondaryItem2, TSecondaryItem3, TSecondaryItem4, TSecondaryItem5, TSecondaryItem6>(
            this IProducer<TPrimary> primary,
            IProducer<(TSecondaryItem1, TSecondaryItem2, TSecondaryItem3, TSecondaryItem4, TSecondaryItem5, TSecondaryItem6)> secondary,
            (TSecondaryItem1, TSecondaryItem2, TSecondaryItem3, TSecondaryItem4, TSecondaryItem5, TSecondaryItem6) initialValue,
            DeliveryPolicy<TPrimary> primaryDeliveryPolicy = null,
            DeliveryPolicy<(TSecondaryItem1, TSecondaryItem2, TSecondaryItem3, TSecondaryItem4, TSecondaryItem5, TSecondaryItem6)> secondaryDeliveryPolicy = null,
            string name = nameof(Pair))
        {
            return Pair(primary, secondary, (p, s) => (p, s.Item1, s.Item2, s.Item3, s.Item4, s.Item5, s.Item6), initialValue, primaryDeliveryPolicy, secondaryDeliveryPolicy, name);
        }

        /// <summary>
        /// Pair with currently available value from a secondary stream.
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
        /// <param name="primaryDeliveryPolicy">An optional delivery policy for the primary stream.</param>
        /// <param name="secondaryDeliveryPolicy">An optional delivery policy for the secondary stream(s).</param>
        /// <param name="name">An optional name for the stream operator.</param>
        /// <returns>Stream of output tuples flattened to arity 7.</returns>
        public static IProducer<(TPrimary, TSecondaryItem1, TSecondaryItem2, TSecondaryItem3, TSecondaryItem4, TSecondaryItem5, TSecondaryItem6)> Pair<TPrimary, TSecondaryItem1, TSecondaryItem2, TSecondaryItem3, TSecondaryItem4, TSecondaryItem5, TSecondaryItem6>(
            this IProducer<TPrimary> primary,
            IProducer<(TSecondaryItem1, TSecondaryItem2, TSecondaryItem3, TSecondaryItem4, TSecondaryItem5, TSecondaryItem6)> secondary,
            DeliveryPolicy<TPrimary> primaryDeliveryPolicy = null,
            DeliveryPolicy<(TSecondaryItem1, TSecondaryItem2, TSecondaryItem3, TSecondaryItem4, TSecondaryItem5, TSecondaryItem6)> secondaryDeliveryPolicy = null,
            string name = nameof(Pair))
        {
            return Pair(primary, secondary, (p, s) => (p, s.Item1, s.Item2, s.Item3, s.Item4, s.Item5, s.Item6), primaryDeliveryPolicy, secondaryDeliveryPolicy, name);
        }

        #endregion Reverse tuple-flattening scalar pairs

        private static IProducer<TOut> Pair<TPrimary, TSecondary, TOut>(
            Pair<TPrimary, TSecondary, TOut> pair,
            IProducer<TPrimary> primary,
            IProducer<TSecondary> secondary,
            DeliveryPolicy<TPrimary> primaryDeliveryPolicy = null,
            DeliveryPolicy<TSecondary> secondaryDeliveryPolicy = null)
        {
            primary.PipeTo(pair.InPrimary, primaryDeliveryPolicy);
            secondary.PipeTo(pair.InSecondary, secondaryDeliveryPolicy);
            return pair;
        }
    }
}
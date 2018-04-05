// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.

namespace Microsoft.Psi
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using Microsoft.Psi.Components;

    /// <summary>
    /// Extension methods that simplify operator usage
    /// </summary>
    public static partial class Operators
    {
#region scalar joins

        /// <summary>
        /// Join with values from a secondary stream.
        /// </summary>
        /// <typeparam name="TPrimary">Type of primary messages.</typeparam>
        /// <typeparam name="TSecondary">Type of secondary messages.</typeparam>
        /// <typeparam name="TOut">Type of output messages.</typeparam>
        /// <param name="primary">Primary stream.</param>
        /// <param name="secondary">Secondary stream.</param>
        /// <param name="interpolator">Interpolator determining match behavior and tolerance.</param>
        /// <param name="outputCreator">Function mapping the primary and secondary messages to an output message type.</param>
        /// <returns>Stream of joined values.</returns>
        public static IProducer<TOut> Join<TPrimary, TSecondary, TOut>(
            this IProducer<TPrimary> primary,
            IProducer<TSecondary> secondary,
            Match.Interpolator<TSecondary> interpolator,
            Func<TPrimary, TSecondary, TOut> outputCreator)
        {
            return Join(
                primary,
                new[] { secondary },
                interpolator,
                (m, sArr) => outputCreator(m, sArr[0]));
        }

        /// <summary>
        /// Join with values from a secondary stream.
        /// </summary>
        /// <remarks>Uses `Match.Exact` interpolator.</remarks>
        /// <typeparam name="TPrimary">Type of primary messages.</typeparam>
        /// <typeparam name="TSecondary">Type of secondary messages.</typeparam>
        /// <param name="primary">Primary stream.</param>
        /// <param name="secondary">Secondary stream.</param>
        /// <returns>Stream of joined values.</returns>
        public static IProducer<ValueTuple<TPrimary, TSecondary>> Join<TPrimary, TSecondary>(
            this IProducer<TPrimary> primary,
            IProducer<TSecondary> secondary)
        {
            return Join(primary, secondary, Match.Exact<TSecondary>());
        }

        /// <summary>
        /// Join with values from a secondary stream.
        /// </summary>
        /// <typeparam name="TPrimary">Type of primary messages.</typeparam>
        /// <typeparam name="TSecondary">Type of secondary messages.</typeparam>
        /// <param name="primary">Primary stream.</param>
        /// <param name="secondary">Secondary stream.</param>
        /// <param name="matchTolerance">Time span of match tolerance.</param>
        /// <returns>Stream of joined values.</returns>
        public static IProducer<ValueTuple<TPrimary, TSecondary>> Join<TPrimary, TSecondary>(
            this IProducer<TPrimary> primary,
            IProducer<TSecondary> secondary,
            TimeSpan matchTolerance)
        {
            return Join(primary, secondary, new RelativeTimeInterval(-matchTolerance, matchTolerance));
        }

        /// <summary>
        /// Join with values from a secondary stream.
        /// </summary>
        /// <typeparam name="TPrimary">Type of primary messages.</typeparam>
        /// <typeparam name="TSecondary">Type of secondary messages.</typeparam>
        /// <param name="primary">Primary stream.</param>
        /// <param name="secondary">Secondary stream.</param>
        /// <param name="matchWindow">Relative time interval of match tolerance.</param>
        /// <returns>Stream of joined values.</returns>
        public static IProducer<ValueTuple<TPrimary, TSecondary>> Join<TPrimary, TSecondary>(
            this IProducer<TPrimary> primary,
            IProducer<TSecondary> secondary,
            RelativeTimeInterval matchWindow)
        {
            return Join(
                primary,
                secondary,
                Match.Best<TSecondary>(matchWindow),
                ValueTuple.Create);
        }

        /// <summary>
        /// Join with values from a secondary stream.
        /// </summary>
        /// <typeparam name="TPrimary">Type of primary messages.</typeparam>
        /// <typeparam name="TSecondary">Type of secondary messages.</typeparam>
        /// <param name="primary">Primary stream.</param>
        /// <param name="secondary">Secondary stream.</param>
        /// <param name="interpolator">Interpolator determining match behavior and tolerance.</param>
        /// <returns>Stream of joined values.</returns>
        public static IProducer<ValueTuple<TPrimary, TSecondary>> Join<TPrimary, TSecondary>(
            this IProducer<TPrimary> primary,
            IProducer<TSecondary> secondary,
            Match.Interpolator<TSecondary> interpolator)
        {
            return Join(primary, secondary, interpolator, ValueTuple.Create);
        }

#endregion scalar joins

#region tuple-flattening scalar joins

        /// <summary>
        /// Join with values from a secondary stream.
        /// </summary>
        /// <typeparam name="TPrimaryItem1">Type of item 1 of primary messages.</typeparam>
        /// <typeparam name="TPrimaryItem2">Type of item 2 of primary messages.</typeparam>
        /// <typeparam name="TSecondary">Type of secondary messages.</typeparam>
        /// <param name="primary">Primary stream of tuples (arity 2).</param>
        /// <param name="secondary">Secondary stream.</param>
        /// <param name="interpolator">Interpolator determining match behavior and tolerance.</param>
        /// <returns>Stream of joined (`ValueTuple`) values flattened to arity 3.</returns>
        public static IProducer<ValueTuple<TPrimaryItem1, TPrimaryItem2, TSecondary>> Join<TPrimaryItem1, TPrimaryItem2, TSecondary>(
            this IProducer<ValueTuple<TPrimaryItem1, TPrimaryItem2>> primary,
            IProducer<TSecondary> secondary,
            Match.Interpolator<TSecondary> interpolator)
        {
            return Join(
                primary,
                secondary,
                interpolator,
                (p, s) => ValueTuple.Create(p.Item1, p.Item2, s));
        }

        /// <summary>
        /// Join with values from a secondary stream.
        /// </summary>
        /// <remarks>Uses `Match.Exact` interpolator.</remarks>
        /// <typeparam name="TPrimaryItem1">Type of item 1 of primary messages.</typeparam>
        /// <typeparam name="TPrimaryItem2">Type of item 2 of primary messages.</typeparam>
        /// <typeparam name="TSecondary">Type of secondary messages.</typeparam>
        /// <param name="primary">Primary stream of tuples (arity 2).</param>
        /// <param name="secondary">Secondary stream.</param>
        /// <returns>Stream of joined (`ValueTuple`) values flattened to arity 3.</returns>
        public static IProducer<ValueTuple<TPrimaryItem1, TPrimaryItem2, TSecondary>> Join<TPrimaryItem1, TPrimaryItem2, TSecondary>(
            this IProducer<ValueTuple<TPrimaryItem1, TPrimaryItem2>> primary,
            IProducer<TSecondary> secondary)
        {
            return Join(
                primary,
                secondary,
                Match.Exact<TSecondary>(),
                (p, s) => ValueTuple.Create(p.Item1, p.Item2, s));
        }

        /// <summary>
        /// Join with values from a secondary stream.
        /// </summary>
        /// <typeparam name="TPrimaryItem1">Type of item 1 of primary messages.</typeparam>
        /// <typeparam name="TPrimaryItem2">Type of item 2 of primary messages.</typeparam>
        /// <typeparam name="TSecondary">Type of secondary messages.</typeparam>
        /// <param name="primary">Primary stream of tuples (arity 2).</param>
        /// <param name="secondary">Secondary stream.</param>
        /// <param name="matchTolerance">Time span of match tolerance.</param>
        /// <returns>Stream of joined (`ValueTuple`) values flattened to arity 3.</returns>
        public static IProducer<ValueTuple<TPrimaryItem1, TPrimaryItem2, TSecondary>> Join<TPrimaryItem1, TPrimaryItem2, TSecondary>(
            this IProducer<ValueTuple<TPrimaryItem1, TPrimaryItem2>> primary,
            IProducer<TSecondary> secondary,
            TimeSpan matchTolerance)
        {
            return Join(
                primary,
                secondary,
                Match.Best<TSecondary>(matchTolerance),
                (p, s) => ValueTuple.Create(p.Item1, p.Item2, s));
        }

        /// <summary>
        /// Join with values from a secondary stream.
        /// </summary>
        /// <typeparam name="TPrimaryItem1">Type of item 1 of primary messages.</typeparam>
        /// <typeparam name="TPrimaryItem2">Type of item 2 of primary messages.</typeparam>
        /// <typeparam name="TSecondary">Type of secondary messages.</typeparam>
        /// <param name="primary">Primary stream of tuples (arity 2).</param>
        /// <param name="secondary">Secondary stream.</param>
        /// <param name="matchWindow">Relative time interval of match tolerance.</param>
        /// <returns>Stream of joined (`ValueTuple`) values flattened to arity 3.</returns>
        public static IProducer<ValueTuple<TPrimaryItem1, TPrimaryItem2, TSecondary>> Join<TPrimaryItem1, TPrimaryItem2, TSecondary>(
            this IProducer<ValueTuple<TPrimaryItem1, TPrimaryItem2>> primary,
            IProducer<TSecondary> secondary,
            RelativeTimeInterval matchWindow)
        {
            return Join(
                primary,
                secondary,
                Match.Best<TSecondary>(matchWindow),
                (p, s) => ValueTuple.Create(p.Item1, p.Item2, s));
        }

        /// <summary>
        /// Join with values from a secondary stream.
        /// </summary>
        /// <typeparam name="TPrimaryItem1">Type of item 1 of primary messages.</typeparam>
        /// <typeparam name="TPrimaryItem2">Type of item 2 of primary messages.</typeparam>
        /// <typeparam name="TPrimaryItem3">Type of item 3 of primary messages.</typeparam>
        /// <typeparam name="TSecondary">Type of secondary messages.</typeparam>
        /// <param name="primary">Primary stream of tuples (arity 3).</param>
        /// <param name="secondary">Secondary stream.</param>
        /// <param name="interpolator">Interpolator determining match behavior and tolerance.</param>
        /// <returns>Stream of joined (`ValueTuple`) values flattened to arity 4.</returns>
        public static IProducer<ValueTuple<TPrimaryItem1, TPrimaryItem2, TPrimaryItem3, TSecondary>> Join<TPrimaryItem1, TPrimaryItem2, TPrimaryItem3, TSecondary>(
            this IProducer<ValueTuple<TPrimaryItem1, TPrimaryItem2, TPrimaryItem3>> primary,
            IProducer<TSecondary> secondary,
            Match.Interpolator<TSecondary> interpolator)
        {
            return Join(
                primary,
                secondary,
                interpolator,
                (p, s) => ValueTuple.Create(p.Item1, p.Item2, p.Item3, s));
        }

        /// <summary>
        /// Join with values from a secondary stream.
        /// </summary>
        /// <remarks>Uses `Match.Exact` interpolator.</remarks>
        /// <typeparam name="TPrimaryItem1">Type of item 1 of primary messages.</typeparam>
        /// <typeparam name="TPrimaryItem2">Type of item 2 of primary messages.</typeparam>
        /// <typeparam name="TPrimaryItem3">Type of item 3 of primary messages.</typeparam>
        /// <typeparam name="TSecondary">Type of secondary messages.</typeparam>
        /// <param name="primary">Primary stream of tuples (arity 3).</param>
        /// <param name="secondary">Secondary stream.</param>
        /// <returns>Stream of joined (`ValueTuple`) values flattened to arity 4.</returns>
        public static IProducer<ValueTuple<TPrimaryItem1, TPrimaryItem2, TPrimaryItem3, TSecondary>> Join<TPrimaryItem1, TPrimaryItem2, TPrimaryItem3, TSecondary>(
            this IProducer<ValueTuple<TPrimaryItem1, TPrimaryItem2, TPrimaryItem3>> primary,
            IProducer<TSecondary> secondary)
        {
            return Join(
                primary,
                secondary,
                Match.Exact<TSecondary>(),
                (p, s) => ValueTuple.Create(p.Item1, p.Item2, p.Item3, s));
        }

        /// <summary>
        /// Join with values from a secondary stream.
        /// </summary>
        /// <typeparam name="TPrimaryItem1">Type of item 1 of primary messages.</typeparam>
        /// <typeparam name="TPrimaryItem2">Type of item 2 of primary messages.</typeparam>
        /// <typeparam name="TPrimaryItem3">Type of item 3 of primary messages.</typeparam>
        /// <typeparam name="TSecondary">Type of secondary messages.</typeparam>
        /// <param name="primary">Primary stream of tuples (arity 3).</param>
        /// <param name="secondary">Secondary stream.</param>
        /// <param name="matchTolerance">Time span of match tolerance.</param>
        /// <returns>Stream of joined (`ValueTuple`) values flattened to arity 4.</returns>
        public static IProducer<ValueTuple<TPrimaryItem1, TPrimaryItem2, TPrimaryItem3, TSecondary>> Join<TPrimaryItem1, TPrimaryItem2, TPrimaryItem3, TSecondary>(
            this IProducer<ValueTuple<TPrimaryItem1, TPrimaryItem2, TPrimaryItem3>> primary,
            IProducer<TSecondary> secondary,
            TimeSpan matchTolerance)
        {
            return Join(
                primary,
                secondary,
                Match.Best<TSecondary>(matchTolerance),
                (p, s) => ValueTuple.Create(p.Item1, p.Item2, p.Item3, s));
        }

        /// <summary>
        /// Join with values from a secondary stream.
        /// </summary>
        /// <typeparam name="TPrimaryItem1">Type of item 1 of primary messages.</typeparam>
        /// <typeparam name="TPrimaryItem2">Type of item 2 of primary messages.</typeparam>
        /// <typeparam name="TPrimaryItem3">Type of item 3 of primary messages.</typeparam>
        /// <typeparam name="TSecondary">Type of secondary messages.</typeparam>
        /// <param name="primary">Primary stream of tuples (arity 3).</param>
        /// <param name="secondary">Secondary stream.</param>
        /// <param name="matchWindow">Relative time interval of match tolerance.</param>
        /// <returns>Stream of joined (`ValueTuple`) values flattened to arity 4.</returns>
        public static IProducer<ValueTuple<TPrimaryItem1, TPrimaryItem2, TPrimaryItem3, TSecondary>> Join<TPrimaryItem1, TPrimaryItem2, TPrimaryItem3, TSecondary>(
            this IProducer<ValueTuple<TPrimaryItem1, TPrimaryItem2, TPrimaryItem3>> primary,
            IProducer<TSecondary> secondary,
            RelativeTimeInterval matchWindow)
        {
            return Join(
                primary,
                secondary,
                Match.Best<TSecondary>(matchWindow),
                (p, s) => ValueTuple.Create(p.Item1, p.Item2, p.Item3, s));
        }

        /// <summary>
        /// Join with values from a secondary stream.
        /// </summary>
        /// <typeparam name="TPrimaryItem1">Type of item 1 of primary messages.</typeparam>
        /// <typeparam name="TPrimaryItem2">Type of item 2 of primary messages.</typeparam>
        /// <typeparam name="TPrimaryItem3">Type of item 3 of primary messages.</typeparam>
        /// <typeparam name="TPrimaryItem4">Type of item 4 of primary messages.</typeparam>
        /// <typeparam name="TSecondary">Type of secondary messages.</typeparam>
        /// <param name="primary">Primary stream of tuples (arity 4).</param>
        /// <param name="secondary">Secondary stream.</param>
        /// <param name="interpolator">Interpolator determining match behavior and tolerance.</param>
        /// <returns>Stream of joined (`ValueTuple`) values flattened to arity 5.</returns>
        public static IProducer<ValueTuple<TPrimaryItem1, TPrimaryItem2, TPrimaryItem3, TPrimaryItem4, TSecondary>> Join<TPrimaryItem1, TPrimaryItem2, TPrimaryItem3, TPrimaryItem4, TSecondary>(
            this IProducer<ValueTuple<TPrimaryItem1, TPrimaryItem2, TPrimaryItem3, TPrimaryItem4>> primary,
            IProducer<TSecondary> secondary,
            Match.Interpolator<TSecondary> interpolator)
        {
            return Join(
                primary,
                secondary,
                interpolator,
                (p, s) => ValueTuple.Create(p.Item1, p.Item2, p.Item3, p.Item4, s));
        }

        /// <summary>
        /// Join with values from a secondary stream.
        /// </summary>
        /// <remarks>Uses `Match.Exact` interpolator.</remarks>
        /// <typeparam name="TPrimaryItem1">Type of item 1 of primary messages.</typeparam>
        /// <typeparam name="TPrimaryItem2">Type of item 2 of primary messages.</typeparam>
        /// <typeparam name="TPrimaryItem3">Type of item 3 of primary messages.</typeparam>
        /// <typeparam name="TPrimaryItem4">Type of item 4 of primary messages.</typeparam>
        /// <typeparam name="TSecondary">Type of secondary messages.</typeparam>
        /// <param name="primary">Primary stream of tuples (arity 4).</param>
        /// <param name="secondary">Secondary stream.</param>
        /// <returns>Stream of joined (`ValueTuple`) values flattened to arity 5.</returns>
        public static IProducer<ValueTuple<TPrimaryItem1, TPrimaryItem2, TPrimaryItem3, TPrimaryItem4, TSecondary>> Join<TPrimaryItem1, TPrimaryItem2, TPrimaryItem3, TPrimaryItem4, TSecondary>(
            this IProducer<ValueTuple<TPrimaryItem1, TPrimaryItem2, TPrimaryItem3, TPrimaryItem4>> primary,
            IProducer<TSecondary> secondary)
        {
            return Join(
                primary,
                secondary,
                Match.Exact<TSecondary>(),
                (p, s) => ValueTuple.Create(p.Item1, p.Item2, p.Item3, p.Item4, s));
        }

        /// <summary>
        /// Join with values from a secondary stream.
        /// </summary>
        /// <typeparam name="TPrimaryItem1">Type of item 1 of primary messages.</typeparam>
        /// <typeparam name="TPrimaryItem2">Type of item 2 of primary messages.</typeparam>
        /// <typeparam name="TPrimaryItem3">Type of item 3 of primary messages.</typeparam>
        /// <typeparam name="TPrimaryItem4">Type of item 4 of primary messages.</typeparam>
        /// <typeparam name="TSecondary">Type of secondary messages.</typeparam>
        /// <param name="primary">Primary stream of tuples (arity 4).</param>
        /// <param name="secondary">Secondary stream.</param>
        /// <param name="matchTolerance">Time span of match tolerance.</param>
        /// <returns>Stream of joined (`ValueTuple`) values flattened to arity 5.</returns>
        public static IProducer<ValueTuple<TPrimaryItem1, TPrimaryItem2, TPrimaryItem3, TPrimaryItem4, TSecondary>> Join<TPrimaryItem1, TPrimaryItem2, TPrimaryItem3, TPrimaryItem4, TSecondary>(
            this IProducer<ValueTuple<TPrimaryItem1, TPrimaryItem2, TPrimaryItem3, TPrimaryItem4>> primary,
            IProducer<TSecondary> secondary,
            TimeSpan matchTolerance)
        {
            return Join(
                primary,
                secondary,
                Match.Best<TSecondary>(matchTolerance),
                (p, s) => ValueTuple.Create(p.Item1, p.Item2, p.Item3, p.Item4, s));
        }

        /// <summary>
        /// Join with values from a secondary stream.
        /// </summary>
        /// <typeparam name="TPrimaryItem1">Type of item 1 of primary messages.</typeparam>
        /// <typeparam name="TPrimaryItem2">Type of item 2 of primary messages.</typeparam>
        /// <typeparam name="TPrimaryItem3">Type of item 3 of primary messages.</typeparam>
        /// <typeparam name="TPrimaryItem4">Type of item 4 of primary messages.</typeparam>
        /// <typeparam name="TSecondary">Type of secondary messages.</typeparam>
        /// <param name="primary">Primary stream of tuples (arity 4).</param>
        /// <param name="secondary">Secondary stream.</param>
        /// <param name="matchWindow">Relative time interval of match tolerance.</param>
        /// <returns>Stream of joined (`ValueTuple`) values flattened to arity 5.</returns>
        public static IProducer<ValueTuple<TPrimaryItem1, TPrimaryItem2, TPrimaryItem3, TPrimaryItem4, TSecondary>> Join<TPrimaryItem1, TPrimaryItem2, TPrimaryItem3, TPrimaryItem4, TSecondary>(
            this IProducer<ValueTuple<TPrimaryItem1, TPrimaryItem2, TPrimaryItem3, TPrimaryItem4>> primary,
            IProducer<TSecondary> secondary,
            RelativeTimeInterval matchWindow)
        {
            return Join(
                primary,
                secondary,
                Match.Best<TSecondary>(matchWindow),
                (p, s) => ValueTuple.Create(p.Item1, p.Item2, p.Item3, p.Item4, s));
        }

        /// <summary>
        /// Join with values from a secondary stream.
        /// </summary>
        /// <typeparam name="TPrimaryItem1">Type of item 1 of primary messages.</typeparam>
        /// <typeparam name="TPrimaryItem2">Type of item 2 of primary messages.</typeparam>
        /// <typeparam name="TPrimaryItem3">Type of item 3 of primary messages.</typeparam>
        /// <typeparam name="TPrimaryItem4">Type of item 4 of primary messages.</typeparam>
        /// <typeparam name="TPrimaryItem5">Type of item 5 of primary messages.</typeparam>
        /// <typeparam name="TSecondary">Type of secondary messages.</typeparam>
        /// <param name="primary">Primary stream of tuples (arity 5).</param>
        /// <param name="secondary">Secondary stream.</param>
        /// <param name="interpolator">Interpolator determining match behavior and tolerance.</param>
        /// <returns>Stream of joined (`ValueTuple`) values flattened to arity 6.</returns>
        public static IProducer<ValueTuple<TPrimaryItem1, TPrimaryItem2, TPrimaryItem3, TPrimaryItem4, TPrimaryItem5, TSecondary>> Join<TPrimaryItem1, TPrimaryItem2, TPrimaryItem3, TPrimaryItem4, TPrimaryItem5, TSecondary>(
            this IProducer<ValueTuple<TPrimaryItem1, TPrimaryItem2, TPrimaryItem3, TPrimaryItem4, TPrimaryItem5>> primary,
            IProducer<TSecondary> secondary,
            Match.Interpolator<TSecondary> interpolator)
        {
            return Join(
                primary,
                secondary,
                interpolator,
                (p, s) => ValueTuple.Create(p.Item1, p.Item2, p.Item3, p.Item4, p.Item5, s));
        }

        /// <summary>
        /// Join with values from a secondary stream.
        /// </summary>
        /// <remarks>Uses `Match.Exact` interpolator.</remarks>
        /// <typeparam name="TPrimaryItem1">Type of item 1 of primary messages.</typeparam>
        /// <typeparam name="TPrimaryItem2">Type of item 2 of primary messages.</typeparam>
        /// <typeparam name="TPrimaryItem3">Type of item 3 of primary messages.</typeparam>
        /// <typeparam name="TPrimaryItem4">Type of item 4 of primary messages.</typeparam>
        /// <typeparam name="TPrimaryItem5">Type of item 5 of primary messages.</typeparam>
        /// <typeparam name="TSecondary">Type of secondary messages.</typeparam>
        /// <param name="primary">Primary stream of tuples (arity 6).</param>
        /// <param name="secondary">Secondary stream.</param>
        /// <returns>Stream of joined (`ValueTuple`) values flattened to arity 6.</returns>
        public static IProducer<ValueTuple<TPrimaryItem1, TPrimaryItem2, TPrimaryItem3, TPrimaryItem4, TPrimaryItem5, TSecondary>> Join<TPrimaryItem1, TPrimaryItem2, TPrimaryItem3, TPrimaryItem4, TPrimaryItem5, TSecondary>(
            this IProducer<ValueTuple<TPrimaryItem1, TPrimaryItem2, TPrimaryItem3, TPrimaryItem4, TPrimaryItem5>> primary,
            IProducer<TSecondary> secondary)
        {
            return Join(
                primary,
                secondary,
                Match.Exact<TSecondary>(),
                (p, s) => ValueTuple.Create(p.Item1, p.Item2, p.Item3, p.Item4, p.Item5, s));
        }

        /// <summary>
        /// Join with values from a secondary stream.
        /// </summary>
        /// <remarks>Uses `Match.Exact` interpolator.</remarks>
        /// <typeparam name="TPrimaryItem1">Type of item 1 of primary messages.</typeparam>
        /// <typeparam name="TPrimaryItem2">Type of item 2 of primary messages.</typeparam>
        /// <typeparam name="TPrimaryItem3">Type of item 3 of primary messages.</typeparam>
        /// <typeparam name="TPrimaryItem4">Type of item 4 of primary messages.</typeparam>
        /// <typeparam name="TPrimaryItem5">Type of item 5 of primary messages.</typeparam>
        /// <typeparam name="TSecondary">Type of secondary messages.</typeparam>
        /// <param name="primary">Primary stream of tuples (arity 5).</param>
        /// <param name="secondary">Secondary stream.</param>
        /// <param name="matchTolerance">Time span of match tolerance.</param>
        /// <returns>Stream of joined (`ValueTuple`) values flattened to arity 6.</returns>
        public static IProducer<ValueTuple<TPrimaryItem1, TPrimaryItem2, TPrimaryItem3, TPrimaryItem4, TPrimaryItem5, TSecondary>> Join<TPrimaryItem1, TPrimaryItem2, TPrimaryItem3, TPrimaryItem4, TPrimaryItem5, TSecondary>(
            this IProducer<ValueTuple<TPrimaryItem1, TPrimaryItem2, TPrimaryItem3, TPrimaryItem4, TPrimaryItem5>> primary,
            IProducer<TSecondary> secondary,
            TimeSpan matchTolerance)
        {
            return Join(
                primary,
                secondary,
                Match.Exact<TSecondary>(),
                (p, s) => ValueTuple.Create(p.Item1, p.Item2, p.Item3, p.Item4, p.Item5, s));
        }

        /// <summary>
        /// Join with values from a secondary stream.
        /// </summary>
        /// <typeparam name="TPrimaryItem1">Type of item 1 of primary messages.</typeparam>
        /// <typeparam name="TPrimaryItem2">Type of item 2 of primary messages.</typeparam>
        /// <typeparam name="TPrimaryItem3">Type of item 3 of primary messages.</typeparam>
        /// <typeparam name="TPrimaryItem4">Type of item 4 of primary messages.</typeparam>
        /// <typeparam name="TPrimaryItem5">Type of item 5 of primary messages.</typeparam>
        /// <typeparam name="TSecondary">Type of secondary messages.</typeparam>
        /// <param name="primary">Primary stream of tuples (arity 5).</param>
        /// <param name="secondary">Secondary stream.</param>
        /// <param name="matchWindow">Relative time interval of match tolerance.</param>
        /// <returns>Stream of joined (`ValueTuple`) values flattened to arity 6.</returns>
        public static IProducer<ValueTuple<TPrimaryItem1, TPrimaryItem2, TPrimaryItem3, TPrimaryItem4, TPrimaryItem5, TSecondary>> Join<TPrimaryItem1, TPrimaryItem2, TPrimaryItem3, TPrimaryItem4, TPrimaryItem5, TSecondary>(
            this IProducer<ValueTuple<TPrimaryItem1, TPrimaryItem2, TPrimaryItem3, TPrimaryItem4, TPrimaryItem5>> primary,
            IProducer<TSecondary> secondary,
            RelativeTimeInterval matchWindow)
        {
            return Join(
                primary,
                secondary,
                Match.Best<TSecondary>(matchWindow),
                (p, s) => ValueTuple.Create(p.Item1, p.Item2, p.Item3, p.Item4, p.Item5, s));
        }

        /// <summary>
        /// Join with values from a secondary stream.
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
        /// <param name="interpolator">Interpolator determining match behavior and tolerance.</param>
        /// <returns>Stream of joined (`ValueTuple`) values flattened to arity 7.</returns>
        public static IProducer<ValueTuple<TPrimaryItem1, TPrimaryItem2, TPrimaryItem3, TPrimaryItem4, TPrimaryItem5, TPrimaryItem6, TSecondary>> Join<TPrimaryItem1, TPrimaryItem2, TPrimaryItem3, TPrimaryItem4, TPrimaryItem5, TPrimaryItem6, TSecondary>(
            this IProducer<ValueTuple<TPrimaryItem1, TPrimaryItem2, TPrimaryItem3, TPrimaryItem4, TPrimaryItem5, TPrimaryItem6>> primary,
            IProducer<TSecondary> secondary,
            Match.Interpolator<TSecondary> interpolator)
        {
            return Join(
                primary,
                secondary,
                interpolator,
                (p, s) => ValueTuple.Create(p.Item1, p.Item2, p.Item3, p.Item4, p.Item5, p.Item6, s));
        }

        /// <summary>
        /// Join with values from a secondary stream.
        /// </summary>
        /// <remarks>Uses `Match.Exact` interpolator.</remarks>
        /// <typeparam name="TPrimaryItem1">Type of item 1 of primary messages.</typeparam>
        /// <typeparam name="TPrimaryItem2">Type of item 2 of primary messages.</typeparam>
        /// <typeparam name="TPrimaryItem3">Type of item 3 of primary messages.</typeparam>
        /// <typeparam name="TPrimaryItem4">Type of item 4 of primary messages.</typeparam>
        /// <typeparam name="TPrimaryItem5">Type of item 5 of primary messages.</typeparam>
        /// <typeparam name="TPrimaryItem6">Type of item 6 of primary messages.</typeparam>
        /// <typeparam name="TSecondary">Type of secondary messages.</typeparam>
        /// <param name="primary">Primary stream of tuples (arity 6).</param>
        /// <param name="secondary">Secondary stream.</param>
        /// <returns>Stream of joined (`ValueTuple`) values flattened to arity 7.</returns>
        public static IProducer<ValueTuple<TPrimaryItem1, TPrimaryItem2, TPrimaryItem3, TPrimaryItem4, TPrimaryItem5, TPrimaryItem6, TSecondary>> Join<TPrimaryItem1, TPrimaryItem2, TPrimaryItem3, TPrimaryItem4, TPrimaryItem5, TPrimaryItem6, TSecondary>(
            this IProducer<ValueTuple<TPrimaryItem1, TPrimaryItem2, TPrimaryItem3, TPrimaryItem4, TPrimaryItem5, TPrimaryItem6>> primary,
            IProducer<TSecondary> secondary)
        {
            return Join(
                primary,
                secondary,
                Match.Exact<TSecondary>(),
                (p, s) => ValueTuple.Create(p.Item1, p.Item2, p.Item3, p.Item4, p.Item5, p.Item6, s));
        }

        /// <summary>
        /// Join with values from a secondary stream.
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
        /// <param name="matchTolerance">Time span of match tolerance.</param>
        /// <returns>Stream of joined (`ValueTuple`) values flattened to arity 7.</returns>
        public static IProducer<ValueTuple<TPrimaryItem1, TPrimaryItem2, TPrimaryItem3, TPrimaryItem4, TPrimaryItem5, TPrimaryItem6, TSecondary>> Join<TPrimaryItem1, TPrimaryItem2, TPrimaryItem3, TPrimaryItem4, TPrimaryItem5, TPrimaryItem6, TSecondary>(
            this IProducer<ValueTuple<TPrimaryItem1, TPrimaryItem2, TPrimaryItem3, TPrimaryItem4, TPrimaryItem5, TPrimaryItem6>> primary,
            IProducer<TSecondary> secondary,
            TimeSpan matchTolerance)
        {
            return Join(
                primary,
                secondary,
                Match.Best<TSecondary>(matchTolerance),
                (p, s) => ValueTuple.Create(p.Item1, p.Item2, p.Item3, p.Item4, p.Item5, p.Item6, s));
        }

        /// <summary>
        /// Join with values from a secondary stream.
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
        /// <param name="matchWindow">Relative time interval of match tolerance.</param>
        /// <returns>Stream of joined (`ValueTuple`) values flattened to arity 7.</returns>
        public static IProducer<ValueTuple<TPrimaryItem1, TPrimaryItem2, TPrimaryItem3, TPrimaryItem4, TPrimaryItem5, TPrimaryItem6, TSecondary>> Join<TPrimaryItem1, TPrimaryItem2, TPrimaryItem3, TPrimaryItem4, TPrimaryItem5, TPrimaryItem6, TSecondary>(
            this IProducer<ValueTuple<TPrimaryItem1, TPrimaryItem2, TPrimaryItem3, TPrimaryItem4, TPrimaryItem5, TPrimaryItem6>> primary,
            IProducer<TSecondary> secondary,
            RelativeTimeInterval matchWindow)
        {
            return Join(
                primary,
                secondary,
                Match.Best<TSecondary>(matchWindow),
                (p, s) => ValueTuple.Create(p.Item1, p.Item2, p.Item3, p.Item4, p.Item5, p.Item6, s));
        }

#endregion tuple-flattening scalar joins

#region reverse tuple-flattening scalar joins

        /// <summary>
        /// Join with values from a secondary stream.
        /// </summary>
        /// <typeparam name="TPrimary">Type of primary messages.</typeparam>
        /// <typeparam name="TSecondaryItem1">Type of item 1 of secondary messages.</typeparam>
        /// <typeparam name="TSecondaryItem2">Type of item 2 of secondary messages.</typeparam>
        /// <param name="primary">Primary stream.</param>
        /// <param name="secondary">Secondary stream of tuples (arity 2).</param>
        /// <param name="interpolator">Interpolator determining match behavior and tolerance.</param>
        /// <returns>Stream of joined (`ValueTuple`) values flattened to arity 3.</returns>
        public static IProducer<ValueTuple<TPrimary, TSecondaryItem1, TSecondaryItem2>> Join<TPrimary, TSecondaryItem1, TSecondaryItem2>(
            this IProducer<TPrimary> primary,
            IProducer<ValueTuple<TSecondaryItem1, TSecondaryItem2>> secondary,
            Match.Interpolator<ValueTuple<TSecondaryItem1, TSecondaryItem2>> interpolator)
        {
            return Join(
                primary,
                secondary,
                interpolator,
                (p, s) => ValueTuple.Create(p, s.Item1, s.Item2));
        }

        /// <summary>
        /// Join with values from a secondary stream.
        /// </summary>
        /// <remarks>Uses `Match.Exact` interpolator.</remarks>
        /// <typeparam name="TPrimary">Type of primary messages.</typeparam>
        /// <typeparam name="TSecondaryItem1">Type of item 1 of secondary messages.</typeparam>
        /// <typeparam name="TSecondaryItem2">Type of item 2 of secondary messages.</typeparam>
        /// <param name="primary">Primary stream.</param>
        /// <param name="secondary">Secondary stream of tuples (arity 2).</param>
        /// <returns>Stream of joined (`ValueTuple`) values flattened to arity 3.</returns>
        public static IProducer<ValueTuple<TPrimary, TSecondaryItem1, TSecondaryItem2>> Join<TPrimary, TSecondaryItem1, TSecondaryItem2>(
            this IProducer<TPrimary> primary,
            IProducer<ValueTuple<TSecondaryItem1, TSecondaryItem2>> secondary)
        {
            return Join(
                primary,
                secondary,
                Match.Exact<ValueTuple<TSecondaryItem1, TSecondaryItem2>>(),
                (p, s) => ValueTuple.Create(p, s.Item1, s.Item2));
        }

        /// <summary>
        /// Join with values from a secondary stream.
        /// </summary>
        /// <typeparam name="TPrimary">Type of primary messages.</typeparam>
        /// <typeparam name="TSecondaryItem1">Type of item 1 of secondary messages.</typeparam>
        /// <typeparam name="TSecondaryItem2">Type of item 2 of secondary messages.</typeparam>
        /// <param name="primary">Primary stream.</param>
        /// <param name="secondary">Secondary stream of tuples (arity 2).</param>
        /// <param name="matchTolerance">Time span of match tolerance.</param>
        /// <returns>Stream of joined (`ValueTuple`) values flattened to arity 3.</returns>
        public static IProducer<ValueTuple<TPrimary, TSecondaryItem1, TSecondaryItem2>> Join<TPrimary, TSecondaryItem1, TSecondaryItem2>(
            this IProducer<TPrimary> primary,
            IProducer<ValueTuple<TSecondaryItem1, TSecondaryItem2>> secondary,
            TimeSpan matchTolerance)
        {
            return Join(
                primary,
                secondary,
                Match.Best<ValueTuple<TSecondaryItem1, TSecondaryItem2>>(matchTolerance),
                (p, s) => ValueTuple.Create(p, s.Item1, s.Item2));
        }

        /// <summary>
        /// Join with values from a secondary stream.
        /// </summary>
        /// <typeparam name="TPrimary">Type of primary messages.</typeparam>
        /// <typeparam name="TSecondaryItem1">Type of item 1 of secondary messages.</typeparam>
        /// <typeparam name="TSecondaryItem2">Type of item 2 of secondary messages.</typeparam>
        /// <param name="primary">Primary stream.</param>
        /// <param name="secondary">Secondary stream of tuples (arity 2).</param>
        /// <param name="matchWindow">Relative time interval of match tolerance.</param>
        /// <returns>Stream of joined (`ValueTuple`) values flattened to arity 3.</returns>
        public static IProducer<ValueTuple<TPrimary, TSecondaryItem1, TSecondaryItem2>> Join<TPrimary, TSecondaryItem1, TSecondaryItem2>(
            this IProducer<TPrimary> primary,
            IProducer<ValueTuple<TSecondaryItem1, TSecondaryItem2>> secondary,
            RelativeTimeInterval matchWindow)
        {
            return Join(
                primary,
                secondary,
                Match.Best<ValueTuple<TSecondaryItem1, TSecondaryItem2>>(matchWindow),
                (p, s) => ValueTuple.Create(p, s.Item1, s.Item2));
        }

        /// <summary>
        /// Join with values from a secondary stream.
        /// </summary>
        /// <typeparam name="TPrimary">Type of primary messages.</typeparam>
        /// <typeparam name="TSecondaryItem1">Type of item 1 of secondary messages.</typeparam>
        /// <typeparam name="TSecondaryItem2">Type of item 2 of secondary messages.</typeparam>
        /// <typeparam name="TSecondaryItem3">Type of item 3 of secondary messages.</typeparam>
        /// <param name="primary">Primary stream.</param>
        /// <param name="secondary">Secondary stream of tuples (arity 3).</param>
        /// <param name="interpolator">Interpolator determining match behavior and tolerance.</param>
        /// <returns>Stream of joined (`ValueTuple`) values flattened to arity 4.</returns>
        public static IProducer<ValueTuple<TPrimary, TSecondaryItem1, TSecondaryItem2, TSecondaryItem3>> Join<TPrimary, TSecondaryItem1, TSecondaryItem2, TSecondaryItem3>(
            this IProducer<TPrimary> primary,
            IProducer<ValueTuple<TSecondaryItem1, TSecondaryItem2, TSecondaryItem3>> secondary,
            Match.Interpolator<ValueTuple<TSecondaryItem1, TSecondaryItem2, TSecondaryItem3>> interpolator)
        {
            return Join(
                primary,
                secondary,
                interpolator,
                (p, s) => ValueTuple.Create(p, s.Item1, s.Item2, s.Item3));
        }

        /// <summary>
        /// Join with values from a secondary stream.
        /// </summary>
        /// <remarks>Uses `Match.Exact` interpolator.</remarks>
        /// <typeparam name="TPrimary">Type of primary messages.</typeparam>
        /// <typeparam name="TSecondaryItem1">Type of item 1 of secondary messages.</typeparam>
        /// <typeparam name="TSecondaryItem2">Type of item 2 of secondary messages.</typeparam>
        /// <typeparam name="TSecondaryItem3">Type of item 3 of secondary messages.</typeparam>
        /// <param name="primary">Primary stream.</param>
        /// <param name="secondary">Secondary stream of tuples (arity 3).</param>
        /// <returns>Stream of joined (`ValueTuple`) values flattened to arity 4.</returns>
        public static IProducer<ValueTuple<TPrimary, TSecondaryItem1, TSecondaryItem2, TSecondaryItem3>> Join<TPrimary, TSecondaryItem1, TSecondaryItem2, TSecondaryItem3>(
            this IProducer<TPrimary> primary,
            IProducer<ValueTuple<TSecondaryItem1, TSecondaryItem2, TSecondaryItem3>> secondary)
        {
            return Join(
                primary,
                secondary,
                Match.Exact<ValueTuple<TSecondaryItem1, TSecondaryItem2, TSecondaryItem3>>(),
                (p, s) => ValueTuple.Create(p, s.Item1, s.Item2, s.Item3));
        }

        /// <summary>
        /// Join with values from a secondary stream.
        /// </summary>
        /// <typeparam name="TPrimary">Type of primary messages.</typeparam>
        /// <typeparam name="TSecondaryItem1">Type of item 1 of secondary messages.</typeparam>
        /// <typeparam name="TSecondaryItem2">Type of item 2 of secondary messages.</typeparam>
        /// <typeparam name="TSecondaryItem3">Type of item 3 of secondary messages.</typeparam>
        /// <param name="primary">Primary stream.</param>
        /// <param name="secondary">Secondary stream of tuples (arity 3).</param>
        /// <param name="matchTolerance">Time span of match tolerance.</param>
        /// <returns>Stream of joined (`ValueTuple`) values flattened to arity 4.</returns>
        public static IProducer<ValueTuple<TPrimary, TSecondaryItem1, TSecondaryItem2, TSecondaryItem3>> Join<TPrimary, TSecondaryItem1, TSecondaryItem2, TSecondaryItem3>(
            this IProducer<TPrimary> primary,
            IProducer<ValueTuple<TSecondaryItem1, TSecondaryItem2, TSecondaryItem3>> secondary,
            TimeSpan matchTolerance)
        {
            return Join(
                primary,
                secondary,
                Match.Best<ValueTuple<TSecondaryItem1, TSecondaryItem2, TSecondaryItem3>>(matchTolerance),
                (p, s) => ValueTuple.Create(p, s.Item1, s.Item2, s.Item3));
        }

        /// <summary>
        /// Join with values from a secondary stream.
        /// </summary>
        /// <typeparam name="TPrimary">Type of primary messages.</typeparam>
        /// <typeparam name="TSecondaryItem1">Type of item 1 of secondary messages.</typeparam>
        /// <typeparam name="TSecondaryItem2">Type of item 2 of secondary messages.</typeparam>
        /// <typeparam name="TSecondaryItem3">Type of item 3 of secondary messages.</typeparam>
        /// <param name="primary">Primary stream.</param>
        /// <param name="secondary">Secondary stream of tuples (arity 3).</param>
        /// <param name="matchWindow">Relative time interval of match tolerance.</param>
        /// <returns>Stream of joined (`ValueTuple`) values flattened to arity 4.</returns>
        public static IProducer<ValueTuple<TPrimary, TSecondaryItem1, TSecondaryItem2, TSecondaryItem3>> Join<TPrimary, TSecondaryItem1, TSecondaryItem2, TSecondaryItem3>(
            this IProducer<TPrimary> primary,
            IProducer<ValueTuple<TSecondaryItem1, TSecondaryItem2, TSecondaryItem3>> secondary,
            RelativeTimeInterval matchWindow)
        {
            return Join(
                primary,
                secondary,
                Match.Best<ValueTuple<TSecondaryItem1, TSecondaryItem2, TSecondaryItem3>>(matchWindow),
                (p, s) => ValueTuple.Create(p, s.Item1, s.Item2, s.Item3));
        }

        /// <summary>
        /// Join with values from a secondary stream.
        /// </summary>
        /// <typeparam name="TPrimary">Type of primary messages.</typeparam>
        /// <typeparam name="TSecondaryItem1">Type of item 1 of secondary messages.</typeparam>
        /// <typeparam name="TSecondaryItem2">Type of item 2 of secondary messages.</typeparam>
        /// <typeparam name="TSecondaryItem3">Type of item 3 of secondary messages.</typeparam>
        /// <typeparam name="TSecondaryItem4">Type of item 4 of secondary messages.</typeparam>
        /// <param name="primary">Primary stream.</param>
        /// <param name="secondary">Secondary stream of tuples (arity 4).</param>
        /// <param name="interpolator">Interpolator determining match behavior and tolerance.</param>
        /// <returns>Stream of joined (`ValueTuple`) values flattened to arity 5.</returns>
        public static IProducer<ValueTuple<TPrimary, TSecondaryItem1, TSecondaryItem2, TSecondaryItem3, TSecondaryItem4>> Join<TPrimary, TSecondaryItem1, TSecondaryItem2, TSecondaryItem3, TSecondaryItem4>(
            this IProducer<TPrimary> primary,
            IProducer<ValueTuple<TSecondaryItem1, TSecondaryItem2, TSecondaryItem3, TSecondaryItem4>> secondary,
            Match.Interpolator<ValueTuple<TSecondaryItem1, TSecondaryItem2, TSecondaryItem3, TSecondaryItem4>> interpolator)
        {
            return Join(
                primary,
                secondary,
                interpolator,
                (p, s) => ValueTuple.Create(p, s.Item1, s.Item2, s.Item3, s.Item4));
        }

        /// <summary>
        /// Join with values from a secondary stream.
        /// </summary>
        /// <remarks>Uses `Match.Exact` interpolator.</remarks>
        /// <typeparam name="TPrimary">Type of primary messages.</typeparam>
        /// <typeparam name="TSecondaryItem1">Type of item 1 of secondary messages.</typeparam>
        /// <typeparam name="TSecondaryItem2">Type of item 2 of secondary messages.</typeparam>
        /// <typeparam name="TSecondaryItem3">Type of item 3 of secondary messages.</typeparam>
        /// <typeparam name="TSecondaryItem4">Type of item 4 of secondary messages.</typeparam>
        /// <param name="primary">Primary stream.</param>
        /// <param name="secondary">Secondary stream of tuples (arity 4).</param>
        /// <returns>Stream of joined (`ValueTuple`) values flattened to arity 5.</returns>
        public static IProducer<ValueTuple<TPrimary, TSecondaryItem1, TSecondaryItem2, TSecondaryItem3, TSecondaryItem4>> Join<TPrimary, TSecondaryItem1, TSecondaryItem2, TSecondaryItem3, TSecondaryItem4>(
            this IProducer<TPrimary> primary,
            IProducer<ValueTuple<TSecondaryItem1, TSecondaryItem2, TSecondaryItem3, TSecondaryItem4>> secondary)
        {
            return Join(
                primary,
                secondary,
                Match.Exact<ValueTuple<TSecondaryItem1, TSecondaryItem2, TSecondaryItem3, TSecondaryItem4>>(),
                (p, s) => ValueTuple.Create(p, s.Item1, s.Item2, s.Item3, s.Item4));
        }

        /// <summary>
        /// Join with values from a secondary stream.
        /// </summary>
        /// <typeparam name="TPrimary">Type of primary messages.</typeparam>
        /// <typeparam name="TSecondaryItem1">Type of item 1 of secondary messages.</typeparam>
        /// <typeparam name="TSecondaryItem2">Type of item 2 of secondary messages.</typeparam>
        /// <typeparam name="TSecondaryItem3">Type of item 3 of secondary messages.</typeparam>
        /// <typeparam name="TSecondaryItem4">Type of item 4 of secondary messages.</typeparam>
        /// <param name="primary">Primary stream.</param>
        /// <param name="secondary">Secondary stream of tuples (arity 4).</param>
        /// <param name="matchTolerance">Time span of match tolerance.</param>
        /// <returns>Stream of joined (`ValueTuple`) values flattened to arity 5.</returns>
        public static IProducer<ValueTuple<TPrimary, TSecondaryItem1, TSecondaryItem2, TSecondaryItem3, TSecondaryItem4>> Join<TPrimary, TSecondaryItem1, TSecondaryItem2, TSecondaryItem3, TSecondaryItem4>(
            this IProducer<TPrimary> primary,
            IProducer<ValueTuple<TSecondaryItem1, TSecondaryItem2, TSecondaryItem3, TSecondaryItem4>> secondary,
            TimeSpan matchTolerance)
        {
            return Join(
                primary,
                secondary,
                Match.Best<ValueTuple<TSecondaryItem1, TSecondaryItem2, TSecondaryItem3, TSecondaryItem4>>(matchTolerance),
                (p, s) => ValueTuple.Create(p, s.Item1, s.Item2, s.Item3, s.Item4));
        }

        /// <summary>
        /// Join with values from a secondary stream.
        /// </summary>
        /// <typeparam name="TPrimary">Type of primary messages.</typeparam>
        /// <typeparam name="TSecondaryItem1">Type of item 1 of secondary messages.</typeparam>
        /// <typeparam name="TSecondaryItem2">Type of item 2 of secondary messages.</typeparam>
        /// <typeparam name="TSecondaryItem3">Type of item 3 of secondary messages.</typeparam>
        /// <typeparam name="TSecondaryItem4">Type of item 4 of secondary messages.</typeparam>
        /// <param name="primary">Primary stream.</param>
        /// <param name="secondary">Secondary stream of tuples (arity 4).</param>
        /// <param name="matchWindow">Relative time interval of match tolerance.</param>
        /// <returns>Stream of joined (`ValueTuple`) values flattened to arity 5.</returns>
        public static IProducer<ValueTuple<TPrimary, TSecondaryItem1, TSecondaryItem2, TSecondaryItem3, TSecondaryItem4>> Join<TPrimary, TSecondaryItem1, TSecondaryItem2, TSecondaryItem3, TSecondaryItem4>(
            this IProducer<TPrimary> primary,
            IProducer<ValueTuple<TSecondaryItem1, TSecondaryItem2, TSecondaryItem3, TSecondaryItem4>> secondary,
            RelativeTimeInterval matchWindow)
        {
            return Join(
                primary,
                secondary,
                Match.Best<ValueTuple<TSecondaryItem1, TSecondaryItem2, TSecondaryItem3, TSecondaryItem4>>(matchWindow),
                (p, s) => ValueTuple.Create(p, s.Item1, s.Item2, s.Item3, s.Item4));
        }

        /// <summary>
        /// Join with values from a secondary stream.
        /// </summary>
        /// <typeparam name="TPrimary">Type of primary messages.</typeparam>
        /// <typeparam name="TSecondaryItem1">Type of item 1 of secondary messages.</typeparam>
        /// <typeparam name="TSecondaryItem2">Type of item 2 of secondary messages.</typeparam>
        /// <typeparam name="TSecondaryItem3">Type of item 3 of secondary messages.</typeparam>
        /// <typeparam name="TSecondaryItem4">Type of item 4 of secondary messages.</typeparam>
        /// <typeparam name="TSecondaryItem5">Type of item 5 of secondary messages.</typeparam>
        /// <param name="primary">Primary stream.</param>
        /// <param name="secondary">Secondary stream of tuples (arity 5).</param>
        /// <param name="interpolator">Interpolator determining match behavior and tolerance.</param>
        /// <returns>Stream of joined (`ValueTuple`) values flattened to arity 6.</returns>
        public static IProducer<ValueTuple<TPrimary, TSecondaryItem1, TSecondaryItem2, TSecondaryItem3, TSecondaryItem4, TSecondaryItem5>> Join<TPrimary, TSecondaryItem1, TSecondaryItem2, TSecondaryItem3, TSecondaryItem4, TSecondaryItem5>(
            this IProducer<TPrimary> primary,
            IProducer<ValueTuple<TSecondaryItem1, TSecondaryItem2, TSecondaryItem3, TSecondaryItem4, TSecondaryItem5>> secondary,
            Match.Interpolator<ValueTuple<TSecondaryItem1, TSecondaryItem2, TSecondaryItem3, TSecondaryItem4, TSecondaryItem5>> interpolator)
        {
            return Join(
                primary,
                secondary,
                interpolator,
                (p, s) => ValueTuple.Create(p, s.Item1, s.Item2, s.Item3, s.Item4, s.Item5));
        }

        /// <summary>
        /// Join with values from a secondary stream.
        /// </summary>
        /// <remarks>Uses `Match.Exact` interpolator.</remarks>
        /// <typeparam name="TPrimary">Type of primary messages.</typeparam>
        /// <typeparam name="TSecondaryItem1">Type of item 1 of secondary messages.</typeparam>
        /// <typeparam name="TSecondaryItem2">Type of item 2 of secondary messages.</typeparam>
        /// <typeparam name="TSecondaryItem3">Type of item 3 of secondary messages.</typeparam>
        /// <typeparam name="TSecondaryItem4">Type of item 4 of secondary messages.</typeparam>
        /// <typeparam name="TSecondaryItem5">Type of item 5 of secondary messages.</typeparam>
        /// <param name="primary">Primary stream.</param>
        /// <param name="secondary">Secondary stream of tuples (arity 5).</param>
        /// <returns>Stream of joined (`ValueTuple`) values flattened to arity 6.</returns>
        public static IProducer<ValueTuple<TPrimary, TSecondaryItem1, TSecondaryItem2, TSecondaryItem3, TSecondaryItem4, TSecondaryItem5>> Join<TPrimary, TSecondaryItem1, TSecondaryItem2, TSecondaryItem3, TSecondaryItem4, TSecondaryItem5>(
            this IProducer<TPrimary> primary,
            IProducer<ValueTuple<TSecondaryItem1, TSecondaryItem2, TSecondaryItem3, TSecondaryItem4, TSecondaryItem5>> secondary)
        {
            return Join(
                primary,
                secondary,
                Match.Exact<ValueTuple<TSecondaryItem1, TSecondaryItem2, TSecondaryItem3, TSecondaryItem4, TSecondaryItem5>>(),
                (p, s) => ValueTuple.Create(p, s.Item1, s.Item2, s.Item3, s.Item4, s.Item5));
        }

        /// <summary>
        /// Join with values from a secondary stream.
        /// </summary>
        /// <typeparam name="TPrimary">Type of primary messages.</typeparam>
        /// <typeparam name="TSecondaryItem1">Type of item 1 of secondary messages.</typeparam>
        /// <typeparam name="TSecondaryItem2">Type of item 2 of secondary messages.</typeparam>
        /// <typeparam name="TSecondaryItem3">Type of item 3 of secondary messages.</typeparam>
        /// <typeparam name="TSecondaryItem4">Type of item 4 of secondary messages.</typeparam>
        /// <typeparam name="TSecondaryItem5">Type of item 5 of secondary messages.</typeparam>
        /// <param name="primary">Primary stream.</param>
        /// <param name="secondary">Secondary stream of tuples (arity 5).</param>
        /// <param name="matchTolerance">Time span of match tolerance.</param>
        /// <returns>Stream of joined (`ValueTuple`) values flattened to arity 6.</returns>
        public static IProducer<ValueTuple<TPrimary, TSecondaryItem1, TSecondaryItem2, TSecondaryItem3, TSecondaryItem4, TSecondaryItem5>> Join<TPrimary, TSecondaryItem1, TSecondaryItem2, TSecondaryItem3, TSecondaryItem4, TSecondaryItem5>(
            this IProducer<TPrimary> primary,
            IProducer<ValueTuple<TSecondaryItem1, TSecondaryItem2, TSecondaryItem3, TSecondaryItem4, TSecondaryItem5>> secondary,
            TimeSpan matchTolerance)
        {
            return Join(
                primary,
                secondary,
                Match.Best<ValueTuple<TSecondaryItem1, TSecondaryItem2, TSecondaryItem3, TSecondaryItem4, TSecondaryItem5>>(matchTolerance),
                (p, s) => ValueTuple.Create(p, s.Item1, s.Item2, s.Item3, s.Item4, s.Item5));
        }

        /// <summary>
        /// Join with values from a secondary stream.
        /// </summary>
        /// <typeparam name="TPrimary">Type of primary messages.</typeparam>
        /// <typeparam name="TSecondaryItem1">Type of item 1 of secondary messages.</typeparam>
        /// <typeparam name="TSecondaryItem2">Type of item 2 of secondary messages.</typeparam>
        /// <typeparam name="TSecondaryItem3">Type of item 3 of secondary messages.</typeparam>
        /// <typeparam name="TSecondaryItem4">Type of item 4 of secondary messages.</typeparam>
        /// <typeparam name="TSecondaryItem5">Type of item 5 of secondary messages.</typeparam>
        /// <param name="primary">Primary stream.</param>
        /// <param name="secondary">Secondary stream of tuples (arity 5).</param>
        /// <param name="matchWindow">Relative time interval of match tolerance.</param>
        /// <returns>Stream of joined (`ValueTuple`) values flattened to arity 6.</returns>
        public static IProducer<ValueTuple<TPrimary, TSecondaryItem1, TSecondaryItem2, TSecondaryItem3, TSecondaryItem4, TSecondaryItem5>> Join<TPrimary, TSecondaryItem1, TSecondaryItem2, TSecondaryItem3, TSecondaryItem4, TSecondaryItem5>(
            this IProducer<TPrimary> primary,
            IProducer<ValueTuple<TSecondaryItem1, TSecondaryItem2, TSecondaryItem3, TSecondaryItem4, TSecondaryItem5>> secondary,
            RelativeTimeInterval matchWindow)
        {
            return Join(
                primary,
                secondary,
                Match.Best<ValueTuple<TSecondaryItem1, TSecondaryItem2, TSecondaryItem3, TSecondaryItem4, TSecondaryItem5>>(matchWindow),
                (p, s) => ValueTuple.Create(p, s.Item1, s.Item2, s.Item3, s.Item4, s.Item5));
        }

        /// <summary>
        /// Join with values from a secondary stream.
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
        /// <param name="interpolator">Interpolator determining match behavior and tolerance.</param>
        /// <returns>Stream of joined (`ValueTuple`) values flattened to arity 7.</returns>
        public static IProducer<ValueTuple<TPrimary, TSecondaryItem1, TSecondaryItem2, TSecondaryItem3, TSecondaryItem4, TSecondaryItem5, TSecondaryItem6>> Join<TPrimary, TSecondaryItem1, TSecondaryItem2, TSecondaryItem3, TSecondaryItem4, TSecondaryItem5, TSecondaryItem6>(
            this IProducer<TPrimary> primary,
            IProducer<ValueTuple<TSecondaryItem1, TSecondaryItem2, TSecondaryItem3, TSecondaryItem4, TSecondaryItem5, TSecondaryItem6>> secondary,
            Match.Interpolator<ValueTuple<TSecondaryItem1, TSecondaryItem2, TSecondaryItem3, TSecondaryItem4, TSecondaryItem5, TSecondaryItem6>> interpolator)
        {
            return Join(
                primary,
                secondary,
                interpolator,
                (p, s) => ValueTuple.Create(p, s.Item1, s.Item2, s.Item3, s.Item4, s.Item5, s.Item6));
        }

        /// <summary>
        /// Join with values from a secondary stream.
        /// </summary>
        /// <remarks>Uses `Match.Exact` interpolator.</remarks>
        /// <typeparam name="TPrimary">Type of primary messages.</typeparam>
        /// <typeparam name="TSecondaryItem1">Type of item 1 of secondary messages.</typeparam>
        /// <typeparam name="TSecondaryItem2">Type of item 2 of secondary messages.</typeparam>
        /// <typeparam name="TSecondaryItem3">Type of item 3 of secondary messages.</typeparam>
        /// <typeparam name="TSecondaryItem4">Type of item 4 of secondary messages.</typeparam>
        /// <typeparam name="TSecondaryItem5">Type of item 5 of secondary messages.</typeparam>
        /// <typeparam name="TSecondaryItem6">Type of item 6 of secondary messages.</typeparam>
        /// <param name="primary">Primary stream.</param>
        /// <param name="secondary">Secondary stream of tuples (arity 6).</param>
        /// <returns>Stream of joined (`ValueTuple`) values flattened to arity 7.</returns>
        public static IProducer<ValueTuple<TPrimary, TSecondaryItem1, TSecondaryItem2, TSecondaryItem3, TSecondaryItem4, TSecondaryItem5, TSecondaryItem6>> Join<TPrimary, TSecondaryItem1, TSecondaryItem2, TSecondaryItem3, TSecondaryItem4, TSecondaryItem5, TSecondaryItem6>(
            this IProducer<TPrimary> primary,
            IProducer<ValueTuple<TSecondaryItem1, TSecondaryItem2, TSecondaryItem3, TSecondaryItem4, TSecondaryItem5, TSecondaryItem6>> secondary)
        {
            return Join(
                primary,
                secondary,
                Match.Exact<ValueTuple<TSecondaryItem1, TSecondaryItem2, TSecondaryItem3, TSecondaryItem4, TSecondaryItem5, TSecondaryItem6>>(),
                (p, s) => ValueTuple.Create(p, s.Item1, s.Item2, s.Item3, s.Item4, s.Item5, s.Item6));
        }

        /// <summary>
        /// Join with values from a secondary stream.
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
        /// <param name="matchTolerance">Time span of match tolerance.</param>
        /// <returns>Stream of joined (`ValueTuple`) values flattened to arity 7.</returns>
        public static IProducer<ValueTuple<TPrimary, TSecondaryItem1, TSecondaryItem2, TSecondaryItem3, TSecondaryItem4, TSecondaryItem5, TSecondaryItem6>> Join<TPrimary, TSecondaryItem1, TSecondaryItem2, TSecondaryItem3, TSecondaryItem4, TSecondaryItem5, TSecondaryItem6>(
            this IProducer<TPrimary> primary,
            IProducer<ValueTuple<TSecondaryItem1, TSecondaryItem2, TSecondaryItem3, TSecondaryItem4, TSecondaryItem5, TSecondaryItem6>> secondary,
            TimeSpan matchTolerance)
        {
            return Join(
                primary,
                secondary,
                Match.Best<ValueTuple<TSecondaryItem1, TSecondaryItem2, TSecondaryItem3, TSecondaryItem4, TSecondaryItem5, TSecondaryItem6>>(matchTolerance),
                (p, s) => ValueTuple.Create(p, s.Item1, s.Item2, s.Item3, s.Item4, s.Item5, s.Item6));
        }

        /// <summary>
        /// Join with values from a secondary stream.
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
        /// <param name="matchWindow">Relative time interval of match tolerance.</param>
        /// <returns>Stream of joined (`ValueTuple`) values flattened to arity 7.</returns>
        public static IProducer<ValueTuple<TPrimary, TSecondaryItem1, TSecondaryItem2, TSecondaryItem3, TSecondaryItem4, TSecondaryItem5, TSecondaryItem6>> Join<TPrimary, TSecondaryItem1, TSecondaryItem2, TSecondaryItem3, TSecondaryItem4, TSecondaryItem5, TSecondaryItem6>(
            this IProducer<TPrimary> primary,
            IProducer<ValueTuple<TSecondaryItem1, TSecondaryItem2, TSecondaryItem3, TSecondaryItem4, TSecondaryItem5, TSecondaryItem6>> secondary,
            RelativeTimeInterval matchWindow)
        {
            return Join(
                primary,
                secondary,
                Match.Best<ValueTuple<TSecondaryItem1, TSecondaryItem2, TSecondaryItem3, TSecondaryItem4, TSecondaryItem5, TSecondaryItem6>>(matchWindow),
                (p, s) => ValueTuple.Create(p, s.Item1, s.Item2, s.Item3, s.Item4, s.Item5, s.Item6));
        }

#endregion reverse tuple-flattening scalar joins

#region vector joins

        /// <summary>
        /// Vector join.
        /// </summary>
        /// <typeparam name="TPrimary">Type of primary stream messages.</typeparam>
        /// <typeparam name="TSecondary">Type of secondary stream messages.</typeparam>
        /// <typeparam name="TOut">Type of output stream messages.</typeparam>
        /// <param name="primary">Primary stream.</param>
        /// <param name="secondaries">Collection of secondary streams.</param>
        /// <param name="interpolator">Interpolator with which to join.</param>
        /// <param name="outputCreator">Mapping function from primary and secondary messages to output.</param>
        /// <returns>Output stream.</returns>
        public static IProducer<TOut> Join<TPrimary, TSecondary, TOut>(
            this IProducer<TPrimary> primary,
            IEnumerable<IProducer<TSecondary>> secondaries,
            Match.Interpolator<TSecondary> interpolator,
            Func<TPrimary, TSecondary[], TOut> outputCreator)
        {
            var join = new Join<TPrimary, TSecondary, TOut>(
                secondaries.First().Out.Pipeline,
                interpolator,
                outputCreator,
                secondaries.Count(),
                null);

            primary.PipeTo(join.InPrimary);

            var i = 0;
            foreach (var input in secondaries)
            {
                input.PipeTo(join.InSecondaries[i++]);
            }

            return join;
        }

        /// <summary>
        /// Vector join.
        /// </summary>
        /// <typeparam name="TIn">Type of input stream messages.</typeparam>
        /// <param name="inputs">Collection of input streams.</param>
        /// <param name="interpolator">Interpolator with which to join.</param>
        /// <returns>Output stream.</returns>
        public static IProducer<TIn[]> Join<TIn>(
            this IEnumerable<IProducer<TIn>> inputs,
            Match.Interpolator<TIn> interpolator)
        {
            var count = inputs.Count();
            if (count > 1)
            {
                var buffer = new TIn[count];
                return Join(
                    inputs.First(),
                    inputs.Skip(1),
                    interpolator,
                    (m, sArr) =>
                    {
                        buffer[0] = m;
                        Array.Copy(sArr, 0, buffer, 1, count - 1);
                        return buffer;
                    });
            }
            else if (count == 1)
            {
                return inputs.First().Select(x => new[] { x });
            }
            else
            {
                throw new ArgumentException("Vector join with empty inputs collection.");
            }
        }
#endregion vector joins

#region sparse vector joins

        /// <summary>
        /// Sparse vector join.
        /// </summary>
        /// <typeparam name="TKeyCollection">Type of key collection.</typeparam>
        /// <typeparam name="TIn">Type of input messages.</typeparam>
        /// <typeparam name="TKey">Type of key values.</typeparam>
        /// <param name="primary">Primary stream.</param>
        /// <param name="inputs">Collection of secondary streams.</param>
        /// <param name="interpolator">Interpolator with which to join.</param>
        /// <param name="keyMapSelector">Selector function mapping keys to key/value pairs.</param>
        /// <returns>Output stream.</returns>
        public static Join<TKeyCollection, TIn, Dictionary<TKey, TIn>> Join<TKeyCollection, TIn, TKey>(
            this IProducer<TKeyCollection> primary,
            IEnumerable<IProducer<TIn>> inputs,
            Match.Interpolator<TIn> interpolator,
            Func<TKeyCollection, IEnumerable<KeyValuePair<TKey, int>>> keyMapSelector)
        {
            var buffer = new Dictionary<TKey, TIn>();
            var join = new Join<TKeyCollection, TIn, Dictionary<TKey, TIn>>(
                primary.Out.Pipeline,
                interpolator,
                (keys, values) =>
                {
                    buffer.Clear();
                    var keyMap = keyMapSelector(keys);
                    foreach (var keyPair in keyMap)
                    {
                        buffer[keyPair.Key] = values[keyPair.Value];
                    }

                    return buffer;
                },
                inputs.Count(),
                keys => keyMapSelector(keys).Select(p => p.Value));

            primary.PipeTo(join.InPrimary);

            var i = 0;
            foreach (var input in inputs)
            {
                input.PipeTo(join.InSecondaries[i++]);
            }

            return join;
        }

        /// <summary>
        /// Sparse vector join.
        /// </summary>
        /// <typeparam name="TIn">Type of input messages.</typeparam>
        /// <typeparam name="TKey">Type of key values.</typeparam>
        /// <param name="primary">Primary stream.</param>
        /// <param name="inputs">Collection of secondary streams.</param>
        /// <param name="interpolator">Interpolator with which to join.</param>
        /// <returns>Output stream.</returns>
        public static Join<Dictionary<TKey, int>, TIn, Dictionary<TKey, TIn>> Join<TIn, TKey>(
            this IProducer<Dictionary<TKey, int>> primary,
            IEnumerable<IProducer<TIn>> inputs,
            Match.Interpolator<TIn> interpolator)
        {
            return Join(primary, inputs, interpolator, keyMap => keyMap);
        }

        /// <summary>
        /// Sparse vector join.
        /// </summary>
        /// <typeparam name="TIn">Type of input messages.</typeparam>
        /// <typeparam name="TKey">Type of key values.</typeparam>
        /// <param name="primary">Primary stream.</param>
        /// <param name="inputs">Collection of secondary streams.</param>
        /// <param name="interpolatorTolerance">Time span in within which to join.</param>
        /// <returns>Output stream.</returns>
        public static Join<Dictionary<TKey, int>, TIn, Dictionary<TKey, TIn>> Join<TIn, TKey>(
            this IProducer<Dictionary<TKey, int>> primary,
            IEnumerable<IProducer<TIn>> inputs = null,
            TimeSpan interpolatorTolerance = default(TimeSpan))
        {
            inputs = inputs ?? Enumerable.Empty<IProducer<TIn>>();
            return Join(primary, inputs, new RelativeTimeInterval(-interpolatorTolerance, interpolatorTolerance));
        }

        /// <summary>
        /// Sparse vector join.
        /// </summary>
        /// <typeparam name="TIn">Type of input messages.</typeparam>
        /// <typeparam name="TKey">Type of key values.</typeparam>
        /// <param name="primary">Primary stream.</param>
        /// <param name="inputs">Collection of secondary streams.</param>
        /// <param name="matchWindow">Relative time interval within which to join.</param>
        /// <returns>Output stream.</returns>
        public static Join<Dictionary<TKey, int>, TIn, Dictionary<TKey, TIn>> Join<TIn, TKey>(
            this IProducer<Dictionary<TKey, int>> primary,
            IEnumerable<IProducer<TIn>> inputs,
            RelativeTimeInterval matchWindow)
        {
            return Join(primary, inputs, Match.Best<TIn>(matchWindow));
        }
    }

#endregion sparse vector joins
}
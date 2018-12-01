// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.

namespace Microsoft.Psi
{
    using System;
    using Microsoft.Psi.Components;

    /// <summary>
    /// Extension methods that simplify operator usage
    /// </summary>
    public static partial class Operators
    {
        #region scalar pairs

        /// <summary>
        /// Pair with latest (in wall-clock sense) values from a secondary stream.
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
        /// <param name="pipeline">The pipeline to which this component belongs (optional, defaults to that of the primary stream).</param>
        /// <returns>Stream of paired values.</returns>
        public static IProducer<TOut> Pair<TPrimary, TSecondary, TOut>(
            this IProducer<TPrimary> primary,
            IProducer<TSecondary> secondary,
            Func<TPrimary, TSecondary, TOut> outputCreator,
            TSecondary initialValue,
            DeliveryPolicy primaryDeliveryPolicy = null,
            DeliveryPolicy secondaryDeliveryPolicy = null,
            Pipeline pipeline = null)
        {
            return Pair(
                new Pair<TPrimary, TSecondary, TOut>(pipeline ?? primary.Out.Pipeline, outputCreator, initialValue),
                primary,
                secondary,
                primaryDeliveryPolicy,
                secondaryDeliveryPolicy);
        }

        /// <summary>
        /// Pair with latest (in wall-clock sense) values from a secondary stream.
        /// </summary>
        /// <typeparam name="TPrimary">Type of primary messages.</typeparam>
        /// <typeparam name="TSecondary">Type of secondary messages.</typeparam>
        /// <typeparam name="TOut">Type of output messages.</typeparam>
        /// <param name="primary">Primary stream.</param>
        /// <param name="secondary">Secondary stream.</param>
        /// <param name="outputCreator">Mapping function from primary/secondary pairs to output type.</param>
        /// <param name="primaryDeliveryPolicy">An optional delivery policy for the primary stream.</param>
        /// <param name="secondaryDeliveryPolicy">An optional delivery policy for the secondary stream(s).</param>
        /// <param name="pipeline">The pipeline to which this component belongs (optional, defaults to that of the primary stream).</param>
        /// <remarks>Primary messages will be dropped until the first secondary message is received (no `initialValue` provided).</remarks>
        /// <returns>Stream of paired values.</returns>
        public static IProducer<TOut> Pair<TPrimary, TSecondary, TOut>(
            this IProducer<TPrimary> primary,
            IProducer<TSecondary> secondary,
            Func<TPrimary, TSecondary, TOut> outputCreator,
            DeliveryPolicy primaryDeliveryPolicy = null,
            DeliveryPolicy secondaryDeliveryPolicy = null,
            Pipeline pipeline = null)
        {
            return Pair(
                new Pair<TPrimary, TSecondary, TOut>(pipeline ?? primary.Out.Pipeline, outputCreator),
                primary,
                secondary,
                primaryDeliveryPolicy,
                secondaryDeliveryPolicy);
        }

        /// <summary>
        /// Pair with latest (in wall-clock sense) values from a secondary stream.
        /// </summary>
        /// <typeparam name="TPrimary">Type of primary messages.</typeparam>
        /// <typeparam name="TSecondary">Type of secondary messages.</typeparam>
        /// <param name="primary">Primary stream.</param>
        /// <param name="secondary">Secondary stream.</param>
        /// <param name="initialValue">An initial value to be used until the first secondary message is received.</param>
        /// <param name="primaryDeliveryPolicy">An optional delivery policy for the primary stream.</param>
        /// <param name="secondaryDeliveryPolicy">An optional delivery policy for the secondary stream(s).</param>
        /// <param name="pipeline">The pipeline to which this component belongs (optional, defaults to that of the primary stream).</param>
        /// <returns>Stream of paired (`ValueTuple`) values.</returns>
        public static IProducer<ValueTuple<TPrimary, TSecondary>> Pair<TPrimary, TSecondary>(
            this IProducer<TPrimary> primary,
            IProducer<TSecondary> secondary,
            TSecondary initialValue,
            DeliveryPolicy primaryDeliveryPolicy = null,
            DeliveryPolicy secondaryDeliveryPolicy = null,
            Pipeline pipeline = null)
        {
            return Pair(
                new Pair<TPrimary, TSecondary, ValueTuple<TPrimary, TSecondary>>(pipeline ?? primary.Out.Pipeline, ValueTuple.Create, initialValue),
                primary,
                secondary,
                primaryDeliveryPolicy,
                secondaryDeliveryPolicy);
        }

        /// <summary>
        /// Pair with latest (in wall-clock sense) values from a secondary stream.
        /// </summary>
        /// <typeparam name="TPrimary">Type of primary messages.</typeparam>
        /// <typeparam name="TSecondary">Type of secondary messages.</typeparam>
        /// <param name="primary">Primary stream.</param>
        /// <param name="secondary">Secondary stream.</param>
        /// <param name="primaryDeliveryPolicy">An optional delivery policy for the primary stream.</param>
        /// <param name="secondaryDeliveryPolicy">An optional delivery policy for the secondary stream(s).</param>
        /// <param name="pipeline">The pipeline to which this component belongs (optional, defaults to that of the primary stream).</param>
        /// <remarks>Primary messages will be dropped until the first secondary message is received (no `initialValue` provided).</remarks>
        /// <returns>Stream of paired (`ValueTuple`) values.</returns>
        public static IProducer<ValueTuple<TPrimary, TSecondary>> Pair<TPrimary, TSecondary>(
            this IProducer<TPrimary> primary,
            IProducer<TSecondary> secondary,
            DeliveryPolicy primaryDeliveryPolicy = null,
            DeliveryPolicy secondaryDeliveryPolicy = null,
            Pipeline pipeline = null)
        {
            return Pair(
                new Pair<TPrimary, TSecondary, ValueTuple<TPrimary, TSecondary>>(pipeline ?? primary.Out.Pipeline, ValueTuple.Create),
                primary,
                secondary,
                primaryDeliveryPolicy,
                secondaryDeliveryPolicy);
        }

        #endregion scalar pairs

        #region tuple-flattening scalar pairs

        /// <summary>
        /// Pair with latest (in wall-clock sense) values from a secondary stream.
        /// </summary>
        /// <typeparam name="TPrimaryItem1">Type of item 1 of primary messages.</typeparam>
        /// <typeparam name="TPrimaryItem2">Type of item 2 of primary messages.</typeparam>
        /// <typeparam name="TSecondary">Type of secondary messages.</typeparam>
        /// <param name="primary">Primary stream of tuples (arity 2).</param>
        /// <param name="secondary">Secondary stream.</param>
        /// <param name="initialValue">An initial value to be used until the first secondary message is received.</param>
        /// <param name="primaryDeliveryPolicy">An optional delivery policy for the primary stream.</param>
        /// <param name="secondaryDeliveryPolicy">An optional delivery policy for the secondary stream(s).</param>
        /// <param name="pipeline">The pipeline to which this component belongs (optional, defaults to that of the primary stream).</param>
        /// <returns>Stream of paired (`ValueTuple`) values flattened to arity 3.</returns>
        public static IProducer<ValueTuple<TPrimaryItem1, TPrimaryItem2, TSecondary>> Pair<TPrimaryItem1, TPrimaryItem2, TSecondary>(
            this IProducer<ValueTuple<TPrimaryItem1, TPrimaryItem2>> primary,
            IProducer<TSecondary> secondary,
            TSecondary initialValue,
            DeliveryPolicy primaryDeliveryPolicy = null,
            DeliveryPolicy secondaryDeliveryPolicy = null,
            Pipeline pipeline = null)
        {
            return Pair(primary, secondary, (p, s) => ValueTuple.Create(p.Item1, p.Item2, s), initialValue, primaryDeliveryPolicy, secondaryDeliveryPolicy, pipeline);
        }

        /// <summary>
        /// Pair with latest (in wall-clock sense) values from a secondary stream.
        /// </summary>
        /// <typeparam name="TPrimaryItem1">Type of item 1 of primary messages.</typeparam>
        /// <typeparam name="TPrimaryItem2">Type of item 2 of primary messages.</typeparam>
        /// <typeparam name="TSecondary">Type of secondary messages.</typeparam>
        /// <param name="primary">Primary stream of tuples (arity 2).</param>
        /// <param name="secondary">Secondary stream.</param>
        /// <param name="primaryDeliveryPolicy">An optional delivery policy for the primary stream.</param>
        /// <param name="secondaryDeliveryPolicy">An optional delivery policy for the secondary stream(s).</param>
        /// <param name="pipeline">The pipeline to which this component belongs (optional, defaults to that of the primary stream).</param>
        /// <returns>Stream of paired (`ValueTuple`) values flattened to arity 3.</returns>
        public static IProducer<ValueTuple<TPrimaryItem1, TPrimaryItem2, TSecondary>> Pair<TPrimaryItem1, TPrimaryItem2, TSecondary>(
            this IProducer<ValueTuple<TPrimaryItem1, TPrimaryItem2>> primary,
            IProducer<TSecondary> secondary,
            DeliveryPolicy primaryDeliveryPolicy = null,
            DeliveryPolicy secondaryDeliveryPolicy = null,
            Pipeline pipeline = null)
        {
            return Pair(primary, secondary, (p, s) => ValueTuple.Create(p.Item1, p.Item2, s), primaryDeliveryPolicy, secondaryDeliveryPolicy, pipeline);
        }

        /// <summary>
        /// Pair with latest (in wall-clock sense) values from a secondary stream.
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
        /// <param name="pipeline">The pipeline to which this component belongs (optional, defaults to that of the primary stream).</param>
        /// <returns>Stream of paired (`ValueTuple`) values flattened to arity 4.</returns>
        public static IProducer<ValueTuple<TPrimaryItem1, TPrimaryItem2, TPrimaryItem3, TSecondary>> Pair<TPrimaryItem1, TPrimaryItem2, TPrimaryItem3, TSecondary>(
            this IProducer<ValueTuple<TPrimaryItem1, TPrimaryItem2, TPrimaryItem3>> primary,
            IProducer<TSecondary> secondary,
            TSecondary initialValue,
            DeliveryPolicy primaryDeliveryPolicy = null,
            DeliveryPolicy secondaryDeliveryPolicy = null,
            Pipeline pipeline = null)
        {
            return Pair(primary, secondary, (p, s) => ValueTuple.Create(p.Item1, p.Item2, p.Item3, s), initialValue, primaryDeliveryPolicy, secondaryDeliveryPolicy, pipeline);
        }

        /// <summary>
        /// Pair with latest (in wall-clock sense) values from a secondary stream.
        /// </summary>
        /// <typeparam name="TPrimaryItem1">Type of item 1 of primary messages.</typeparam>
        /// <typeparam name="TPrimaryItem2">Type of item 2 of primary messages.</typeparam>
        /// <typeparam name="TPrimaryItem3">Type of item 3 of primary messages.</typeparam>
        /// <typeparam name="TSecondary">Type of secondary messages.</typeparam>
        /// <param name="primary">Primary stream of tuples (arity 3).</param>
        /// <param name="secondary">Secondary stream.</param>
        /// <param name="primaryDeliveryPolicy">An optional delivery policy for the primary stream.</param>
        /// <param name="secondaryDeliveryPolicy">An optional delivery policy for the secondary stream(s).</param>
        /// <param name="pipeline">The pipeline to which this component belongs (optional, defaults to that of the primary stream).</param>
        /// <returns>Stream of paired (`ValueTuple`) values flattened to arity 4.</returns>
        public static IProducer<ValueTuple<TPrimaryItem1, TPrimaryItem2, TPrimaryItem3, TSecondary>> Pair<TPrimaryItem1, TPrimaryItem2, TPrimaryItem3, TSecondary>(
            this IProducer<ValueTuple<TPrimaryItem1, TPrimaryItem2, TPrimaryItem3>> primary,
            IProducer<TSecondary> secondary,
            DeliveryPolicy primaryDeliveryPolicy = null,
            DeliveryPolicy secondaryDeliveryPolicy = null,
            Pipeline pipeline = null)
        {
            return Pair(primary, secondary, (p, s) => ValueTuple.Create(p.Item1, p.Item2, p.Item3, s), primaryDeliveryPolicy, secondaryDeliveryPolicy, pipeline);
        }

        /// <summary>
        /// Pair with latest (in wall-clock sense) values from a secondary stream.
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
        /// <param name="pipeline">The pipeline to which this component belongs (optional, defaults to that of the primary stream).</param>
        /// <returns>Stream of paired (`ValueTuple`) values flattened to arity 5.</returns>
        public static IProducer<ValueTuple<TPrimaryItem1, TPrimaryItem2, TPrimaryItem3, TPrimaryItem4, TSecondary>> Pair<TPrimaryItem1, TPrimaryItem2, TPrimaryItem3, TPrimaryItem4, TSecondary>(
            this IProducer<ValueTuple<TPrimaryItem1, TPrimaryItem2, TPrimaryItem3, TPrimaryItem4>> primary,
            IProducer<TSecondary> secondary,
            TSecondary initialValue,
            DeliveryPolicy primaryDeliveryPolicy = null,
            DeliveryPolicy secondaryDeliveryPolicy = null,
            Pipeline pipeline = null)
        {
            return Pair(primary, secondary, (p, s) => ValueTuple.Create(p.Item1, p.Item2, p.Item3, p.Item4, s), initialValue, primaryDeliveryPolicy, secondaryDeliveryPolicy, pipeline);
        }

        /// <summary>
        /// Pair with latest (in wall-clock sense) values from a secondary stream.
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
        /// <param name="pipeline">The pipeline to which this component belongs (optional, defaults to that of the primary stream).</param>
        /// <returns>Stream of paired (`ValueTuple`) values flattened to arity 5.</returns>
        public static IProducer<ValueTuple<TPrimaryItem1, TPrimaryItem2, TPrimaryItem3, TPrimaryItem4, TSecondary>> Pair<TPrimaryItem1, TPrimaryItem2, TPrimaryItem3, TPrimaryItem4, TSecondary>(
            this IProducer<ValueTuple<TPrimaryItem1, TPrimaryItem2, TPrimaryItem3, TPrimaryItem4>> primary,
            IProducer<TSecondary> secondary,
            DeliveryPolicy primaryDeliveryPolicy = null,
            DeliveryPolicy secondaryDeliveryPolicy = null,
            Pipeline pipeline = null)
        {
            return Pair(primary, secondary, (p, s) => ValueTuple.Create(p.Item1, p.Item2, p.Item3, p.Item4, s), primaryDeliveryPolicy, secondaryDeliveryPolicy, pipeline);
        }

        /// <summary>
        /// Pair with latest (in wall-clock sense) values from a secondary stream.
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
        /// <param name="pipeline">The pipeline to which this component belongs (optional, defaults to that of the primary stream).</param>
        /// <returns>Stream of paired (`ValueTuple`) values flattened to arity 6.</returns>
        public static IProducer<ValueTuple<TPrimaryItem1, TPrimaryItem2, TPrimaryItem3, TPrimaryItem4, TPrimaryItem5, TSecondary>> Pair<TPrimaryItem1, TPrimaryItem2, TPrimaryItem3, TPrimaryItem4, TPrimaryItem5, TSecondary>(
            this IProducer<ValueTuple<TPrimaryItem1, TPrimaryItem2, TPrimaryItem3, TPrimaryItem4, TPrimaryItem5>> primary,
            IProducer<TSecondary> secondary,
            TSecondary initialValue,
            DeliveryPolicy primaryDeliveryPolicy = null,
            DeliveryPolicy secondaryDeliveryPolicy = null,
            Pipeline pipeline = null)
        {
            return Pair(primary, secondary, (p, s) => ValueTuple.Create(p.Item1, p.Item2, p.Item3, p.Item4, p.Item5, s), initialValue, primaryDeliveryPolicy, secondaryDeliveryPolicy, pipeline);
        }

        /// <summary>
        /// Pair with latest (in wall-clock sense) values from a secondary stream.
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
        /// <param name="pipeline">The pipeline to which this component belongs (optional, defaults to that of the primary stream).</param>
        /// <returns>Stream of paired (`ValueTuple`) values flattened to arity 6.</returns>
        public static IProducer<ValueTuple<TPrimaryItem1, TPrimaryItem2, TPrimaryItem3, TPrimaryItem4, TPrimaryItem5, TSecondary>> Pair<TPrimaryItem1, TPrimaryItem2, TPrimaryItem3, TPrimaryItem4, TPrimaryItem5, TSecondary>(
            this IProducer<ValueTuple<TPrimaryItem1, TPrimaryItem2, TPrimaryItem3, TPrimaryItem4, TPrimaryItem5>> primary,
            IProducer<TSecondary> secondary,
            DeliveryPolicy primaryDeliveryPolicy = null,
            DeliveryPolicy secondaryDeliveryPolicy = null,
            Pipeline pipeline = null)
        {
            return Pair(primary, secondary, (p, s) => ValueTuple.Create(p.Item1, p.Item2, p.Item3, p.Item4, p.Item5, s), primaryDeliveryPolicy, secondaryDeliveryPolicy, pipeline);
        }

        /// <summary>
        /// Pair with latest (in wall-clock sense) values from a secondary stream.
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
        /// <param name="pipeline">The pipeline to which this component belongs (optional, defaults to that of the primary stream).</param>
        /// <returns>Stream of paired (`ValueTuple`) values flattened to arity 7.</returns>
        public static IProducer<ValueTuple<TPrimaryItem1, TPrimaryItem2, TPrimaryItem3, TPrimaryItem4, TPrimaryItem5, TPrimaryItem6, TSecondary>> Pair<TPrimaryItem1, TPrimaryItem2, TPrimaryItem3, TPrimaryItem4, TPrimaryItem5, TPrimaryItem6, TSecondary>(
            this IProducer<ValueTuple<TPrimaryItem1, TPrimaryItem2, TPrimaryItem3, TPrimaryItem4, TPrimaryItem5, TPrimaryItem6>> primary,
            IProducer<TSecondary> secondary,
            TSecondary initialValue,
            DeliveryPolicy primaryDeliveryPolicy = null,
            DeliveryPolicy secondaryDeliveryPolicy = null,
            Pipeline pipeline = null)
        {
            return Pair(primary, secondary, (p, s) => ValueTuple.Create(p.Item1, p.Item2, p.Item3, p.Item4, p.Item5, p.Item6, s), initialValue, primaryDeliveryPolicy, secondaryDeliveryPolicy, pipeline);
        }

        /// <summary>
        /// Pair with latest (in wall-clock sense) values from a secondary stream.
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
        /// <param name="pipeline">The pipeline to which this component belongs (optional, defaults to that of the primary stream).</param>
        /// <returns>Stream of paired (`ValueTuple`) values flattened to arity 7.</returns>
        public static IProducer<ValueTuple<TPrimaryItem1, TPrimaryItem2, TPrimaryItem3, TPrimaryItem4, TPrimaryItem5, TPrimaryItem6, TSecondary>> Pair<TPrimaryItem1, TPrimaryItem2, TPrimaryItem3, TPrimaryItem4, TPrimaryItem5, TPrimaryItem6, TSecondary>(
            this IProducer<ValueTuple<TPrimaryItem1, TPrimaryItem2, TPrimaryItem3, TPrimaryItem4, TPrimaryItem5, TPrimaryItem6>> primary,
            IProducer<TSecondary> secondary,
            DeliveryPolicy primaryDeliveryPolicy = null,
            DeliveryPolicy secondaryDeliveryPolicy = null,
            Pipeline pipeline = null)
        {
            return Pair(primary, secondary, (p, s) => ValueTuple.Create(p.Item1, p.Item2, p.Item3, p.Item4, p.Item5, p.Item6, s), primaryDeliveryPolicy, secondaryDeliveryPolicy, pipeline);
        }

        #endregion tuple-flattening scalar pairs

        #region reverse tuple-flattening scalar pairs

        /// <summary>
        /// Pair with latest (in wall-clock sense) values from a secondary stream.
        /// </summary>
        /// <typeparam name="TPrimary">Type of primary messages.</typeparam>
        /// <typeparam name="TSecondaryItem1">Type of item 1 of secondary messages.</typeparam>
        /// <typeparam name="TSecondaryItem2">Type of item 2 of secondary messages.</typeparam>
        /// <param name="primary">Primary stream.</param>
        /// <param name="secondary">Secondary stream of tuples (arity 2).</param>
        /// <param name="initialValue">An initial value to be used until the first secondary message is received.</param>
        /// <param name="primaryDeliveryPolicy">An optional delivery policy for the primary stream.</param>
        /// <param name="secondaryDeliveryPolicy">An optional delivery policy for the secondary stream(s).</param>
        /// <param name="pipeline">The pipeline to which this component belongs (optional, defaults to that of the primary stream).</param>
        /// <returns>Stream of paired (`ValueTuple`) values flattened to arity 3.</returns>
        public static IProducer<ValueTuple<TPrimary, TSecondaryItem1, TSecondaryItem2>> Pair<TPrimary, TSecondaryItem1, TSecondaryItem2>(
            this IProducer<TPrimary> primary,
            IProducer<ValueTuple<TSecondaryItem1, TSecondaryItem2>> secondary,
            ValueTuple<TSecondaryItem1, TSecondaryItem2> initialValue,
            DeliveryPolicy primaryDeliveryPolicy = null,
            DeliveryPolicy secondaryDeliveryPolicy = null,
            Pipeline pipeline = null)
        {
            return Pair(primary, secondary, (p, s) => ValueTuple.Create(p, s.Item1, s.Item2), initialValue, primaryDeliveryPolicy, secondaryDeliveryPolicy, pipeline);
        }

        /// <summary>
        /// Pair with latest (in wall-clock sense) values from a secondary stream.
        /// </summary>
        /// <typeparam name="TPrimary">Type of primary messages.</typeparam>
        /// <typeparam name="TSecondaryItem1">Type of item 1 of secondary messages.</typeparam>
        /// <typeparam name="TSecondaryItem2">Type of item 2 of secondary messages.</typeparam>
        /// <param name="primary">Primary stream.</param>
        /// <param name="secondary">Secondary stream of tuples (arity 2).</param>
        /// <param name="primaryDeliveryPolicy">An optional delivery policy for the primary stream.</param>
        /// <param name="secondaryDeliveryPolicy">An optional delivery policy for the secondary stream(s).</param>
        /// <param name="pipeline">The pipeline to which this component belongs (optional, defaults to that of the primary stream).</param>
        /// <returns>Stream of paired (`ValueTuple`) values flattened to arity 3.</returns>
        public static IProducer<ValueTuple<TPrimary, TSecondaryItem1, TSecondaryItem2>> Pair<TPrimary, TSecondaryItem1, TSecondaryItem2>(
            this IProducer<TPrimary> primary,
            IProducer<ValueTuple<TSecondaryItem1, TSecondaryItem2>> secondary,
            DeliveryPolicy primaryDeliveryPolicy = null,
            DeliveryPolicy secondaryDeliveryPolicy = null,
            Pipeline pipeline = null)
        {
            return Pair(primary, secondary, (p, s) => ValueTuple.Create(p, s.Item1, s.Item2), primaryDeliveryPolicy, secondaryDeliveryPolicy, pipeline);
        }

        /// <summary>
        /// Pair with latest (in wall-clock sense) values from a secondary stream.
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
        /// <param name="pipeline">The pipeline to which this component belongs (optional, defaults to that of the primary stream).</param>
        /// <returns>Stream of paired (`ValueTuple`) values flattened to arity 4.</returns>
        public static IProducer<ValueTuple<TPrimary, TSecondaryItem1, TSecondaryItem2, TSecondaryItem3>> Pair<TPrimary, TSecondaryItem1, TSecondaryItem2, TSecondaryItem3>(
            this IProducer<TPrimary> primary,
            IProducer<ValueTuple<TSecondaryItem1, TSecondaryItem2, TSecondaryItem3>> secondary,
            ValueTuple<TSecondaryItem1, TSecondaryItem2, TSecondaryItem3> initialValue,
            DeliveryPolicy primaryDeliveryPolicy = null,
            DeliveryPolicy secondaryDeliveryPolicy = null,
            Pipeline pipeline = null)
        {
            return Pair(primary, secondary, (p, s) => ValueTuple.Create(p, s.Item1, s.Item2, s.Item3), initialValue, primaryDeliveryPolicy, secondaryDeliveryPolicy, pipeline);
        }

        /// <summary>
        /// Pair with latest (in wall-clock sense) values from a secondary stream.
        /// </summary>
        /// <typeparam name="TPrimary">Type of primary messages.</typeparam>
        /// <typeparam name="TSecondaryItem1">Type of item 1 of secondary messages.</typeparam>
        /// <typeparam name="TSecondaryItem2">Type of item 2 of secondary messages.</typeparam>
        /// <typeparam name="TSecondaryItem3">Type of item 3 of secondary messages.</typeparam>
        /// <param name="primary">Primary stream.</param>
        /// <param name="secondary">Secondary stream of tuples (arity 3).</param>
        /// <param name="primaryDeliveryPolicy">An optional delivery policy for the primary stream.</param>
        /// <param name="secondaryDeliveryPolicy">An optional delivery policy for the secondary stream(s).</param>
        /// <param name="pipeline">The pipeline to which this component belongs (optional, defaults to that of the primary stream).</param>
        /// <returns>Stream of paired (`ValueTuple`) values flattened to arity 4.</returns>
        public static IProducer<ValueTuple<TPrimary, TSecondaryItem1, TSecondaryItem2, TSecondaryItem3>> Pair<TPrimary, TSecondaryItem1, TSecondaryItem2, TSecondaryItem3>(
            this IProducer<TPrimary> primary,
            IProducer<ValueTuple<TSecondaryItem1, TSecondaryItem2, TSecondaryItem3>> secondary,
            DeliveryPolicy primaryDeliveryPolicy = null,
            DeliveryPolicy secondaryDeliveryPolicy = null,
            Pipeline pipeline = null)
        {
            return Pair(primary, secondary, (p, s) => ValueTuple.Create(p, s.Item1, s.Item2, s.Item3), primaryDeliveryPolicy, secondaryDeliveryPolicy, pipeline);
        }

        /// <summary>
        /// Pair with latest (in wall-clock sense) values from a secondary stream.
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
        /// <param name="pipeline">The pipeline to which this component belongs (optional, defaults to that of the primary stream).</param>
        /// <returns>Stream of paired (`ValueTuple`) values flattened to arity 5.</returns>
        public static IProducer<ValueTuple<TPrimary, TSecondaryItem1, TSecondaryItem2, TSecondaryItem3, TSecondaryItem4>> Pair<TPrimary, TSecondaryItem1, TSecondaryItem2, TSecondaryItem3, TSecondaryItem4>(
            this IProducer<TPrimary> primary,
            IProducer<ValueTuple<TSecondaryItem1, TSecondaryItem2, TSecondaryItem3, TSecondaryItem4>> secondary,
            ValueTuple<TSecondaryItem1, TSecondaryItem2, TSecondaryItem3, TSecondaryItem4> initialValue,
            DeliveryPolicy primaryDeliveryPolicy = null,
            DeliveryPolicy secondaryDeliveryPolicy = null,
            Pipeline pipeline = null)
        {
            return Pair(primary, secondary, (p, s) => ValueTuple.Create(p, s.Item1, s.Item2, s.Item3, s.Item4), initialValue, primaryDeliveryPolicy, secondaryDeliveryPolicy, pipeline);
        }

        /// <summary>
        /// Pair with latest (in wall-clock sense) values from a secondary stream.
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
        /// <param name="pipeline">The pipeline to which this component belongs (optional, defaults to that of the primary stream).</param>
        /// <returns>Stream of paired (`ValueTuple`) values flattened to arity 5.</returns>
        public static IProducer<ValueTuple<TPrimary, TSecondaryItem1, TSecondaryItem2, TSecondaryItem3, TSecondaryItem4>> Pair<TPrimary, TSecondaryItem1, TSecondaryItem2, TSecondaryItem3, TSecondaryItem4>(
            this IProducer<TPrimary> primary,
            IProducer<ValueTuple<TSecondaryItem1, TSecondaryItem2, TSecondaryItem3, TSecondaryItem4>> secondary,
            DeliveryPolicy primaryDeliveryPolicy = null,
            DeliveryPolicy secondaryDeliveryPolicy = null,
            Pipeline pipeline = null)
        {
            return Pair(primary, secondary, (p, s) => ValueTuple.Create(p, s.Item1, s.Item2, s.Item3, s.Item4), primaryDeliveryPolicy, secondaryDeliveryPolicy, pipeline);
        }

        /// <summary>
        /// Pair with latest (in wall-clock sense) values from a secondary stream.
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
        /// <param name="pipeline">The pipeline to which this component belongs (optional, defaults to that of the primary stream).</param>
        /// <returns>Stream of paired (`ValueTuple`) values flattened to arity 6.</returns>
        public static IProducer<ValueTuple<TPrimary, TSecondaryItem1, TSecondaryItem2, TSecondaryItem3, TSecondaryItem4, TSecondaryItem5>> Pair<TPrimary, TSecondaryItem1, TSecondaryItem2, TSecondaryItem3, TSecondaryItem4, TSecondaryItem5>(
            this IProducer<TPrimary> primary,
            IProducer<ValueTuple<TSecondaryItem1, TSecondaryItem2, TSecondaryItem3, TSecondaryItem4, TSecondaryItem5>> secondary,
            ValueTuple<TSecondaryItem1, TSecondaryItem2, TSecondaryItem3, TSecondaryItem4, TSecondaryItem5> initialValue,
            DeliveryPolicy primaryDeliveryPolicy = null,
            DeliveryPolicy secondaryDeliveryPolicy = null,
            Pipeline pipeline = null)
        {
            return Pair(primary, secondary, (p, s) => ValueTuple.Create(p, s.Item1, s.Item2, s.Item3, s.Item4, s.Item5), initialValue, primaryDeliveryPolicy, secondaryDeliveryPolicy, pipeline);
        }

        /// <summary>
        /// Pair with latest (in wall-clock sense) values from a secondary stream.
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
        /// <param name="pipeline">The pipeline to which this component belongs (optional, defaults to that of the primary stream).</param>
        /// <returns>Stream of paired (`ValueTuple`) values flattened to arity 6.</returns>
        public static IProducer<ValueTuple<TPrimary, TSecondaryItem1, TSecondaryItem2, TSecondaryItem3, TSecondaryItem4, TSecondaryItem5>> Pair<TPrimary, TSecondaryItem1, TSecondaryItem2, TSecondaryItem3, TSecondaryItem4, TSecondaryItem5>(
            this IProducer<TPrimary> primary,
            IProducer<ValueTuple<TSecondaryItem1, TSecondaryItem2, TSecondaryItem3, TSecondaryItem4, TSecondaryItem5>> secondary,
            DeliveryPolicy primaryDeliveryPolicy = null,
            DeliveryPolicy secondaryDeliveryPolicy = null,
            Pipeline pipeline = null)
        {
            return Pair(primary, secondary, (p, s) => ValueTuple.Create(p, s.Item1, s.Item2, s.Item3, s.Item4, s.Item5), primaryDeliveryPolicy, secondaryDeliveryPolicy, pipeline);
        }

        /// <summary>
        /// Pair with latest (in wall-clock sense) values from a secondary stream.
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
        /// <param name="pipeline">The pipeline to which this component belongs (optional, defaults to that of the primary stream).</param>
        /// <returns>Stream of paired (`ValueTuple`) values flattened to arity 7.</returns>
        public static IProducer<ValueTuple<TPrimary, TSecondaryItem1, TSecondaryItem2, TSecondaryItem3, TSecondaryItem4, TSecondaryItem5, TSecondaryItem6>> Pair<TPrimary, TSecondaryItem1, TSecondaryItem2, TSecondaryItem3, TSecondaryItem4, TSecondaryItem5, TSecondaryItem6>(
            this IProducer<TPrimary> primary,
            IProducer<ValueTuple<TSecondaryItem1, TSecondaryItem2, TSecondaryItem3, TSecondaryItem4, TSecondaryItem5, TSecondaryItem6>> secondary,
            ValueTuple<TSecondaryItem1, TSecondaryItem2, TSecondaryItem3, TSecondaryItem4, TSecondaryItem5, TSecondaryItem6> initialValue,
            DeliveryPolicy primaryDeliveryPolicy = null,
            DeliveryPolicy secondaryDeliveryPolicy = null,
            Pipeline pipeline = null)
        {
            return Pair(primary, secondary, (p, s) => ValueTuple.Create(p, s.Item1, s.Item2, s.Item3, s.Item4, s.Item5, s.Item6), initialValue, primaryDeliveryPolicy, secondaryDeliveryPolicy, pipeline);
        }

        /// <summary>
        /// Pair with latest (in wall-clock sense) values from a secondary stream.
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
        /// <param name="pipeline">The pipeline to which this component belongs (optional, defaults to that of the primary stream).</param>
        /// <returns>Stream of paired (`ValueTuple`) values flattened to arity 7.</returns>
        public static IProducer<ValueTuple<TPrimary, TSecondaryItem1, TSecondaryItem2, TSecondaryItem3, TSecondaryItem4, TSecondaryItem5, TSecondaryItem6>> Pair<TPrimary, TSecondaryItem1, TSecondaryItem2, TSecondaryItem3, TSecondaryItem4, TSecondaryItem5, TSecondaryItem6>(
            this IProducer<TPrimary> primary,
            IProducer<ValueTuple<TSecondaryItem1, TSecondaryItem2, TSecondaryItem3, TSecondaryItem4, TSecondaryItem5, TSecondaryItem6>> secondary,
            DeliveryPolicy primaryDeliveryPolicy = null,
            DeliveryPolicy secondaryDeliveryPolicy = null,
            Pipeline pipeline = null)
        {
            return Pair(primary, secondary, (p, s) => ValueTuple.Create(p, s.Item1, s.Item2, s.Item3, s.Item4, s.Item5, s.Item6), primaryDeliveryPolicy, secondaryDeliveryPolicy, pipeline);
        }

#endregion reverse tuple-flattening scalar pairs

        private static IProducer<TOut> Pair<TPrimary, TSecondary, TOut>(
            Pair<TPrimary, TSecondary, TOut> pair,
            IProducer<TPrimary> primary,
            IProducer<TSecondary> secondary,
            DeliveryPolicy primaryDeliveryPolicy = null,
            DeliveryPolicy secondaryDeliveryPolicy = null)
        {
            primary.PipeTo(pair.InPrimary, primaryDeliveryPolicy);
            secondary.PipeTo(pair.InSecondary, secondaryDeliveryPolicy);
            return pair;
        }
    }
}
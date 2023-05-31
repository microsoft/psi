// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.

namespace Microsoft.Psi.Visualization.Extensions
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Windows;
    using MathNet.Spatial.Euclidean;

    /// <summary>
    /// Extension methods for use with visualizations.
    /// </summary>
    public static class VisualizationExtensions
    {
        /// <summary>
        /// Converts stream of enumerables of T to a stream of arrays of T.
        /// </summary>
        /// <typeparam name="T">The type of enumerable elements.</typeparam>
        /// <typeparam name="TEnumerable">The type of enumerable.</typeparam>
        /// <param name="source">The stream of enumerables of T.</param>
        /// <param name="deliveryPolicy">An optional delivery policy.</param>
        /// <returns>A stream of the converted array of T.</returns>
        public static IProducer<T[]> ToArray<T, TEnumerable>(this IProducer<TEnumerable> source, DeliveryPolicy<TEnumerable> deliveryPolicy = null)
            where TEnumerable : IEnumerable<T>
        {
            return source.Select(s => s.ToArray(), deliveryPolicy);
        }

        /// <summary>
        /// Converts stream of dictionaries of TKey and TValue to a stream of collections of TValue.
        /// </summary>
        /// <typeparam name="TKey">The type of dictionary keys.</typeparam>
        /// <typeparam name="TValue">The type of dictionary values.</typeparam>
        /// <param name="source">The stream of dictionaries of TKey and TValue.</param>
        /// <param name="deliveryPolicy">An optional delivery policy.</param>
        /// <returns>A stream of the converted collections of TValue.</returns>
        public static IProducer<Dictionary<TKey, TValue>.ValueCollection> Values<TKey, TValue>(this IProducer<Dictionary<TKey, TValue>> source, DeliveryPolicy<Dictionary<TKey, TValue>> deliveryPolicy = null)
        {
            return source.Select(s => s.Values, deliveryPolicy);
        }

        /// <summary>
        /// Converts stream of dictionaries of 2d points to a stream of list of named points.
        /// </summary>
        /// <typeparam name="TKey">The type of dictionary keys.</typeparam>
        /// <param name="source">The stream of dictionaries of 2d points.</param>
        /// <param name="deliveryPolicy">An optional delivery policy.</param>
        /// <returns>A stream of the converted list of named points.</returns>
        public static IProducer<List<Tuple<Point, string>>> ToScatterPoints2D<TKey>(this IProducer<Dictionary<TKey, Point2D>> source, DeliveryPolicy<Dictionary<TKey, Point2D>> deliveryPolicy = null)
        {
            return source.Select(d => d.Select(kvp => Tuple.Create(new Point(kvp.Value.X, kvp.Value.Y), kvp.Key.ToString())).ToList(), deliveryPolicy);
        }

        /// <summary>
        /// Converts stream of dictionaries of rectangles to a stream of list of named rectangles.
        /// </summary>
        /// <typeparam name="TKey">The type of dictionary keys.</typeparam>
        /// <param name="source">The stream of dictionaries of rectangles.</param>
        /// <param name="deliveryPolicy">An optional delivery policy.</param>
        /// <returns>A stream of the converted list of named rectangles.</returns>
        public static IProducer<List<Tuple<System.Drawing.Rectangle, string>>> ToScatterRectangle<TKey>(this IProducer<Dictionary<TKey, System.Drawing.Rectangle>> source, DeliveryPolicy<Dictionary<TKey, System.Drawing.Rectangle>> deliveryPolicy = null)
        {
            return source.Select(d => d.Select(kvp => Tuple.Create(kvp.Value, kvp.Key.ToString())).ToList(), deliveryPolicy);
        }
    }
}

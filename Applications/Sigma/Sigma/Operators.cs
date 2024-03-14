// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the Microsoft Research license.

namespace Sigma
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using MathNet.Spatial.Euclidean;
    using Microsoft.Psi;
    using Microsoft.Psi.MixedReality;
    using Microsoft.Psi.MixedReality.Applications;
    using Microsoft.Psi.MixedReality.OpenXR;

    /// <summary>
    /// Implements a set of helper operators for the Sigma app.
    /// </summary>
    public static class Operators
    {
        /// <summary>
        /// Updates the set of keys in a dictionary, based on a new set of keys.
        /// </summary>
        /// <typeparam name="TKey">The type of keys in the dictionary.</typeparam>
        /// <typeparam name="TValue">The type of values in the dictionary.</typeparam>
        /// <param name="dictionary">The dictionary to update.</param>
        /// <param name="newKeys">The new set of keys.</param>
        /// <param name="createKey">A function used to create new values when a new key is added to the dictionary.</param>
        /// <param name="editKey">A function used to edit values when a key already exists in the dictionary.</param>
        /// <param name="removeKey">A function used to specify an action to perform a key is removed from the dictionary.</param>
        public static void Update<TKey, TValue>(
            this Dictionary<TKey, TValue> dictionary,
            IEnumerable<TKey> newKeys,
            Func<TKey, TValue> createKey = null,
            Func<TKey, TValue> editKey = null,
            Action<TKey> removeKey = null)
        {
            createKey ??= key => default;

            // check out if any keys have disappeared
            var removeKeys = dictionary.Keys.Where(key => !newKeys.Contains(key)).ToArray();

            // remove these keys from the current dictionary
            foreach (var key in removeKeys)
            {
                removeKey?.Invoke(key);
                dictionary.Remove(key);
            }

            // call the edit action on the existing keys
            if (editKey != null)
            {
                foreach (var key in dictionary.Keys.ToList())
                {
                    dictionary[key] = editKey.Invoke(key);
                }
            }

            // add new keys
            foreach (var newKey in newKeys)
            {
                if (!dictionary.ContainsKey(newKey))
                {
                    dictionary.Add(newKey, createKey(newKey));
                }
            }
        }

        /// <summary>
        /// Converts the messages on a stream to present time.
        /// </summary>
        /// <typeparam name="T">The type of the stream data.</typeparam>
        /// <param name="source">The source stream.</param>
        /// <param name="deliveryPolicy">An optional delivery policy.</param>
        /// <param name="name">An optional name for the stream operator.</param>
        /// <returns>A stream with the same data as the source stream, but with present time.</returns>
        public static IProducer<T> ToPresent<T>(this IProducer<T> source, DeliveryPolicy<T> deliveryPolicy = null, string name = nameof(ToPresent))
            => source.Process<T, T>(
                (m, _, emitter) => emitter.Post(m, source.Out.Pipeline.GetCurrentTime()),
                deliveryPolicy,
                name: name);

        /// <summary>
        /// Determines whether a specified hand is pinching.
        /// </summary>
        /// <param name="hand">The hand.</param>
        /// <param name="pinchPoint">The pinch point, halfway between the index and thumb tips.</param>
        /// <returns>True if the hand is pinching, false otherwise.</returns>
        public static bool IsPinching(this Hand hand, out Point3D pinchPoint)
        {
            if (hand != null && hand.IsActive && hand.JointsValid[(int)HandJointIndex.IndexTip] && hand.JointsValid[(int)HandJointIndex.ThumbTip])
            {
                var distance = hand[HandJointIndex.IndexTip].Origin.DistanceTo(hand[HandJointIndex.ThumbTip].Origin);
                if (distance < 0.03)
                {
                    pinchPoint = Point3D.MidPoint(hand[HandJointIndex.IndexTip].Origin, hand[HandJointIndex.ThumbTip].Origin);
                    return true;
                }
            }

            pinchPoint = default;
            return false;
        }

        /// <summary>
        /// Determines whether the palm of the specified hand is facing upwards.
        /// </summary>
        /// <param name="hand">The hand.</param>
        /// <param name="palmPoint">The origin of the palm joint.</param>
        /// <returns>True if the palm is facing upwards, false otherwise.</returns>
        public static bool IsPalmUp(this Hand hand, out Point3D palmPoint)
        {
            if (hand != null && hand.IsActive && hand.JointsValid[(int)HandJointIndex.Palm])
            {
                var palm = hand[HandJointIndex.Palm];
                double dotProduct = palm.ZAxis.DotProduct(UnitVector3D.ZAxis);
                if (dotProduct < -0.9)
                {
                    palmPoint = palm.Origin;
                    return true;
                }
            }

            palmPoint = default;
            return false;
        }

        /// <summary>
        /// Gets a coordinate system at a specified position, with the X axis oriented towards a specified point.
        /// </summary>
        /// <param name="position">The position of the coordinate system.</param>
        /// <param name="target">The target point to orient the system towards.</param>
        /// <returns>The coordinate system at a specified position, with the X axis oriented towards a specified point.</returns>
        public static CoordinateSystem GetTargetOrientedCoordinateSystem(Point3D position, Point3D target)
        {
            var xAxis = (new Point3D(target.X, target.Y, target.Z) - position).Normalize();
            var yAxis = UnitVector3D.ZAxis.CrossProduct(xAxis);
            var zAxis = xAxis.CrossProduct(yAxis);
            return new CoordinateSystem(position, xAxis, yAxis, zAxis);
        }

        /// <summary>
        /// Gets a value indicating whether the palm is facing up.
        /// </summary>
        /// <param name="userState">The user state.</param>
        /// <param name="palmPoint">The palm point.</param>
        /// <returns>True if the palm is facing up, or false otherwise.</returns>
        public static bool IsPalmUp(this UserState userState, out Point3D palmPoint)
        {
            palmPoint = default;
            return
                (userState.HandLeft != null && userState.HandLeft.IsPalmUp(out palmPoint)) ||
                (userState.HandRight != null && userState.HandRight.IsPalmUp(out palmPoint));
        }

        /// <summary>
        /// Computes the slope of a stream of doubles.
        /// </summary>
        /// <param name="source">The source stream.</param>
        /// <param name="timeSpan">The time span over which to compute the slope.</param>
        /// <param name="deliveryPolicy">An optional delivery policy.</param>
        /// <returns>A stream containing the slope of the source stream.</returns>
        public static IProducer<double> Slope(this IProducer<double> source, TimeSpan timeSpan, DeliveryPolicy<double> deliveryPolicy = null)
        {
            return source.Window(
                new RelativeTimeInterval(-TimeSpan.FromTicks(timeSpan.Ticks / 2), TimeSpan.FromTicks(timeSpan.Ticks / 2)),
                buffer =>
                {
                    var nonNaN = buffer.Where(m => !double.IsNaN(m.Data));
                    if (buffer.Any(m => double.IsNaN(m.Data) || buffer.Count() <= 2))
                    {
                        return double.NaN;
                    }
                    else
                    {
                        var times = nonNaN.Select(m => m.OriginatingTime.Ticks / 10000000d);
                        var timeMean = times.Average();
                        var values = nonNaN.Select(m => m.Data);
                        var valuesMean = values.Average();
                        var numerator = nonNaN.Select(m => (m.OriginatingTime.Ticks / 10000000d - timeMean) * (m.Data - valuesMean)).Aggregate((x, y) => x + y);
                        var denominator = nonNaN.Select(m => (m.OriginatingTime.Ticks / 10000000d - timeMean) * (m.OriginatingTime.Ticks / 10000000d - timeMean)).Aggregate((x, y) => x + y);
                        return numerator / denominator;
                    }
                },
                deliveryPolicy);
        }

        /// <summary>
        /// Computes whether two enumerables are equal.
        /// </summary>
        /// <typeparam name="T">The type of the enumerable.</typeparam>
        /// <param name="enumerable">The first enumerable.</param>
        /// <param name="other">The second enumerable.</param>
        /// <returns>True if the two enumerables are equal.</returns>
        internal static bool EnumerableEquals<T>(IEnumerable<T> enumerable, IEnumerable<T> other)
        {
            if (enumerable == null)
            {
                return other == null;
            }
            else if (other == null)
            {
                return false;
            }
            else
            {
                return Enumerable.SequenceEqual(enumerable, other);
            }
        }

        /// <summary>
        /// Shifts the coordinate system by (u, v) in the UV plane (i.e. the Y,-Z plane).
        /// </summary>
        /// <param name="coordinateSystem">The input coordinate system.</param>
        /// <param name="u">The amount to shift in the u direction.</param>
        /// <param name="v">The amount to shift in the v direction.</param>
        /// <returns>The resulting coordinate system.</returns>
        internal static CoordinateSystem ApplyUV(this CoordinateSystem coordinateSystem, float u, float v)
            => new CoordinateSystem(new Point3D(0, u, -v), UnitVector3D.XAxis, UnitVector3D.YAxis, UnitVector3D.ZAxis)
                .TransformBy(coordinateSystem);
    }
}
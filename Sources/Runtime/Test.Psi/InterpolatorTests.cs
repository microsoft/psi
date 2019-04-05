// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.

namespace Test.Psi
{
    using System;
    using Microsoft.Psi;
    using Microsoft.VisualStudio.TestTools.UnitTesting;

    [TestClass]
    public class InterpolatorTests
    {
        [TestMethod]
        [Timeout(60000)]
        public void NearestValue_UnboundedDontRequireNext()
        {
            var interpolator = Match.Any<int>();
            var messages = new Message<int>[]
            {
                new Message<int>(1, new DateTime(10), new DateTime(11), 0, 0),
                new Message<int>(2, new DateTime(20), new DateTime(21), 0, 1),
                new Message<int>(3, new DateTime(30), new DateTime(31), 0, 2)
            };

            // Interpolate at point later than last message
            var result = interpolator.Match(new DateTime(100), messages, null);
            Assert.AreEqual(this.MakeResult(messages[2]), result);

            // Interpolate at point before first message
            result = interpolator.Match(new DateTime(9), messages, null);
            Assert.AreEqual(this.MakeResult(messages[0]), result);

            // Interpolate between first two messages (closer to earlier message)
            result = interpolator.Match(new DateTime(14), messages, null);
            Assert.AreEqual(this.MakeResult(messages[0]), result);

            // Interpolate between first two messages (exactly mid-way)
            result = interpolator.Match(new DateTime(15), messages, null);
            Assert.AreEqual(this.MakeResult(messages[1]), result);

            // Interpolate between last two messages (closer to later message)
            result = interpolator.Match(new DateTime(26), messages, null);
            Assert.AreEqual(this.MakeResult(messages[2]), result);
        }

        [TestMethod]
        [Timeout(60000)]
        public void NearestValue_UnboundedRequireNext()
        {
            var interpolator = Match.Best<int>(RelativeTimeInterval.Infinite);
            var messages = new Message<int>[]
            {
                new Message<int>(1, new DateTime(10), new DateTime(11), 0, 0),
                new Message<int>(2, new DateTime(20), new DateTime(21), 0, 1),
                new Message<int>(3, new DateTime(30), new DateTime(31), 0, 2)
            };

            // Interpolate at point later than last message
            var result = interpolator.Match(new DateTime(100), messages, null);
            Assert.AreEqual(MatchResult<int>.InsufficientData(DateTime.MinValue), result);

            // Interpolate at point before first message
            result = interpolator.Match(new DateTime(9), messages, null);
            Assert.AreEqual(this.MakeResult(messages[0]), result);

            // Interpolate between first two messages (closer to earlier message)
            result = interpolator.Match(new DateTime(14), messages, null);
            Assert.AreEqual(this.MakeResult(messages[0]), result);

            // Interpolate between first two messages (exactly mid-way)
            result = interpolator.Match(new DateTime(15), messages, null);
            Assert.AreEqual(this.MakeResult(messages[1]), result);

            // Interpolate between last two messages (closer to later message)
            result = interpolator.Match(new DateTime(26), messages, null);
            Assert.AreEqual(this.MakeResult(messages[2]), result);
        }

        [TestMethod]
        [Timeout(60000)]
        public void NearestValue_LeftBoundedDontRequireNext()
        {
            var interpolator = Match.Any<int>(new RelativeTimeInterval(TimeSpan.MinValue, TimeSpan.Zero));
            var messages = new Message<int>[]
            {
                new Message<int>(1, new DateTime(10), new DateTime(11), 0, 0),
                new Message<int>(2, new DateTime(20), new DateTime(21), 0, 1),
                new Message<int>(3, new DateTime(30), new DateTime(31), 0, 2)
            };

            // Interpolate at point later than last message
            var result = interpolator.Match(new DateTime(100), messages, null);
            Assert.AreEqual(this.MakeResult(messages[2]), result);

            // Interpolate at point before first message
            result = interpolator.Match(new DateTime(9), messages, null);
            Assert.AreEqual(MatchResult<int>.DoesNotExist(new DateTime(9)), result);

            // Interpolate between first two messages (closer to earlier message)
            result = interpolator.Match(new DateTime(14), messages, null);
            Assert.AreEqual(this.MakeResult(messages[0]), result);

            // Interpolate between first two messages (exactly mid-way)
            result = interpolator.Match(new DateTime(15), messages, null);
            Assert.AreEqual(this.MakeResult(messages[0]), result);

            // Interpolate between last two messages (closer to later message)
            result = interpolator.Match(new DateTime(26), messages, null);
            Assert.AreEqual(this.MakeResult(messages[1]), result);
        }

        [TestMethod]
        [Timeout(60000)]
        public void NearestValue_LeftBoundedRequireNext()
        {
            var interpolator = Match.Best<int>(new RelativeTimeInterval(TimeSpan.MinValue, TimeSpan.Zero));
            var messages = new Message<int>[]
            {
                new Message<int>(1, new DateTime(10), new DateTime(11), 0, 0),
                new Message<int>(2, new DateTime(20), new DateTime(21), 0, 1),
                new Message<int>(3, new DateTime(30), new DateTime(31), 0, 2)
            };

            // Interpolate at point later than last message
            var result = interpolator.Match(new DateTime(100), messages, null);
            Assert.AreEqual(MatchResult<int>.InsufficientData(DateTime.MinValue), result);

            // Interpolate at point before first message
            result = interpolator.Match(new DateTime(9), messages, null);
            Assert.AreEqual(MatchResult<int>.DoesNotExist(new DateTime(9)), result);

            // Interpolate between first two messages (closer to earlier message)
            result = interpolator.Match(new DateTime(14), messages, null);
            Assert.AreEqual(this.MakeResult(messages[0]), result);

            // Interpolate between first two messages (exactly mid-way)
            result = interpolator.Match(new DateTime(15), messages, null);
            Assert.AreEqual(this.MakeResult(messages[0]), result);

            // Interpolate between last two messages (closer to later message)
            result = interpolator.Match(new DateTime(26), messages, null);
            Assert.AreEqual(this.MakeResult(messages[1]), result);
        }

        [TestMethod]
        [Timeout(60000)]
        public void NearestValue_RightBoundedDontRequireNext()
        {
            var interpolator = Match.Any<int>(new RelativeTimeInterval(TimeSpan.Zero, TimeSpan.MaxValue));
            var messages = new Message<int>[]
            {
                new Message<int>(1, new DateTime(10), new DateTime(11), 0, 0),
                new Message<int>(2, new DateTime(20), new DateTime(21), 0, 1),
                new Message<int>(3, new DateTime(30), new DateTime(31), 0, 2)
            };

            // Interpolate at point later than last message
            var result = interpolator.Match(new DateTime(100), messages, null);
            Assert.AreEqual(MatchResult<int>.InsufficientData(DateTime.MinValue), result);

            // Interpolate at point before first message
            result = interpolator.Match(new DateTime(9), messages, null);
            Assert.AreEqual(this.MakeResult(messages[0]), result);

            // Interpolate between first two messages (closer to earlier message)
            result = interpolator.Match(new DateTime(14), messages, null);
            Assert.AreEqual(this.MakeResult(messages[1]), result);

            // Interpolate between first two messages (exactly mid-way)
            result = interpolator.Match(new DateTime(15), messages, null);
            Assert.AreEqual(this.MakeResult(messages[1]), result);

            // Interpolate between last two messages (closer to later message)
            result = interpolator.Match(new DateTime(26), messages, null);
            Assert.AreEqual(this.MakeResult(messages[2]), result);
        }

        [TestMethod]
        [Timeout(60000)]
        public void NearestValue_RightBoundedRequireNext()
        {
            var interpolator = Match.Best<int>(new RelativeTimeInterval(TimeSpan.Zero, TimeSpan.MaxValue));
            var messages = new Message<int>[]
            {
                new Message<int>(1, new DateTime(10), new DateTime(11), 0, 0),
                new Message<int>(2, new DateTime(20), new DateTime(21), 0, 1),
                new Message<int>(3, new DateTime(30), new DateTime(31), 0, 2)
            };

            // Interpolate at point later than last message
            var result = interpolator.Match(new DateTime(100), messages, null);
            Assert.AreEqual(MatchResult<int>.InsufficientData(DateTime.MinValue), result);

            // Interpolate at point before first message
            result = interpolator.Match(new DateTime(9), messages, null);
            Assert.AreEqual(this.MakeResult(messages[0]), result);

            // Interpolate between first two messages (closer to earlier message)
            result = interpolator.Match(new DateTime(14), messages, null);
            Assert.AreEqual(this.MakeResult(messages[1]), result);

            // Interpolate between first two messages (exactly mid-way)
            result = interpolator.Match(new DateTime(15), messages, null);
            Assert.AreEqual(this.MakeResult(messages[1]), result);

            // Interpolate between last two messages (closer to later message)
            result = interpolator.Match(new DateTime(26), messages, null);
            Assert.AreEqual(this.MakeResult(messages[2]), result);
        }

        [TestMethod]
        [Timeout(60000)]
        public void NearestValue_BoundedDontRequireNext()
        {
            var interpolator = Match.Any<int>(new RelativeTimeInterval(TimeSpan.FromTicks(-5), TimeSpan.FromTicks(5)));
            var messages = new Message<int>[]
            {
                new Message<int>(1, new DateTime(10), new DateTime(11), 0, 0),
                new Message<int>(2, new DateTime(20), new DateTime(21), 0, 1),
                new Message<int>(3, new DateTime(30), new DateTime(31), 0, 2)
            };

            // Interpolate at point later than last message (outside upper bound)
            var result = interpolator.Match(new DateTime(36), messages, null);
            Assert.AreEqual(MatchResult<int>.InsufficientData(DateTime.MinValue), result);

            // Interpolate at point later than last message (within upper bound)
            result = interpolator.Match(new DateTime(35), messages, null);
            Assert.AreEqual(this.MakeResult(messages[2]), result);

            // Interpolate at point before first message (outside lower bound)
            result = interpolator.Match(new DateTime(4), messages, null);
            Assert.AreEqual(MatchResult<int>.DoesNotExist(new DateTime(4)), result);

            // Interpolate at point before first message (within lower bound)
            result = interpolator.Match(new DateTime(5), messages, null);
            Assert.AreEqual(this.MakeResult(messages[0]), result);

            // Interpolate between first two messages (closer to earlier message)
            result = interpolator.Match(new DateTime(14), messages, null);
            Assert.AreEqual(this.MakeResult(messages[0]), result);

            // Interpolate between first two messages (exactly mid-way)
            result = interpolator.Match(new DateTime(15), messages, null);
            Assert.AreEqual(this.MakeResult(messages[1]), result);

            // Interpolate between last two messages (closer to later message)
            result = interpolator.Match(new DateTime(26), messages, null);
            Assert.AreEqual(this.MakeResult(messages[2]), result);
        }

        [TestMethod]
        [Timeout(60000)]
        public void NearestValue_BoundedRequireNext()
        {
            var interpolator = Match.Best<int>(new RelativeTimeInterval(TimeSpan.FromTicks(-5), TimeSpan.FromTicks(5)));
            var messages = new Message<int>[]
            {
                new Message<int>(1, new DateTime(10), new DateTime(11), 0, 0),
                new Message<int>(2, new DateTime(20), new DateTime(21), 0, 1),
                new Message<int>(3, new DateTime(30), new DateTime(31), 0, 2)
            };

            // Interpolate at point later than last message (outside upper bound)
            var result = interpolator.Match(new DateTime(36), messages, null);
            Assert.AreEqual(MatchResult<int>.InsufficientData(DateTime.MinValue), result);

            // Interpolate at point later than last message (within upper bound)
            result = interpolator.Match(new DateTime(35), messages, null);
            Assert.AreEqual(MatchResult<int>.InsufficientData(DateTime.MinValue), result);

            // Interpolate at point before first message (outside lower bound)
            result = interpolator.Match(new DateTime(4), messages, null);
            Assert.AreEqual(MatchResult<int>.DoesNotExist(new DateTime(4)), result);

            // Interpolate at point before first message (within lower bound)
            result = interpolator.Match(new DateTime(5), messages, null);
            Assert.AreEqual(this.MakeResult(messages[0]), result);

            // Interpolate between first two messages (closer to earlier message)
            result = interpolator.Match(new DateTime(14), messages, null);
            Assert.AreEqual(this.MakeResult(messages[0]), result);

            // Interpolate between first two messages (exactly mid-way)
            result = interpolator.Match(new DateTime(15), messages, null);
            Assert.AreEqual(this.MakeResult(messages[1]), result);

            // Interpolate between last two messages (closer to later message)
            result = interpolator.Match(new DateTime(26), messages, null);
            Assert.AreEqual(this.MakeResult(messages[2]), result);
        }

        [TestMethod]
        [Timeout(60000)]
        public void InterpolatorKinds()
        {
            var tolerance = TimeSpan.FromSeconds(1);
            var window = new RelativeTimeInterval(-tolerance, tolerance);

            this.AssertInterpolatorProperties<int>(Match.Any<int>(), TimeSpan.MinValue, TimeSpan.MaxValue, false, false);
            this.AssertInterpolatorProperties<int>(Match.Any<int>(tolerance), -tolerance, tolerance, false, false);
            this.AssertInterpolatorProperties<int>(Match.Any<int>(window), -tolerance, tolerance, false, false);
            this.AssertInterpolatorProperties<int>(Match.AnyOrDefault<int>(), TimeSpan.MinValue, TimeSpan.MaxValue, false, true);
            this.AssertInterpolatorProperties<int>(Match.AnyOrDefault<int>(tolerance), -tolerance, tolerance, false, true);
            this.AssertInterpolatorProperties<int>(Match.AnyOrDefault<int>(window), -tolerance, tolerance, false, true);
            this.AssertInterpolatorProperties<int>(Match.Best<int>(), TimeSpan.MinValue, TimeSpan.MaxValue, true, false);
            this.AssertInterpolatorProperties<int>(Match.Best<int>(tolerance), -tolerance, tolerance, true, false);
            this.AssertInterpolatorProperties<int>(Match.Best<int>(window), -tolerance, tolerance, true, false);
            this.AssertInterpolatorProperties<int>(Match.BestOrDefault<int>(), TimeSpan.MinValue, TimeSpan.MaxValue, true, true);
            this.AssertInterpolatorProperties<int>(Match.BestOrDefault<int>(tolerance), -tolerance, tolerance, true, true);
            this.AssertInterpolatorProperties<int>(Match.BestOrDefault<int>(window), -tolerance, tolerance, true, true);
            this.AssertInterpolatorProperties<int>(Match.Exact<int>(), TimeSpan.Zero, TimeSpan.Zero, true, false);
            this.AssertInterpolatorProperties<int>(Match.ExactOrDefault<int>(), TimeSpan.Zero, TimeSpan.Zero, true, true);
        }

        [TestMethod]
        [Timeout(60000)]
        public void ImplicitCastsToInterpolator()
        {
            var tolerance = TimeSpan.FromSeconds(1);
            var window = new RelativeTimeInterval(-tolerance, tolerance);

            // note: takes Match.Interpolator, but passing TimeSpan/RelativeTimeInterval below
            Func<Match.Interpolator<int>, Match.Interpolator<int>> testFn = id => id;
            this.AssertInterpolatorProperties<int>(testFn(tolerance /* TimeSpan, converted to Match.Best<int>() */), -tolerance, tolerance, true, false);
            this.AssertInterpolatorProperties<int>(testFn(new RelativeTimeInterval(-tolerance, tolerance) /* RelativeTimeInterval, converted to Match.Best<int>() */), -tolerance, tolerance, true, false);
        }

        private MatchResult<T> MakeResult<T>(Message<T> msg)
        {
            return MatchResult<T>.Create(msg.Data, msg.OriginatingTime);
        }

        private void AssertInterpolatorProperties<T>(Match.Interpolator<T> interpolator, TimeSpan windowLeft, TimeSpan windowRight, bool requireNextValue, bool orDefault)
        {
            Assert.AreEqual(windowLeft, interpolator.Window.Left);
            Assert.AreEqual(windowRight, interpolator.Window.Right);
            Assert.AreEqual(requireNextValue, interpolator.RequireNextValue);
            Assert.AreEqual(orDefault, interpolator.OrDefault);
        }
    }
}

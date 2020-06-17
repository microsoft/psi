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
        public void AvailableNearest()
        {
            var interpolator = Available.Nearest<int>();
            var messages = new Message<int>[]
            {
                new Message<int>(1, new DateTime(10), new DateTime(11), 0, 0),
                new Message<int>(2, new DateTime(20), new DateTime(21), 0, 1),
                new Message<int>(3, new DateTime(30), new DateTime(31), 0, 2),
            };

            // Interpolate at point later than last message
            var result = interpolator.Interpolate(new DateTime(100), messages, null);
            Assert.AreEqual(this.MakeResult(messages[2]), result);

            // Interpolate at point later than last message, but with stream closed
            result = interpolator.Interpolate(new DateTime(100), messages, new DateTime(40));
            Assert.AreEqual(this.MakeResult(messages[2]), result);

            // Interpolate at point before first message
            result = interpolator.Interpolate(new DateTime(9), messages, null);
            Assert.AreEqual(this.MakeResult(messages[0]), result);

            // Interpolate between first two messages (closer to earlier message)
            result = interpolator.Interpolate(new DateTime(14), messages, null);
            Assert.AreEqual(this.MakeResult(messages[0]), result);

            // Interpolate between first two messages (exactly mid-way)
            result = interpolator.Interpolate(new DateTime(15), messages, null);
            Assert.AreEqual(this.MakeResult(messages[1]), result);

            // Interpolate between last two messages (closer to later message)
            result = interpolator.Interpolate(new DateTime(26), messages, null);
            Assert.AreEqual(this.MakeResult(messages[2]), result);

            // Interpolate right on the second message
            result = interpolator.Interpolate(new DateTime(20), messages, null);
            Assert.AreEqual(this.MakeResult(messages[1]), result);

            // Interpolate with no messages available
            var noMessages = new Message<int>[] { };
            result = interpolator.Interpolate(new DateTime(26), noMessages, null);
            Assert.AreEqual(InterpolationResult<int>.DoesNotExist(DateTime.MinValue), result);
        }

        [TestMethod]
        [Timeout(60000)]
        public void AvailableNearest_LeftBounded()
        {
            var interpolator = Available.Nearest<int>(new RelativeTimeInterval(TimeSpan.MinValue, TimeSpan.Zero));
            var messages = new Message<int>[]
            {
                new Message<int>(1, new DateTime(10), new DateTime(11), 0, 0),
                new Message<int>(2, new DateTime(20), new DateTime(21), 0, 1),
                new Message<int>(3, new DateTime(30), new DateTime(31), 0, 2),
            };

            // Interpolate at point later than last message
            var result = interpolator.Interpolate(new DateTime(100), messages, null);
            Assert.AreEqual(this.MakeResult(messages[2]), result);

            // Interpolate at point later than last message, but with stream closed
            result = interpolator.Interpolate(new DateTime(100), messages, new DateTime(40));
            Assert.AreEqual(this.MakeResult(messages[2]), result);

            // Interpolate at point before first message
            result = interpolator.Interpolate(new DateTime(9), messages, null);
            Assert.AreEqual(InterpolationResult<int>.DoesNotExist(new DateTime(9)), result);

            // Interpolate between first two messages (closer to earlier message)
            result = interpolator.Interpolate(new DateTime(14), messages, null);
            Assert.AreEqual(this.MakeResult(messages[0]), result);

            // Interpolate between first two messages (exactly mid-way)
            result = interpolator.Interpolate(new DateTime(15), messages, null);
            Assert.AreEqual(this.MakeResult(messages[0]), result);

            // Interpolate between last two messages (closer to later message)
            result = interpolator.Interpolate(new DateTime(26), messages, null);
            Assert.AreEqual(this.MakeResult(messages[1]), result);

            // Interpolate right on the second message
            result = interpolator.Interpolate(new DateTime(20), messages, null);
            Assert.AreEqual(this.MakeResult(messages[1]), result);

            // Interpolate with no messages available
            var noMessages = new Message<int>[] { };
            result = interpolator.Interpolate(new DateTime(26), noMessages, null);
            Assert.AreEqual(InterpolationResult<int>.DoesNotExist(DateTime.MinValue), result);
        }

        [TestMethod]
        [Timeout(60000)]
        public void AvailableNearest_RightBounded()
        {
            var interpolator = Available.Nearest<int>(new RelativeTimeInterval(TimeSpan.Zero, TimeSpan.MaxValue));
            var messages = new Message<int>[]
            {
                new Message<int>(1, new DateTime(10), new DateTime(11), 0, 0),
                new Message<int>(2, new DateTime(20), new DateTime(21), 0, 1),
                new Message<int>(3, new DateTime(30), new DateTime(31), 0, 2),
            };

            // Interpolate at point later than last message
            var result = interpolator.Interpolate(new DateTime(100), messages, null);
            Assert.AreEqual(InterpolationResult<int>.DoesNotExist(new DateTime(100)), result);

            // Interpolate at point later than last message, but with stream closed
            result = interpolator.Interpolate(new DateTime(100), messages, new DateTime(40));
            Assert.AreEqual(InterpolationResult<int>.DoesNotExist(new DateTime(100)), result);

            // Interpolate at point before first message
            result = interpolator.Interpolate(new DateTime(9), messages, null);
            Assert.AreEqual(this.MakeResult(messages[0]), result);

            // Interpolate between first two messages (closer to earlier message)
            result = interpolator.Interpolate(new DateTime(14), messages, null);
            Assert.AreEqual(this.MakeResult(messages[1]), result);

            // Interpolate between first two messages (exactly mid-way)
            result = interpolator.Interpolate(new DateTime(15), messages, null);
            Assert.AreEqual(this.MakeResult(messages[1]), result);

            // Interpolate between last two messages (closer to later message)
            result = interpolator.Interpolate(new DateTime(26), messages, null);
            Assert.AreEqual(this.MakeResult(messages[2]), result);

            // Interpolate right on the second message
            result = interpolator.Interpolate(new DateTime(20), messages, null);
            Assert.AreEqual(this.MakeResult(messages[1]), result);

            // Interpolate with no messages available
            var noMessages = new Message<int>[] { };
            result = interpolator.Interpolate(new DateTime(26), noMessages, null);
            Assert.AreEqual(InterpolationResult<int>.DoesNotExist(DateTime.MinValue), result);
        }

        [TestMethod]
        [Timeout(60000)]
        public void AvailableNearest_Bounded()
        {
            var interpolator = Available.Nearest<int>(new RelativeTimeInterval(TimeSpan.FromTicks(-5), TimeSpan.FromTicks(5)));
            var messages = new Message<int>[]
            {
                new Message<int>(1, new DateTime(10), new DateTime(11), 0, 0),
                new Message<int>(2, new DateTime(20), new DateTime(21), 0, 1),
                new Message<int>(3, new DateTime(30), new DateTime(31), 0, 2),
            };

            // Interpolate at point later than last message (outside upper bound)
            var result = interpolator.Interpolate(new DateTime(36), messages, null);
            Assert.AreEqual(InterpolationResult<int>.DoesNotExist(new DateTime(36)), result);

            // Interpolate at point later than last message (outside upper bound), but with stream closing
            result = interpolator.Interpolate(new DateTime(36), messages, new DateTime(40));
            Assert.AreEqual(InterpolationResult<int>.DoesNotExist(new DateTime(36)), result);

            // Interpolate at point later than last message (within upper bound)
            result = interpolator.Interpolate(new DateTime(35), messages, null);
            Assert.AreEqual(this.MakeResult(messages[2]), result);

            // Interpolate at point before first message (outside lower bound)
            result = interpolator.Interpolate(new DateTime(4), messages, null);
            Assert.AreEqual(InterpolationResult<int>.DoesNotExist(new DateTime(4)), result);

            // Interpolate at point before first message (within lower bound)
            result = interpolator.Interpolate(new DateTime(5), messages, null);
            Assert.AreEqual(this.MakeResult(messages[0]), result);

            // Interpolate between first two messages (closer to earlier message)
            result = interpolator.Interpolate(new DateTime(14), messages, null);
            Assert.AreEqual(this.MakeResult(messages[0]), result);

            // Interpolate between first two messages (exactly mid-way)
            result = interpolator.Interpolate(new DateTime(15), messages, null);
            Assert.AreEqual(this.MakeResult(messages[1]), result);

            // Interpolate between last two messages (closer to later message)
            result = interpolator.Interpolate(new DateTime(26), messages, null);
            Assert.AreEqual(this.MakeResult(messages[2]), result);

            // Interpolate right on the second message
            result = interpolator.Interpolate(new DateTime(20), messages, null);
            Assert.AreEqual(this.MakeResult(messages[1]), result);

            // Interpolate with no messages available
            var noMessages = new Message<int>[] { };
            result = interpolator.Interpolate(new DateTime(26), noMessages, null);
            Assert.AreEqual(InterpolationResult<int>.DoesNotExist(DateTime.MinValue), result);
        }

        [TestMethod]
        [Timeout(60000)]
        public void AvailableNearest_OpenIntervalTest()
        {
            var interpolatorRightOpen = Available.Nearest<int>(new RelativeTimeInterval(TimeSpan.FromTicks(-6), true, TimeSpan.FromTicks(4), false));
            var interpolatorLeftOpen = Available.Nearest<int>(new RelativeTimeInterval(TimeSpan.FromTicks(-4), false, TimeSpan.FromTicks(6), true));
            var messages = new Message<int>[]
            {
                new Message<int>(1, new DateTime(10), new DateTime(11), 0, 0),
                new Message<int>(2, new DateTime(20), new DateTime(21), 0, 1),
                new Message<int>(3, new DateTime(30), new DateTime(31), 0, 2),
            };

            // Interpolate should not match the right end of the interval
            var result = interpolatorRightOpen.Interpolate(new DateTime(26), messages, null);
            Assert.AreEqual(this.MakeResult(messages[1]), result);

            // Interpolate should not match the right end of the interval
            result = interpolatorRightOpen.Interpolate(new DateTime(26), messages, new DateTime(40));
            Assert.AreEqual(this.MakeResult(messages[1]), result);

            // Interpolate should not match the left end of the interval
            result = interpolatorLeftOpen.Interpolate(new DateTime(24), messages, null);
            Assert.AreEqual(this.MakeResult(messages[2]), result);

            // Interpolate should not match the left end of the interval
            result = interpolatorLeftOpen.Interpolate(new DateTime(24), messages, new DateTime(40));
            Assert.AreEqual(this.MakeResult(messages[2]), result);
        }

        [TestMethod]
        [Timeout(60000)]
        public void AvailableFirstLeftUnboundedShouldThrow()
        {
            var exceptionThrown = false;
            try
            {
                var interpolator = Available.First<int>(RelativeTimeInterval.Infinite);
            }
            catch (NotSupportedException)
            {
                exceptionThrown = true;
            }

            Assert.IsTrue(exceptionThrown);

            exceptionThrown = false;
            try
            {
                var interpolator = Available.First<int>(RelativeTimeInterval.Past());
            }
            catch (NotSupportedException)
            {
                exceptionThrown = true;
            }

            Assert.IsTrue(exceptionThrown);
        }

        [TestMethod]
        [Timeout(60000)]
        public void AvailableFirst_LeftBounded()
        {
            var interpolator = Available.First<int>(new RelativeTimeInterval(TimeSpan.MinValue, TimeSpan.Zero));
            var messages = new Message<int>[]
            {
                new Message<int>(1, new DateTime(10), new DateTime(11), 0, 0),
                new Message<int>(2, new DateTime(20), new DateTime(21), 0, 1),
                new Message<int>(3, new DateTime(30), new DateTime(31), 0, 2),
            };

            // Interpolate at point later than last message
            var result = interpolator.Interpolate(new DateTime(100), messages, null);
            Assert.AreEqual(this.MakeResult(messages[0]), result);

            // Interpolate at point later than last message, but with stream closed
            result = interpolator.Interpolate(new DateTime(100), messages, new DateTime(40));
            Assert.AreEqual(this.MakeResult(messages[0]), result);

            // Interpolate at point before first message
            result = interpolator.Interpolate(new DateTime(9), messages, null);
            Assert.AreEqual(InterpolationResult<int>.DoesNotExist(DateTime.MinValue), result);

            // Interpolate between first two messages (closer to earlier message)
            result = interpolator.Interpolate(new DateTime(14), messages, null);
            Assert.AreEqual(this.MakeResult(messages[0]), result);

            // Interpolate between first two messages (exactly mid-way)
            result = interpolator.Interpolate(new DateTime(15), messages, null);
            Assert.AreEqual(this.MakeResult(messages[0]), result);

            // Interpolate between last two messages (closer to later message)
            result = interpolator.Interpolate(new DateTime(26), messages, null);
            Assert.AreEqual(this.MakeResult(messages[0]), result);

            // Interpolate right on the second message
            result = interpolator.Interpolate(new DateTime(20), messages, null);
            Assert.AreEqual(this.MakeResult(messages[0]), result);

            // Interpolate with no messages available
            var noMessages = new Message<int>[] { };
            result = interpolator.Interpolate(new DateTime(26), noMessages, null);
            Assert.AreEqual(InterpolationResult<int>.DoesNotExist(DateTime.MinValue), result);
        }

        [TestMethod]
        [Timeout(60000)]
        public void AvailableFirst_RightBounded()
        {
            var interpolator = Available.First<int>(new RelativeTimeInterval(TimeSpan.Zero, TimeSpan.MaxValue));
            var messages = new Message<int>[]
            {
                new Message<int>(1, new DateTime(10), new DateTime(11), 0, 0),
                new Message<int>(2, new DateTime(20), new DateTime(21), 0, 1),
                new Message<int>(3, new DateTime(30), new DateTime(31), 0, 2),
            };

            // Interpolate at point later than last message
            var result = interpolator.Interpolate(new DateTime(100), messages, null);
            Assert.AreEqual(InterpolationResult<int>.DoesNotExist(new DateTime(100)), result);

            // Interpolate at point later than last message, but with stream closed
            result = interpolator.Interpolate(new DateTime(100), messages, new DateTime(40));
            Assert.AreEqual(InterpolationResult<int>.DoesNotExist(new DateTime(100)), result);

            // Interpolate at point before first message
            result = interpolator.Interpolate(new DateTime(9), messages, null);
            Assert.AreEqual(this.MakeResult(messages[0]), result);

            // Interpolate between first two messages (closer to earlier message)
            result = interpolator.Interpolate(new DateTime(14), messages, null);
            Assert.AreEqual(this.MakeResult(messages[1]), result);

            // Interpolate between first two messages (exactly mid-way)
            result = interpolator.Interpolate(new DateTime(15), messages, null);
            Assert.AreEqual(this.MakeResult(messages[1]), result);

            // Interpolate between last two messages (closer to later message)
            result = interpolator.Interpolate(new DateTime(26), messages, null);
            Assert.AreEqual(this.MakeResult(messages[2]), result);

            // Interpolate right on the second message
            result = interpolator.Interpolate(new DateTime(20), messages, null);
            Assert.AreEqual(this.MakeResult(messages[1]), result);

            // Interpolate with no messages available
            var noMessages = new Message<int>[] { };
            result = interpolator.Interpolate(new DateTime(26), noMessages, null);
            Assert.AreEqual(InterpolationResult<int>.DoesNotExist(DateTime.MinValue), result);
        }

        [TestMethod]
        [Timeout(60000)]
        public void AvailableFirst_Bounded()
        {
            var interpolator = Available.First<int>(new RelativeTimeInterval(TimeSpan.FromTicks(-5), TimeSpan.FromTicks(5)));
            var messages = new Message<int>[]
            {
                new Message<int>(1, new DateTime(10), new DateTime(11), 0, 0),
                new Message<int>(2, new DateTime(20), new DateTime(21), 0, 1),
                new Message<int>(3, new DateTime(30), new DateTime(31), 0, 2),
            };

            // Interpolate at point later than last message (outside upper bound)
            var result = interpolator.Interpolate(new DateTime(36), messages, null);
            Assert.AreEqual(InterpolationResult<int>.DoesNotExist(new DateTime(31)), result);

            // Interpolate at point later than last message (outside upper bound), but with stream closing
            result = interpolator.Interpolate(new DateTime(36), messages, new DateTime(40));
            Assert.AreEqual(InterpolationResult<int>.DoesNotExist(new DateTime(31)), result);

            // Interpolate at point later than last message (within upper bound)
            result = interpolator.Interpolate(new DateTime(35), messages, null);
            Assert.AreEqual(this.MakeResult(messages[2]), result);

            // Interpolate at point before first message (outside lower bound)
            result = interpolator.Interpolate(new DateTime(4), messages, null);
            Assert.AreEqual(InterpolationResult<int>.DoesNotExist(DateTime.MinValue), result);

            // Interpolate at point before first message (within lower bound)
            result = interpolator.Interpolate(new DateTime(5), messages, null);
            Assert.AreEqual(this.MakeResult(messages[0]), result);

            // Interpolate between first two messages (closer to earlier message)
            result = interpolator.Interpolate(new DateTime(14), messages, null);
            Assert.AreEqual(this.MakeResult(messages[0]), result);

            // Interpolate between first two messages (exactly mid-way)
            result = interpolator.Interpolate(new DateTime(15), messages, null);
            Assert.AreEqual(this.MakeResult(messages[0]), result);

            // Interpolate between last two messages (closer to later message)
            result = interpolator.Interpolate(new DateTime(26), messages, null);
            Assert.AreEqual(this.MakeResult(messages[2]), result);

            // Interpolate right on the second message
            result = interpolator.Interpolate(new DateTime(20), messages, null);
            Assert.AreEqual(this.MakeResult(messages[1]), result);

            // Interpolate with no messages available
            var noMessages = new Message<int>[] { };
            result = interpolator.Interpolate(new DateTime(26), noMessages, null);
            Assert.AreEqual(InterpolationResult<int>.DoesNotExist(DateTime.MinValue), result);
        }

        [TestMethod]
        [Timeout(60000)]
        public void AvailableFirst_OpenIntervalTest()
        {
            var interpolatorRightOpen = Available.First<int>(new RelativeTimeInterval(TimeSpan.FromTicks(-5), true, TimeSpan.FromTicks(4), false));
            var interpolatorLeftOpen = Available.First<int>(new RelativeTimeInterval(TimeSpan.FromTicks(-4), false, TimeSpan.FromTicks(6), true));
            var messages = new Message<int>[]
            {
                new Message<int>(1, new DateTime(10), new DateTime(11), 0, 0),
                new Message<int>(2, new DateTime(20), new DateTime(21), 0, 1),
                new Message<int>(3, new DateTime(30), new DateTime(31), 0, 2),
            };

            // Interpolate should not match the right end of the interval because it's open.
            var result = interpolatorRightOpen.Interpolate(new DateTime(26), messages, null);
            Assert.AreEqual(InterpolationResult<int>.DoesNotExist(new DateTime(21)), result);

            // Interpolate should not match the right end of the interval because it's open.
            result = interpolatorRightOpen.Interpolate(new DateTime(26), messages, new DateTime(40));
            Assert.AreEqual(InterpolationResult<int>.DoesNotExist(new DateTime(21)), result);

            // Interpolate should not match the left end of the interval because it's open.
            result = interpolatorLeftOpen.Interpolate(new DateTime(24), messages, null);
            Assert.AreEqual(this.MakeResult(messages[2]), result);

            // Interpolate should not match the left end of the interval because it's open.
            result = interpolatorLeftOpen.Interpolate(new DateTime(24), messages, new DateTime(40));
            Assert.AreEqual(this.MakeResult(messages[2]), result);
        }

        [TestMethod]
        [Timeout(60000)]
        public void AvailableLast()
        {
            var interpolator = Available.Last<int>(RelativeTimeInterval.Infinite);
            var messages = new Message<int>[]
            {
                new Message<int>(1, new DateTime(10), new DateTime(11), 0, 0),
                new Message<int>(2, new DateTime(20), new DateTime(21), 0, 1),
                new Message<int>(3, new DateTime(30), new DateTime(31), 0, 2),
            };

            // Interpolate at point later than last message
            var result = interpolator.Interpolate(new DateTime(100), messages, null);
            Assert.AreEqual(this.MakeResult(messages[2]), result);

            // Interpolate at point later than last message, but with stream closed
            result = interpolator.Interpolate(new DateTime(100), messages, new DateTime(40));
            Assert.AreEqual(this.MakeResult(messages[2]), result);

            // Interpolate at point before first message
            result = interpolator.Interpolate(new DateTime(9), messages, null);
            Assert.AreEqual(this.MakeResult(messages[2]), result);

            // Interpolate between first two messages (closer to earlier message)
            result = interpolator.Interpolate(new DateTime(14), messages, null);
            Assert.AreEqual(this.MakeResult(messages[2]), result);

            // Interpolate between first two messages (exactly mid-way)
            result = interpolator.Interpolate(new DateTime(15), messages, null);
            Assert.AreEqual(this.MakeResult(messages[2]), result);

            // Interpolate between last two messages (closer to later message)
            result = interpolator.Interpolate(new DateTime(26), messages, null);
            Assert.AreEqual(this.MakeResult(messages[2]), result);

            // Interpolate right on the second message
            result = interpolator.Interpolate(new DateTime(20), messages, null);
            Assert.AreEqual(this.MakeResult(messages[2]), result);

            // Interpolate with no messages available
            var noMessages = new Message<int>[] { };
            result = interpolator.Interpolate(new DateTime(26), noMessages, null);
            Assert.AreEqual(InterpolationResult<int>.DoesNotExist(DateTime.MinValue), result);
        }

        [TestMethod]
        [Timeout(60000)]
        public void AvailableLast_LeftBounded()
        {
            var interpolator = Available.Last<int>(new RelativeTimeInterval(TimeSpan.MinValue, TimeSpan.Zero));
            var messages = new Message<int>[]
            {
                new Message<int>(1, new DateTime(10), new DateTime(11), 0, 0),
                new Message<int>(2, new DateTime(20), new DateTime(21), 0, 1),
                new Message<int>(3, new DateTime(30), new DateTime(31), 0, 2),
            };

            // Interpolate at point later than last message
            var result = interpolator.Interpolate(new DateTime(100), messages, null);
            Assert.AreEqual(this.MakeResult(messages[2]), result);

            // Interpolate at point later than last message, but with stream closed
            result = interpolator.Interpolate(new DateTime(100), messages, new DateTime(40));
            Assert.AreEqual(this.MakeResult(messages[2]), result);

            // Interpolate at point before first message
            result = interpolator.Interpolate(new DateTime(9), messages, null);
            Assert.AreEqual(InterpolationResult<int>.DoesNotExist(DateTime.MinValue), result);

            // Interpolate between first two messages (closer to earlier message)
            result = interpolator.Interpolate(new DateTime(14), messages, null);
            Assert.AreEqual(this.MakeResult(messages[0]), result);

            // Interpolate between first two messages (exactly mid-way)
            result = interpolator.Interpolate(new DateTime(15), messages, null);
            Assert.AreEqual(this.MakeResult(messages[0]), result);

            // Interpolate between last two messages (closer to later message)
            result = interpolator.Interpolate(new DateTime(26), messages, null);
            Assert.AreEqual(this.MakeResult(messages[1]), result);

            // Interpolate right on the second message
            result = interpolator.Interpolate(new DateTime(20), messages, null);
            Assert.AreEqual(this.MakeResult(messages[1]), result);

            // Interpolate with no messages available
            var noMessages = new Message<int>[] { };
            result = interpolator.Interpolate(new DateTime(26), noMessages, null);
            Assert.AreEqual(InterpolationResult<int>.DoesNotExist(DateTime.MinValue), result);
        }

        [TestMethod]
        [Timeout(60000)]
        public void AvailableLast_RightBounded()
        {
            var interpolator = Available.Last<int>(new RelativeTimeInterval(TimeSpan.Zero, TimeSpan.MaxValue));
            var messages = new Message<int>[]
            {
                new Message<int>(1, new DateTime(10), new DateTime(11), 0, 0),
                new Message<int>(2, new DateTime(20), new DateTime(21), 0, 1),
                new Message<int>(3, new DateTime(30), new DateTime(31), 0, 2),
            };

            // Interpolate at point later than last message
            var result = interpolator.Interpolate(new DateTime(100), messages, null);
            Assert.AreEqual(InterpolationResult<int>.DoesNotExist(new DateTime(100)), result);

            // Interpolate at point later than last message, but with stream closed
            result = interpolator.Interpolate(new DateTime(100), messages, new DateTime(40));
            Assert.AreEqual(InterpolationResult<int>.DoesNotExist(new DateTime(100)), result);

            // Interpolate at point before first message
            result = interpolator.Interpolate(new DateTime(9), messages, null);
            Assert.AreEqual(this.MakeResult(messages[2]), result);

            // Interpolate between first two messages (closer to earlier message)
            result = interpolator.Interpolate(new DateTime(14), messages, null);
            Assert.AreEqual(this.MakeResult(messages[2]), result);

            // Interpolate between first two messages (exactly mid-way)
            result = interpolator.Interpolate(new DateTime(15), messages, null);
            Assert.AreEqual(this.MakeResult(messages[2]), result);

            // Interpolate between last two messages (closer to later message)
            result = interpolator.Interpolate(new DateTime(26), messages, null);
            Assert.AreEqual(this.MakeResult(messages[2]), result);

            // Interpolate right on the second message
            result = interpolator.Interpolate(new DateTime(20), messages, null);
            Assert.AreEqual(this.MakeResult(messages[2]), result);

            // Interpolate with no messages available
            var noMessages = new Message<int>[] { };
            result = interpolator.Interpolate(new DateTime(26), noMessages, null);
            Assert.AreEqual(InterpolationResult<int>.DoesNotExist(DateTime.MinValue), result);
        }

        [TestMethod]
        [Timeout(60000)]
        public void AvailableLast_Bounded()
        {
            var interpolator = Available.Last<int>(new RelativeTimeInterval(TimeSpan.FromTicks(-5), TimeSpan.FromTicks(5)));
            var messages = new Message<int>[]
            {
                new Message<int>(1, new DateTime(10), new DateTime(11), 0, 0),
                new Message<int>(2, new DateTime(20), new DateTime(21), 0, 1),
                new Message<int>(3, new DateTime(30), new DateTime(31), 0, 2),
            };

            // Interpolate at point later than last message (outside upper bound)
            var result = interpolator.Interpolate(new DateTime(36), messages, null);
            Assert.AreEqual(InterpolationResult<int>.DoesNotExist(new DateTime(31)), result);

            // Interpolate at point later than last message (outside upper bound), but with stream closing
            result = interpolator.Interpolate(new DateTime(36), messages, new DateTime(40));
            Assert.AreEqual(InterpolationResult<int>.DoesNotExist(new DateTime(31)), result);

            // Interpolate at point later than last message (within upper bound)
            result = interpolator.Interpolate(new DateTime(35), messages, null);
            Assert.AreEqual(this.MakeResult(messages[2]), result);

            // Interpolate at point before first message (outside lower bound)
            result = interpolator.Interpolate(new DateTime(4), messages, null);
            Assert.AreEqual(InterpolationResult<int>.DoesNotExist(DateTime.MinValue), result);

            // Interpolate at point before first message (within lower bound)
            result = interpolator.Interpolate(new DateTime(5), messages, null);
            Assert.AreEqual(this.MakeResult(messages[0]), result);

            // Interpolate between first two messages (closer to earlier message)
            result = interpolator.Interpolate(new DateTime(14), messages, null);
            Assert.AreEqual(this.MakeResult(messages[0]), result);

            // Interpolate between first two messages (exactly mid-way)
            result = interpolator.Interpolate(new DateTime(15), messages, null);
            Assert.AreEqual(this.MakeResult(messages[1]), result);

            // Interpolate between last two messages (closer to later message)
            result = interpolator.Interpolate(new DateTime(26), messages, null);
            Assert.AreEqual(this.MakeResult(messages[2]), result);

            // Interpolate between last two messages (closer to later message), but with stream closing
            result = interpolator.Interpolate(new DateTime(26), messages, new DateTime(40));
            Assert.AreEqual(this.MakeResult(messages[2]), result);

            // Interpolate right on the second message
            result = interpolator.Interpolate(new DateTime(20), messages, null);
            Assert.AreEqual(this.MakeResult(messages[1]), result);

            // Interpolate with no messages available
            var noMessages = new Message<int>[] { };
            result = interpolator.Interpolate(new DateTime(26), noMessages, null);
            Assert.AreEqual(InterpolationResult<int>.DoesNotExist(DateTime.MinValue), result);
        }

        [TestMethod]
        [Timeout(60000)]
        public void AvailableLast_OpenIntervalTest()
        {
            var interpolatorRightOpen = Available.Last<int>(new RelativeTimeInterval(TimeSpan.FromTicks(-5), true, TimeSpan.FromTicks(4), false));
            var interpolatorLeftOpen = Available.Last<int>(new RelativeTimeInterval(TimeSpan.FromTicks(-4), false, TimeSpan.FromTicks(5), true));
            var messages = new Message<int>[]
            {
                new Message<int>(1, new DateTime(10), new DateTime(11), 0, 0),
                new Message<int>(2, new DateTime(20), new DateTime(21), 0, 1),
                new Message<int>(3, new DateTime(30), new DateTime(31), 0, 2),
            };

            // Interpolate should return does not exist b/c message at 30 falls after the open window
            var result = interpolatorRightOpen.Interpolate(new DateTime(26), messages, null);
            Assert.AreEqual(InterpolationResult<int>.DoesNotExist(new DateTime(21)), result);

            // Interpolate should not match the right end of the interval because it's open.
            result = interpolatorRightOpen.Interpolate(new DateTime(26), messages, new DateTime(40));
            Assert.AreEqual(InterpolationResult<int>.DoesNotExist(new DateTime(21)), result);

            // Interpolate should return does not exist b/c message at 30 falls after the open window
            result = interpolatorLeftOpen.Interpolate(new DateTime(24), messages, null);
            Assert.AreEqual(InterpolationResult<int>.DoesNotExist(new DateTime(20)), result);

            // Interpolate should not match the left end of the interval because it's open.
            result = interpolatorLeftOpen.Interpolate(new DateTime(24), messages, new DateTime(40));
            Assert.AreEqual(InterpolationResult<int>.DoesNotExist(new DateTime(20)), result);
        }

        [TestMethod]
        [Timeout(60000)]
        public void ReproducibleNearest()
        {
            var interpolator = Reproducible.Nearest<int>(RelativeTimeInterval.Infinite);
            var messages = new Message<int>[]
            {
                new Message<int>(1, new DateTime(10), new DateTime(11), 0, 0),
                new Message<int>(2, new DateTime(20), new DateTime(21), 0, 1),
                new Message<int>(3, new DateTime(30), new DateTime(31), 0, 2),
            };

            // Interpolate at point later than last message
            var result = interpolator.Interpolate(new DateTime(100), messages, null);
            Assert.AreEqual(InterpolationResult<int>.InsufficientData(), result);

            // Interpolate at point later than last message, but with stream closed
            result = interpolator.Interpolate(new DateTime(100), messages, new DateTime(40));
            Assert.AreEqual(this.MakeResult(messages[2]), result);

            // Interpolate at point before first message
            result = interpolator.Interpolate(new DateTime(9), messages, null);
            Assert.AreEqual(this.MakeResult(messages[0]), result);

            // Interpolate between first two messages (closer to earlier message)
            result = interpolator.Interpolate(new DateTime(14), messages, null);
            Assert.AreEqual(this.MakeResult(messages[0]), result);

            // Interpolate between first two messages (exactly mid-way)
            result = interpolator.Interpolate(new DateTime(15), messages, null);
            Assert.AreEqual(this.MakeResult(messages[1]), result);

            // Interpolate between last two messages (closer to later message)
            result = interpolator.Interpolate(new DateTime(26), messages, null);
            Assert.AreEqual(this.MakeResult(messages[2]), result);

            // Interpolate right on the second message
            result = interpolator.Interpolate(new DateTime(20), messages, null);
            Assert.AreEqual(this.MakeResult(messages[1]), result);

            // Interpolate with no messages available
            var noMessages = new Message<int>[] { };
            result = interpolator.Interpolate(new DateTime(26), noMessages, null);
            Assert.AreEqual(InterpolationResult<int>.InsufficientData(), result);
        }

        [TestMethod]
        [Timeout(60000)]
        public void ReproducibleNearest_LeftBounded()
        {
            var interpolator = Reproducible.Nearest<int>(new RelativeTimeInterval(TimeSpan.MinValue, TimeSpan.Zero));
            var messages = new Message<int>[]
            {
                new Message<int>(1, new DateTime(10), new DateTime(11), 0, 0),
                new Message<int>(2, new DateTime(20), new DateTime(21), 0, 1),
                new Message<int>(3, new DateTime(30), new DateTime(31), 0, 2),
            };

            // Interpolate at point later than last message
            var result = interpolator.Interpolate(new DateTime(100), messages, null);
            Assert.AreEqual(InterpolationResult<int>.InsufficientData(), result);

            // Interpolate at point later than last message, but with stream closed
            result = interpolator.Interpolate(new DateTime(100), messages, new DateTime(40));
            Assert.AreEqual(this.MakeResult(messages[2]), result);

            // Interpolate at point before first message
            result = interpolator.Interpolate(new DateTime(9), messages, null);
            Assert.AreEqual(InterpolationResult<int>.DoesNotExist(new DateTime(9)), result);

            // Interpolate between first two messages (closer to earlier message)
            result = interpolator.Interpolate(new DateTime(14), messages, null);
            Assert.AreEqual(this.MakeResult(messages[0]), result);

            // Interpolate between first two messages (exactly mid-way)
            result = interpolator.Interpolate(new DateTime(15), messages, null);
            Assert.AreEqual(this.MakeResult(messages[0]), result);

            // Interpolate between last two messages (closer to later message)
            result = interpolator.Interpolate(new DateTime(26), messages, null);
            Assert.AreEqual(this.MakeResult(messages[1]), result);

            // Interpolate right on the second message
            result = interpolator.Interpolate(new DateTime(20), messages, null);
            Assert.AreEqual(this.MakeResult(messages[1]), result);

            // Interpolate with no messages available
            var noMessages = new Message<int>[] { };
            result = interpolator.Interpolate(new DateTime(26), noMessages, null);
            Assert.AreEqual(InterpolationResult<int>.InsufficientData(), result);
        }

        [TestMethod]
        [Timeout(60000)]
        public void ReproducibleNearest_RightBounded()
        {
            var interpolator = Reproducible.Nearest<int>(new RelativeTimeInterval(TimeSpan.Zero, TimeSpan.MaxValue));
            var messages = new Message<int>[]
            {
                new Message<int>(1, new DateTime(10), new DateTime(11), 0, 0),
                new Message<int>(2, new DateTime(20), new DateTime(21), 0, 1),
                new Message<int>(3, new DateTime(30), new DateTime(31), 0, 2),
            };

            // Interpolate at point later than last message
            var result = interpolator.Interpolate(new DateTime(100), messages, null);
            Assert.AreEqual(InterpolationResult<int>.InsufficientData(), result);

            // Interpolate at point later than last message, but with stream closed
            result = interpolator.Interpolate(new DateTime(100), messages, new DateTime(40));
            Assert.AreEqual(InterpolationResult<int>.DoesNotExist(new DateTime(100)), result);

            // Interpolate at point before first message
            result = interpolator.Interpolate(new DateTime(9), messages, null);
            Assert.AreEqual(this.MakeResult(messages[0]), result);

            // Interpolate between first two messages (closer to earlier message)
            result = interpolator.Interpolate(new DateTime(14), messages, null);
            Assert.AreEqual(this.MakeResult(messages[1]), result);

            // Interpolate between first two messages (exactly mid-way)
            result = interpolator.Interpolate(new DateTime(15), messages, null);
            Assert.AreEqual(this.MakeResult(messages[1]), result);

            // Interpolate between last two messages (closer to later message)
            result = interpolator.Interpolate(new DateTime(26), messages, null);
            Assert.AreEqual(this.MakeResult(messages[2]), result);

            // Interpolate right on the second message
            result = interpolator.Interpolate(new DateTime(20), messages, null);
            Assert.AreEqual(this.MakeResult(messages[1]), result);

            // Interpolate with no messages available
            var noMessages = new Message<int>[] { };
            result = interpolator.Interpolate(new DateTime(26), noMessages, null);
            Assert.AreEqual(InterpolationResult<int>.InsufficientData(), result);
        }

        [TestMethod]
        [Timeout(60000)]
        public void ReproducibleNearest_Bounded()
        {
            var interpolator = Reproducible.Nearest<int>(new RelativeTimeInterval(TimeSpan.FromTicks(-5), TimeSpan.FromTicks(5)));
            var messages = new Message<int>[]
            {
                new Message<int>(1, new DateTime(10), new DateTime(11), 0, 0),
                new Message<int>(2, new DateTime(20), new DateTime(21), 0, 1),
                new Message<int>(3, new DateTime(30), new DateTime(31), 0, 2),
            };

            // Interpolate at point later than last message (outside upper bound)
            var result = interpolator.Interpolate(new DateTime(36), messages, null);
            Assert.AreEqual(InterpolationResult<int>.InsufficientData(), result);

            // Interpolate at point later than last message (outside upper bound), but with stream closing
            result = interpolator.Interpolate(new DateTime(36), messages, new DateTime(40));
            Assert.AreEqual(InterpolationResult<int>.DoesNotExist(new DateTime(36)), result);

            // Interpolate at point later than last message (within upper bound)
            result = interpolator.Interpolate(new DateTime(35), messages, null);
            Assert.AreEqual(InterpolationResult<int>.InsufficientData(), result);

            // Interpolate at point before first message (outside lower bound)
            result = interpolator.Interpolate(new DateTime(4), messages, null);
            Assert.AreEqual(InterpolationResult<int>.DoesNotExist(new DateTime(4)), result);

            // Interpolate at point before first message (within lower bound)
            result = interpolator.Interpolate(new DateTime(5), messages, null);
            Assert.AreEqual(this.MakeResult(messages[0]), result);

            // Interpolate between first two messages (closer to earlier message)
            result = interpolator.Interpolate(new DateTime(14), messages, null);
            Assert.AreEqual(this.MakeResult(messages[0]), result);

            // Interpolate between first two messages (exactly mid-way)
            result = interpolator.Interpolate(new DateTime(15), messages, null);
            Assert.AreEqual(this.MakeResult(messages[1]), result);

            // Interpolate between last two messages (closer to later message)
            result = interpolator.Interpolate(new DateTime(26), messages, null);
            Assert.AreEqual(this.MakeResult(messages[2]), result);

            // Interpolate right on the second message
            result = interpolator.Interpolate(new DateTime(20), messages, null);
            Assert.AreEqual(this.MakeResult(messages[1]), result);

            // Interpolate with no messages available
            var noMessages = new Message<int>[] { };
            result = interpolator.Interpolate(new DateTime(26), noMessages, null);
            Assert.AreEqual(InterpolationResult<int>.InsufficientData(), result);
        }

        [TestMethod]
        [Timeout(60000)]
        public void ReproducibleNearest_OpenIntervalTest()
        {
            var interpolatorRightOpen = Reproducible.Nearest<int>(new RelativeTimeInterval(TimeSpan.FromTicks(-6), true, TimeSpan.FromTicks(4), false));
            var interpolatorLeftOpen = Reproducible.Nearest<int>(new RelativeTimeInterval(TimeSpan.FromTicks(-4), false, TimeSpan.FromTicks(6), true));
            var messages = new Message<int>[]
            {
                new Message<int>(1, new DateTime(10), new DateTime(11), 0, 0),
                new Message<int>(2, new DateTime(20), new DateTime(21), 0, 1),
                new Message<int>(3, new DateTime(30), new DateTime(31), 0, 2),
            };

            // Interpolate should not match the right end of the interval
            var result = interpolatorRightOpen.Interpolate(new DateTime(26), messages, null);
            Assert.AreEqual(this.MakeResult(messages[1]), result);

            // Interpolate should not match the right end of the interval
            result = interpolatorRightOpen.Interpolate(new DateTime(26), messages, new DateTime(40));
            Assert.AreEqual(this.MakeResult(messages[1]), result);

            // Interpolate should not match the left end of the interval
            result = interpolatorLeftOpen.Interpolate(new DateTime(24), messages, null);
            Assert.AreEqual(this.MakeResult(messages[2]), result);

            // Interpolate should not match the left end of the interval
            result = interpolatorLeftOpen.Interpolate(new DateTime(24), messages, new DateTime(40));
            Assert.AreEqual(this.MakeResult(messages[2]), result);
        }

        [TestMethod]
        [Timeout(60000)]
        public void ReproducibleFirstLeftUnboundedShouldThrow()
        {
            var exceptionThrown = false;
            try
            {
                var interpolator = Reproducible.First<int>(RelativeTimeInterval.Infinite);
            }
            catch (NotSupportedException)
            {
                exceptionThrown = true;
            }

            Assert.IsTrue(exceptionThrown);

            exceptionThrown = false;
            try
            {
                var interpolator = Reproducible.First<int>(RelativeTimeInterval.Past());
            }
            catch (NotSupportedException)
            {
                exceptionThrown = true;
            }

            Assert.IsTrue(exceptionThrown);
        }

        [TestMethod]
        [Timeout(60000)]
        public void ReproducibleFirst_LeftBounded()
        {
            var interpolator = Reproducible.First<int>(new RelativeTimeInterval(TimeSpan.MinValue, TimeSpan.Zero));
            var messages = new Message<int>[]
            {
                new Message<int>(1, new DateTime(10), new DateTime(11), 0, 0),
                new Message<int>(2, new DateTime(20), new DateTime(21), 0, 1),
                new Message<int>(3, new DateTime(30), new DateTime(31), 0, 2),
            };

            // Interpolate at point later than last message
            var result = interpolator.Interpolate(new DateTime(100), messages, null);
            Assert.AreEqual(this.MakeResult(messages[0]), result);

            // Interpolate at point later than last message, but with stream closed
            result = interpolator.Interpolate(new DateTime(100), messages, new DateTime(40));
            Assert.AreEqual(this.MakeResult(messages[0]), result);

            // Interpolate at point before first message
            result = interpolator.Interpolate(new DateTime(9), messages, null);
            Assert.AreEqual(InterpolationResult<int>.DoesNotExist(DateTime.MinValue), result);

            // Interpolate between first two messages (closer to earlier message)
            result = interpolator.Interpolate(new DateTime(14), messages, null);
            Assert.AreEqual(this.MakeResult(messages[0]), result);

            // Interpolate between first two messages (exactly mid-way)
            result = interpolator.Interpolate(new DateTime(15), messages, null);
            Assert.AreEqual(this.MakeResult(messages[0]), result);

            // Interpolate between last two messages (closer to later message)
            result = interpolator.Interpolate(new DateTime(26), messages, null);
            Assert.AreEqual(this.MakeResult(messages[0]), result);

            // Interpolate right on the second message
            result = interpolator.Interpolate(new DateTime(20), messages, null);
            Assert.AreEqual(this.MakeResult(messages[0]), result);

            // Interpolate with no messages available
            var noMessages = new Message<int>[] { };
            result = interpolator.Interpolate(new DateTime(26), noMessages, null);
            Assert.AreEqual(InterpolationResult<int>.InsufficientData(), result);
        }

        [TestMethod]
        [Timeout(60000)]
        public void ReproducibleFirst_RightBounded()
        {
            var interpolator = Reproducible.First<int>(new RelativeTimeInterval(TimeSpan.Zero, TimeSpan.MaxValue));
            var messages = new Message<int>[]
            {
                new Message<int>(1, new DateTime(10), new DateTime(11), 0, 0),
                new Message<int>(2, new DateTime(20), new DateTime(21), 0, 1),
                new Message<int>(3, new DateTime(30), new DateTime(31), 0, 2),
            };

            // Interpolate at point later than last message
            var result = interpolator.Interpolate(new DateTime(100), messages, null);
            Assert.AreEqual(InterpolationResult<int>.InsufficientData(), result);

            // Interpolate at point later than last message, but with stream closed
            result = interpolator.Interpolate(new DateTime(100), messages, new DateTime(40));
            Assert.AreEqual(InterpolationResult<int>.DoesNotExist(new DateTime(100)), result);

            // Interpolate at point before first message
            result = interpolator.Interpolate(new DateTime(9), messages, null);
            Assert.AreEqual(this.MakeResult(messages[0]), result);

            // Interpolate between first two messages (closer to earlier message)
            result = interpolator.Interpolate(new DateTime(14), messages, null);
            Assert.AreEqual(this.MakeResult(messages[1]), result);

            // Interpolate between first two messages (exactly mid-way)
            result = interpolator.Interpolate(new DateTime(15), messages, null);
            Assert.AreEqual(this.MakeResult(messages[1]), result);

            // Interpolate between last two messages (closer to later message)
            result = interpolator.Interpolate(new DateTime(26), messages, null);
            Assert.AreEqual(this.MakeResult(messages[2]), result);

            // Interpolate right on the second message
            result = interpolator.Interpolate(new DateTime(20), messages, null);
            Assert.AreEqual(this.MakeResult(messages[1]), result);

            // Interpolate with no messages available
            var noMessages = new Message<int>[] { };
            result = interpolator.Interpolate(new DateTime(26), noMessages, null);
            Assert.AreEqual(InterpolationResult<int>.InsufficientData(), result);
        }

        [TestMethod]
        [Timeout(60000)]
        public void ReproducibleFirst_Bounded()
        {
            var interpolator = Reproducible.First<int>(new RelativeTimeInterval(TimeSpan.FromTicks(-5), TimeSpan.FromTicks(5)));
            var messages = new Message<int>[]
            {
                new Message<int>(1, new DateTime(10), new DateTime(11), 0, 0),
                new Message<int>(2, new DateTime(20), new DateTime(21), 0, 1),
                new Message<int>(3, new DateTime(30), new DateTime(31), 0, 2),
            };

            // Interpolate at point later than last message (outside upper bound)
            var result = interpolator.Interpolate(new DateTime(36), messages, null);
            Assert.AreEqual(InterpolationResult<int>.InsufficientData(), result);

            // Interpolate at point later than last message (outside upper bound), but with stream closing
            result = interpolator.Interpolate(new DateTime(36), messages, new DateTime(40));
            Assert.AreEqual(InterpolationResult<int>.DoesNotExist(new DateTime(31)), result);

            // Interpolate at point later than last message (within upper bound)
            result = interpolator.Interpolate(new DateTime(35), messages, null);
            Assert.AreEqual(this.MakeResult(messages[2]), result);

            // Interpolate at point before first message (outside lower bound)
            result = interpolator.Interpolate(new DateTime(4), messages, null);
            Assert.AreEqual(InterpolationResult<int>.DoesNotExist(DateTime.MinValue), result);

            // Interpolate at point before first message (within lower bound)
            result = interpolator.Interpolate(new DateTime(5), messages, null);
            Assert.AreEqual(this.MakeResult(messages[0]), result);

            // Interpolate between first two messages (closer to earlier message)
            result = interpolator.Interpolate(new DateTime(14), messages, null);
            Assert.AreEqual(this.MakeResult(messages[0]), result);

            // Interpolate between first two messages (exactly mid-way)
            result = interpolator.Interpolate(new DateTime(15), messages, null);
            Assert.AreEqual(this.MakeResult(messages[0]), result);

            // Interpolate between last two messages (closer to later message)
            result = interpolator.Interpolate(new DateTime(26), messages, null);
            Assert.AreEqual(this.MakeResult(messages[2]), result);

            // Interpolate right on the second message
            result = interpolator.Interpolate(new DateTime(20), messages, null);
            Assert.AreEqual(this.MakeResult(messages[1]), result);

            // Interpolate with no messages available
            var noMessages = new Message<int>[] { };
            result = interpolator.Interpolate(new DateTime(26), noMessages, null);
            Assert.AreEqual(InterpolationResult<int>.InsufficientData(), result);
        }

        [TestMethod]
        [Timeout(60000)]
        public void ReproducibleFirst_OpenIntervalTest()
        {
            var interpolatorRightOpen = Reproducible.First<int>(new RelativeTimeInterval(TimeSpan.FromTicks(-5), true, TimeSpan.FromTicks(4), false));
            var interpolatorLeftOpen = Reproducible.First<int>(new RelativeTimeInterval(TimeSpan.FromTicks(-4), false, TimeSpan.FromTicks(6), true));
            var messages = new Message<int>[]
            {
                new Message<int>(1, new DateTime(10), new DateTime(11), 0, 0),
                new Message<int>(2, new DateTime(20), new DateTime(21), 0, 1),
                new Message<int>(3, new DateTime(30), new DateTime(31), 0, 2),
            };

            // Interpolate should not match the right end of the interval because it's open.
            var result = interpolatorRightOpen.Interpolate(new DateTime(26), messages, null);
            Assert.AreEqual(InterpolationResult<int>.DoesNotExist(new DateTime(21)), result);

            // Interpolate should not match the right end of the interval because it's open.
            result = interpolatorRightOpen.Interpolate(new DateTime(26), messages, new DateTime(40));
            Assert.AreEqual(InterpolationResult<int>.DoesNotExist(new DateTime(21)), result);

            // Interpolate should not match the left end of the interval because it's open.
            result = interpolatorLeftOpen.Interpolate(new DateTime(24), messages, null);
            Assert.AreEqual(this.MakeResult(messages[2]), result);

            // Interpolate should not match the left end of the interval because it's open.
            result = interpolatorLeftOpen.Interpolate(new DateTime(24), messages, new DateTime(40));
            Assert.AreEqual(this.MakeResult(messages[2]), result);
        }

        [TestMethod]
        [Timeout(60000)]
        public void ReproducibleLast()
        {
            var interpolator = Reproducible.Last<int>(RelativeTimeInterval.Infinite);
            var messages = new Message<int>[]
            {
                new Message<int>(1, new DateTime(10), new DateTime(11), 0, 0),
                new Message<int>(2, new DateTime(20), new DateTime(21), 0, 1),
                new Message<int>(3, new DateTime(30), new DateTime(31), 0, 2),
            };

            // Interpolate at point later than last message
            var result = interpolator.Interpolate(new DateTime(100), messages, null);
            Assert.AreEqual(InterpolationResult<int>.InsufficientData(), result);

            // Interpolate at point later than last message, but with stream closed
            result = interpolator.Interpolate(new DateTime(100), messages, new DateTime(40));
            Assert.AreEqual(this.MakeResult(messages[2]), result);

            // Interpolate at point before first message
            result = interpolator.Interpolate(new DateTime(9), messages, null);
            Assert.AreEqual(InterpolationResult<int>.InsufficientData(), result);

            // Interpolate between first two messages (closer to earlier message)
            result = interpolator.Interpolate(new DateTime(14), messages, null);
            Assert.AreEqual(InterpolationResult<int>.InsufficientData(), result);

            // Interpolate between first two messages (exactly mid-way)
            result = interpolator.Interpolate(new DateTime(15), messages, null);
            Assert.AreEqual(InterpolationResult<int>.InsufficientData(), result);

            // Interpolate between last two messages (closer to later message)
            result = interpolator.Interpolate(new DateTime(26), messages, null);
            Assert.AreEqual(InterpolationResult<int>.InsufficientData(), result);

            // Interpolate right on the second message
            result = interpolator.Interpolate(new DateTime(20), messages, null);
            Assert.AreEqual(InterpolationResult<int>.InsufficientData(), result);

            // Interpolate with no messages available
            var noMessages = new Message<int>[] { };
            result = interpolator.Interpolate(new DateTime(26), noMessages, null);
            Assert.AreEqual(InterpolationResult<int>.InsufficientData(), result);
        }

        [TestMethod]
        [Timeout(60000)]
        public void ReproducibleLast_LeftBounded()
        {
            var interpolator = Reproducible.Last<int>(new RelativeTimeInterval(TimeSpan.MinValue, TimeSpan.Zero));
            var messages = new Message<int>[]
            {
                new Message<int>(1, new DateTime(10), new DateTime(11), 0, 0),
                new Message<int>(2, new DateTime(20), new DateTime(21), 0, 1),
                new Message<int>(3, new DateTime(30), new DateTime(31), 0, 2),
            };

            // Interpolate at point later than last message
            var result = interpolator.Interpolate(new DateTime(100), messages, null);
            Assert.AreEqual(InterpolationResult<int>.InsufficientData(), result);

            // Interpolate at point later than last message, but with stream closed
            result = interpolator.Interpolate(new DateTime(100), messages, new DateTime(40));
            Assert.AreEqual(this.MakeResult(messages[2]), result);

            // Interpolate at point before first message
            result = interpolator.Interpolate(new DateTime(9), messages, null);
            Assert.AreEqual(InterpolationResult<int>.DoesNotExist(DateTime.MinValue), result);

            // Interpolate between first two messages (closer to earlier message)
            result = interpolator.Interpolate(new DateTime(14), messages, null);
            Assert.AreEqual(this.MakeResult(messages[0]), result);

            // Interpolate between first two messages (exactly mid-way)
            result = interpolator.Interpolate(new DateTime(15), messages, null);
            Assert.AreEqual(this.MakeResult(messages[0]), result);

            // Interpolate between last two messages (closer to later message)
            result = interpolator.Interpolate(new DateTime(26), messages, null);
            Assert.AreEqual(this.MakeResult(messages[1]), result);

            // Interpolate right on the second message
            result = interpolator.Interpolate(new DateTime(20), messages, null);
            Assert.AreEqual(this.MakeResult(messages[1]), result);

            // Interpolate with no messages available
            var noMessages = new Message<int>[] { };
            result = interpolator.Interpolate(new DateTime(26), noMessages, null);
            Assert.AreEqual(InterpolationResult<int>.InsufficientData(), result);
        }

        [TestMethod]
        [Timeout(60000)]
        public void ReproducibleLast_RightBounded()
        {
            var interpolator = Reproducible.Last<int>(new RelativeTimeInterval(TimeSpan.Zero, TimeSpan.MaxValue));
            var messages = new Message<int>[]
            {
                new Message<int>(1, new DateTime(10), new DateTime(11), 0, 0),
                new Message<int>(2, new DateTime(20), new DateTime(21), 0, 1),
                new Message<int>(3, new DateTime(30), new DateTime(31), 0, 2),
            };

            // Interpolate at point later than last message
            var result = interpolator.Interpolate(new DateTime(100), messages, null);
            Assert.AreEqual(InterpolationResult<int>.InsufficientData(), result);

            // Interpolate at point later than last message, but with stream closed
            result = interpolator.Interpolate(new DateTime(100), messages, new DateTime(40));
            Assert.AreEqual(InterpolationResult<int>.DoesNotExist(new DateTime(100)), result);

            // Interpolate at point before first message
            result = interpolator.Interpolate(new DateTime(9), messages, null);
            Assert.AreEqual(InterpolationResult<int>.InsufficientData(), result);

            // Interpolate between first two messages (closer to earlier message)
            result = interpolator.Interpolate(new DateTime(14), messages, null);
            Assert.AreEqual(InterpolationResult<int>.InsufficientData(), result);

            // Interpolate between first two messages (exactly mid-way)
            result = interpolator.Interpolate(new DateTime(15), messages, null);
            Assert.AreEqual(InterpolationResult<int>.InsufficientData(), result);

            // Interpolate between last two messages (closer to later message)
            result = interpolator.Interpolate(new DateTime(26), messages, null);
            Assert.AreEqual(InterpolationResult<int>.InsufficientData(), result);

            // Interpolate right on the second message
            result = interpolator.Interpolate(new DateTime(20), messages, null);
            Assert.AreEqual(InterpolationResult<int>.InsufficientData(), result);

            // Interpolate with no messages available
            var noMessages = new Message<int>[] { };
            result = interpolator.Interpolate(new DateTime(26), noMessages, null);
            Assert.AreEqual(InterpolationResult<int>.InsufficientData(), result);
        }

        [TestMethod]
        [Timeout(60000)]
        public void ReproducibleLast_Bounded()
        {
            var interpolator = Reproducible.Last<int>(new RelativeTimeInterval(TimeSpan.FromTicks(-5), TimeSpan.FromTicks(5)));
            var messages = new Message<int>[]
            {
                new Message<int>(1, new DateTime(10), new DateTime(11), 0, 0),
                new Message<int>(2, new DateTime(20), new DateTime(21), 0, 1),
                new Message<int>(3, new DateTime(30), new DateTime(31), 0, 2),
            };

            // Interpolate at point later than last message (outside upper bound)
            var result = interpolator.Interpolate(new DateTime(36), messages, null);
            Assert.AreEqual(InterpolationResult<int>.InsufficientData(), result);

            // Interpolate at point later than last message (outside upper bound), but with stream closing
            result = interpolator.Interpolate(new DateTime(36), messages, new DateTime(40));
            Assert.AreEqual(InterpolationResult<int>.DoesNotExist(new DateTime(31)), result);

            // Interpolate at point later than last message (within upper bound)
            result = interpolator.Interpolate(new DateTime(35), messages, null);
            Assert.AreEqual(InterpolationResult<int>.InsufficientData(), result);

            // Interpolate at point before first message (outside lower bound)
            result = interpolator.Interpolate(new DateTime(4), messages, null);
            Assert.AreEqual(InterpolationResult<int>.DoesNotExist(DateTime.MinValue), result);

            // Interpolate at point before first message (within lower bound)
            result = interpolator.Interpolate(new DateTime(5), messages, null);
            Assert.AreEqual(this.MakeResult(messages[0]), result);

            // Interpolate between first two messages (closer to earlier message)
            result = interpolator.Interpolate(new DateTime(14), messages, null);
            Assert.AreEqual(this.MakeResult(messages[0]), result);

            // Interpolate between first two messages (exactly mid-way)
            result = interpolator.Interpolate(new DateTime(15), messages, null);
            Assert.AreEqual(this.MakeResult(messages[1]), result);

            // Interpolate between last two messages (closer to later message)
            result = interpolator.Interpolate(new DateTime(26), messages, null);
            Assert.AreEqual(InterpolationResult<int>.InsufficientData(), result);

            // Interpolate between last two messages (closer to later message), but with stream closing
            result = interpolator.Interpolate(new DateTime(26), messages, new DateTime(40));
            Assert.AreEqual(this.MakeResult(messages[2]), result);

            // Interpolate right on the second message
            result = interpolator.Interpolate(new DateTime(20), messages, null);
            Assert.AreEqual(this.MakeResult(messages[1]), result);

            // Interpolate with no messages available
            var noMessages = new Message<int>[] { };
            result = interpolator.Interpolate(new DateTime(26), noMessages, null);
            Assert.AreEqual(InterpolationResult<int>.InsufficientData(), result);
        }

        [TestMethod]
        [Timeout(60000)]
        public void ReproducibleLast_OpenIntervalTest()
        {
            var interpolatorRightOpen = Reproducible.Last<int>(new RelativeTimeInterval(TimeSpan.FromTicks(-5), true, TimeSpan.FromTicks(4), false));
            var interpolatorLeftOpen = Reproducible.Last<int>(new RelativeTimeInterval(TimeSpan.FromTicks(-4), false, TimeSpan.FromTicks(5), true));
            var messages = new Message<int>[]
            {
                new Message<int>(1, new DateTime(10), new DateTime(11), 0, 0),
                new Message<int>(2, new DateTime(20), new DateTime(21), 0, 1),
                new Message<int>(3, new DateTime(30), new DateTime(31), 0, 2),
            };

            // Interpolate should return does not exist b/c message at 30 falls after the open window
            var result = interpolatorRightOpen.Interpolate(new DateTime(26), messages, null);
            Assert.AreEqual(InterpolationResult<int>.DoesNotExist(new DateTime(21)), result);

            // Interpolate should not match the right end of the interval because it's open.
            result = interpolatorRightOpen.Interpolate(new DateTime(26), messages, new DateTime(40));
            Assert.AreEqual(InterpolationResult<int>.DoesNotExist(new DateTime(21)), result);

            // Interpolate should return does not exist b/c message at 30 falls after the open window
            result = interpolatorLeftOpen.Interpolate(new DateTime(24), messages, null);
            Assert.AreEqual(InterpolationResult<int>.DoesNotExist(new DateTime(20)), result);

            // Interpolate should not match the left end of the interval because it's open.
            result = interpolatorLeftOpen.Interpolate(new DateTime(24), messages, new DateTime(40));
            Assert.AreEqual(InterpolationResult<int>.DoesNotExist(new DateTime(20)), result);
        }

        [TestMethod]
        [Timeout(60000)]
        public void Linear()
        {
            var interpolator = Reproducible.Linear();
            var messages = new Message<double>[]
            {
                new Message<double>(1, new DateTime(10), new DateTime(11), 0, 0),
                new Message<double>(2, new DateTime(20), new DateTime(21), 0, 1),
                new Message<double>(3, new DateTime(30), new DateTime(31), 0, 2),
            };

            // Interpolate at point later than last message
            var result = interpolator.Interpolate(new DateTime(100), messages, null);
            Assert.AreEqual(InterpolationResult<double>.InsufficientData(), result);

            // Interpolate at point later than last message, but with stream closed
            result = interpolator.Interpolate(new DateTime(100), messages, new DateTime(40));
            Assert.AreEqual(InterpolationResult<double>.DoesNotExist(new DateTime(30)), result);

            // Interpolate at point before first message
            result = interpolator.Interpolate(new DateTime(9), messages, null);
            Assert.AreEqual(InterpolationResult<double>.DoesNotExist(DateTime.MinValue), result);

            // Interpolate between first two messages (closer to earlier message)
            result = interpolator.Interpolate(new DateTime(14), messages, null);
            Assert.AreEqual(result.Type, InterpolationResultType.Created);
            Assert.AreEqual(result.ObsoleteTime, new DateTime(10));
            Assert.AreEqual(result.Value, 1.4, 0.0000001);

            // Interpolate between first two messages (exactly mid-way)
            result = interpolator.Interpolate(new DateTime(15), messages, null);
            Assert.AreEqual(result.Type, InterpolationResultType.Created);
            Assert.AreEqual(result.ObsoleteTime, new DateTime(10));
            Assert.AreEqual(result.Value, 1.5, 0.0000001);

            // Interpolate between last two messages (closer to later message)
            result = interpolator.Interpolate(new DateTime(26), messages, null);
            Assert.AreEqual(result.Type, InterpolationResultType.Created);
            Assert.AreEqual(result.ObsoleteTime, new DateTime(20));
            Assert.AreEqual(result.Value, 2.6, 0.0000001);

            // Interpolate right on the first message
            result = interpolator.Interpolate(new DateTime(10), messages, null);
            Assert.AreEqual(result.Type, InterpolationResultType.Created);
            Assert.AreEqual(result.ObsoleteTime, new DateTime(10));
            Assert.AreEqual(result.Value, 1.0, 0.0000001);

            // Interpolate right on the second message
            result = interpolator.Interpolate(new DateTime(20), messages, null);
            Assert.AreEqual(result.Type, InterpolationResultType.Created);
            Assert.AreEqual(result.ObsoleteTime, new DateTime(20));
            Assert.AreEqual(result.Value, 2.0, 0.0000001);

            // Interpolate right on the last message
            result = interpolator.Interpolate(new DateTime(30), messages, null);
            Assert.AreEqual(result.Type, InterpolationResultType.Created);
            Assert.AreEqual(result.ObsoleteTime, new DateTime(30));
            Assert.AreEqual(result.Value, 3.0, 0.0000001);

            // Interpolate with no messages available
            var noMessages = new Message<double>[] { };
            result = interpolator.Interpolate(new DateTime(26), noMessages, null);
            Assert.AreEqual(InterpolationResult<double>.InsufficientData(), result);
        }

        private InterpolationResult<T> MakeResult<T>(Message<T> msg)
        {
            return InterpolationResult<T>.Create(msg.Data, msg.OriginatingTime);
        }

        private InterpolationResult<T> MakeResult<T>(T data, DateTime originatingTime)
        {
            return InterpolationResult<T>.Create(data, originatingTime);
        }
    }
}

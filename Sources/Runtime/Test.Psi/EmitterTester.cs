// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.

namespace Test.Psi
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Threading;
    using System.Threading.Tasks;
    using Microsoft.Psi;
    using Microsoft.Psi.Executive;
    using Microsoft.VisualStudio.TestTools.UnitTesting;

    [TestClass]
    public class EmitterTester
    {
        // custom delivery policy that attempts synchronous delivery for testing purposes
        private static readonly DeliveryPolicy Immediate = new DeliveryPolicy(1, int.MaxValue, null, null, true);

        // maintain our own receiver ids
        private static int lastReceiverId = -1;

        // creates a custom receiver that does not enforce isolation on synchronous delivery
        private static Receiver<T> CreateNonIsolatedReceiver<T>(Pipeline pipeline, object owner, Action<Message<T>> action, string name)
        {
            PipelineElement node = pipeline.GetOrCreateNode(owner);

            // create custom receivers with a negative receiver id to ensure they do not clash with those created by the public Pipeline methods
            var receiver = new Receiver<T>(Interlocked.Decrement(ref lastReceiverId), name, node, owner, action, node.SyncContext, pipeline, enforceIsolation: false);
            node.AddInput(name, receiver);
            return receiver;
        }

        [TestMethod]
        [Timeout(60000)]
        public void ReceiveClassByValue()
        {
            var c = new STClass();
            STClass result = null;
            using (var p = Pipeline.Create())
            {
                var receiver = p.CreateReceiver<STClass>(
                    this,
                    msg =>
                    {
                        result = msg.Data;
                    },
                    "receiver");

                Generators.Return(p, c).PipeTo(receiver, DeliveryPolicy.Unlimited);
                p.Run();
            }

            Assert.IsTrue(result.Same(c));
            Assert.AreNotEqual(result, c); // we expect a different instance
        }

        [TestMethod]
        [Timeout(60000)]
        public void ReceiveClassByRef()
        {
            var c = new STClass();
            STClass result = null;
            using (var p = Pipeline.Create("ReceiveClassByRef", DeliveryPolicy.Unlimited, allowSchedulingOnExternalThreads: true))
            {
                var receiver = CreateNonIsolatedReceiver<STClass>(
                    p,
                    this,
                    msg =>
                    {
                        result = msg.Data;
                    },
                    "receiver");

                Generators.Return(p, c).PipeTo(receiver, Immediate);
                p.Run();
            }

            Assert.IsTrue(result.Same(c));
            Assert.AreEqual(result, c); // we expect the same instance
        }

        [TestMethod]
        [Timeout(60000)]
        public void ReceiveStructByValue()
        {
            var c = new STClass();
            var s = new STStruct(123, c);
            STStruct result = default(STStruct);
            using (var p = Pipeline.Create("ReceiveClassByRef", DeliveryPolicy.Unlimited, allowSchedulingOnExternalThreads: true))
            {
                var receiver = p.CreateReceiver<STStruct>(
                    this,
                    msg =>
                    {
                        result = msg.Data;
                    },
                    "receiver");

                Generators.Return(p, s).PipeTo(receiver, DeliveryPolicy.Unlimited);
                p.Run();
            }

            Assert.AreEqual(result.Value, 123);
            Assert.IsTrue(result.Reference.Same(c));
            Assert.AreNotEqual(result.Reference, c); // we expect a different instance
        }

        [TestMethod]
        [Timeout(60000)]
        public void ReceiveStructByRef()
        {
            var c = new STClass();
            var s = new STStruct(123, c);
            STStruct result = default(STStruct);

            using (var p = Pipeline.Create("ReceiveStructByRef", DeliveryPolicy.Unlimited, allowSchedulingOnExternalThreads: true))
            {
                var receiver = CreateNonIsolatedReceiver<STStruct>(
                    p,
                    this,
                    msg =>
                    {
                        result = msg.Data;
                    },
                    "receiver");

                Generators.Return(p, s).PipeTo(receiver, Immediate);
                p.Run();
            }

            Assert.AreEqual(result.Value, 123);
            Assert.AreEqual(result.Reference, c); // we expect the same instance
        }

        [TestMethod]
        [Timeout(60000)]
        public void ValidateMessages()
        {
            var results = new List<int>();

            using (var p = Pipeline.Create())
            {
                // create an emitter with a user-supplied validator
                var validatingEmitter = p.CreateEmitter<int>(
                    this,
                    "validatingEmitter",
                    (msg, env) =>
                    {
                        if (msg > 5)
                        {
                            throw new ArgumentOutOfRangeException("Maximum value exceeded!");
                        }
                    });

                // receiver that posts whatever it receives on the validating emitter
                var receiver = p.CreateReceiver<int>(
                    this,
                    (msg, env) =>
                    {
                        validatingEmitter.Post(msg, env.OriginatingTime);
                    },
                    "receiver");

                // drive the test with a sequence of increasing integers
                Generators.Sequence(p, 0, i => i + 1, new TimeSpan(1)).PipeTo(receiver);

                // capture output from the validatingEmitter
                validatingEmitter.Do(msg => results.Add(msg));

                try
                {
                    p.Run();
                    Assert.Fail("Expected exception was not thrown.");
                }
                catch (AggregateException errors)
                {
                    // expecting at least one ArgumentOutOfRangeException wrapped in an AggregateException
                    Assert.IsTrue(errors.InnerExceptions.Count > 0);
                    Assert.IsInstanceOfType(errors.InnerException, typeof(ArgumentOutOfRangeException));
                    CollectionAssert.AreEqual(new[] { 0, 1, 2, 3, 4, 5 }, results);
                }
            }
        }

        [TestMethod]
        [Timeout(60000)]
        public void DeliverOutOfOrderSequenceIdsShouldThrow()
        {
            var exceptionThrown = false;
            try
            {
                using (var p = Pipeline.Create())
                {
                    var time = DateTime.UtcNow;
                    var emitter = p.CreateEmitter<int>(this, "test");
                    emitter.Deliver(123, new Envelope(time, time, emitter.Id, 2));
                    emitter.Deliver(456, new Envelope(time.AddTicks(1), time.AddTicks(1), emitter.Id, 1)); // this should fail (posting with out of order sequence ID)
                }
            }
            catch (InvalidOperationException ex)
            {
                exceptionThrown = true;
                Assert.IsTrue(ex.Message.StartsWith("Attempted to post a message with a sequence ID that is out of order"));
            }

            Assert.IsTrue(exceptionThrown);
        }

        [TestMethod]
        [Timeout(60000)]
        public void DeliverOutOfOrderOriginatingTimesShouldThrow()
        {
            var exceptionThrown = false;
            try
            {
                using (var p = Pipeline.Create())
                {
                    var time = DateTime.UtcNow;
                    var emitter = p.CreateEmitter<int>(this, "test");
                    emitter.Deliver(123, new Envelope(time, time, emitter.Id, 2));
                    emitter.Deliver(456, new Envelope(time, time.AddTicks(1), emitter.Id, 3)); // this should fail (posting with non-increasing originating times)
                }
            }
            catch (InvalidOperationException ex)
            {
                exceptionThrown = true;
                Assert.IsTrue(ex.Message.StartsWith("Attempted to post a message without strictly increasing originating times"));
            }

            Assert.IsTrue(exceptionThrown);
        }

        [TestMethod]
        [Timeout(60000)]
        public void DeliverOutOfOrderCreationTimesShouldThrow()
        {
            var exceptionThrown = false;
            try
            {
                using (var p = Pipeline.Create())
                {
                    var time = DateTime.UtcNow;
                    var emitter = p.CreateEmitter<int>(this, "test");
                    emitter.Deliver(123, new Envelope(time, time, emitter.Id, 2));
                    emitter.Deliver(456, new Envelope(time.AddTicks(1), time.AddTicks(-1), emitter.Id, 3)); // this should fail (posting with out of order creation times)
                }
            }
            catch (InvalidOperationException ex)
            {
                exceptionThrown = true;
                Assert.IsTrue(ex.Message.StartsWith("Attempted to post a message that is out of order in wall-clock time"));
            }

            Assert.IsTrue(exceptionThrown);
        }

#if DEBUG
        [TestMethod]
        [Timeout(60000)]
        public void PostOutsideOfReceiverShouldThrow()
        {
            var exceptionThrown = false;
            try
            {
                using (var p = Pipeline.Create())
                {
                    var emitter = p.CreateEmitter<int>(this, "test");
                    emitter.Post(123, p.GetCurrentTime()); // this should fail (posting from outside receiver in non-ISourceComponent)
                }
            }
            catch (InvalidOperationException ex)
            {
                exceptionThrown = true;
                Assert.IsTrue(ex.Message.StartsWith("Emitter unexpectedly posted to from outside of a receiver"));
            }

            Assert.IsTrue(exceptionThrown);
        }

        [TestMethod]
        [Timeout(60000)]
        public void PostAcrossComponentsShouldThrow()
        {
            var exceptionThrown = false;
            try
            {
                using (var p = Pipeline.Create())
                {
                    var componentA = new object();
                    var componentB = new object();
                    var emitter = p.CreateEmitter<int>(componentB, "emitterOnB");
                    var receiver = p.CreateReceiver<int>(componentA, x => emitter.Post(x, p.GetCurrentTime()), "receiverOnA");
                    Generators.Return(p, 123).PipeTo(receiver);
                    p.Run();
                }
            }
            catch (AggregateException ex)
            {
                exceptionThrown = true;
                Assert.AreEqual<int>(1, ex.InnerExceptions.Count);
                Assert.IsTrue(ex.InnerExceptions[0].Message.StartsWith("Emitter of one component unexpectedly received post from a receiver of another component"));
            }

            Assert.IsTrue(exceptionThrown);
        }

        [TestMethod]
        [Timeout(60000)]
        public void PostAcrossAsyncComponentsShouldThrow()
        {
            var exceptionThrown = false;
            try
            {
                using (var p = Pipeline.Create())
                {
                    var componentA = new object();
                    var componentB = new object();
                    var emitter = p.CreateEmitter<int>(componentB, "emitterOnB");
                    var receiver = p.CreateAsyncReceiver<int>(
                        componentA,
                        async x =>
                        {
                            await Task.Delay(1);
                            emitter.Post(x, p.GetCurrentTime());
                        },
                        "receiverOnA");
                    Generators.Return(p, 123).PipeTo(receiver);
                    p.Run();
                }
            }
            catch (AggregateException ex)
            {
                exceptionThrown = true;
                Assert.AreEqual<int>(1, ex.InnerExceptions.Count);

                // since this is an async receiver, exceptions are also wrapped in an AggregateException
                var aggEx = ex.InnerExceptions[0] as AggregateException;
                Assert.IsNotNull(aggEx);

                Assert.AreEqual<int>(1, aggEx.InnerExceptions.Count);
                Assert.IsTrue(aggEx.InnerExceptions[0].Message.StartsWith("Emitter of one component unexpectedly received post from a receiver of another component"));
            }

            Assert.IsTrue(exceptionThrown);
        }

        [TestMethod]
        [Timeout(60000)]
        public void PostWithinAsyncComponentShouldNotThrow()
        {
            var exceptionThrown = false;
            try
            {
                using (var p = Pipeline.Create())
                {
                    var componentA = new object();
                    var emitter = p.CreateEmitter<int>(componentA, "emitter");
                    var receiver = p.CreateAsyncReceiver<int>(
                        componentA,
                        async x =>
                        {
                            await Task.Delay(1);
                            emitter.Post(x, p.GetCurrentTime());
                        },
                        "receiver");
                    Generators.Return(p, 123).PipeTo(receiver);
                    p.Run();
                }
            }
            catch (AggregateException)
            {
                exceptionThrown = true;
            }

            Assert.IsFalse(exceptionThrown);
        }

        [TestMethod]
        [Timeout(60000)]
        public void SubscriptionAcrossPipelinesShouldThrow()
        {
            var exceptionThrown = false;
            try
            {
                using (var p = Pipeline.Create())
                {
                    var s = Subpipeline.Create(p, "subpipeline");
                    var emitter = p.CreateEmitter<int>(this, "pipelineEmitter");
                    var receiver = s.CreateReceiver<int>(this, x => emitter.Post(x, p.GetCurrentTime()), "subpipelineReceiver");
                    Generators.Return(p, 123).PipeTo(receiver);
                    p.Run();
                }
            }
            catch (InvalidOperationException ex)
            {
                exceptionThrown = true;
                Assert.IsTrue(ex.Message.StartsWith("Receiver cannot subscribe to an emitter from a different pipeline"));
            }

            Assert.IsTrue(exceptionThrown);
        }
#endif

        public struct STStruct
        {
            internal readonly int Value;
            internal readonly STClass Reference;

            internal STStruct(int value, STClass reference)
            {
                this.Value = value;
                this.Reference = reference;
            }
        }

        public class STClass
        {
            private int count;
            private string label;
            private double[] buffer;

            internal STClass()
            {
                this.count = 10;
                this.label = "Something with ten";
                this.buffer = new double[] { 0, 1, 2, 3, 4, 5, 6, 7, 8, 9 };
            }

            public bool Same(STClass that)
            {
                return this.count == that.count && this.label == that.label && (this.buffer.Except(that.buffer).Count() == 0) && (that.buffer.Except(this.buffer).Count() == 0);
            }
        }
    }
}

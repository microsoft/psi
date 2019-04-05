// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.

namespace Test.Psi
{
    using System;
    using System.Linq;
    using System.Threading;
    using System.Threading.Tasks;
    using Microsoft.Psi;
    using Microsoft.VisualStudio.TestTools.UnitTesting;

    [TestClass]
    public class EmitterTester
    {
        // make out local version of immediate, to make sure it's synchronous even in debug builds
        private static readonly DeliveryPolicy immediate = new DeliveryPolicy(1, int.MaxValue, null, true, true);

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
                var receiver = p.CreateReceiver<STClass>(
                    this,
                    msg =>
                    {
                        result = msg.Data;
                    },
                    "receiver");

                Generators.Return(p, c).PipeTo(receiver, immediate);
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
                var receiver = p.CreateReceiver<STStruct>(
                    this,
                    msg =>
                    {
                        result = msg.Data;
                    },
                    "receiver");

                Generators.Return(p, s).PipeTo(receiver, immediate);
                p.Run();
            }

            Assert.AreEqual(result.Value, 123);
            Assert.AreEqual(result.Reference, c); // we expect the same instance
        }

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
                    p.Run(enableExceptionHandling: true);
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
                    p.Run(enableExceptionHandling: true);
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
                    p.Run(enableExceptionHandling: true);
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
        public void PostAcrossPipelinesShouldThrow()
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
                    p.Run(enableExceptionHandling: true);
                }
            }
            catch (AggregateException ex)
            {
                exceptionThrown = true;
                Assert.AreEqual<int>(1, ex.InnerExceptions.Count);
                Assert.IsTrue(ex.InnerExceptions[0].Message.StartsWith("Emitter created in one pipeline unexpectedly received post from a receiver in another pipeline"));
            }

            Assert.IsTrue(exceptionThrown);
        }
#endif
    }
}

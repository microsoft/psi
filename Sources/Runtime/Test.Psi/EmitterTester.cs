// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.

namespace Test.Psi
{
    using System;
    using System.Linq;
    using System.Threading;
    using Microsoft.Psi;
    using Microsoft.VisualStudio.TestTools.UnitTesting;

    [TestClass]
    public class EmitterTester
    {
        // make out local version of immediate, to make sure it's synchronous even in debug builds
        private static DeliveryPolicy immediate = new DeliveryPolicy() { ThrottleWhenFull = true, LagEnforcement = LagConstraints.None, QueueSize = 1, IsSynchronous = true };

        [TestMethod]
        [Timeout(60000)]
        public void ReceiveClassByValue()
        {
            var c = new STClass();
            STClass result = null;
            using (var p = Pipeline.Create())
            {
                var emitter = p.CreateEmitter<STClass>(this, "emitter");
                var receiver = p.CreateReceiver<STClass>(
                    this,
                    msg =>
                    {
                        result = msg.Data;
                    },
                    "receiver");

                emitter.PipeTo(receiver, DeliveryPolicy.Unlimited);
                p.RunAsync();
                emitter.Post(c, DateTime.MinValue);
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
                var emitter = p.CreateEmitter<STClass>(this, "emitter");
                var receiver = p.CreateReceiver<STClass>(
                    this,
                    msg =>
                    {
                        result = msg.Data;
                    },
                    "receiver");

                emitter.PipeTo(receiver, immediate);
                p.RunAsync();
                emitter.Post(c, DateTime.MinValue);
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
                var emitter = p.CreateEmitter<STStruct>(this, "emitter");
                var receiver = p.CreateReceiver<STStruct>(
                    this,
                    msg =>
                    {
                        result = msg.Data;
                    },
                    "receiver");

                emitter.PipeTo(receiver, DeliveryPolicy.Unlimited);
                p.RunAsync();
                emitter.Post(s, DateTime.MinValue);
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
                var emitter = p.CreateEmitter<STStruct>(this, "emitter");
                var receiver = p.CreateReceiver<STStruct>(
                    this,
                    msg =>
                    {
                        result = msg.Data;
                    },
                    "receiver");

                emitter.PipeTo(receiver, immediate);
                p.RunAsync();
                emitter.Post(s, DateTime.MinValue);
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
    }
}

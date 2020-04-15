// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.

namespace Test.Psi
{
    using System;
    using System.Collections.Generic;
    using System.Threading;
    using Microsoft.Psi;
    using Microsoft.Psi.Components;
    using Microsoft.Psi.Scheduling;
    using Microsoft.VisualStudio.TestTools.UnitTesting;

    [TestClass]
    public class ReceiverTest : ISourceComponent
    {
        [TestMethod]
        [Timeout(60000)]
        public void ConcurrentMessagePassing()
        {
            int iter = 16;
            List<SimpleMsg> postedValues = new List<SimpleMsg>(iter);
            List<SimpleMsg> receivedValues = new List<SimpleMsg>(iter);
            using (Pipeline p = Pipeline.Create())
            {
                var emitter = new Emitter<SimpleMsg>(0, null, this, null, p);
                var receiver = new Receiver<SimpleMsg>(0, string.Empty, null, this, t => { receivedValues.Add(t.Data); }, new SynchronizationLock(null), p, true);
                emitter.PipeTo(receiver, DeliveryPolicy.Unlimited);
                p.RunAsync();
                for (int i = 0; i < iter; i++)
                {
                    SimpleMsg val = new SimpleMsg(i, i.ToString());
                    emitter.Post(val, Time.GetCurrentTime());
                    postedValues.Add(val);
                }

                while (receivedValues.Count != iter)
                {
                    Thread.Sleep(16 - iter);
                }
            }

            for (int i = 0; i < iter; i++)
            {
                Assert.AreNotEqual(postedValues[i], receivedValues[i]); // results should be clones. not references
                Assert.AreEqual(postedValues[i].Count, receivedValues[i].Count);
                Assert.AreEqual(postedValues[i].Label, receivedValues[i].Label);
            }
        }

        public class SimpleMsg
        {
            public SimpleMsg(int count, string label)
            {
                this.Count = count;
                this.Label = label;
            }

            public int Count { get; private set; }

            public string Label { get; private set; }
        }

        /// <inheritdoc/>
        public void Start(Action<DateTime> notifyCompletionTime)
        {
            notifyCompletionTime(DateTime.MaxValue);
        }

        /// <inheritdoc/>
        public void Stop(DateTime finalOriginatingTime, Action notifyCompleted)
        {
            notifyCompleted();
        }
    }
}

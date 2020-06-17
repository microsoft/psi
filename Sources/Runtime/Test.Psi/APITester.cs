// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.

namespace Test.Psi
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using Microsoft.Psi;
    using Microsoft.VisualStudio.TestTools.UnitTesting;

    /// <summary>
    /// Scenarios and usage around simple pipelines, join and repeat.
    /// </summary>
    [TestClass]
    public class APITester
    {
        [TestMethod]
        [Timeout(60000)]
        public void SimplePipeline()
        {
            using (var p = Pipeline.Create("SimplePipeline"))
            {
                var generate = Generators.Sequence(p, new[] { 0d, 1d }, x => new[] { x[0] + 0.1, x[1] + 0.1 }, 10, TimeSpan.FromTicks(1));
                var transform = new ScalarMultiplier(p, 100);
                generate.PipeTo(transform);
                transform.Do(a => Console.WriteLine($"[{a[0]}, {a[1]}]"));

                // start and run the pipeline
                p.Run();
            }
        }

        [TestMethod]
        [Timeout(60000)]
        public void JoinPipeline()
        {
            using (var p = Pipeline.Create("JoinPipeline"))
            {
                // create a generator that will produce a finite sequence
                var generator = Generators.Sequence(p, new[] { 0d, 1d }, x => new[] { x[0] + 0.1, x[1] + 0.1 }, 10, TimeSpan.FromTicks(1));

                // instantiate our sample component
                var multiply = new ScalarMultiplier(p, 10);
                var divide = new ScalarMultiplier(p, 0.1);

                // create a pipeline of the form:
                // generator -> multiply \
                //                        -> join -> add -> WriteLine
                // generator -> divide   /
                generator.PipeTo(multiply);
                generator.PipeTo(divide);
                multiply
                    .Join(divide, TimeSpan.Zero)
                    .Select(t => new[] { t.Item1[0] + t.Item2[0], t.Item1[1] + t.Item2[1] })
                    .Do(a => Console.WriteLine($"[{a[0]}, {a[1]}]"));

                // start and run the pipeline
                p.Run();
            }
        }

        [TestMethod]
        [Timeout(60000)]
        public void RepeaterPipeline()
        {
            using (var p = Pipeline.Create("RepeaterPipeline"))
            {
                // create a generator that will produce a finite sequence
                var generator = Generators.Sequence(p, 1, x => x + 1, 100, TimeSpan.FromMilliseconds(1));

                var some = generator.Where(x => x % 10 == 0);

                var repeat = generator.Pair(some).Item2();

                // var output = repeat.Do((x, e) => Console.WriteLine($"{x} [{e.SequenceId}]"));
                // var check = repeat.Do((d, e) => Assert.AreEqual(d, e.SequenceId));
                var ls = new List<int>();
                var collect = repeat.Do(x => ls.Add(x));

                // start and run the pipeline
                p.Run();

                Assert.IsTrue(ls.Sum() > 0);
            }
        }

        [TestMethod]
        [Timeout(60000)]
        [ExpectedException(typeof(DivideByZeroException))]
        public void ExceptionHandling()
        {
            using (var p = Pipeline.Create(nameof(this.ExceptionHandling)))
            {
                var source = Generators.Sequence(p, 1, x => x + 1, 100, TimeSpan.FromTicks(1));
                var sin = source.Select(t => t / 0); // trigger an exception
                try
                {
                    p.Run();
                }
                catch (AggregateException e)
                {
                    // exceptions from within the pipeline (including subpipelines) are wrapped in an AggregateException
                    throw e.InnerException;
                }
            }
        }

        [TestMethod]
        [Timeout(60000)]
        [ExpectedException(typeof(DivideByZeroException))]
        public void ExceptionFromPipelineRunHandler()
        {
            using (var p = Pipeline.Create("pipeline"))
            {
                var source = Generators.Sequence(p, 1, x => x + 1, 100, TimeSpan.FromTicks(1));
                p.PipelineRun += (s, e) =>
                {
                    int x = 0;
                    x /= 0; // trigger an exception
                };

                p.Run();
            }
        }

        [TestMethod]
        [Timeout(60000)]
        [ExpectedException(typeof(DivideByZeroException))]
        public void ExceptionFromSubpipelineRunHandler()
        {
            using (var p = Pipeline.Create("pipeline"))
            {
                var sub = new Subpipeline(p, "subpipeline");
                var generator = Generators.Sequence(p, 1, x => x + 1, 100, TimeSpan.FromTicks(1));

                sub.PipelineRun += (s, e) =>
                {
                    int x = 0;
                    x /= 0; // trigger an exception
                };

                try
                {
                    p.Run();
                }
                catch (AggregateException e)
                {
                    // exceptions from within the pipeline (including subpipelines) are wrapped in an AggregateException
                    throw e.InnerException;
                }
            }
        }

        // any class can be a \psi component
        private class ScalarMultiplier : IConsumer<double[]>, IProducer<double[]>
        {
            private readonly double scalar;
            private readonly Receiver<double[]> input;
            private readonly Emitter<double[]> output;
            private double[] buffer = new double[0];

            // The constructor should avoid taking global resources (e.g cameras, OS handles)
            // Implement IStartable instead to provide an explicit initialization step
            public ScalarMultiplier(Pipeline pipeline, double scalar)
            {
                this.input = pipeline.CreateReceiver<double[]>(this, this.Receive, nameof(this.input));
                this.output = pipeline.CreateEmitter<double[]>(this, nameof(this.output));
                this.scalar = scalar;
            }

            public Receiver<double[]> In => this.input;

            public Emitter<double[]> Out => this.output;

            // This method will be called at most once for every input message (depending on the delivery policy, some messages might never get here)
            // The messages are guaranteed to arrive in the order of their originating time
            private void Receive(double[] input, Envelope envelope)
            {
                // To improve performance, avoid making allocations unless needed, and use cached buffers instead
                // Since the input methods are exclusive and not re-entrant, we can access the state variables without locking.
                if (this.buffer.Length != input.Length)
                {
                    this.buffer = new double[input.Length];
                }

                // The input object is valid only for the lifetime of this method. Once Receive returns, the object becomes invalid.
                // If you need to store it (or parts of it), you must create a deep clone (use Microsoft.Psi.Serialization.Serializer.Clone(input)).
                // it is ok, however, to re-post it (or parts of it) without cloning first
                for (int i = 0; i < input.Length; i++)
                {
                    this.buffer[i] = input[i] * this.scalar;
                }

                // Publish the result, as if it was a sync call.
                // Post publishes "by value". Once the call returns, we can re-use the message right away without any effect on the downstream components,
                // because by then, they have their own copies of the message.
                // When large buffers are involved and a post-by-ref is needed for performance reasons, use the Shared<T> class
                this.Out.Post(this.buffer, envelope.OriginatingTime);
            }
        }
    }
}

// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.

namespace Test.Psi
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Threading;
    using System.Threading.Tasks.Dataflow;
    using Microsoft.Psi;
    using Microsoft.Psi.Common;
    using Microsoft.Psi.Serialization;
    using Microsoft.VisualStudio.TestTools.UnitTesting;

    /// <summary>
    /// Runs a series of tests with variations of a pipeline with a fixed structure,
    /// of many producers feeding in parallel to a series of transforms and eventually joining to a single target.
    /// The number of parallel branches and transforms per branch is configurable, as is the frequency of the data generation and tthe type of transforms applied.
    /// </summary>
    [TestClass]
    public class FunctionalTests
    {
        // perf test settings
#if Perf
        private const int SourceCount = 5;
        private const int ParallelBranchMultiplier = 1;
        private const int ParallelBranchCount = SourceCount * ParallelBranchMultiplier;
        private const int TransformCount = 10;
        private const int ArraySize = 100;
        private const int IncMultiplier = 1;
        private const uint frequency = 1000; //Hz
        private TimeSpan duration = TimeSpan.FromSeconds(10000);
#else
        // daily test settings
        private const int SourceCount = 2;
        private const int ParallelBranchMultiplier = 1;
        private const int ParallelBranchCount = SourceCount * ParallelBranchMultiplier;
        private const int TransformCount = 1;
        private const int ArraySize = 100;
        private const int IncMultiplier = 1;
        private uint frequency = 200; // Hz
        private TimeSpan duration = TimeSpan.FromSeconds(0.5);
#endif

        public static int[] Set(int[] target, int val)
        {
            for (int i = 0; i < target.Length; i++)
            {
                target[i] = val;
            }

            return target;
        }

        public static int[] Inc(int[] target, int[] a, int val)
        {
            for (int j = 0; j < IncMultiplier; j++)
            {
                for (int i = 0; i < a.Length; i++)
                {
                    target[i] = a[i] + val;
                }
            }

            return target;
        }

        public static int[] Add(int[] target, int[] a, int[] b)
        {
            for (int i = 0; i < a.Length; i++)
            {
                target[i] = a[i] + b[i];
            }

            return target;
        }

        // uses types that don't need cloning
        [TestMethod]
        [Timeout(60000)]
        public void SimpleValueTypePipeline()
        {
            this.RunPipeline<int>(
                create: i => 0,
                initialize: (_, i) => i,
                increment: (_, i) => i + 1,
                add: (_, i, j) => i + j,
                extract: i => i,
                validateNoLoss: true,
                validateSync: true);
        }

        // DataFlow baseline. Uses types that don't need cloning.
        // Similar CPU & throughput results, but memory keeps growing.
#if Perf
        [TestMethod, Timeout(60000)]
#endif
        public void SimpleValueTypePipelineDF()
        {
            this.RunDataFlowPipeline<int>(
                create: i => 0,
                initialize: (_, i) => i,
                increment: (_, i) => i + 1,
                add: (_, i, j) => i + j,
                extract: i => i,
                validateNoLoss: true,
                validateSync: true);
        }

        // uses types that need cloning
        [TestMethod]
        [Timeout(60000)]
        public void ImmutablePipeline()
        {
            this.RunPipeline<Immutable<int[]>>(
                create: i => new int[0],
                initialize: (a, i) => Enumerable.Repeat(i, ArraySize).ToArray(),
                increment: (_, a) => a.Value.Select(i => i + 1).ToArray(),
                add: (_, a, b) => a.Value.Zip(b.Value, (i, j) => i + j).ToArray(),
                extract: a => a.Value.First(),
                validateNoLoss: true,
                validateSync: false);
        }

        // uses types that need cloning but can be reclaimed
        [TestMethod]
        [Timeout(60000)]
        public void RefTypePipeline()
        {
            this.RunPipeline<int[]>(
                   create: i => new int[ArraySize],
                   initialize: Set,
                   increment: (tgt, a) => Inc(tgt, a, 1),
                   add: Add,
                   extract: a => a[0],
                   validateNoLoss: true,
                   validateSync: false);
        }

        // DataFlow baseline. Similar CPU & throughput results, but memory keeps growing.
#if Perf
        [TestMethod, Timeout(60000)]
#endif
        public void RefTypePipelineDF()
        {
            this.RunPipeline<int[]>(
                   create: i => new int[ArraySize],
                   initialize: Set,
                   increment: (tgt, a) => Inc(tgt, a, 1),
                   add: Add,
                   extract: a => a[0],
                   validateNoLoss: true,
                   validateSync: false);
        }

        [TestMethod]
        [Timeout(60000)]
        public void RefCountedPipeline()
        {
            var sharedPool = new SharedPool<int[]>(() => new int[ArraySize], ParallelBranchCount * TransformCount);
            this.RunPipeline<Shared<int[]>>(
                   create: i => sharedPool.GetOrCreate(),
                   initialize: (old, i) =>
                   {
                       old.Dispose();
                       var tgt = sharedPool.GetOrCreate();
                       Set(tgt.Resource, i);
                       return tgt;
                   },
                   increment: (old, a) =>
                   {
                       old.Dispose();
                       var tgt = sharedPool.GetOrCreate();
                       Inc(tgt.Resource, a.Resource, 1);
                       return tgt;
                   },
                   add: (old, a, b) =>
                    {
                       old.Dispose();
                       var tgt = sharedPool.GetOrCreate();
                       Add(tgt.Resource, a.Resource, b.Resource);
                       return tgt;
                    },
                   extract: a => a.Resource[0],
                   validateNoLoss: true,
                   validateSync: false);
        }

        public void RunPipeline<T>(Func<int, T> create, Func<T, int, T> initialize, Func<T, T, T> increment, Func<T, T, T, T> add, Func<T, int> extract, bool validateNoLoss, bool validateSync)
        {
            int resultCount = 0;
            var p = Pipeline.Create(nameof(FunctionalTests));

            Console.WriteLine($"Running {ParallelBranchCount} branches and {TransformCount} transforms at {this.frequency}Hz");

            // create several parallel branches of components
            var branches = new IProducer<Wrap<T>>[ParallelBranchCount];
            for (int i = 0; i < SourceCount; i++)
            {
                // make a timer for each source
                var timer = Timers.Timer(p, TimeSpan.FromMilliseconds(1000 / this.frequency)).Select((d, e) => e.SequenceId);

                // branch and generate data
                for (int k = 0; k < ParallelBranchMultiplier; k++)
                {
                    int b = (i * ParallelBranchMultiplier) + k;

                    branches[b] = timer.Aggregate(new Wrap<T>(create(b), 0), (tgt, seqId) => new Wrap<T>(initialize(tgt.Inner, seqId), seqId));

                    // apply a sequence of transforms
                    for (int j = 0; j < TransformCount; j++)
                    {
                        branches[b] = branches[b].Aggregate(new Wrap<T>(create(b), 0), (tgt, src) => new Wrap<T>(increment(tgt.Inner, src.Inner), src.ExpectedResult + 1));
                    }

                    // make sure we didn't lose messages
                    branches[b] = branches[b].Do((d, e) => this.CheckMessageId(e.SequenceId + TransformCount, d.ExpectedResult, validateNoLoss));
                }
            }

            // join all
            var fullJoin = branches[0];
            for (int i = 1; i < ParallelBranchCount; i++)
            {
                var join = fullJoin.Join(branches[i], TimeSpan.MaxValue);
                fullJoin = join.Aggregate(new Wrap<T>(create(i), 0), (tgt, tpl) => new Wrap<T>(add(tgt.Inner, tpl.Item1.Inner, tpl.Item2.Inner), tpl.Item1.ExpectedResult + tpl.Item2.ExpectedResult));
            }

            // extract final result
            var result = fullJoin.Select(w => new Wrap<long>(extract(w.Inner), w.ExpectedResult));

            // validate result
            var final = result.Do((d, e) =>
            {
                resultCount++;
                this.CheckMessageId(e.SequenceId, resultCount, validateNoLoss);
                if (d.Inner != d.ExpectedResult)
                {
                    throw new Exception("Unexpected computation result.");
                }
            });

            // run the pipeline
            using (p)
            {
                var now = Time.GetCurrentTime();
                p.RunAsync(new ReplayDescriptor(now, now + this.duration));
                while (!p.WaitAll(1000))
                {
                    Console.WriteLine(resultCount);
                }
            }

            Console.WriteLine(resultCount);
            Assert.AreNotEqual(0, resultCount);
        }

        // DataFlow baseline. Similar CPU & throughput results, but memory keeps growing.
        public void RunDataFlowPipeline<T>(Func<int, T> create, Func<int, T, T> initialize, Func<T, T, T> increment, Func<T, T, T, T> add, Func<T, int> extract, bool validateNoLoss, bool validateSync)
        {
            int resultCount = 0;
            var dfo = new DataflowLinkOptions();
            dfo.Append = true;
            List<object> saved = new List<object>();

            // create several parallel branches of components
            var branches = new ISourceBlock<Wrap<T>>[ParallelBranchCount];
            var sources = new Time.TimerDelegate[SourceCount];
            for (int i = 0; i < SourceCount; i++)
            {
                // make a timer for each source
                var timerSeqId = 0;
                var timer = new TransformBlock<int, int>(ts => timerSeqId++);
                sources[i] = new Time.TimerDelegate((uint timerID, uint msg, UIntPtr userCtx, UIntPtr dw1, UIntPtr dw2) => timer.Post(i));
                saved.Add(timer);

                // branch and generate data
                for (int k = 0; k < ParallelBranchMultiplier; k++)
                {
                    int b = (i * ParallelBranchMultiplier) + k;
                    var initInst = new Wrap<T>(create(b), 0);
                    var init = new TransformBlock<int, Wrap<T>>(seqId => initInst = new Wrap<T>(initialize(seqId, initInst.Inner), seqId).DeepClone());
                    timer.LinkTo(init, dfo);
                    branches[b] = init;
                    saved.Add(init);

                    // apply a sequence of transforms
                    for (int j = 0; j < TransformCount; j++)
                    {
                        var incInst = new Wrap<T>(create(b), 0);
                        var inc = new TransformBlock<Wrap<T>, Wrap<T>>(src => incInst = new Wrap<T>(increment(incInst.Inner, src.Inner), src.ExpectedResult + 1).DeepClone());
                        branches[b].LinkTo(inc, dfo);
                        branches[b] = inc;
                        saved.Add(inc);
                    }

                    // make sure we didn't lose messages
                    // branches[b] = branches[b].DoT(m => CheckMessageId(m.SequenceId + TransformCount, m.Data.ExpectedResult, validateNoLoss), true, true);
                }
            }

            // join all
            var fullJoin = branches[0];
            for (int i = 1; i < ParallelBranchCount; i++)
            {
                var joinGo = new GroupingDataflowBlockOptions();
                joinGo.Greedy = false;
                var join = new JoinBlock<Wrap<T>, Wrap<T>>(joinGo);
                fullJoin.LinkTo(join.Target1, dfo);
                branches[i].LinkTo(join.Target2, dfo);
                var addInst = new Wrap<T>(create(i), 0);
                var select = new TransformBlock<Tuple<Wrap<T>, Wrap<T>>, Wrap<T>>(tpl => addInst = new Wrap<T>(add(addInst.Inner, tpl.Item1.Inner, tpl.Item2.Inner), tpl.Item1.ExpectedResult + tpl.Item2.ExpectedResult).DeepClone());
                join.LinkTo(select, dfo);
                fullJoin = select;
                saved.Add(join);
                saved.Add(select);
            }

            // extract final result
            var result = new TransformBlock<Wrap<T>, Wrap<long>>(w => new Wrap<long>(extract(w.Inner), w.ExpectedResult));
            fullJoin.LinkTo(result, dfo);
            saved.Add(result);

            // validate result
            int actionSeqId = 0;
            var final = new ActionBlock<Wrap<long>>(w =>
            {
                resultCount++;
                this.CheckMessageId(++actionSeqId, resultCount, validateNoLoss);
                if (w.Inner != w.ExpectedResult)
                {
                    throw new Exception("Unexpected computation result.");
                }
            });
            result.LinkTo(final, dfo);
            saved.Add(final);

            // run the pipeline
            for (int i = 0; i < SourceCount; i++)
            {
                Platform.Specific.TimerStart(1000 / this.frequency, sources[i]);
            }

            while (!final.Completion.Wait(1000))
            {
                Console.WriteLine(resultCount);
                if (sources.Length == 0)
                {
                    throw new Exception("This was here just to keep source alive in release mode, why did it hit?");
                }
            }

            Console.WriteLine("Stopped");
            Assert.AreNotEqual(0, resultCount);
        }

        private void CheckThread(int threadId, bool throwIfFalse)
        {
            if (throwIfFalse && Thread.CurrentThread.ManagedThreadId != threadId)
            {
                throw new Exception("This operation runs async when sync was expected.");
            }
        }

        private void CheckMessageId(int mid, int expected, bool throwIfFalse)
        {
            if (throwIfFalse && mid != expected)
            {
                throw new Exception($"Lost {expected - mid} messages out of {expected}.");
            }
        }

        private struct Wrap<T>
        {
            public T Inner;
            public int ThreadId;
            public int ExpectedResult;

            public Wrap(T inner, int expectedResult)
            {
                this.Inner = inner;
                this.ExpectedResult = expectedResult;
                this.ThreadId = Thread.CurrentThread.ManagedThreadId;
            }
        }
    }

    [Serializer(typeof(Immutable<>.CustomSerializer))]
    public class Immutable<T>
        where T : class
    {
        public T Value;

        public static implicit operator T(Immutable<T> source)
        {
            return source.Value;
        }

        public static implicit operator Immutable<T>(T source)
        {
            return new Immutable<T>() { Value = source };
        }

        private class CustomSerializer : ISerializer<Immutable<T>>
        {
            public int Version => 1;

            public bool? IsClearRequired => false;

            public TypeSchema Initialize(KnownSerializers serializers, TypeSchema targetSchema)
            {
                return null;
            }

            public void Clear(ref Immutable<T> target, SerializationContext context)
            {
            }

            public void Clone(Immutable<T> instance, ref Immutable<T> target, SerializationContext context)
            {
                target = instance;
            }

            public void Deserialize(BufferReader reader, ref Immutable<T> target, SerializationContext context)
            {
                Serializer.Deserialize(reader, ref target.Value, context);
            }

            public void PrepareCloningTarget(Immutable<T> instance, ref Immutable<T> target, SerializationContext context)
            {
            }

            public void PrepareDeserializationTarget(BufferReader reader, ref Immutable<T> target, SerializationContext context)
            {
            }

            public void Serialize(BufferWriter writer, Immutable<T> instance, SerializationContext context)
            {
                Serializer.Serialize(writer, instance.Value, context);
            }
        }
    }
}

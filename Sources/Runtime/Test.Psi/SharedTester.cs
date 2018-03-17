// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.

namespace Test.Psi
{
    using System;
    using System.Collections.Generic;
    using System.Diagnostics;
    using System.Threading;
    using Microsoft.Psi;
    using Microsoft.Psi.Common;
    using Microsoft.Psi.Serialization;
    using Microsoft.VisualStudio.TestTools.UnitTesting;

    [TestClass]
    public class SharedTester
    {
        [TestMethod]
        [Timeout(60000)]
        public void AddRefReleaseTest()
        {
            SharedPool<object> pool = new SharedPool<object>(1);

            // Verify that nothing is available (no objects have been allocated in the pool yet)
            Assert.AreEqual(pool.AvailableCount, 0);

            // Create our first object
            var origObj = pool.GetOrCreate(() => new int?(1234));
            Assert.AreEqual(pool.TotalCount, 1); // One object in pool
            Assert.AreEqual(pool.AvailableCount, 0); // None available for use (since only object is in use)

            // Take another reference on the object
            var refObj = origObj.AddRef();

            // Sanity check that underlying resource is the same
            Assert.AreEqual(refObj.Resource, 1234);
            Assert.AreEqual(origObj.Resource, 1234);

            // Get rid of our second reference. There should still be none available
            // in the pool since the refObj.inner hasn't been released yet
            refObj.Dispose();
            Assert.AreEqual(pool.AvailableCount, 0);

            // Next release the original object. Now everything should be released
            origObj.Dispose();
            Assert.AreEqual(pool.AvailableCount, 1);

            Assert.AreEqual(origObj.Inner, null);
        }

        [TestMethod]
        [Timeout(60000)]
        public void RefCountedTest()
        {
            var recycler = new SharedPool<UnmanagedBuffer>(1);
            var shared = recycler.GetOrCreate(() => UnmanagedBuffer.Allocate(100)); // refcount = 1

            // a private copy shoudl point to the same resource
            var otherShared = shared.DeepClone(); // refcount = 1 + 1
            Assert.AreNotEqual(shared, otherShared);
            Assert.AreEqual(shared.Inner, otherShared.Inner);
            Assert.AreEqual(shared.Resource, otherShared.Resource);

            // a clone should point to the same resource, but should not reuse the container
            var cloned = otherShared;
            Serializer.Clone(shared, ref cloned, new SerializationContext()); // refcount = 2 - 1 + 1
            Assert.AreNotEqual(shared, cloned);
            Assert.AreEqual(shared.Inner, cloned.Inner);
            Assert.AreEqual(shared.Resource, cloned.Resource);

            // disposing should not affect other copies
            shared.Dispose(); // refcount = 2 - 1
            Assert.AreEqual(0, recycler.AvailableCount);
            Assert.IsNull(shared.Inner);
            Assert.IsNull(shared.Resource);
            Assert.IsNotNull(cloned.Inner);
            Assert.IsNotNull(cloned.Resource);

            // disposing the last copy should return the resource to the pool
            cloned.Dispose(); // refcount = 1 - 1
            Assert.AreEqual(1, recycler.AvailableCount);
            Assert.IsNull(cloned.Inner);
            Assert.IsNull(cloned.Resource);
        }

        // [TestMethod, Timeout(60000)]
        public void RefCountedFinalizationTest()
        {
            var recycler = new SharedPool<UnmanagedBuffer>(1);
            var shared = recycler.GetOrCreate(() => UnmanagedBuffer.Allocate(100));
            var otherShared = shared.DeepClone();
            shared = null;

            // after GC and finalization, the live copy is not affected
            GC.Collect();
            GC.WaitForPendingFinalizers();
            Assert.AreNotEqual(IntPtr.Zero, otherShared.Resource.Data);
            otherShared = null;

            // after GC and finalization of all live copies, the resource goes back to the pool without being finalized itself
            GC.Collect();
            GC.WaitForPendingFinalizers();
            var wasRecycled = recycler.TryGet(out shared);
            Assert.IsTrue(wasRecycled);
            Assert.IsNotNull(shared.Resource);
            Assert.AreNotEqual(IntPtr.Zero, shared.Resource.Data);
        }

        [TestMethod]
        [Timeout(60000)]
        public void DoubleDispose()
        {
            var recycler = new SharedPool<UnmanagedBuffer>(1);
            var shared = recycler.GetOrCreate(() => UnmanagedBuffer.Allocate(100));
            shared.Dispose();
            try
            {
                shared.Dispose();
                Assert.Fail("Expected an exception from the second Dispose call");
            }
            catch (Exception e)
            {
                Assert.IsTrue(e is ObjectDisposedException);
            }
        }

        // this was used to measure the impact of finalizers and GC.SuppressFinalize calls
        [TestMethod]
        [Timeout(60000)]
        public void RecyclerPerf()
        {
            var recycler = new SharedPool<UnmanagedBuffer>(1);
            var shared = recycler.GetOrCreate(() => UnmanagedBuffer.Allocate(100));
            shared.Dispose();

            Stopwatch sw = Stopwatch.StartNew();
            int iterations = 10;
            for (int i = 0; i < iterations; i++)
            {
                recycler.TryGet(out shared);
                shared.Dispose();
            }

            sw.Stop();
            Console.WriteLine($"Get + Release = {sw.ElapsedMilliseconds * 1000000d / iterations} ns");
        }

        [TestMethod]
        [Timeout(60000)]
        public void SerializationClear()
        {
            Shared<byte[]> s = new Shared<byte[]>(new byte[10], null);
            Assert.AreNotEqual(null, s.Resource);
            var vt1 = Tuple.Create(10, s);
            var vt2 = Tuple.Create(11, s);
            var a = new Dictionary<int, Tuple<int, Shared<byte[]>>>();
            a.Add(0, vt1);
            a.Add(1, vt2);
            Assert.AreNotEqual(null, a[0].Item2);
            Assert.AreNotEqual(null, a[1].Item2);

            Serializer.Clear(ref a, new SerializationContext());
            Assert.AreEqual(null, s.Resource);
            Assert.AreEqual(null, a[0].Item2.Resource);
            Assert.AreEqual(null, a[1].Item2.Resource);

            Serializer.Clear(ref a, new SerializationContext());
        }

        [TestMethod]
        [Timeout(60000)]
        public void Serialize()
        {
            var buffer = new BufferWriter(100);
            var pool = new SharedPool<byte[]>(1);

            Shared<byte[]> s = pool.GetOrCreate(() => new byte[10]);
            s.Resource[0] = 255;
            s.Resource[9] = 128;

            Shared<byte[]> s2 = pool.GetOrCreate(() => new byte[10]);
            s2.Resource[0] = 1;
            s2.Resource[9] = 1;

            // serialize twice
            Serializer.Serialize(buffer, s, new SerializationContext());
            Serializer.Serialize(buffer, s2, new SerializationContext());
            Shared<byte[]> target = null;
            var reader = new BufferReader(buffer.Buffer);
            Serializer.Deserialize(reader, ref target, new SerializationContext());

            Assert.AreEqual(255, target.Resource[0]);
            Assert.AreEqual(0, target.Resource[1]);
            Assert.AreEqual(0, target.Resource[8]);
            Assert.AreEqual(128, target.Resource[9]);

            // deserialize again reusing the first instance, make sure the first instance is not trampled over
            var firstTarget = target.AddRef();
            Serializer.Deserialize(reader, ref target, new SerializationContext());
            Assert.IsFalse(object.ReferenceEquals(firstTarget, target));
            Assert.IsFalse(object.ReferenceEquals(firstTarget.Inner, target.Inner));
            Assert.IsFalse(object.ReferenceEquals(firstTarget.Resource, target.Resource));
            Assert.AreEqual(1, target.Resource[0]);
            Assert.AreEqual(0, target.Resource[1]);
            Assert.AreEqual(0, target.Resource[8]);
            Assert.AreEqual(1, target.Resource[9]);

            // this should not throw, since refcount should be 1 on both
            firstTarget.Dispose();
            target.Dispose();
        }

        [TestMethod]
        [Timeout(60000)]
        public void SerializeGraph()
        {
            var buffer = new BufferWriter(100);
            var pool = new SharedPool<byte[]>(1);

            Shared<byte[]> s = pool.GetOrCreate(() => new byte[10]);
            s.Resource[0] = 255;
            s.Resource[9] = 128;

            var t = Tuple.Create(s, s);
            Serializer.Serialize(buffer, t, new SerializationContext());
            Tuple<Shared<byte[]>, Shared<byte[]>> target = null;
            Serializer.Deserialize(new BufferReader(buffer.Buffer), ref target, new SerializationContext());

            Assert.ReferenceEquals(target.Item1, target.Item2);
            Assert.AreEqual(255, target.Item1.Resource[0]);
            Assert.AreEqual(0, target.Item1.Resource[1]);
            Assert.AreEqual(0, target.Item1.Resource[8]);
            Assert.AreEqual(128, target.Item1.Resource[9]);
            s.Dispose();
            target.Item1.Dispose();
        }

        [TestMethod]
        [Timeout(60000)]
        public void DeserializePooled()
        {
            const int iterations = 10;
            var writer = new BufferWriter(100);
            var pool = new SharedPool<byte[]>(1);

            using (var s = pool.GetOrCreate(() => new byte[10]))
            {
                for (int i = 0; i < iterations; i++)
                {
                    Serializer.Serialize(writer, s, new SerializationContext());
                }
            }

            // the array should be back in the pool
            Assert.AreEqual(1, pool.AvailableCount);
            Assert.AreEqual(1, pool.TotalCount);

            var reader = new BufferReader(writer.Buffer);
            var s2 = pool.GetOrCreate(() => throw new Exception("Expected a free entry in the pool!"));
            for (int i = 0; i < iterations; i++)
            {
                Serializer.Deserialize(reader, ref s2, new SerializationContext());

                // verify that the pool doesn't grow
                Assert.AreEqual(pool, s2.Recycler);
                Assert.AreEqual(0, pool.AvailableCount);
                Assert.AreEqual(1, pool.TotalCount);
            }

            s2.Dispose();

            // disposing the last deserialized shared should release the array back to the pool
            Assert.AreEqual(1, pool.AvailableCount);
        }

        [TestMethod]
        [Timeout(60000)]
        public void PipelineOfShared()
        {
            int sum = 0;
            using (var p = Pipeline.Create())
            {
                var pool = new SharedPool<int[]>(1);
                var g = Generators
                    .Range(p, 0, 10)
                    .Process<int, Shared<int[]>>(
                    (i, e, emitter) =>
                    {
                        using (var shared = pool.GetOrCreate(() => new int[1]))
                        {
                            shared.Resource[0] = i;
                            emitter.Post(shared, e.OriginatingTime);
                        }
                    });
                g.Select(a => a.Resource[0]).Do(v => sum = sum + v);
                p.Run();

                Assert.AreEqual(5 * 9, sum);
                Assert.IsTrue(pool.TotalCount > 0);
                Assert.AreEqual(pool.TotalCount, pool.AvailableCount);
            }
        }

        [TestMethod]
        [Timeout(60000)]
        public void JoinOfShared()
        {
            var sum = 0;
            using (var p = Pipeline.Create())
            {
                var pool = new SharedPool<int[]>(1);
                var g = Generators.Range(p, 0, 10);
                var s1 = g.Process<int, Shared<int[]>>(
                    (i, e, emitter) =>
                    {
                        using (var shared = pool.GetOrCreate(() => new int[1]))
                        {
                            shared.Resource[0] = i;
                            emitter.Post(shared, e.OriginatingTime);
                        }
                    });

                var j = s1.Join(g);
                j.Select(t => t.Item2 - t.Item1.Resource[0]).Do(v => sum = sum + v);
                p.Run();

                Assert.AreEqual(0, sum);
                Assert.IsTrue(pool.TotalCount > 0);
                Assert.AreEqual(pool.TotalCount, pool.AvailableCount);
            }
        }
    }
}

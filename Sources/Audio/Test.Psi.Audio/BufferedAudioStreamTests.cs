// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.

namespace Test.Psi.Audio
{
    using System.Threading.Tasks;
    using Microsoft.Psi.Audio;
    using Microsoft.VisualStudio.TestTools.UnitTesting;

    [TestClass]
    public class BufferedAudioStreamTests
    {
        [TestMethod]
        [Timeout(60000)]
        public void BufferedAudioStream_TestFill()
        {
            BufferedAudioStream bas = new BufferedAudioStream(3);
            byte[] data = new byte[] { 0, 1, 2 };
            bas.Write(data, 0, data.Length);
            Assert.AreEqual(3, bas.BytesAvailable);
            CollectionAssert.AreEqual(data, bas.Read());
            Assert.AreEqual(0, bas.BytesAvailable);
        }

        [TestMethod]
        [Timeout(60000)]
        public void BufferedAudioStream_TestBlockingRead()
        {
            BufferedAudioStream bas = new BufferedAudioStream(3);

            // This task should block until the write happens
            Task.Run(() =>
            {
                CollectionAssert.AreEqual(new byte[] { 0, 1, 2 }, bas.Read());
            });

            // We are writing more than the capacity, which should block the
            // write until the read frees up more space for it to complete.
            byte[] data = new byte[] { 0, 1, 2, 3, 4, 5 };
            bas.Write(data, 0, data.Length);

            Assert.AreEqual(3, bas.BytesAvailable);
            CollectionAssert.AreEqual(new byte[] { 3, 4, 5 }, bas.Read());
            Assert.AreEqual(0, bas.BytesAvailable);
        }

        [TestMethod]
        [Timeout(60000)]
        public void BufferedAudioStream_TestBlockingWrite()
        {
            BufferedAudioStream bas = new BufferedAudioStream(3);
            byte[] data = new byte[] { 0, 1, 2 };
            bas.Write(data, 0, data.Length);

            // This task should block until the read happens
            Task writeTask = Task.Run(() =>
            {
                byte[] moreData = new byte[] { 3, 4, 5 };
                bas.Write(moreData, 0, moreData.Length);
            });

            // Read in two chunks. The first chunk will be the data that was
            // pre-populated in the stream. Once that chunk is read the write
            // should unblock and the next chunk should get written to the stream.
            Assert.AreEqual(3, bas.BytesAvailable);
            CollectionAssert.AreEqual(new byte[] { 0, 1, 2 }, bas.Read());
            writeTask.Wait();
            Assert.AreEqual(3, bas.BytesAvailable);
            CollectionAssert.AreEqual(new byte[] { 3, 4, 5 }, bas.Read());
            Assert.AreEqual(0, bas.BytesAvailable);
        }
    }
}

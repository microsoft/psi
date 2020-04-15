// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.

namespace Test.Psi.Speech
{
    using System.Collections.Generic;
    using System.Linq;
    using Microsoft.Psi;
    using Microsoft.Psi.Audio;
    using Microsoft.Psi.Speech;
    using Microsoft.VisualStudio.TestTools.UnitTesting;

    [TestClass]
    public class SimpleVoiceActivityDetectorTester
    {
        [TestMethod]
        [Timeout(60000)]
        public void VoiceActivity_DetectFromFile()
        {
            // Initialize components and wire up pipeline.
            using (var pipeline = Pipeline.Create(nameof(this.VoiceActivity_DetectFromFile)))
            {
                var vad = new SimpleVoiceActivityDetector(pipeline, new SimpleVoiceActivityDetectorConfiguration());
                var audioInput = new WaveFileAudioSource(pipeline, "16kHz1chan.wav", null, 20);
                audioInput.PipeTo(vad);

                // Add results from outputs.
                var results = new List<bool>();
                vad.Out.Do(r => results.Add(r));

                // Run pipeline and wait for completion.
                pipeline.Run(null, false);

                Assert.IsTrue(results.Count > 0, "No results!");
                CollectionAssert.AreEqual(Enumerable.Repeat(false, 29).ToList(), results.GetRange(0, 29), "Initial silence detection failed!");
                CollectionAssert.AreEqual(Enumerable.Repeat(true, 182).ToList(), results.GetRange(29, 182), "Voice activity detection failed!");
                CollectionAssert.AreEqual(Enumerable.Repeat(false, 62).ToList(), results.GetRange(211, 62), "Trailing silence detection failed!");
            }
        }
    }
}

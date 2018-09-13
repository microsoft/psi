// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.

namespace Test.Psi.Speech.Windows
{
    using System.Collections.Generic;
    using System.Linq;
    using System.Speech.Recognition;
    using System.Threading;
    using Microsoft.Psi;
    using Microsoft.Psi.Audio;
    using Microsoft.Psi.Speech;
    using Microsoft.VisualStudio.TestTools.UnitTesting;

    [TestClass]
    public class SystemSpeechRecognizerTests
    {
        /// <summary>
        /// Test speech recognition of 16 kHz 1-channel PCM audio.
        /// </summary>
        [TestMethod]
        [Timeout(60000)]
        public void SystemSpeech_Recognize16kHz1Channel()
        {
            this.RecognizeSpeechFromWaveFile(@"TestFiles\16kHz1chan.wav", "the quick brown fox jumped over the lazy dog");
        }

        /// <summary>
        /// Test speech recognition of 44.1 kHz 2-channel PCM audio.
        /// </summary>
        [TestMethod]
        [Timeout(60000)]
        public void SystemSpeech_Recognize44kHz2Channel()
        {
            this.RecognizeSpeechFromWaveFile(@"TestFiles\44kHz2chan.wav", "i am looking for the kitchen");
        }

        /// <summary>
        /// Test speech recognition of 16 kHz 1-channel PCM audio with custom grammar update.
        /// </summary>
        [TestMethod]
        [Timeout(60000)]
        public void SystemSpeech_RecognizeWithGrammarUpdate()
        {
#pragma warning disable SA1118 // Parameter must not span multiple lines
            this.RecognizeSpeechFromWaveFile(
                @"TestFiles\16kHz1chan.wav",
                "de qwik braun vox chumps ober de lacy dock",
                "<grammar xml:lang=\"en-US\" root=\"root\" tag-format=\"properties-ms/1.0\" version=\"1.0\" xmlns=\"http://www.w3.org/2001/06/grammar\">" +
                    "<rule id=\"root\" scope=\"private\">" +
                        "<item repeat=\"0-10\">" +
                            "<one-of>" +
                                "<item>de</item>" +
                                "<item>qwik</item>" +
                                "<item>braun</item>" +
                                "<item>vox</item>" +
                                "<item>chumps</item>" +
                                "<item>ober</item>" +
                                "<item>lacy</item>" +
                                "<item>dock</item>" +
                            "</one-of>" +
                        "</item>" +
                    "</rule>" +
                "</grammar>");
#pragma warning restore SA1118 // Parameter must not span multiple lines
        }

        /// <summary>
        /// Tests speech recognition from any Wave file containing PCM audio in a format that the
        /// speech recognizer component accepts.
        /// </summary>
        /// <param name="filename">The Wave file containing the audio data.</param>
        /// <param name="expectedText">The expected recognized text.</param>
        /// <param name="srgsXmlGrammar">The grammar to use when decoding.</param>
        private void RecognizeSpeechFromWaveFile(string filename, string expectedText, string srgsXmlGrammar = null)
        {
            if (SpeechRecognitionEngine.InstalledRecognizers().Count == 0)
            {
                // Skip test if no installed recognizers on system.
                return;
            }

            // Read the WaveFormat from the file header so we can set the recognizer configuaration.
            WaveFormat format = WaveFileHelper.ReadWaveFileHeader(filename);

            // Initialize components and wire up pipeline.
            using (var pipeline = Pipeline.Create(nameof(this.RecognizeSpeechFromWaveFile)))
            {
                var recognizer = new SystemSpeechRecognizer(pipeline, new SystemSpeechRecognizerConfiguration() { BufferLengthInMs = 10000, InputFormat = format });
                var audioInput = new WaveFileAudioSource(pipeline, filename);
                audioInput.Out.PipeTo(recognizer.In);

                // Test dynamic update of speech recognition grammar
                if (srgsXmlGrammar != null)
                {
                    var grammarUpdater = Generators.Return<IEnumerable<string>>(pipeline, new string[] { srgsXmlGrammar });
                    grammarUpdater.PipeTo(recognizer.ReceiveGrammars);
                }

                // Add results from outputs. Note that we need to call DeepClone on each result as we
                // do not want them to be resused by the runtime.
                var results = new List<IStreamingSpeechRecognitionResult>();
                recognizer.Out.Do(r => results.Add(r.DeepClone()));
                recognizer.PartialRecognitionResults.Do(r => results.Add(r.DeepClone()));

                // Run pipeline and wait for completion.
                pipeline.Run();

                Assert.IsTrue(results.Count > 0, "No recognition results!");
                Assert.IsTrue(results.Count > 1, "No partial hypotheses!");

                // Verify partial results.
                for (int i = 0; i < results.Count - 1; ++i)
                {
                    var partialResult = results[i];
                    Assert.IsFalse(partialResult.IsFinal);
                    Assert.IsTrue(partialResult.Confidence.HasValue);
                    Assert.IsTrue(partialResult.Confidence.Value > 0);
                    Assert.IsFalse(string.IsNullOrEmpty(partialResult.Text));
                }

                // Verify final results.
                var finalResult = results.Last();
                Assert.IsTrue(finalResult.IsFinal);
                Assert.IsTrue(finalResult.Confidence.HasValue);
                Assert.IsTrue(finalResult.Confidence.Value > 0);
                Assert.AreEqual(expectedText, finalResult.Text, true);
                Assert.IsTrue(finalResult.Alternates.Length > 0);
                Assert.AreEqual(expectedText, finalResult.Alternates[0].Text, true);
                Assert.AreEqual(finalResult.Alternates[0].Confidence.Value, finalResult.Confidence.Value);
                Assert.IsTrue(finalResult.Audio.Length > 0);
            }
        }
    }
}

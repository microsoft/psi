// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.

namespace Test.Psi.CognitiveServices.Speech
{
    using Microsoft.Psi.CognitiveServices.Speech.Service;
    using Microsoft.VisualStudio.TestTools.UnitTesting;

    [TestClass]
    public class SpeechServiceTester
    {
        [TestMethod]
        [Timeout(60000)]
        public void SpeechService_TurnStartMessage()
        {
            var json = "X-RequestId:ad7c8760806b43eead0ccc43aa69ea59\r\n" +
                "Content-Type:application/json; charset=utf-8\r\n" +
                "Path:turn.start\r\n\r\n" +
                @"{
                    ""context"":
                    {
                        ""serviceTag"": ""b39ee6e388714c9dbf3fe7d08d386f7b""
                    }
                }";

            var message = SpeechServiceMessage.Deserialize(json);
            Assert.AreEqual("ad7c8760806b43eead0ccc43aa69ea59", message.RequestId);
            Assert.AreEqual("turn.start", message.Path);

            Assert.IsInstanceOfType(message, typeof(TurnStartMessage));
            var message2 = (TurnStartMessage)message;

            Assert.AreEqual("b39ee6e388714c9dbf3fe7d08d386f7b", message2.Context.ServiceTag);
        }

        [TestMethod]
        [Timeout(60000)]
        public void SpeechService_SpeechStartDetectedMessage()
        {
            var json = "X-RequestId:ad7c8760806b43eead0ccc43aa69ea59\r\n" +
                "Content-Type:application/json; charset=utf-8\r\n" +
                "Path:speech.startDetected\r\n\r\n" +
                @"{""Offset"":7600000}";

            var message = SpeechServiceMessage.Deserialize(json);
            Assert.AreEqual("ad7c8760806b43eead0ccc43aa69ea59", message.RequestId);
            Assert.AreEqual("speech.startDetected", message.Path);

            Assert.IsInstanceOfType(message, typeof(SpeechStartDetectedMessage));
            var message2 = (SpeechStartDetectedMessage)message;

            Assert.AreEqual(7600000, message2.Offset);
        }

        [TestMethod]
        [Timeout(60000)]
        public void SpeechService_SpeechHypothesisMessage()
        {
            var json = "X-RequestId:ad7c8760806b43eead0ccc43aa69ea59\r\n" +
                "Content-Type:application/json; charset=utf-8\r\n" +
                "Path:speech.hypothesis\r\n\r\n" +
                @"{""Text"":""to"",""Offset"":8100000,""Duration"":2000000}";

            var message = SpeechServiceMessage.Deserialize(json);
            Assert.AreEqual("ad7c8760806b43eead0ccc43aa69ea59", message.RequestId);
            Assert.AreEqual("speech.hypothesis", message.Path);

            Assert.IsInstanceOfType(message, typeof(SpeechHypothesisMessage));
            var message2 = (SpeechHypothesisMessage)message;

            Assert.AreEqual("to", message2.Text);
            Assert.AreEqual(8100000, message2.Offset);
            Assert.AreEqual(2000000, message2.Duration);
        }

        [TestMethod]
        [Timeout(60000)]
        public void SpeechService_SpeechEndDetectedMessage()
        {
            var json = "X-RequestId:ad7c8760806b43eead0ccc43aa69ea59\r\n" +
                "Content-Type:application/json; charset=utf-8\r\n" +
                "Path:speech.endDetected\r\n\r\n" +
                @"{""Offset"":39000000}";

            var message = SpeechServiceMessage.Deserialize(json);
            Assert.AreEqual("ad7c8760806b43eead0ccc43aa69ea59", message.RequestId);
            Assert.AreEqual("speech.endDetected", message.Path);

            Assert.IsInstanceOfType(message, typeof(SpeechEndDetectedMessage));
            var message2 = (SpeechEndDetectedMessage)message;

            Assert.AreEqual(39000000, message2.Offset);
        }

        [TestMethod]
        [Timeout(60000)]
        public void SpeechService_SpeechPhraseMessage()
        {
            var json = "X-RequestId:ad7c8760806b43eead0ccc43aa69ea59\r\n" +
                "Content-Type:application/json; charset=utf-8\r\n" +
                "Path:speech.phrase\r\n\r\n" +
                @"{
                    ""RecognitionStatus"":""Success"",
                    ""DisplayText"":""2345."",
                    ""Offset"":8100000,
                    ""Duration"":30200000,
                    ""NBest"":
                    [
                        {
                            ""Confidence"":0.9507294,
                            ""Lexical"":""two three four five"",
                            ""ITN"":""2345"",
                            ""MaskedITN"":""****"",
                            ""Display"":""2345.""
                        }
                    ]
                }";

            var message = SpeechServiceMessage.Deserialize(json);
            Assert.AreEqual("ad7c8760806b43eead0ccc43aa69ea59", message.RequestId);
            Assert.AreEqual("speech.phrase", message.Path);

            Assert.IsInstanceOfType(message, typeof(SpeechPhraseMessage));
            var message2 = (SpeechPhraseMessage)message;

            Assert.AreEqual("2345.", message2.DisplayText);
            Assert.AreEqual(30200000, message2.Duration);
            Assert.AreEqual(8100000, message2.Offset);
            Assert.AreEqual(RecognitionStatus.Success, message2.RecognitionStatus);
            Assert.AreEqual(1, message2.NBest.Length);
            Assert.AreEqual(0.9507294, message2.NBest[0].Confidence);
            Assert.AreEqual("2345.", message2.NBest[0].Display);
            Assert.AreEqual("2345", message2.NBest[0].ITN);
            Assert.AreEqual("two three four five", message2.NBest[0].Lexical);
            Assert.AreEqual("****", message2.NBest[0].MaskedITN);
        }

        [TestMethod]
        [Timeout(60000)]
        public void SpeechService_TurnEndMessage()
        {
            var json = "X-RequestId:ad7c8760806b43eead0ccc43aa69ea59\r\n" +
                "Content-Type:application/json; charset=utf-8\r\n" +
                "Path:turn.end\r\n\r\n" +
                @"{}";

            var message = SpeechServiceMessage.Deserialize(json);
            Assert.AreEqual("ad7c8760806b43eead0ccc43aa69ea59", message.RequestId);
            Assert.AreEqual("turn.end", message.Path);

            Assert.IsInstanceOfType(message, typeof(TurnEndMessage));
        }
    }
}

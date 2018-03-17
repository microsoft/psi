// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.

namespace Test.Psi.CognitiveServices.Language
{
    using Microsoft.Psi.CognitiveServices.Language;
    using Microsoft.Psi.Language;
    using Microsoft.VisualStudio.TestTools.UnitTesting;

    [TestClass]
    public class LUISTester
    {
        [TestMethod]
        [Timeout(60000)]
        public void LUIS_EmptyResponse()
        {
            var json = "{}";
            var intent = LUISIntentDetector.DeserializeIntentData(json);
            Assert.AreEqual(string.Empty, intent.Query);
            Assert.AreEqual(0, intent.Intents.Length);
            Assert.AreEqual(0, intent.Entities.Length);
        }

        [TestMethod]
        [Timeout(60000)]
        public void LUIS_Response()
        {
            var json = @"{
                ""query"": ""This is a query"",
                ""intents"": [
                    {
                        ""intent"": ""Intent_1"",
                        ""score"": 0.987654321 
                    },
                    {
                        ""intent"": ""Intent_2"",
                        ""score"": 0.123456789
                    }
                ],
                ""entities"": [
                    {
                        ""entity"": ""Entity_1"",
                        ""score"": 0.54321
                    },
                    {
                        ""entity"": ""Entity_2"",
                        ""score"": 0.12345
                    }
                ]}";

            var intent = LUISIntentDetector.DeserializeIntentData(json);
            Assert.AreEqual("This is a query", intent.Query);
            Assert.AreEqual(2, intent.Intents.Length);
            Assert.AreEqual(2, intent.Entities.Length);
            Assert.AreEqual("Intent_1", intent.Intents[0].Value);
            Assert.AreEqual(0.987654321, intent.Intents[0].Score);
            Assert.AreEqual("Intent_2", intent.Intents[1].Value);
            Assert.AreEqual(0.123456789, intent.Intents[1].Score);
            Assert.AreEqual("Entity_1", intent.Entities[0].Value);
            Assert.AreEqual(0.54321, intent.Entities[0].Score);
            Assert.AreEqual("Entity_2", intent.Entities[1].Value);
            Assert.AreEqual(0.12345, intent.Entities[1].Score);
        }
    }
}

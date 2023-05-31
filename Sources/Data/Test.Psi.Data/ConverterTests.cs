// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.

namespace Test.Psi.Data
{
    using System.IO;
    using System.Runtime.Serialization;
    using System.Text;
    using Microsoft.Psi.Data.Converters;
    using Microsoft.VisualStudio.TestTools.UnitTesting;
    using Newtonsoft.Json;

    [TestClass]
    public class ConverterTests
    {
        [TestMethod]
        [Timeout(60000)]
        public void RelativePathConvertTo()
        {
            var rootFile = "C:\\subdir1\\dataset.pds";

            // Test RelativePathConverter path conversion relative to rootFile
            var serializerSettings = new JsonSerializerSettings()
            {
                Context = new StreamingContext(StreamingContextStates.File, rootFile),
                Converters = new[] { new RelativePathConverter() },
            };

            var sb = new StringBuilder();
            var stringWriter = new StringWriter(sb);
            var serializer = JsonSerializer.Create(serializerSettings);

            serializer.Serialize(stringWriter, "file.txt");
            Assert.AreEqual("\"file.txt\"", sb.ToString());

            sb.Clear();
            serializer.Serialize(stringWriter, ".\\file.txt");
            Assert.AreEqual("\".\\\\file.txt\"", sb.ToString());

            sb.Clear();
            serializer.Serialize(stringWriter, "./file.txt");
            Assert.AreEqual("\".\\\\file.txt\"", sb.ToString());

            sb.Clear();
            serializer.Serialize(stringWriter, "subdir1\\file.txt");
            Assert.AreEqual("\"subdir1\\\\file.txt\"", sb.ToString());

            sb.Clear();
            serializer.Serialize(stringWriter, "subdir1\\subdir2\\file.txt");
            Assert.AreEqual("\"subdir1\\\\subdir2\\\\file.txt\"", sb.ToString());

            sb.Clear();
            serializer.Serialize(stringWriter, "C:\\file.txt");
            Assert.AreEqual("\"..\\\\file.txt\"", sb.ToString());

            sb.Clear();
            serializer.Serialize(stringWriter, "C:\\subdir3\\file.txt");
            Assert.AreEqual("\"..\\\\subdir3\\\\file.txt\"", sb.ToString());

            sb.Clear();
            serializer.Serialize(stringWriter, "\\subdir1\\file.txt");
            Assert.AreEqual("\"\\\\subdir1\\\\file.txt\"", sb.ToString());

            sb.Clear();
            serializer.Serialize(stringWriter, "\\\\networkpath\\share\\file.txt");
            Assert.AreEqual("\"\\\\\\\\networkpath\\\\share\\\\file.txt\"", sb.ToString());

            sb.Clear();
            serializer.Serialize(stringWriter, "C:\\subdir1\\subdir2\\file.txt");
            Assert.AreEqual("\"subdir2\\\\file.txt\"", sb.ToString());

            sb.Clear();
            serializer.Serialize(stringWriter, "D:\\subdir1\\subdir2\\file.txt");
            Assert.AreEqual("\"D:\\\\subdir1\\\\subdir2\\\\file.txt\"", sb.ToString());
        }

        [TestMethod]
        [Timeout(60000)]
        public void RelativePathConvertFrom()
        {
            var rootFile = "C:\\subdir1\\dataset.pds";

            // Test RelativePathConverter path conversion relative to rootFile
            var serializerSettings = new JsonSerializerSettings()
            {
                Context = new StreamingContext(StreamingContextStates.File, rootFile),
                Converters = new[] { new RelativePathConverter() },
            };

            var serializer = JsonSerializer.Create(serializerSettings);

            var stringReader = new StringReader("\"file.txt\"");
            Assert.AreEqual("C:\\subdir1\\file.txt", serializer.Deserialize(stringReader, typeof(string)));

            stringReader = new StringReader("\".\\\\file.txt\"");
            Assert.AreEqual("C:\\subdir1\\file.txt", serializer.Deserialize(stringReader, typeof(string)));

            stringReader = new StringReader("\"./file.txt\"");
            Assert.AreEqual("C:\\subdir1\\file.txt", serializer.Deserialize(stringReader, typeof(string)));

            stringReader = new StringReader("\"subdir1\\\\file.txt\"");
            Assert.AreEqual("C:\\subdir1\\subdir1\\file.txt", serializer.Deserialize(stringReader, typeof(string)));

            stringReader = new StringReader("\"subdir1\\\\subdir2\\\\file.txt\"");
            Assert.AreEqual("C:\\subdir1\\subdir1\\subdir2\\file.txt", serializer.Deserialize(stringReader, typeof(string)));

            stringReader = new StringReader("\"../file.txt\"");
            Assert.AreEqual("C:\\file.txt", serializer.Deserialize(stringReader, typeof(string)));

            stringReader = new StringReader("\"..\\\\subdir3\\\\file.txt\"");
            Assert.AreEqual("C:\\subdir3\\file.txt", serializer.Deserialize(stringReader, typeof(string)));

            stringReader = new StringReader("\"\\\\subdir1\\\\file.txt\"");
            Assert.AreEqual("C:\\subdir1\\file.txt", serializer.Deserialize(stringReader, typeof(string)));

            stringReader = new StringReader("\"\\\\\\\\networkpath\\\\share\\\\file.txt\"");
            Assert.AreEqual("\\\\networkpath\\share\\file.txt", serializer.Deserialize(stringReader, typeof(string)));

            stringReader = new StringReader("\"C:/subdir1/subdir2/file.txt\"");
            Assert.AreEqual("C:\\subdir1\\subdir2\\file.txt", serializer.Deserialize(stringReader, typeof(string)));

            stringReader = new StringReader("\"D:\\\\subdir1\\\\subdir2\\\\file.txt\"");
            Assert.AreEqual("D:\\subdir1\\subdir2\\file.txt", serializer.Deserialize(stringReader, typeof(string)));
        }
    }
}

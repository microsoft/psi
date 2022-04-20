// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.

namespace Test.Psi
{
    using System;
    using System.Collections;
    using System.Collections.Generic;
    using System.Diagnostics;
    using System.IO;
    using System.Linq;
    using System.Reflection;
    using Microsoft.Psi;
    using Microsoft.VisualStudio.TestTools.UnitTesting;
    using Test.Psi.Common;

    /// <summary>
    /// This file/class is included in both Test.Psi.csproj and Test.Psi.Windows.csproj to facilitate
    /// testing serialization across .NET frameworks (.NET Core and .NET Framework respectively). All
    /// tests in this class are available in both test projects. To generate a store in the "other"
    /// framework for testing, we invoke the <see cref="OtherTestExe"/> with the name of a method
    /// (preceded by a '!' since these methods do not have the [TestMethod] attribute) defined in
    /// this class which will create the store in the test folder. Note that these methods set the
    /// <see cref="cleanupTestFolder"/> flag to false on exit so that the test folder is not deleted
    /// and the store will be available to the current test method (it will be cleaned up after the
    /// test completes).
    /// </summary>
    [TestClass]
    public class CrossFrameworkSerializationTester
    {
        // Define path constants to the test executable for the other framework
#if DEBUG
        private static readonly string Configuration = "Debug";
#else
        private static readonly string Configuration = "Release";
#endif

        // Modify these preprocessor directives if the TargetFramework is changed in Test.Psi.csproj or Test.Psi.Windows.csproj
#if NETCOREAPP3_1
        private static readonly string OtherTestExe = Path.Combine(Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location), $@"..\..\..\..\Test.Psi.Windows\bin\{Configuration}\net472\Test.Psi.Windows.exe");
#elif NET472
        private static readonly string OtherTestExe = Path.Combine(Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location), $@"..\..\..\..\Test.Psi\bin\{Configuration}\netcoreapp3.1\Test.Psi.exe");
#endif

        // Folder in which to create stores for testing
        private readonly string testPath = Path.Combine(Environment.CurrentDirectory, nameof(CrossFrameworkSerializationTester));
        private bool cleanupTestFolder = true;

        [TestInitialize]
        public void Setup()
        {
            Directory.CreateDirectory(this.testPath);
        }

        [TestCleanup]
        public void Cleanup()
        {
            if (this.cleanupTestFolder)
            {
                TestRunner.SafeDirectoryDelete(this.testPath, true);
            }
        }

        /// <summary>
        /// Method that generates a store for cross-serialization tests.
        /// </summary>
        /// <remarks>
        /// This method will be invoked in a separate process by the <see cref="CrossFrameworkDeserialize"/>
        /// test method to generate a store to test deserialization across different .NET frameworks.
        /// </remarks>
        public void CrossFrameworkSerialize()
        {
            int intValue = 0x7777AAA;
            byte byteValue = 0xBB;
            bool boolValue = true;
            short shortValue = 0x7CDD;
            long longValue = 0x77777777EEEEEEEE;
            char charValue = 'G';
            string stringValue = "This is a test.";
            double doubleValue = Math.PI;
            float floatValue = -1.234f;
            float[] floatArray = new[] { 0.1f, 2.3f };
            List<string> stringList = new List<string> { "one", "two" };
            ArraySegment<string> stringArraySegment = new ArraySegment<string>(new[] { "aaa", "bbb", "ccc" }, 1, 2);
            Queue<TimeSpan> queue = new Queue<TimeSpan>(new[] { TimeSpan.Zero, TimeSpan.FromSeconds(1) });
            EqualityComparer<int> intComparer = EqualityComparer<int>.Default;
            Tuple<long, string> tuple = Tuple.Create(0x77777777EEEEEEEE, "This is a tuple.");
            (DateTime, Stack<int>) valueTuple = (new DateTime(2020, 1, 2), new Stack<int>(new[] { 33, 782 }));
            Array intArray = new[] { 0, 3 };
            ICollection stringArray = new[] { "three", "four" };
            IEqualityComparer enumComparer = EqualityComparer<DayOfWeek>.Default;
            Dictionary<string, int> dictionary = new Dictionary<string, int> { { "one", 1 }, { "two", 2 } };

            using (var p = Pipeline.Create())
            {
                var store = PsiStore.Create(p, "Store1", this.testPath);
                Generators.Return(p, intValue).Write("int", store);
                Generators.Return(p, byteValue).Write("byte", store);
                Generators.Return(p, boolValue).Write("bool", store);
                Generators.Return(p, shortValue).Write("short", store);
                Generators.Return(p, longValue).Write("long", store);
                Generators.Return(p, charValue).Write("char", store);
                Generators.Return(p, stringValue).Write("string", store);
                Generators.Return(p, doubleValue).Write("double", store);
                Generators.Return(p, floatValue).Write("float", store);
                Generators.Return(p, floatArray).Write("floatArray", store);
                Generators.Return(p, stringList).Write("stringList", store);
                Generators.Return(p, stringArraySegment).Write("stringArraySegment", store);
                Generators.Return(p, queue).Write("queue", store);
                Generators.Return(p, intComparer).Write("intComparer", store);
                Generators.Return(p, tuple).Write("tuple", store);
                Generators.Return(p, valueTuple).Write("dateStackTuple", store);
                Generators.Return(p, intArray).Write("intArray", store);
                Generators.Return(p, stringArray).Write("stringArray", store);
                Generators.Return(p, enumComparer).Write("enumComparer", store);
                Generators.Return(p, dictionary).Write("dictionary", store);

                p.Run();
            }

            // retain test store for cross-framework tests to run against after this process exits
            this.cleanupTestFolder = false;
        }

        [TestMethod]
        [Timeout(60000)]
        public void CrossFrameworkDeserialize()
        {
            if (!File.Exists(OtherTestExe))
            {
                Assert.Inconclusive($"Unable to locate {OtherTestExe} to generate the test store for {nameof(this.CrossFrameworkDeserializeMembers)}.");
            }

            this.ExecuteTest(OtherTestExe, $"!{nameof(this.CrossFrameworkSerialize)}");

            int intValue = 0;
            byte byteValue = 0;
            bool boolValue = false;
            short shortValue = 0;
            long longValue = 0;
            char charValue = '\0';
            string stringValue = null;
            double doubleValue = 0;
            float floatValue = 0;
            float[] floatArray = null;
            List<string> stringList = null;
            ArraySegment<string> stringArraySegment = default;
            Queue<TimeSpan> queue = null;
            EqualityComparer<int> intComparer = null;
            Tuple<long, string> tuple = null;
            (DateTime, Stack<int>) valueTuple = default;
            Array intArray = null;
            ICollection stringArray = null;
            IEqualityComparer enumComparer = null;
            Dictionary<string, int> dictionary = null;

            using (var p = Pipeline.Create())
            {
                var store = PsiStore.Open(p, "Store1", this.testPath);
                store.Serializers.Register<System.Drawing.Point>();
                store.OpenStream<int>("int").Do(x => intValue = x);
                store.OpenStream<byte>("byte").Do(x => byteValue = x);
                store.OpenStream<bool>("bool").Do(x => boolValue = x);
                store.OpenStream<short>("short").Do(x => shortValue = x);
                store.OpenStream<long>("long").Do(x => longValue = x);
                store.OpenStream<char>("char").Do(x => charValue = x);
                store.OpenStream<string>("string").Do(x => stringValue = x);
                store.OpenStream<double>("double").Do(x => doubleValue = x);
                store.OpenStream<float>("float").Do(x => floatValue = x);
                store.OpenStream<float[]>("floatArray").Do(x => floatArray = x.DeepClone());
                store.OpenStream<List<string>>("stringList").Do(x => stringList = x.DeepClone());
                store.OpenStream<ArraySegment<string>>("stringArraySegment").Do(x => stringArraySegment = x.DeepClone());
                store.OpenStream<Queue<TimeSpan>>("queue").Do(x => queue = x.DeepClone());
                store.OpenStream<EqualityComparer<int>>("intComparer").Do(x => intComparer = x.DeepClone());
                store.OpenStream<Tuple<long, string>>("tuple").Do(x => tuple = x.DeepClone());
                store.OpenStream<(DateTime, Stack<int>)>("dateStackTuple").Do(x => valueTuple = x.DeepClone());
                store.OpenStream<Array>("intArray").Do(x => intArray = x.DeepClone());
                store.OpenStream<ICollection>("stringArray").Do(x => stringArray = x.DeepClone());
                store.OpenStream<IEqualityComparer>("enumComparer").Do(x => enumComparer = x.DeepClone());
                store.OpenStream<Dictionary<string, int>>("dictionary").Do(x => dictionary = x.DeepClone());
                p.Run();
            }

            Assert.AreEqual(0x7777AAA, intValue);
            Assert.AreEqual(0xBB, byteValue);
            Assert.AreEqual(true, boolValue);
            Assert.AreEqual(0x7CDD, shortValue);
            Assert.AreEqual(0x77777777EEEEEEEE, longValue);
            Assert.AreEqual('G', charValue);
            Assert.AreEqual("This is a test.", stringValue);
            Assert.AreEqual(Math.PI, doubleValue);
            Assert.AreEqual(-1.234f, floatValue);
            CollectionAssert.AreEqual(new[] { 0.1f, 2.3f }, floatArray);
            CollectionAssert.AreEqual(new[] { "one", "two" }, stringList);
            CollectionAssert.AreEqual(new[] { "bbb", "ccc" }, stringArraySegment.ToArray());
            CollectionAssert.AreEqual(new[] { TimeSpan.Zero, TimeSpan.FromSeconds(1) }, queue);
            Assert.IsTrue(intComparer.Equals(1952, 1952));
            Assert.AreEqual(0x77777777EEEEEEEE, tuple.Item1);
            Assert.AreEqual("This is a tuple.", tuple.Item2);
            Assert.AreEqual(new DateTime(2020, 1, 2), valueTuple.Item1);
            CollectionAssert.AreEqual(new[] { 782, 33 }, valueTuple.Item2);
            CollectionAssert.AreEqual(new[] { 0, 3 }, intArray);
            CollectionAssert.AreEqual(new[] { "three", "four" }, stringArray);
            Assert.IsTrue(enumComparer.Equals(DayOfWeek.Friday, DayOfWeek.Friday));
            Assert.AreEqual(2, dictionary.Count);
            Assert.AreEqual(1, dictionary["one"]);
            Assert.AreEqual(2, dictionary["two"]);
        }

        public class TypeMembers
        {
            public int IntValue;
            public byte ByteValue;
            public bool BoolValue;
            public short ShortValue;
            public long LongValue;
            public char CharValue;
            public string StringValue;
            public double DoubleValue;
            public float FloatValue;
            public float[] FloatArray;
            public List<string> StringList;
            public ArraySegment<string> StringArraySegment;
            public Queue<TimeSpan> Queue;
            public EqualityComparer<int> IntComparer;
            public Tuple<long, string> Tuple;
            public (DateTime, Stack<int>) ValueTuple;
            public Array IntArray;
            public ICollection StringArray;
            public IEqualityComparer EnumComparer;
            public Dictionary<string, int> Dictionary;
        }

        /// <summary>
        /// Method that generates a store for cross-serialization tests.
        /// </summary>
        /// <remarks>
        /// This method will be invoked in a separate process by the <see cref="CrossFrameworkDeserializeMembers"/>
        /// test method to generate a store to test deserialization across different .NET frameworks.
        /// </remarks>
        public void CrossFrameworkSerializeMembers()
        {
            using (var p = Pipeline.Create())
            {
                var testObj = new TypeMembers
                {
                    IntValue = 0x7777AAA,
                    ByteValue = 0xBB,
                    BoolValue = true,
                    ShortValue = 0x7CDD,
                    LongValue = 0x77777777EEEEEEEE,
                    CharValue = 'G',
                    StringValue = "This is a test.",
                    DoubleValue = Math.PI,
                    FloatValue = -1.234f,
                    FloatArray = new[] { 0.1f, 2.3f },
                    StringList = new List<string> { "one", "two" },
                    StringArraySegment = new ArraySegment<string>(new[] { "aaa", "bbb", "ccc" }, 1, 2),
                    Queue = new Queue<TimeSpan>(new[] { TimeSpan.Zero, TimeSpan.FromSeconds(1) }),
                    IntComparer = EqualityComparer<int>.Default,
                    Tuple = Tuple.Create(0x77777777EEEEEEEE, "This is a tuple."),
                    ValueTuple = (new DateTime(2020, 1, 2), new Stack<int>(new[] { 33, 782 })),
                    IntArray = new[] { 0, 3 },
                    StringArray = new[] { "three", "four" },
                    EnumComparer = EqualityComparer<DayOfWeek>.Default,
                    Dictionary = new Dictionary<string, int> { { "one", 1 }, { "two", 2 } },
                };

                var store = PsiStore.Create(p, "Store2", this.testPath);
                Generators.Return(p, testObj).Write("TypeMembers", store);
                p.Run();
            }

            // retain test store for cross-framework tests to run against after this process exits
            this.cleanupTestFolder = false;
        }

        [TestMethod]
        [Timeout(60000)]
        public void CrossFrameworkDeserializeMembers()
        {
            if (!File.Exists(OtherTestExe))
            {
                Assert.Inconclusive($"Unable to locate {OtherTestExe} to generate the test store for {nameof(this.CrossFrameworkDeserializeMembers)}.");
            }

            this.ExecuteTest(OtherTestExe, $"!{nameof(this.CrossFrameworkSerializeMembers)}");

            TypeMembers result = null;

            using (var p = Pipeline.Create())
            {
                var store = PsiStore.Open(p, "Store2", this.testPath);
                store.OpenStream<TypeMembers>("TypeMembers").Do(x => result = x.DeepClone());
                p.Run();
            }

            Assert.AreEqual(0x7777AAA, result.IntValue);
            Assert.AreEqual(0xBB, result.ByteValue);
            Assert.AreEqual(true, result.BoolValue);
            Assert.AreEqual(0x7CDD, result.ShortValue);
            Assert.AreEqual(0x77777777EEEEEEEE, result.LongValue);
            Assert.AreEqual('G', result.CharValue);
            Assert.AreEqual("This is a test.", result.StringValue);
            Assert.AreEqual(Math.PI, result.DoubleValue);
            Assert.AreEqual(-1.234f, result.FloatValue);
            CollectionAssert.AreEqual(new[] { 0.1f, 2.3f }, result.FloatArray);
            CollectionAssert.AreEqual(new[] { "one", "two" }, result.StringList);
            CollectionAssert.AreEqual(new[] { "bbb", "ccc" }, result.StringArraySegment.ToArray());
            CollectionAssert.AreEqual(new[] { TimeSpan.Zero, TimeSpan.FromSeconds(1) }, result.Queue);
            Assert.IsTrue(result.IntComparer.Equals(1952, 1952));
            Assert.AreEqual("This is a tuple.", result.Tuple.Item2);
            Assert.AreEqual(new DateTime(2020, 1, 2), result.ValueTuple.Item1);
            CollectionAssert.AreEqual(new[] { 782, 33 }, result.ValueTuple.Item2);
            CollectionAssert.AreEqual(new[] { "three", "four" }, result.StringArray);
            Assert.IsTrue(result.EnumComparer.Equals(DayOfWeek.Friday, DayOfWeek.Friday));
            Assert.AreEqual(2, result.Dictionary.Count);
            Assert.AreEqual(1, result.Dictionary["one"]);
            Assert.AreEqual(2, result.Dictionary["two"]);
        }

        private void ExecuteTest(string testPath, string testMethod)
        {
            var procInfo = new ProcessStartInfo(testPath, testMethod);
            procInfo.UseShellExecute = false;
            procInfo.RedirectStandardInput = true;
            procInfo.RedirectStandardOutput = true;
            procInfo.RedirectStandardError = true;

            procInfo.WorkingDirectory = Environment.CurrentDirectory;
            var process = Process.Start(procInfo);

            // test runner expects a newline to terminate execution
            process.StandardInput.WriteLine();
            process.WaitForExit();
        }
    }
}

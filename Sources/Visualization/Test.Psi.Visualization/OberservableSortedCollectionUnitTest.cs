// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.

namespace Test.Psi.Visualization
{
    using System;
    using System.Collections.Generic;
    using System.Collections.Specialized;
    using Microsoft.Psi.Visualization;
    using Microsoft.VisualStudio.TestTools.UnitTesting;

    /// <summary>
    /// Observable sorted collection unit tests.
    /// </summary>
    [TestClass]
    public class OberservableSortedCollectionUnitTest
    {
        /// <summary>
        /// Basis sorted list for all tests.
        /// </summary>
        private SortedList<double, double> basis;

        /// <summary>
        /// Test observable sorted collection for all tests.
        /// </summary>
        private ObservableSortedCollection<double> test;

        /// <summary>
        /// Initialization for each unit test.
        /// </summary>
        [TestInitialize]
        public void Initialize()
        {
            this.basis = new SortedList<double, double>();

            var eventCount = 0;
            var itemCount = 0;
            NotifyCollectionChangedEventHandler testCollectionChanged = (s, e) => { this.CollectionChangedHandler(e, ref eventCount, ref itemCount); };
            this.test = new ObservableSortedCollection<double>();
            this.test.DetailedCollectionChanged += testCollectionChanged;

            double[] values = { 15, 65, -1, 2, 44, 100, 123, -456, 0, 10 };
            foreach (var value in values)
            {
                this.basis.Add(value, value);
                this.test.Add(value);
            }

            Assert.AreEqual(eventCount, values.Length);
            Assert.AreEqual(itemCount, values.Length);
            Assert.AreEqual(this.basis.Count, values.Length);
            UnitTestHelper.AssertAreEqual(this.basis.Values, this.test, values.Length);

            this.test.DetailedCollectionChanged -= testCollectionChanged;
        }

        /// <summary>
        /// Insert tests.
        /// </summary>
        [TestMethod]
        [Timeout(60000)]
        public void AddTest()
        {
            this.test.Add(6);
            this.basis.Add(6, 6);
            UnitTestHelper.AssertAreEqual(this.basis.Values, this.test, this.test.Count);
        }

        /// <summary>
        /// AddRange tests.
        /// </summary>
        [TestMethod]
        [Timeout(60000)]
        public void AddRangeTest()
        {
            double[] range = { 320, 45, 16, -19, 1, 12 };

            var eventCount = 0;
            var itemCount = 0;
            NotifyCollectionChangedEventHandler testCollectionChanged = (s, e) => { this.CollectionChangedHandler(e, ref eventCount, ref itemCount); };
            this.test.DetailedCollectionChanged += testCollectionChanged;

            this.test.AddRange(range);
            foreach (var item in range)
            {
                this.basis.Add(item, item);
            }

            Assert.AreEqual(eventCount, 1);
            Assert.AreEqual(itemCount, range.Length);
            Assert.AreEqual(this.basis.Count, this.test.Count);
            UnitTestHelper.AssertAreEqual(this.basis.Values, this.test, this.test.Count);

            this.test.DetailedCollectionChanged -= testCollectionChanged;
        }

        /// <summary>
        /// UpdateOrAdd tests.
        /// </summary>
        [TestMethod]
        [Timeout(60000)]
        public void UpdateOrAddTest()
        {
            var eventCount = 0;
            var itemCount = 0;
            NotifyCollectionChangedEventHandler testCollectionChanged = (s, e) => { this.CollectionChangedHandler(e, ref eventCount, ref itemCount); };
            this.test.DetailedCollectionChanged += testCollectionChanged;

            // Update pre-existing value
            this.test.UpdateOrAdd(44);
            Assert.AreEqual(eventCount, 1);
            Assert.AreEqual(itemCount, 1);
            Assert.AreEqual(this.basis.Count, this.test.Count);
            UnitTestHelper.AssertAreEqual(this.basis.Values, this.test, this.test.Count);

            eventCount = 0;
            itemCount = 0;

            // Add new value
            this.basis.Add(45, 45);
            this.test.UpdateOrAdd(45);
            Assert.AreEqual(eventCount, 1);
            Assert.AreEqual(itemCount, 1);
            Assert.AreEqual(this.basis.Count, this.test.Count);
            UnitTestHelper.AssertAreEqual(this.basis.Values, this.test, this.test.Count);

            this.test.DetailedCollectionChanged -= testCollectionChanged;
        }

        /// <summary>
        /// Clear tests.
        /// </summary>
        [TestMethod]
        [Timeout(60000)]
        public void ClearTest()
        {
            Assert.AreNotEqual(this.test.Count, 0);
            Assert.AreEqual(this.basis.Count, this.test.Count);
            this.test.Clear();
            this.basis.Clear();
            Assert.AreEqual(this.test.Count, 0);
            Assert.AreEqual(this.basis.Count, this.test.Count);
        }

        /// <summary>
        /// Contains tests.
        /// </summary>
        [TestMethod]
        [Timeout(60000)]
        public void ContainsTest()
        {
            Assert.IsTrue(this.test.Contains(44));
            Assert.AreEqual(this.basis.ContainsKey(44), this.test.Contains(44));
            Assert.IsFalse(this.test.Contains(45));
            Assert.AreEqual(this.basis.ContainsKey(45), this.test.Contains(45));
        }

        /// <summary>
        /// CopyTo tests.
        /// </summary>
        [TestMethod]
        [Timeout(60000)]
        public void CopyToTest()
        {
            var testArray = new double[10];
            var basisArray = new double[10];
            this.test.CopyTo(testArray, 0);
            this.basis.Values.CopyTo(basisArray, 0);
            Assert.AreEqual(testArray.Length, 10);
            for (var i = 0; i < testArray.Length; i++)
            {
                Assert.AreEqual(basisArray[i], testArray[i]);
            }

            var smallArray = new double[1];
            UnitTestHelper.TestException(() => this.test.CopyTo(smallArray, 0), typeof(ArgumentException));
            UnitTestHelper.TestException(() => this.basis.Values.CopyTo(smallArray, 0), typeof(ArgumentException));
        }

        /// <summary>
        /// Empty list tests.
        /// </summary>
        [TestMethod]
        [Timeout(60000)]
        public void EmptyTest()
        {
            var basisEmpty = new SortedList<double, double>();
            var testEmpty = new ObservableSortedCollection<double>();

            Assert.AreEqual(testEmpty.Count, 0);
            Assert.AreEqual(basisEmpty.Count, testEmpty.Count);
        }

        /// <summary>
        /// Enumerator tests.
        /// </summary>
        [TestMethod]
        [Timeout(60000)]
        public void EnumeratorTest()
        {
            using (var basisEnum = this.basis.GetEnumerator())
            using (var testEnum = this.test.GetEnumerator())
            {
                while (basisEnum.MoveNext())
                {
                    Assert.IsTrue(testEnum.MoveNext());
                    Assert.AreEqual(basisEnum.Current.Value, testEnum.Current);
                }

                Assert.IsFalse(testEnum.MoveNext());
            }
        }

        /// <summary>
        /// IndexOf tests.
        /// </summary>
        [TestMethod]
        [Timeout(60000)]
        public void IndexOfTest()
        {
            Assert.AreEqual(this.test.IndexOf(100), 8);
            Assert.AreEqual(this.basis.IndexOfKey(100), this.test.IndexOf(100));
            Assert.AreEqual(this.test.IndexOf(101), -1);
            Assert.AreEqual(this.basis.IndexOfKey(101), this.test.IndexOf(101));
        }

        /// <summary>
        /// Indexer tests.
        /// </summary>
        [TestMethod]
        [Timeout(60000)]
        public void IndexerTest()
        {
            Assert.AreEqual(this.test[5], 15);
            Assert.AreEqual(this.basis.Values[5], this.test[5]);
            UnitTestHelper.TestException(() => this.test[5] = 105, typeof(NotSupportedException));
            UnitTestHelper.TestException(() => this.basis.Values[5] = 105, typeof(NotSupportedException));

            double x = 0;
            UnitTestHelper.TestException(() => x = this.test[100], typeof(IndexOutOfRangeException));
            UnitTestHelper.TestException(() => x = this.basis.Values[100], typeof(ArgumentOutOfRangeException));
        }

        /// <summary>
        /// Remove tests.
        /// </summary>
        [TestMethod]
        [Timeout(60000)]
        public void RemoveTest()
        {
            this.test.RemoveAt(6);
            this.basis.RemoveAt(6);
            UnitTestHelper.AssertAreEqual(this.basis.Values, this.test, this.test.Count);

            UnitTestHelper.TestException(() => this.test.RemoveAt(100), typeof(ArgumentOutOfRangeException));
            UnitTestHelper.TestException(() => this.basis.RemoveAt(100), typeof(ArgumentOutOfRangeException));

            this.test.Remove(-456);
            this.basis.Remove(-456);
            UnitTestHelper.AssertAreEqual(this.basis.Values, this.test, this.test.Count);
        }

        /// <summary>
        /// RemoveRange tests.
        /// </summary>
        [TestMethod]
        [Timeout(60000)]
        public void RemoveRangeTest()
        {
            this.test.RemoveRange(3, 2);
            for (int i = 0; i < 2; i++)
            {
                this.basis.RemoveAt(3);
            }

            UnitTestHelper.AssertAreEqual(this.basis.Values, this.test, this.test.Count);

            this.test.RemoveRange(this.test.Count - 1, 1);
            this.basis.RemoveAt(this.basis.Count - 1);
            UnitTestHelper.AssertAreEqual(this.basis.Values, this.test, this.test.Count);

            UnitTestHelper.TestException(() => this.test.RemoveRange(this.test.Count - 1, 2), typeof(ArgumentOutOfRangeException));
            UnitTestHelper.TestException(() => this.test.RemoveRange(100, 1), typeof(ArgumentOutOfRangeException));
            UnitTestHelper.TestException(() => this.basis.RemoveAt(100), typeof(ArgumentOutOfRangeException));
        }

        /// <summary>
        /// Helper method for tracking changes to collections under test.
        /// </summary>
        /// <param name="e">Collection changed event arguments.</param>
        /// <param name="eventCount">Reference to the number of collection changed events received.</param>
        /// <param name="itemCount">Item count.</param>
        private void CollectionChangedHandler(NotifyCollectionChangedEventArgs e, ref int eventCount, ref int itemCount)
        {
            eventCount++;

            if (e.Action == NotifyCollectionChangedAction.Add)
            {
                itemCount += e.NewItems.Count;
            }
            else if (e.Action == NotifyCollectionChangedAction.Reset)
            {
                itemCount = this.test.Count - itemCount;
            }
            else if (e.Action == NotifyCollectionChangedAction.Replace)
            {
                itemCount = e.NewItems.Count;
            }
            else
            {
                throw new InvalidOperationException();
            }
        }
    }
}

// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.

namespace Test.Psi.Visualization
{
    using System;
    using System.Collections.Generic;
    using System.Collections.Specialized;
    using Microsoft.Psi.Visualization;
    using Microsoft.VisualStudio.TestTools.UnitTesting;

    [TestClass]
    public class ObservableKeyedCacheUnitTest
    {
        /// <summary>
        /// Basis sorted list for all tests.
        /// </summary>
        private SortedList<double, double> basis;

        /// <summary>
        /// Test observable keyed cache for all tests.
        /// </summary>
        private ObservableKeyedCache<double, double> test;

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
            this.test = new ObservableKeyedCache<double, double>((d) => d);
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
            var testEmpty = new ObservableKeyedCache<double, double>((d) => d);

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
        /// Get view tests.
        /// </summary>
        [TestMethod]
        [Timeout(60000)]
        public void GetViewTest()
        {
            var view1 = this.test.GetView(ObservableKeyedViewMode.Fixed, 2, 44, 0, null);
            Assert.IsInstanceOfType(view1, typeof(IReadOnlyList<double>));
            Assert.AreEqual(view1.Count, 3);

            var view2 = this.test.GetView(ObservableKeyedViewMode.Fixed, 2, 44, 0, null);
            UnitTestHelper.AssertAreEqual(view1, view2, view1.Count);
            Assert.AreEqual(view1, view2);

            var view3 = this.test.GetView(ObservableKeyedViewMode.Fixed, 1, 45, 0, null);
            Assert.AreEqual(view3.Count, 4);

            var view4 = this.test.GetView(ObservableKeyedViewMode.Fixed, -1000, 1000, 0, null);
            Assert.AreEqual(view4.Count, 10);

            var view5 = this.test.GetView(ObservableKeyedViewMode.Fixed, -456, 123.1, 0, null);
            Assert.AreEqual(view5.Count, 10);
            UnitTestHelper.AssertAreEqual(view4, view5, view4.Count);
            Assert.AreNotEqual(view4, view5);

            UnitTestHelper.TestException(
                () =>
                this.test.GetView(ObservableKeyedViewMode.Fixed, 100, -100, 0, null), typeof(ArgumentException));
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
        /// TryGetValue tests.
        /// </summary>
        [TestMethod]
        [Timeout(60000)]
        public void TryGetValueTest()
        {
            double value = 0;
            Assert.IsTrue(this.test.TryGetValue(100, out value));
            Assert.AreEqual(value, 100);
            Assert.IsTrue(this.test.TryGetValue(15, out value));
            Assert.AreEqual(value, 15);
            Assert.IsTrue(this.test.TryGetValue(10, out value));
            Assert.AreEqual(value, 10);
            Assert.IsFalse(this.test.TryGetValue(101, out value));
            Assert.AreEqual(value, 0);

            // Empty test
            var empty = new ObservableKeyedCache<double, double>((d) => d);
            Assert.IsFalse(empty.TryGetValue(0, out value));
        }

        /// <summary>
        /// View empty view tests.
        /// </summary>
        [TestMethod]
        [Timeout(60000)]
        public void ViewEmptyTest()
        {
            var view1 = this.test.GetView(ObservableKeyedViewMode.Fixed, 1, 2, 0, null);
            Assert.AreEqual(view1.Count, 0);

            var view2 = this.test.GetView(ObservableKeyedViewMode.Fixed, 1, 1, 0, null);
            Assert.AreEqual(view2.Count, 0);
        }

        /// <summary>
        /// View Enumerator tests.
        /// </summary>
        [TestMethod]
        [Timeout(60000)]
        public void ViewEnumeratorTest()
        {
            var view = this.test.GetView(ObservableKeyedViewMode.Fixed, 2, 44, 0, null);
            var array = new double[] { 2, 10, 15 };

            using (var viewEnumerator = view.GetEnumerator())
            {
                var testEnumerator = array.GetEnumerator();
                while (viewEnumerator.MoveNext())
                {
                    Assert.IsTrue(testEnumerator.MoveNext());
                    Assert.AreEqual(viewEnumerator.Current, testEnumerator.Current);
                }

                Assert.IsFalse(testEnumerator.MoveNext());

                viewEnumerator.Reset();
                Assert.AreEqual(viewEnumerator.Current, default(double));
            }
        }

        /// <summary>
        /// View IndexOf tests.
        /// </summary>
        [TestMethod]
        [Timeout(60000)]
        public void ViewIndexOfTest()
        {
            var view = this.test.GetView(ObservableKeyedViewMode.Fixed, 2, 44, 0, null);

            Assert.AreEqual(view.IndexOf(10), 1);
            Assert.AreEqual(view.IndexOf(44), -1);
        }

        /// <summary>
        /// View Indexer tests.
        /// </summary>
        [TestMethod]
        [Timeout(60000)]
        public void ViewIndexerTest()
        {
            var view = this.test.GetView(ObservableKeyedViewMode.Fixed, 2, 44, 0, null);

            Assert.AreEqual(view[1], 10);
            UnitTestHelper.TestException(() => view[2] = 105, typeof(NotSupportedException));

            double x = 0;
            UnitTestHelper.TestException(() => x = view[100], typeof(ArgumentOutOfRangeException));
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

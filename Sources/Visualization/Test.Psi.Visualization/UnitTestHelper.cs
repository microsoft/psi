// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.

namespace Test.Psi.Visualization
{
    using System;
    using System.Collections.Generic;
    using Microsoft.VisualStudio.TestTools.UnitTesting;

    /// <summary>
    /// Helper class that provides methods for helping unit test development, like handling
    /// expected exceptions and serialization of classes.
    /// </summary>
    internal static class UnitTestHelper
    {
        /// <summary>
        /// Assert that the basis collection and test collection have the same size and contents.
        /// </summary>
        /// <param name="basis">The basis collection.</param>
        /// <param name="test">The test collection.</param>
        /// <param name="size">The expected size of the collections.</param>
        /// <typeparam name="T">Type of list elements.</typeparam>
        public static void AssertAreEqual<T>(IList<T> basis, IList<T> test, int size)
        {
            Assert.AreEqual(test.Count, size);
            Assert.AreEqual(basis.Count, test.Count);
            for (var i = 0; i < test.Count; i++)
            {
                Assert.AreEqual(basis[i], test[i]);
            }
        }

        /// <summary>
        /// Assert that the basis collection and test collection have the same size and contents.
        /// </summary>
        /// <param name="basis">The basis collection.</param>
        /// <param name="test">The test collection.</param>
        /// <param name="size">The expected size of the collections.</param>
        /// <typeparam name="T">Type of list elements.</typeparam>
        public static void AssertAreEqual<T>(IReadOnlyList<T> basis, IReadOnlyList<T> test, int size)
        {
            Assert.AreEqual(test.Count, size);
            Assert.AreEqual(basis.Count, test.Count);
            for (var i = 0; i < test.Count; i++)
            {
                Assert.AreEqual(basis[i], test[i]);
            }
        }

        /// <summary>
        /// Tests method for an expected exception being thrown.
        /// </summary>
        /// <param name="action">Method under test.</param>
        /// <param name="exception">Expected exception.</param>
        public static void TestException(Action action, Type exception)
        {
            try
            {
                // Call the delegate, fail the test if it doesn't throw an exception.
                action();
                Assert.Fail("Test should have thrown exception " + exception.ToString() + ".");
            }
            catch (AssertFailedException)
            {
                // If it's a failed assert, then just rethrow because it's our failure above.
                throw;
            }
            catch (Exception e)
            {
                // Test that the method threw the exception that we were expecting.
                Assert.IsInstanceOfType(e, exception, "Exception is not of the expected type.", exception.ToString());
            }
        }
    }
}

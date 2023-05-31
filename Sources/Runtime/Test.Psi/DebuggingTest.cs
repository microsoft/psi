// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.

namespace Test.Psi
{
    using System;
    using System.Threading;
    using Microsoft.Psi;
    using Microsoft.VisualStudio.TestTools.UnitTesting;

    [TestClass]
    public class DebuggingTest
    {
        [TestMethod]
        [Timeout(60000)]
        public void DebugView()
        {
            DebugExtensions.EnableDebugViews();
            using (var p = Pipeline.Create())
            {
                var name = Generators.Sequence(p, new[] { 1, 2, 3 }, TimeSpan.FromTicks(1)).DebugView();
                Assert.IsNotNull(name);
            }

            DebugExtensions.DisableDebugViews();
        }
    }
}

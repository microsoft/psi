// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.

namespace Test.Psi.PsiStudio
{
    using Test.Psi.Common;

    /// <summary>
    /// Test runner to make debugging easier and faster.
    /// </summary>
    public class ConsoleMain
    {
        /// <summary>
        /// Entry point to make debugging easier and faster.
        /// </summary>
        /// <param name="args">Arguments (may be specific unit test names).</param>
        public static void Main(string[] args)
        {
            TestRunner.RunAll(args);
        }
    }
}

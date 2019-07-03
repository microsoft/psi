// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.

namespace Test.Psi.CognitiveServices.Language
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
        /// <param name="args">Command-line args.</param>
        public static void Main(string[] args)
        {
            TestRunner.RunAll(args);
        }
    }
}

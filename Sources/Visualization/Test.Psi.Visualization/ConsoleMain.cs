// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.

namespace Test.Psi.Visualization.Client
{
    using Test.Psi.Common;

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

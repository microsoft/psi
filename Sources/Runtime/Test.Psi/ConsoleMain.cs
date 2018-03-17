// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.

namespace Test.Psi
{
    using System;
    using System.IO;
    using System.Reflection;
    using Test.Psi.Common;

    public class ConsoleMain
    {
        /// <summary>
        /// Entry point to make debugging easier and faster.
        /// </summary>
        public static void Main(string[] args)
        {
            Environment.CurrentDirectory = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
            TestRunner.RunAll(args);
        }
    }
}

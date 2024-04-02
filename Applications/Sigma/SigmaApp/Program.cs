// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the Microsoft Research license.

namespace Sigma
{
    using Sigma.Diamond;

    /// <summary>
    /// Main program class.
    /// </summary>
    public static class Program
    {
        /// <summary>
        /// Main method.
        /// </summary>
        /// <param name="args">The list of arguments.</param>
        public static void Main(string[] args)
        {
            new SigmaApp([typeof(DiamondAppConfiguration)]).Run();
        }
    }
}

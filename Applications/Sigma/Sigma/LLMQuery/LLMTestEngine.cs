// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the Microsoft Research license.

namespace Sigma
{
    using System;
    using System.Collections.Generic;
    using System.Linq;

    /// <summary>
    /// Implements a test engine for LLM queries.
    /// </summary>
    public class LLMTestEngine
    {
        private readonly LLMQueryLibrary library;

        /// <summary>
        /// Initializes a new instance of the <see cref="LLMTestEngine"/> class.
        /// </summary>
        /// <param name="library">The LLM query library.</param>
        public LLMTestEngine(LLMQueryLibrary library)
        {
            this.library = library;
        }

        /// <summary>
        /// Runs all tests.
        /// </summary>
        public void RunAllTests()
        {
            foreach (var query in this.library.Queries)
            {
                Console.WriteLine($"Query: {query.Name} ({query.LLMPrompts.Sum(p => p.TestCases.Count)} test cases total)");
                foreach (var prompt in query.LLMPrompts)
                {
                    if (prompt.TestCases.Count > 0)
                    {
                        var correct = 0;
                        var total = 0;
                        var failedCases = new Dictionary<int, string>();
                        for (int i = 0; i < prompt.TestCases.Count; i++)
                        {
                            var testCase = prompt.TestCases[i];
                            var result = prompt.Run(testCase.ParameterValues).Result;
                            if (result.Trim().ToLower() == testCase.ExpectedResult.Trim().ToLower())
                            {
                                correct++;
                            }
                            else
                            {
                                failedCases.Add(i, result);
                            }

                            total++;
                        }

                        var accuracy = correct / (double)total;

                        Console.WriteLine($"  {prompt.Name}: {100 * accuracy:0.00}% [Min={100 * prompt.MinimumTestAccuracy:0.00}%]");

                        foreach (var index in failedCases.Keys)
                        {
                            Console.WriteLine($"  Fail: {failedCases[index]} != {prompt.TestCases[index].ExpectedResult}, for {string.Join(";", prompt.TestCases[index].ParameterValuesAsShortString)}");
                        }
                    }
                    else
                    {
                        Console.WriteLine("  No test cases.");
                    }
                }
            }
        }
    }
}

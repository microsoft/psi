// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.

namespace Test.Psi.Common
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Linq;
    using System.Reflection;
    using System.Threading;
    using System.Threading.Tasks;
    using Microsoft.VisualStudio.TestTools.UnitTesting;

    /// <summary>
    /// Test runner.
    /// </summary>
    public class TestRunner
    {
        public static readonly string TestResourcesPath = Environment.GetEnvironmentVariable("PsiTestResources");

        /// <summary>
        /// Runs the specified test cases , or all the tests in the current assembly if no arguments are specified.
        /// It can also run static methods that are not marked with the TestMethod attribute, if the method name is prefixed with "!" in the argument string (the "!" is dropped before searching for the method).
        /// </summary>
        /// <param name="args">Command-line arguments (e.g. individual test names).</param>
        public static void RunAll(string[] args)
        {
            if (args.Length > 0)
            {
                foreach (var arg in args)
                {
                    RunAll(arg);
                }
            }
            else
            {
               RunAll();
            }

            Console.WriteLine("Done.");
            Console.ReadLine();
        }

        /// <summary>
        /// Runs all the tests, just like the Test Explorer.
        /// </summary>
        /// <param name="nameSubstring">If specified, only the test suites containing this substring are executed.</param>
        public static void RunAll(string nameSubstring = "")
        {
            int passed = 0;
            int skipped = 0;
            var failed = new List<string>();

            var testClasses = AppDomain.CurrentDomain.GetAssemblies()
                .SelectMany(a => a.GetTypes())
                .Where(t => t.GetCustomAttributes().Any(a => a.GetType() == typeof(TestClassAttribute)));

            // allow running methods not marked with TestMethod attribute if the search string starts with "!"
            var isDefinitiveName = nameSubstring.StartsWith("!");
            if (isDefinitiveName)
            {
                nameSubstring = nameSubstring.Substring(1);
            }

            foreach (var testClass in testClasses)
            {
                var t = Activator.CreateInstance(testClass);

                IEnumerable<MethodInfo> allTestMethods = testClass.GetMethods();

                // allow running methods not marked with TestMethod attribute if the search string starts with "!"
                if (!isDefinitiveName)
                {
                    // by default, run only methods marked with TestMethod and not marked with Ignore.
                    allTestMethods = allTestMethods
                        .Where(m => m.GetCustomAttribute<TestMethodAttribute>() != null)
                        .Where(m => m.GetCustomAttribute<IgnoreAttribute>() == null);
                }

                var testMethods = allTestMethods
                    .Where(m => m.Name.Contains(nameSubstring) || m.DeclaringType.FullName.Contains(nameSubstring));

                skipped += allTestMethods.Count() - testMethods.Count();
                if (testMethods.Count() == 0)
                {
                    continue;
                }

                Console.WriteLine("------------------------------------------------------");
                Console.WriteLine("Test suite {0} ({1} test cases).", testClass.Name, testMethods.Count());
                Console.WriteLine("------------------------------------------------------");

                // run the test setup
                var setup = testClass.GetMethods().Where(m => m.GetCustomAttribute<TestInitializeAttribute>() != null).FirstOrDefault();
                if (setup != null)
                {
                    Console.Write("Test case init... ");
                    setup.Invoke(t, null);
                    Console.WriteLine("Done.");
                    Console.WriteLine();
                }

                // run all tests
                foreach (var m in testMethods)
                {
                    Console.WriteLine("Test case " + m.Name + ":");
                    Exception exception = null;
                    string error = null;
                    try
                    {
                        var result = m.Invoke(t, null) as Task;
                        if (result != null)
                        {
                            result.Wait();
                        }
                    }
                    catch (Exception e)
                    {
                        exception = e.InnerException;
                        error = exception.ToString();
                    }

                    // if the method expects this exception, consider the test passed
                    var expectedExceptionAttribute = m.GetCustomAttribute<ExpectedExceptionAttribute>();
                    if (expectedExceptionAttribute != null)
                    {
                        if (exception == null)
                        {
                            error = "Expected exception of type " + expectedExceptionAttribute.ExceptionType.Name;
                        }
                        else if (m.GetCustomAttribute<ExpectedExceptionAttribute>().ExceptionType == exception.GetType())
                        {
                            error = null;
                        }
                    }

                    if (error == null)
                    {
                        GC.Collect();
                        Console.WriteLine("Passed.");
                        passed++;
                    }
                    else
                    {
                        var color = Console.ForegroundColor;
                        Console.ForegroundColor = ConsoleColor.Red;
                        Console.WriteLine(exception);
                        Console.ForegroundColor = color;
                        Console.WriteLine("Failed.");
                        failed.Add(testClass.Name + "." + m.Name);
                    }

                    Console.WriteLine();
                }

                // run the test setup
                var cleanup = testClass.GetMethods().Where(m => m.GetCustomAttribute<TestCleanupAttribute>() != null).FirstOrDefault();
                if (cleanup != null)
                {
                    Console.Write("Test case cleanup... ");
                    cleanup.Invoke(t, null);
                    Console.WriteLine("Done.");
                    Console.WriteLine();
                }
            }

            if (passed >= 0)
            {
                var color = Console.ForegroundColor;
                Console.ForegroundColor = ConsoleColor.Green;
                Console.WriteLine("{0} tests passed.", passed);
                Console.ForegroundColor = color;
            }

            if (skipped > 0)
            {
                var color = Console.ForegroundColor;
                Console.ForegroundColor = ConsoleColor.Yellow;
                Console.WriteLine("{0} tests skipped.", skipped);
                Console.ForegroundColor = color;
            }

            if (failed.Count > 0)
            {
                var color = Console.ForegroundColor;
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine("{0} tests failed.", failed.Count);

                foreach (var n in failed)
                {
                    Console.WriteLine("\t" + n);
                }

                Console.ForegroundColor = color;
            }
        }

        /// <summary>
        /// Due to the runtime's asynchronous behavior, we may try to delete our test directory
        /// before the runtime has finished updating it. This method will keep trying to delete
        /// the directory until the runtime shuts down.
        /// </summary>
        /// <param name="path">The path to the Directory to be deleted.</param>
        /// <param name="recursive">Delete all subdirectories and files.</param>
        public static void SafeDirectoryDelete(string path, bool recursive)
        {
            for (int iteration = 0; iteration < 10; iteration++)
            {
                try
                {
                    Directory.Delete(path, recursive);
                    return;
                }
                catch (UnauthorizedAccessException)
                {
                    // Something in the directory is probably still being
                    // accessed by the process under test, so try again shortly.
                    Thread.Sleep(200);
                }
            }

            throw new ApplicationException(string.Format("Unable to delete directory \"{0}\" after multiple attempts", path));
        }

        /// <summary>
        /// Due to the asynchronous behavior when disposing file readers and writers in the
        /// runtime, we may try to delete our test files before they have finished closing.
        /// This method will keep trying to delete the file for a limited number of attempts.
        /// </summary>
        /// <param name="path">The path to the file to be deleted.</param>
        public static void SafeFileDelete(string path)
        {
            for (int iteration = 0; iteration < 10; iteration++)
            {
                try
                {
                    File.Delete(path);
                    return;
                }
                catch (UnauthorizedAccessException)
                {
                    // The file has probably not finished closing, so try again shortly.
                    Thread.Sleep(200);
                }
            }

            throw new ApplicationException(string.Format("Unable to delete file \"{0}\" after multiple attempts", path));
        }
    }
}

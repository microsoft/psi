// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.

namespace Microsoft.Psi
{
    using System;
    using System.Collections.Generic;
    using System.Diagnostics;
    using System.Linq;
    using System.Security;
    using System.Security.Principal;

    /// <summary>
    /// Provides methods for creating and updating the performance counters.
    /// </summary>
    internal static class PerfCounterManager
    {
        /// <summary>
        /// The counter categories we know about.
        /// </summary>
        private static Dictionary<string, Dictionary<int, CounterCreationData>> categories = new Dictionary<string, Dictionary<int, CounterCreationData>>();

        /// <summary>
        /// The runtime counters created in the process.
        /// </summary>
        private static Dictionary<string, Dictionary<string, Dictionary<int, PerformanceCounter>>> counters = new Dictionary<string, Dictionary<string, Dictionary<int, PerformanceCounter>>>();

        /// <summary>
        /// The process name used to name the counter instances so that multiple processes can each have their own counters.
        /// </summary>
        private static string processName;

        static PerfCounterManager()
        {
            // to make debugging easier, only append the process ID if there is more than one process with the same name
            processName = Process.GetCurrentProcess().ProcessName;
            if (Process.GetProcessesByName(processName).Length != 1)
            {
                processName = processName + Process.GetCurrentProcess().Id;
            }
        }

        /// <summary>
        /// Installs the performance counters. Requires admin privileges.
        /// </summary>
        /// <param name="categoryName">The name of the category to create.</param>
        /// <param name="counterDefinitions">The set of counter definitions.</param>
        /// <returns>True if the counters were installed.</returns>
        public static bool TrySetupCounters(string categoryName, Dictionary<int, CounterCreationData> counterDefinitions)
        {
            bool found = false;

            try
            {
                if (PerformanceCounterCategory.Exists(categoryName))
                {
                    found = true;

                    // make sure we have enough perms to update the counters
                    WindowsPrincipal principal = new WindowsPrincipal(WindowsIdentity.GetCurrent());
                    if (!principal.IsInRole(WindowsBuiltInRole.Administrator))
                    {
                        return found;
                    }

                    PerformanceCounterCategory.Delete(categoryName);
                    Console.WriteLine("\tPerformance counters {0} removed.", categoryName);
                }

                CounterCreationDataCollection collection = new CounterCreationDataCollection(counterDefinitions.OrderBy(kv => kv.Key).Select(kv => kv.Value).ToArray());
                PerformanceCounterCategory.Create(categoryName, "Microsoft Psi Runtime performance counters", PerformanceCounterCategoryType.MultiInstance, collection);
                Console.WriteLine("\tPerformance counters {0} installed.", categoryName);
                return true;
            }
            catch (SecurityException)
            {
            }
            catch (UnauthorizedAccessException)
            {
            }

            return found;
        }

        /// <summary>
        /// Add counter definitions.
        /// </summary>
        /// <param name="categoryName">The name of the category.</param>
        /// <param name="counterDefinitions">The set of counter definitions.</param>
        internal static void AddCounterDefinitions(string categoryName, Dictionary<int, CounterCreationData> counterDefinitions)
        {
            if (categories.ContainsKey(categoryName))
            {
                return;
            }

            try
            {
                if (!TrySetupCounters(categoryName, counterDefinitions))
                {
                    return;
                }
            }
            catch (UnauthorizedAccessException)
            {
                return;
            }

            categories.Add(categoryName, counterDefinitions);
            counters[categoryName] = new Dictionary<string, Dictionary<int, PerformanceCounter>>();
        }

        /// <summary>
        /// Creates all the counters for the specified instance.
        /// </summary>
        /// <param name="categoryName">Name of the group to add an instance to.</param>
        /// <param name="instanceName">The name of the instance to add.</param>
        /// <returns>The counters for the specified instance.</returns>
        /// <typeparam name="T">An enum type to use for identifying the counters.</typeparam>
        internal static Dictionary<T, PerformanceCounter> AddInstance<T>(string categoryName, string instanceName)
            where T : struct
        {
            if (!counters.ContainsKey(categoryName))
            {
                return null;
            }

            string processQualifiedInstanceName = GetProcessQualifiedInstanceName(instanceName);
            Dictionary<int, PerformanceCounter> instanceCounters = new Dictionary<int, PerformanceCounter>(categories[categoryName].Count);
            foreach (KeyValuePair<int, CounterCreationData> def in categories[categoryName])
            {
                PerformanceCounter counter = new PerformanceCounter();
                counter.InstanceLifetime = PerformanceCounterInstanceLifetime.Process;
                counter.CategoryName = categoryName;
                counter.InstanceName = processQualifiedInstanceName;
                counter.CounterName = def.Value.CounterName;
                counter.ReadOnly = false;
                instanceCounters[def.Key] = counter;
            }

            counters[categoryName][processQualifiedInstanceName] = instanceCounters;
            return GetCounters<T>(categoryName, instanceName);
        }

        /// <summary>
        /// Retrieves the counters based on instance name.
        /// </summary>
        /// <param name="categoryName">Name of the group to add an instance to.</param>
        /// <param name="instanceName">The name of the counter instance.</param>
        /// <returns>The corresponding counters.</returns>
        /// <typeparam name="T">An enum type to use in identifying the counters.</typeparam>
        internal static Dictionary<T, PerformanceCounter> GetCounters<T>(string categoryName, string instanceName)
            where T : struct
        {
            if (!counters.ContainsKey(categoryName))
            {
                return null;
            }

            string processQualifiedInstanceName = GetProcessQualifiedInstanceName(instanceName);
            Dictionary<T, PerformanceCounter> typedCounters = new Dictionary<T, PerformanceCounter>(counters.Count);
            foreach (KeyValuePair<int, PerformanceCounter> counterPair in counters[categoryName][processQualifiedInstanceName])
            {
                typedCounters[(T)(object)counterPair.Key] = counterPair.Value;
            }

            return typedCounters;
        }

        /// <summary>
        /// Failure to create the performance counter category (security restriction).
        /// </summary>
        private static void CounterCreationFailed()
        {
            Debug.WriteLine("Performance counters could not be created. You need to run the application as an administrator at least once.");
            counters = null;
        }

        /// <summary>
        /// Appends the process ID to the instance name.
        /// </summary>
        /// <param name="instanceName">The counter instance name.</param>
        /// <returns>A unique instance name to be used when setting up the counters.</returns>
        private static string GetProcessQualifiedInstanceName(string instanceName)
        {
            return string.Format("{0} ({1})", instanceName, processName);
        }
    }
}

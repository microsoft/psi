// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the Microsoft Research license.

namespace Sigma
{
    using System;
    using System.IO;
    using System.Linq;
    using MathNet.Numerics;
    using Microsoft.Psi;
    using Microsoft.Psi.MixedReality.Applications;

    /// <summary>
    /// Implements the compute server for the Sigma app.
    /// </summary>
    public static class SigmaComputeServer
    {
        private static readonly string ServerConfigPath = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments),
            "Sigma",
            Environment.MachineName);

        private static readonly string ServerConfigFilename = Path.Combine(
            ServerConfigPath,
            "sigma.server.config.xml");

        private static readonly string ServerLogFilename = Path.Combine(
            ServerConfigPath,
            "sigma.serverlog.txt");

        /// <summary>
        /// Runs the live compute server.
        /// </summary>
        /// <param name="availableConfigurationTypes">The types of configurations available.</param>
        public static void RunLive(Type[] availableConfigurationTypes)
        {
            // Create the configuration folder if it doesn't exist
            if (!Directory.Exists(ServerConfigPath))
            {
                Directory.CreateDirectory(ServerConfigPath);
            }

            // Set up the default configuration
            var defaultConfiguration = new SigmaLiveComputeServerConfiguration
            {
                ServerLogFilename = ServerLogFilename,
                PipelineConfigurations = availableConfigurationTypes
                    .Select(t => Activator.CreateInstance(t, ConfigurationType.Template) as SigmaComputeServerPipelineConfiguration)
                    .ToArray(),
            };

            var configuration = ConfigurationHelper.ReadFromFileOrDefault(
                ServerConfigFilename,
                defaultConfiguration,
                createFileIfNotExist: true,
                extraTypes: availableConfigurationTypes);

            // Also write the default configuration file template (this is helpful if the configuration
            // type changes to see the full template
            ConfigurationHelper.WriteToFile(ServerConfigFilename + ".template", defaultConfiguration, extraTypes: availableConfigurationTypes);

            // Set up the log file for the application
            AppConsole.LogFilename = configuration.ServerLogFilename;

            AppConsole.WriteLine();
            AppConsole.WriteLine($"========================================================");
            AppConsole.WriteLine($"STARTING SIGMA COMPUTE SERVER @ {DateTime.Now:MM/dd/yyyy HH:mm:ss.ffff}");
            AppConsole.WriteLine($"========================================================");

            if (Control.TryUseNativeMKL())
            {
                AppConsole.WriteLine("Using Native MKL.");
            }

            Console.WriteLine(Control.Describe());

            // Run the live compute server which will listen for requests from the client app
            // and run the appropriate compute pipeline based on the rendezvous process name
            // from the client app.
            SigmaLiveComputeServer.Run(configuration);
        }

        /// <summary>
        /// Runs the compute server in batch mode for a specified store and configuration.
        /// </summary>
        /// <param name="availableConfigurationTypes">The types of configurations available.</param>
        /// <param name="configurationName">The name of the configuration to run.</param>
        /// <param name="inputStorePath">The path to the input store.</param>
        /// <param name="outputStoreName">The name of the output store.</param>
        public static void RunBatch(Type[] availableConfigurationTypes, string configurationName, string inputStorePath, string outputStoreName = "ReRun")
        {
            // Set up the default configuration
            var defaultConfiguration = new SigmaLiveComputeServerConfiguration
            {
                ServerLogFilename = ServerLogFilename,
                PipelineConfigurations = availableConfigurationTypes.Select(t => Activator.CreateInstance(t) as SigmaComputeServerPipelineConfiguration).ToArray(),
            };

            // Read the server configuration
            var configuration = ConfigurationHelper.ReadFromFileOrDefault(
                ServerConfigFilename,
                defaultConfiguration,
                createFileIfNotExist: true,
                extraTypes: availableConfigurationTypes);

            if (Control.TryUseNativeMKL())
            {
                AppConsole.WriteLine("Using Native MKL.");
            }

            Console.WriteLine(Control.Describe());

            // Run the catch compute server
            var pipelineConfiguration = configuration.PipelineConfigurations.FirstOrDefault(c => c.Name == configurationName);
            if (pipelineConfiguration == null)
            {
                Console.WriteLine($"Cannot find configuration with name {configurationName}");
            }
            else
            {
                SigmaBatchComputeServer.ReRun(inputStorePath, pipelineConfiguration, outputStoreName);
            }
        }

        /// <summary>
        /// Runs the compute server in batch mode for a specified store and configuration.
        /// </summary>
        /// <param name="configuration">The compute server pipeline configuration.</param>
        /// <param name="inputStorePath">The path to the input store.</param>
        /// <param name="outputStoreName">The name of the output store.</param>
        public static void RunBatch(SigmaComputeServerPipelineConfiguration configuration, string inputStorePath, string outputStoreName = "ReRun")
        {
            if (Control.TryUseNativeMKL())
            {
                AppConsole.WriteLine("Using Native MKL.");
            }

            Console.WriteLine(Control.Describe());

            // Run the catch compute server
            SigmaBatchComputeServer.ReRun(inputStorePath, configuration, outputStoreName);
        }
    }
}

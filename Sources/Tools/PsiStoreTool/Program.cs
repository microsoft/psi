// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.

namespace PsiStoreTool
{
    using System;
    using System.Collections.Generic;
    using CommandLine;

    /// <summary>
    /// Psi store command-line tool.
    /// </summary>
    public class Program
    {
        /// <summary>
        /// Display command-line parser errors.
        /// </summary>
        /// <param name="errors">Errors reported.</param>
        /// <returns>Success flag.</returns>
        private static int DisplayParseErrors(IEnumerable<Error> errors)
        {
            Console.WriteLine("Errors:");
            var ret = 0;
            foreach (var error in errors)
            {
                Console.WriteLine($"{error}");
                if (error.StopsProcessing)
                {
                    ret = 1;
                }
            }

            return ret;
        }

        private static int Main(string[] args)
        {
            Console.WriteLine($"Platform for Situated Intelligence Store Tool");
            try
            {
                return Parser.Default.ParseArguments<Verbs.ListStreams, Verbs.Info, Verbs.RemoveStream, Verbs.Messages, Verbs.Save, Verbs.Send, Verbs.Concat, Verbs.Crop, Verbs.Encode, Verbs.ListTasks, Verbs.Exec, Verbs.AnalyzeStreams>(args)
                    .MapResult(
                        (Verbs.ListStreams opts) => Utility.ListStreams(opts.Store, opts.Path, opts.ShowSize),
                        (Verbs.Info opts) => Utility.DisplayStreamInfo(opts.Stream, opts.Store, opts.Path),
                        (Verbs.RemoveStream opts) => Utility.RemoveStream(opts.Stream, opts.Store, opts.Path),
                        (Verbs.Messages opts) => Utility.DisplayStreamMessages(opts.Stream, opts.Store, opts.Path, opts.Number),
                        (Verbs.Save opts) => Utility.SaveStreamMessages(opts.Stream, opts.Store, opts.Path, opts.File, opts.Format),
                        (Verbs.Send opts) => Utility.SendStreamMessages(opts.Stream, opts.Store, opts.Path, opts.Topic, opts.Address, opts.Format),
                        (Verbs.Concat opts) => Utility.ConcatenateStores(opts.Store, opts.Path, opts.Output, opts.OutputPath),
                        (Verbs.Crop opts) => Utility.CropStore(opts.Store, opts.Path, opts.Output, opts.OutputPath, opts.Start, opts.Length),
                        (Verbs.Encode opts) => Utility.EncodeStore(opts.Store, opts.Path, opts.Output, opts.OutputPath, opts.Quality),
                        (Verbs.ListTasks opts) => Utility.ListTasks(opts.Assemblies),
                        (Verbs.Exec opts) => Utility.ExecuteTask(opts.Stream, opts.Store, opts.Path, opts.Name, opts.Assemblies, opts.Arguments),
                        (Verbs.AnalyzeStreams opts) => Utility.AnalyzeStreams(opts.Store, opts.Path, opts.Order),
                        DisplayParseErrors);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error: {ex.Message}");
                return 1;
            }
        }
    }
}

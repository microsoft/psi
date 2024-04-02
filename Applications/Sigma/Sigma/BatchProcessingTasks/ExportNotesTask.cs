// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the Microsoft Research license.

namespace Sigma
{
    using System.Collections.Generic;
    using System.ComponentModel;
    using System.IO;
    using System.Linq;
    using System.Runtime.Serialization;
    using HoloLensCaptureInterop;
    using Microsoft.Psi;
    using Microsoft.Psi.Data;
    using Microsoft.Psi.Speech;

    /// <summary>
    /// Batch task for exporting Sigma data.
    /// </summary>
    [BatchProcessingTask(
        "Sigma - Export User Notes",
        Description = "This task exports notes taken by the user to a tab-delimited file.")]
    public class ExportNotesTask : BatchProcessingTask<ExportNotesTask.ExportNotesTaskConfiguration>
    {
        // A list of stream writers to export data with (these will be closed once the session is exported)
        private readonly List<StreamWriter> streamWritersToClose = new ();
        private bool firstSessionRun = true;

        /// <inheritdoc/>
        public override void Run(Pipeline pipeline, SessionImporter sessionImporter, Exporter exporter, ExportNotesTaskConfiguration configuration)
        {
            // Compute the text output filename
            if (this.firstSessionRun)
            {
                EnsurePathExists(configuration.NotesOutputFilename);
                if (File.Exists(configuration.NotesOutputFilename))
                {
                    File.Delete(configuration.NotesOutputFilename);
                }

                this.firstSessionRun = false;
            }

            var notesFile = File.AppendText(configuration.NotesOutputFilename);
            this.streamWritersToClose.Add(notesFile);

            // Get references to the various streams. If a stream is not present in the store,
            // the reference will be null.
            var recognitionResults = sessionImporter.OpenStreamOrDefault<IStreamingSpeechRecognitionResult>("Sigma.SpeechRecognition.RecognitionResults");
            var sessionName = sessionImporter.PartitionImporters["Sigma"].StorePath.Split('\\').Last();

            var lastIsTakeANote = false;
            recognitionResults.Do(
                    (rr, envelope) =>
                    {
                        if (lastIsTakeANote)
                        {
                            notesFile.WriteLine($"{sessionName}@{envelope.OriginatingTime.ToText()}\t{rr.Text}");
                            lastIsTakeANote = false;
                        }
                        else if (rr.Text?.ToLower() == "take a note")
                        {
                            lastIsTakeANote = true;
                        }
                    });
        }

        /// <inheritdoc/>
        public override void OnStartProcessingSession()
        {
            this.streamWritersToClose.Clear();
        }

        /// <inheritdoc/>
        public override void OnEndProcessingSession()
        {
            foreach (var sw in this.streamWritersToClose)
            {
                sw?.Close();
                sw?.Dispose();
            }

            this.streamWritersToClose.Clear();
        }

        /// <inheritdoc/>
        public override void OnCanceledProcessingSession() => this.OnEndProcessingSession();

        /// <inheritdoc/>
        public override void OnExceptionProcessingSession() => this.OnEndProcessingSession();

        /// <summary>
        /// Ensures that a specified path exists.
        /// </summary>
        /// <param name="path">The path to ensure the existence of.</param>
        /// <returns>The path.</returns>
        internal static string EnsurePathExists(string path)
        {
            var directory = Path.GetDirectoryName(path);
            if (!Directory.Exists(directory))
            {
                Directory.CreateDirectory(directory);
            }

            return path;
        }

        /// <summary>
        /// Represents the configuration for the <see cref="ExportNotesTask"/>.
        /// </summary>
#pragma warning disable SA1402 // File may only contain a single type
        public class ExportNotesTaskConfiguration : BatchProcessingTaskConfiguration
#pragma warning restore SA1402 // File may only contain a single type
        {
            private string notesOutputFilename = null;

            /// <summary>
            /// Initializes a new instance of the <see cref="ExportNotesTaskConfiguration"/> class.
            /// </summary>
            public ExportNotesTaskConfiguration()
                : base()
            {
                this.DeliveryPolicySpec = DeliveryPolicySpec.Unlimited;
                this.ReplayAllRealTime = false;
            }

            /// <summary>
            /// Gets or sets the name of a text output file.
            /// </summary>
            [DataMember]
            [DisplayName("Text Output Filename")]
            [Description("If specified, the recognition results and their timings will be written to this file, in the partition folder.")]
            public string NotesOutputFilename
            {
                get => this.notesOutputFilename;
                set { this.Set(nameof(this.NotesOutputFilename), ref this.notesOutputFilename, value); }
            }
        }
    }
}
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
    using MathNet.Spatial.Euclidean;
    using Microsoft.Psi;
    using Microsoft.Psi.Audio;
    using Microsoft.Psi.Data;
    using Microsoft.Psi.MixedReality.Applications;
    using Microsoft.Psi.MixedReality.OpenXR;
    using Microsoft.Psi.MixedReality.WinRT;
    using Microsoft.Psi.Spatial.Euclidean;

    /// <summary>
    /// Batch task for exporting Sigma data.
    /// </summary>
    [BatchProcessingTask(
        "Sigma - Export Captured Data",
        Description = "This task exports the data collected by the Sigma app to a set of text files.")]
    public class ExportDataTask : BatchProcessingTask<ExportDataTaskConfiguration>
    {
        // A list of stream writers to export data with (these will be closed once the session is exported)
        private readonly List<StreamWriter> streamWritersToClose = new ();

        /// <inheritdoc/>
        public override void Run(Pipeline pipeline, SessionImporter sessionImporter, Exporter exporter, ExportDataTaskConfiguration configuration)
        {
            // Get the input store name and store path
            var storeName = sessionImporter.PartitionImporters["Sigma"].StoreName;
            var storePath = sessionImporter.PartitionImporters["Sigma"].StorePath;
            var outputPath = storePath;

            // Get references to the various streams. If a stream is not present in the store,
            // the reference will be null.
            var head = default(IProducer<CoordinateSystem>);
            var handLeft = default(IProducer<Hand>);
            var handRight = default(IProducer<Hand>);
            var eyes = default(IProducer<Eyes>);

            // Keep backcompat for sessions that persisted via UserState
            if (sessionImporter.Contains("UserInterfaceStreams.UserState"))
            {
                var userState = sessionImporter.OpenStreamOrDefault<UserState>("UserInterfaceStreams.UserState");
                head = userState?.Select(us => us.Head);
                handLeft = userState?.Select(us => us.HandLeft);
                handRight = userState?.Select(us => us.HandRight);
                eyes = userState?.Select(us => us.Eyes);
            }
            else
            {
                head = sessionImporter.OpenStreamOrDefault<CoordinateSystem>("UserInterfaceStreams.EyesAndHead.Head");
                eyes = sessionImporter.OpenStreamOrDefault<Eyes>("UserInterfaceStreams.EyesAndHead.Eyes");
                handLeft = sessionImporter.OpenStreamOrDefault<Hand>("UserInterfaceStreams.Hands.Left");
                handRight = sessionImporter.OpenStreamOrDefault<Hand>("UserInterfaceStreams.Hands.Right");
            }

            var audio = sessionImporter.OpenStreamOrDefault<AudioBuffer>("HoloLensStreams.Audio");
            var videoEncodedImageCameraView = sessionImporter.OpenStreamOrDefault<EncodedImageCameraView>("HoloLensStreams.VideoEncodedImageCameraView");
            var videoImageCameraView = default(IProducer<ImageCameraView>);
            var previewEncodedImageCameraView = sessionImporter.OpenStreamOrDefault<EncodedImageCameraView>("HoloLensStreams.PreviewEncodedImageCameraView");
            var previewImageCameraView = default(IProducer<ImageCameraView>);
            var depthImageCameraView = sessionImporter.OpenStreamOrDefault<DepthImageCameraView>("HoloLensStreams.DepthImageCameraView");

            var videoExportImageCameraView = HoloLensCaptureInterop.Operators.Export("Video\\Images", videoImageCameraView, videoEncodedImageCameraView, null, isNV12: true, outputPath, this.streamWritersToClose);
            var previewExportImageCameraView = HoloLensCaptureInterop.Operators.Export("Preview\\Images", previewImageCameraView, previewEncodedImageCameraView, null, isNV12: true, outputPath, this.streamWritersToClose);

            // Export various depth image camera views
            depthImageCameraView?.Export("Depth", outputPath, this.streamWritersToClose);

            // Export head, eyes and hands streams
            head?.Export("Head", outputPath, this.streamWritersToClose);
            eyes?.Export("Eyes", outputPath, this.streamWritersToClose);
            handLeft?.Export("Hands", "Left", outputPath, this.streamWritersToClose);
            handRight?.Export("Hands", "Right", outputPath, this.streamWritersToClose);

            // Export audio
            audio?.Export("Audio", outputPath, this.streamWritersToClose);

            // Export the preview stream to mpeg
            if (configuration.ExportPreviewMpeg)
            {
                HoloLensCaptureInterop.Operators.ExportToMpeg(
                    previewExportImageCameraView,
                    null,
                    "HoloLensStreams.PreviewEncodedImageCameraView",
                    audio,
                    "HoloLensStreams.Audio",
                    storeName,
                    storePath,
                    Path.Combine(outputPath, "Preview"),
                    this.streamWritersToClose);
            }

            // Export the video stream to mpeg
            if (configuration.ExportVideoMpeg)
            {
                HoloLensCaptureInterop.Operators.ExportToMpeg(
                    videoExportImageCameraView,
                    null,
                    "HoloLensStreams.VideoEncodedImageCameraView",
                    audio,
                    "HoloLensStreams.Audio",
                    storeName,
                    storePath,
                    Path.Combine(outputPath, "Video"),
                    this.streamWritersToClose);
            }
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
    }

    /// <summary>
    /// Represents the configuration for the <see cref="ExportDataTask"/>.
    /// </summary>
#pragma warning disable SA1402 // File may only contain a single type

    public class ExportDataTaskConfiguration : BatchProcessingTaskConfiguration
#pragma warning restore SA1402 // File may only contain a single type
    {
        private bool exportVideoMpeg = false;
        private bool exportPreviewMpeg = false;

        /// <summary>
        /// Initializes a new instance of the <see cref="ExportDataTaskConfiguration"/> class.
        /// </summary>
        public ExportDataTaskConfiguration()
            : base()
        {
            this.DeliveryPolicySpec = DeliveryPolicySpec.Throttle;
        }

        /// <summary>
        /// Gets or sets a value indicating whether to export the video stream to mpeg.
        /// </summary>
        [DataMember]
        [DisplayName("Export Video to Mpeg")]
        [Description("Specifies whether to export the video stream to mpeg.")]
        public bool ExportVideoMpeg
        {
            get => this.exportVideoMpeg;
            set { this.Set(nameof(this.ExportVideoMpeg), ref this.exportVideoMpeg, value); }
        }

        /// <summary>
        /// Gets or sets a value indicating whether to export the preview stream to mpeg.
        /// </summary>
        [DataMember]
        [DisplayName("Export Preview to Mpeg")]
        [Description("Specifies whether to export the preview stream to mpeg.")]
        public bool ExportPreviewMpeg
        {
            get => this.exportPreviewMpeg;
            set { this.Set(nameof(this.ExportPreviewMpeg), ref this.exportPreviewMpeg, value); }
        }
    }
}
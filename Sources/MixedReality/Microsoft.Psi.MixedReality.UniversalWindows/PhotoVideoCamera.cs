// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.

namespace Microsoft.Psi.MixedReality
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Text;
    using System.Threading.Tasks;
    using MathNet.Numerics.LinearAlgebra;
    using MathNet.Spatial.Euclidean;
    using Microsoft.Psi;
    using Microsoft.Psi.Calibration;
    using Microsoft.Psi.Components;
    using Microsoft.Psi.Imaging;
    using Windows.Foundation;
    using Windows.Graphics.Imaging;
    using Windows.Media.Capture;
    using Windows.Media.Capture.Frames;

    /// <summary>
    /// Photo/video (PV) camera source component.
    /// </summary>
    public class PhotoVideoCamera : ISourceComponent, IDisposable
    {
        private readonly PhotoVideoCameraConfiguration configuration;
        private readonly Pipeline pipeline;
        private readonly Task initMediaCaptureTask;

        private MediaCapture mediaCapture;
        private MediaFrameReader videoFrameReader;
        private MediaFrameReader previewFrameReader;
        private TypedEventHandler<MediaFrameReader, MediaFrameArrivedEventArgs> videoFrameHandler;
        private TypedEventHandler<MediaFrameReader, MediaFrameArrivedEventArgs> previewFrameHandler;

        /// <summary>
        /// Initializes a new instance of the <see cref="PhotoVideoCamera"/> class.
        /// </summary>
        /// <param name="pipeline">The pipeline to add the component to.</param>
        /// <param name="configuration">The configuration for this component.</param>
        public PhotoVideoCamera(Pipeline pipeline, PhotoVideoCameraConfiguration configuration = null)
        {
            this.pipeline = pipeline;
            this.configuration = configuration ?? new PhotoVideoCameraConfiguration();
            this.VideoImage = pipeline.CreateEmitter<Shared<Image>>(this, nameof(this.VideoImage));
            this.PreviewImage = pipeline.CreateEmitter<Shared<Image>>(this, nameof(this.PreviewImage));
            this.VideoIntrinsics = pipeline.CreateEmitter<ICameraIntrinsics>(this, nameof(this.VideoIntrinsics));
            this.VideoPose = pipeline.CreateEmitter<CoordinateSystem>(this, nameof(this.VideoPose));
            this.PreviewIntrinsics = pipeline.CreateEmitter<ICameraIntrinsics>(this, nameof(this.PreviewIntrinsics));
            this.PreviewPose = pipeline.CreateEmitter<CoordinateSystem>(this, nameof(this.PreviewPose));

            // Call this here (rather than in the Start() method, which is executed on the thread pool) to
            // ensure that MediaCapture.InitializeAsync() is called from an STA thread (this constructor must
            // itself be called from an STA thread in order for this to be true). Calls from an MTA thread may
            // result in undefined behavior, per the following documentation:
            // https://docs.microsoft.com/en-us/uwp/api/windows.media.capture.mediacapture.initializeasync
            this.initMediaCaptureTask = this.InitializeMediaCaptureAsync();
        }

        /// <summary>
        /// Gets the video image stream.
        /// </summary>
        public Emitter<Shared<Image>> VideoImage { get; }

        /// <summary>
        /// Gets the video camera pose stream.
        /// </summary>
        public Emitter<CoordinateSystem> VideoPose { get; }

        /// <summary>
        /// Gets the video camera intrinsics stream.
        /// </summary>
        public Emitter<ICameraIntrinsics> VideoIntrinsics { get; }

        /// <summary>
        /// Gets the preview image stream.
        /// </summary>
        public Emitter<Shared<Image>> PreviewImage { get; }

        /// <summary>
        /// Gets the preview camera pose stream.
        /// </summary>
        public Emitter<CoordinateSystem> PreviewPose { get; }

        /// <summary>
        /// Gets the preview camera intrinsics stream.
        /// </summary>
        public Emitter<ICameraIntrinsics> PreviewIntrinsics { get; }

        /// <inheritdoc/>
        public void Dispose()
        {
            if (this.mediaCapture != null)
            {
                this.mediaCapture.Dispose();
                this.mediaCapture = null;
            }
        }

        /// <inheritdoc/>
        public async void Start(Action<DateTime> notifyCompletionTime)
        {
            // notify that this is an infinite source component
            notifyCompletionTime(DateTime.MaxValue);

            // Ensure that media capture initialization has finished
            await this.initMediaCaptureTask;

            // Start the media frame reader for the Video stream, if configured
            if (this.videoFrameReader != null)
            {
                var status = await this.videoFrameReader.StartAsync();
                if (status != MediaFrameReaderStartStatus.Success)
                {
                    throw new InvalidOperationException($"Video stream media frame reader failed to start: {status}");
                }

                if (this.configuration.VideoStreamSettings.MixedRealityCapture != null)
                {
                    // Add the mixed-reality effect to the VideoRecord stream so we can capture the video with holograms.
                    // Note that this is done *after* capture has started, as outlined in the documentation here:
                    // https://docs.microsoft.com/en-us/windows/mixed-reality/develop/platform-capabilities-and-apis/mixed-reality-capture-for-developers#mrc-access-for-developers
                    await this.mediaCapture.AddVideoEffectAsync(this.configuration.VideoStreamSettings.MixedRealityCapture, MediaStreamType.VideoRecord);
                }

                // Create the frame handler - this handles the FrameArrived event which is raised
                // whenever a new Video frame is available. The frame image, pose and intrinsics
                // (if configured) are then posted on the respective output emitters.
                this.videoFrameHandler = this.CreateMediaFrameHandler(
                    this.VideoImage,
                    this.configuration.VideoStreamSettings.OutputIntrinsics ? this.VideoIntrinsics : null,
                    this.configuration.VideoStreamSettings.OutputPose ? this.VideoPose : null);

                this.videoFrameReader.FrameArrived += this.videoFrameHandler;
            }

            // Start the media frame reader for the Preview stream, if configured
            if (this.previewFrameReader != null)
            {
                var status = await this.previewFrameReader.StartAsync();
                if (status != MediaFrameReaderStartStatus.Success)
                {
                    throw new InvalidOperationException($"Preview stream media frame reader failed to start: {status}");
                }

                if (this.configuration.PreviewStreamSettings.MixedRealityCapture != null)
                {
                    // Add the mixed-reality effect to the VideoPreview stream so we can capture the video with holograms.
                    // Note that this is done *after* capture has started, as outlined in the documentation here:
                    // https://docs.microsoft.com/en-us/windows/mixed-reality/develop/platform-capabilities-and-apis/mixed-reality-capture-for-developers#mrc-access-for-developers
                    await this.mediaCapture.AddVideoEffectAsync(this.configuration.PreviewStreamSettings.MixedRealityCapture, MediaStreamType.VideoPreview);
                }

                // Create the frame handler - this handles the FrameArrived event which is raised
                // whenever a new Preview frame is available. The frame image, pose and intrinsics
                // (if configured) are then posted on the respective output emitters.
                this.previewFrameHandler = this.CreateMediaFrameHandler(
                    this.PreviewImage,
                    this.configuration.PreviewStreamSettings.OutputIntrinsics ? this.PreviewIntrinsics : null,
                    this.configuration.PreviewStreamSettings.OutputPose ? this.PreviewPose : null);

                this.previewFrameReader.FrameArrived += this.previewFrameHandler;
            }
        }

        /// <inheritdoc/>
        public async void Stop(DateTime finalOriginatingTime, Action notifyCompleted)
        {
            if (this.videoFrameReader != null)
            {
                this.videoFrameReader.FrameArrived -= this.videoFrameHandler;

                await this.videoFrameReader.StopAsync();
                this.videoFrameReader.Dispose();
                this.videoFrameReader = null;
            }

            if (this.previewFrameReader != null)
            {
                this.previewFrameReader.FrameArrived -= this.previewFrameHandler;

                await this.previewFrameReader.StopAsync();
                this.previewFrameReader.Dispose();
                this.previewFrameReader = null;
            }

            notifyCompleted();
        }

        /// <summary>
        /// Initializes the MediaCapture object and creates the MediaFrameReaders for the configured capture streams.
        /// </summary>
        /// <returns>A task representing the asynchronous operation.</returns>
        private async Task InitializeMediaCaptureAsync()
        {
            // Try to find the media capture settings for the requested capture configuration
            var settings = await this.CreateMediaCaptureSettingsAsync();

            // If we couldn't create the settings, retrieve and print all the supported capture modes
            if (settings == null)
            {
                var supportedModes = await this.GetSupportedMediaCaptureModesAsync();

                // Pretty-print the list of supported modes
                var msg = new StringBuilder();
                msg.AppendLine("No media frame source group was found that matched the requested capture parameters. Please select from the following profiles and resolutions:");
                foreach (var profileModes in supportedModes.GroupBy(x => x.Profile.Id))
                {
                    msg.AppendLine($"Profile: {profileModes.Key}");
                    foreach (var mode in profileModes
                        .OrderByDescending(x => x.Type)
                        .ThenByDescending(x => x.Description.Width)
                        .ThenBy(x => x.Description.FrameRate))
                    {
                        msg.AppendLine($"    {mode.Type}: {mode.Description.Width}x{mode.Description.Height} @ {mode.Description.FrameRate}fps");
                    }

                    msg.AppendLine();
                }

                msg.AppendLine("If capturing both the Video and Preview streams, the requested resolutions must both be supported by the same profile.");

                // Display the list of supported modes in the exception message
                throw new InvalidOperationException(msg.ToString());
            }

            var selectedSourceGroup = settings.SourceGroup;

            // Initialize the MediaCapture object
            this.mediaCapture = new MediaCapture();
            await this.mediaCapture.InitializeAsync(settings);

            // Create the MediaFrameReader for the Video stream
            if (this.configuration.VideoStreamSettings != null)
            {
                this.videoFrameReader = await this.CreateMediaFrameReaderAsync(
                    selectedSourceGroup,
                    this.configuration.VideoStreamSettings.ImageWidth,
                    this.configuration.VideoStreamSettings.ImageHeight,
                    this.configuration.VideoStreamSettings.FrameRate,
                    MediaStreamType.VideoRecord);

                if (this.videoFrameReader == null)
                {
                    throw new InvalidOperationException("Could not create a frame reader for the requested video settings.");
                }
            }

            // Create the MediaFrameReader for the Preview stream
            if (this.configuration.PreviewStreamSettings != null)
            {
                this.previewFrameReader = await this.CreateMediaFrameReaderAsync(
                    selectedSourceGroup,
                    this.configuration.PreviewStreamSettings.ImageWidth,
                    this.configuration.PreviewStreamSettings.ImageHeight,
                    this.configuration.PreviewStreamSettings.FrameRate,
                    MediaStreamType.VideoPreview);

                if (this.previewFrameReader == null)
                {
                    throw new InvalidOperationException("Could not create a frame reader for the requested preview settings.");
                }
            }
        }

        /// <summary>
        /// Gets all the supported media capture modes supported by the current device.
        /// </summary>
        /// <returns>A list of supported media capture modes (profile and description).</returns>
        private async Task<List<(MediaStreamType Type, MediaCaptureVideoProfile Profile, MediaCaptureVideoProfileMediaDescription Description)>> GetSupportedMediaCaptureModesAsync()
        {
            var supportedModes = new List<(MediaStreamType Type, MediaCaptureVideoProfile Profile, MediaCaptureVideoProfileMediaDescription Description)>();
            var mediaFrameSourceGroups = await MediaFrameSourceGroup.FindAllAsync();
            foreach (var mediaFrameSourceGroup in mediaFrameSourceGroups)
            {
                var knownProfiles = MediaCapture.FindAllVideoProfiles(mediaFrameSourceGroup.Id);

                // Search for Video and Preview stream types
                foreach (var knownProfile in knownProfiles)
                {
                    foreach (var knownDesc in knownProfile.SupportedRecordMediaDescription)
                    {
                        supportedModes.Add((MediaStreamType.VideoRecord, knownProfile, knownDesc));
                    }

                    foreach (var knownDesc in knownProfile.SupportedPreviewMediaDescription)
                    {
                        supportedModes.Add((MediaStreamType.VideoPreview, knownProfile, knownDesc));
                    }
                }
            }

            return supportedModes;
        }

        /// <summary>
        /// Creates the initialization settings for the MediaCapture object that will support
        /// all the requested capture settings specified in the configuration object. This method
        /// will iterate through all the device's video capture profiles to find one that supports
        /// the requested capture frame dimensions and frame rate. If both Video and Preview streams
        /// are selected (e.g. for simultaneous mixed reality capture), then the selected profile must
        /// support the capture modes for both streams.
        /// </summary>
        /// <returns>
        /// A MediaCaptureInitializationSettings object for the first profile that satisfies all the
        /// requested capture settings in the configuration object, or null if no such profile was found.
        /// </returns>
        private async Task<MediaCaptureInitializationSettings> CreateMediaCaptureSettingsAsync()
        {
            MediaFrameSourceGroup selectedSourceGroup = null;
            MediaCaptureVideoProfile profile = null;
            MediaCaptureVideoProfileMediaDescription videoDesc = null;
            MediaCaptureVideoProfileMediaDescription previewDesc = null;

            var mediaFrameSourceGroups = await MediaFrameSourceGroup.FindAllAsync();

            // Search all source groups
            foreach (var mediaFrameSourceGroup in mediaFrameSourceGroups)
            {
                // Search for a profile that supports the requested capture modes
                var knownProfiles = MediaCapture.FindAllVideoProfiles(mediaFrameSourceGroup.Id);
                foreach (var knownProfile in knownProfiles)
                {
                    // If a video stream capture mode was specified
                    if (this.configuration.VideoStreamSettings != null)
                    {
                        // Clear any partial matches and continue searching
                        profile = null;
                        videoDesc = null;
                        selectedSourceGroup = null;

                        // Search the supported video (recording) modes for the requested resolution and frame rate
                        foreach (var knownDesc in knownProfile.SupportedRecordMediaDescription)
                        {
                            if (knownDesc.Width == this.configuration.VideoStreamSettings.ImageWidth &&
                                knownDesc.Height == this.configuration.VideoStreamSettings.ImageHeight &&
                                knownDesc.FrameRate == this.configuration.VideoStreamSettings.FrameRate)
                            {
                                // Found a match for video. Need to also match the requested preview mode (if any)
                                // within the same profile and source group, otherwise we have to keep searching.
                                profile = knownProfile;
                                videoDesc = knownDesc;
                                selectedSourceGroup = mediaFrameSourceGroup;
                                break;
                            }
                        }

                        if (profile == null)
                        {
                            // This profile does not support the requested video stream capture parameters - try the next profile
                            continue;
                        }
                    }

                    // If a preview stream capture mode was specified
                    if (this.configuration.PreviewStreamSettings != null)
                    {
                        // Clear any partial matches and continue searching
                        profile = null;
                        previewDesc = null;
                        selectedSourceGroup = null;

                        // Search the supported preview modes for the requested resolution and frame rate
                        foreach (var knownDesc in knownProfile.SupportedPreviewMediaDescription)
                        {
                            if (knownDesc.Width == this.configuration.PreviewStreamSettings.ImageWidth &&
                                knownDesc.Height == this.configuration.PreviewStreamSettings.ImageHeight &&
                                knownDesc.FrameRate == this.configuration.PreviewStreamSettings.FrameRate)
                            {
                                // Found a match
                                profile = knownProfile;
                                previewDesc = knownDesc;
                                selectedSourceGroup = mediaFrameSourceGroup;
                                break;
                            }
                        }

                        if (profile == null)
                        {
                            // This profile does not support the requested preview mode - try the next profile
                            continue;
                        }
                    }

                    if (profile != null)
                    {
                        // Found a valid profile that supports the requested capture settings
                        return new MediaCaptureInitializationSettings
                        {
                            VideoProfile = profile,
                            RecordMediaDescription = videoDesc,
                            PreviewMediaDescription = previewDesc,
                            VideoDeviceId = selectedSourceGroup.Id,
                            StreamingCaptureMode = StreamingCaptureMode.Video,
                            MemoryPreference = MediaCaptureMemoryPreference.Cpu,
                            SharingMode = MediaCaptureSharingMode.ExclusiveControl,
                            SourceGroup = selectedSourceGroup,
                        };
                    }
                }
            }

            // No matching settings were found
            return null;
        }

        /// <summary>
        /// Creates a MediaFrameReader from the media source group for the given target capture settings.
        /// </summary>
        /// <param name="sourceGroup">The media source group.</param>
        /// <param name="targetWidth">The requested capture frame width.</param>
        /// <param name="targetHeight">The requested capture frame height.</param>
        /// <param name="targetFrameRate">The requested capture frame rate.</param>
        /// <param name="targetStreamType">The requested capture stream type.</param>
        /// <returns>A task representing the asynchronous operation.</returns>
        private async Task<MediaFrameReader> CreateMediaFrameReaderAsync(MediaFrameSourceGroup sourceGroup, int targetWidth, int targetHeight, int targetFrameRate, MediaStreamType targetStreamType)
        {
            // Search all color frame sources of the requested stream type (Video or Preview)
            foreach (var sourceInfo in sourceGroup.SourceInfos
                .Where(si => si.SourceKind == MediaFrameSourceKind.Color && si.MediaStreamType == targetStreamType))
            {
                var frameSource = this.mediaCapture.FrameSources[sourceInfo.Id];

                // Check if the frame source supports the requested format
                foreach (var format in frameSource.SupportedFormats)
                {
                    int frameRate = (int)Math.Round((double)format.FrameRate.Numerator / format.FrameRate.Denominator);
                    if (format.VideoFormat.Width == targetWidth &&
                        format.VideoFormat.Height == targetHeight &&
                        frameRate == targetFrameRate)
                    {
                        // Found a frame source for the requested format - create the frame reader
                        await frameSource.SetFormatAsync(format);
                        return await this.mediaCapture.CreateFrameReaderAsync(frameSource);
                    }
                }
            }

            // No frame source was found for the requested format
            return null;
        }

        /// <summary>
        /// Creates an event handler that handles the FrameArrived event of the MediaFrameReader.
        /// </summary>
        /// <param name="imageStream">The stream on which to post the output image.</param>
        /// <param name="intrinsicsStream">The stream on which to post the camera intrinsics.</param>
        /// <param name="poseStream">The stream on which to post the camera pose.</param>
        /// <returns>The event handler.</returns>
        private TypedEventHandler<MediaFrameReader, MediaFrameArrivedEventArgs> CreateMediaFrameHandler(
            Emitter<Shared<Image>> imageStream,
            Emitter<ICameraIntrinsics> intrinsicsStream = null,
            Emitter<CoordinateSystem> poseStream = null)
        {
            // Cache the intrinsics
            ICameraIntrinsics cameraIntrinsics = null;

            return (sender, args) =>
            {
                using var frame = sender.TryAcquireLatestFrame();
                if (frame != null)
                {
                    // Convert frame QPC time to pipeline time
                    var frameTimestamp = frame.SystemRelativeTime.Value.Ticks;
                    var originatingTime = this.pipeline.GetCurrentTimeFromElapsedTicks(frameTimestamp);

                    // Post the camera intrinsics if requested
                    if (intrinsicsStream != null)
                    {
                        cameraIntrinsics ??= this.GetCameraIntrinsics(frame);
                        intrinsicsStream.Post(cameraIntrinsics, originatingTime);
                    }

                    // Post the camera pose if requested
                    if (poseStream != null)
                    {
                        // Convert the frame coordinate system to world pose in psi basis
                        var worldPose = frame.CoordinateSystem?.TryConvertSpatialCoordinateSystemToPsiCoordinateSystem();
                        poseStream.Post(worldPose, originatingTime);
                    }

                    // Accessing the VideoMediaFrame.SoftwareBitmap property creates a strong reference which needs to be Disposed, per the remarks here:
                    // https://docs.microsoft.com/en-us/uwp/api/windows.media.capture.frames.mediaframereference?view=winrt-19041#remarks
                    using var frameBitmap = frame.VideoMediaFrame.SoftwareBitmap;

                    // Convert from NV12 to BGRA32
                    using var softwareBitmap = SoftwareBitmap.Convert(frameBitmap, BitmapPixelFormat.Bgra8);

                    // Copy bitmap data into a Shared<Image>
                    unsafe
                    {
                        using var sharedImage = ImagePool.GetOrCreate(softwareBitmap.PixelWidth, softwareBitmap.PixelHeight, PixelFormat.BGRA_32bpp);
                        using var input = softwareBitmap.LockBuffer(BitmapBufferAccessMode.Read);
                        using var inputReference = input.CreateReference();
                        ((UnsafeNative.IMemoryBufferByteAccess)inputReference).GetBuffer(out byte* imageData, out uint size);

                        // Debug.Assert(size == sharedImage.Resource.Size);
                        sharedImage.Resource.CopyFrom((IntPtr)imageData);

                        // Post image stream
                        imageStream.Post(sharedImage, originatingTime);
                    }
                }
            };
        }

        /// <summary>
        /// Extracts the camera intrinsics from the supplied frame.
        /// </summary>
        /// <param name="frame">The frame from which to extract the camera intrinsics.</param>
        /// <returns>The camera intrinsics.</returns>
        private CameraIntrinsics GetCameraIntrinsics(MediaFrameReference frame)
        {
            var intrinsics = frame.VideoMediaFrame.CameraIntrinsics;

            var transform = Matrix<double>.Build.Dense(3, 3);
            transform[0, 0] = intrinsics.FocalLength.X;
            transform[1, 1] = intrinsics.FocalLength.Y;
            transform[0, 2] = intrinsics.PrincipalPoint.X;
            transform[1, 2] = intrinsics.PrincipalPoint.Y;
            transform[2, 2] = 1;

            var radialDistortion = Vector<double>.Build.Dense(6, 0);
            radialDistortion[0] = intrinsics.RadialDistortion.X;
            radialDistortion[1] = intrinsics.RadialDistortion.Y;
            radialDistortion[2] = intrinsics.RadialDistortion.Z;

            var tangentialDistortion = Vector<double>.Build.Dense(2, 0);
            tangentialDistortion[0] = intrinsics.TangentialDistortion.X;
            tangentialDistortion[1] = intrinsics.TangentialDistortion.Y;

            return new CameraIntrinsics(
                (int)intrinsics.ImageWidth,
                (int)intrinsics.ImageHeight,
                transform,
                radialDistortion,
                tangentialDistortion);
        }
    }
}

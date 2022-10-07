
namespace CMU.Smartlab.Rtsp
{
    using System;
    using System.Collections.Generic;
    using System.Drawing;
    using System.Drawing.Imaging;
    using System.Net;
    using System.Threading;
    using Microsoft.Psi;
    using Microsoft.Psi.Audio;
    using Microsoft.Psi.Components;
    using Microsoft.Psi.Imaging;
    using Microsoft.Psi.Media;
    using Microsoft.Psi.Media_Interop;
    using RtspClientSharp;
    using RtspClientSharp.RawFrames;
    using RtspClientSharp.RawFrames.Audio;
    using RtspClientSharp.RawFrames.Video;

    public class RtspCapture : IProducer<Shared<Microsoft.Psi.Imaging.Image>>, ISourceComponent, IDisposable, IMediaCapture
    {
        private readonly Pipeline pipeline;

        /// <summary>
        /// The video camera configuration.
        /// </summary>
        private readonly MediaCaptureConfiguration configuration;
        private readonly Uri uri;
        private readonly NetworkCredential credential;
        private readonly Dictionary<FFmpegVideoCodecId, FFmpegVideoDecoder> videoDecodersMap =
            new Dictionary<FFmpegVideoCodecId, FFmpegVideoDecoder>();

        private readonly Dictionary<FFmpegAudioCodecId, FFmpegAudioDecoder> audioDecodersMap =
            new Dictionary<FFmpegAudioCodecId, FFmpegAudioDecoder>();

        private TransformParameters transformParameters;

        /// <summary>
        /// The video capture device.
        /// </summary>
        private MediaCaptureDevice camera;

        private IRawFramesSource rawFramesSource;

        /// <summary>
        /// Defines attributes of properties exposed by MediaCaptureDevice.
        /// </summary>
        private RtspClient rtspClient;
        private CancellationTokenSource cancellationTokenSource;

        // private CaptureFormat rtspFormat;

        /// <summary>
        /// Initializes a new instance of the <see cref="MediaCaptureRtsp"/> class.
        /// </summary>
        /// <param name="pipeline">Pipeline this component is a part of.</param>
        /// <param name="uri">RTSP Uri.</param>
        /// <param name="credential">User name and password to log in to the RTSP camera.</param>
        /// <param name="captureAudio">Should we create an audio capture device.</param>
        /// <param name="persistVideoFrames">Indicates whether video frames should be persisted.</param>
        /// <param name="useInSharedMode">Indicates whether camera is shared amongst multiple applications.</param>
        public RtspCapture(Pipeline pipeline, Uri uri, NetworkCredential credential = null, bool captureAudio = false, bool persistVideoFrames = false, bool useInSharedMode = false)
    : this(pipeline)
        {
            this.configuration = new MediaCaptureConfiguration()
            {
                UseInSharedMode = useInSharedMode,
                CaptureAudio = captureAudio,
            };
            this.uri = uri;
            this.credential = credential;
            if (this.configuration.CaptureAudio)
            {
                // this.audio = new Audio.AudioCapture(pipeline, new Audio.AudioCaptureConfiguration() { OutputFormat = Psi.Audio.WaveFormat.Create16kHz1Channel16BitPcm() });
                this.Audio = pipeline.CreateEmitter<AudioBuffer>(this, nameof(this.Audio));
            }
        }

        private RtspCapture(Pipeline pipeline)
        {
            this.pipeline = pipeline;
            this.Out = pipeline.CreateEmitter<Shared<Microsoft.Psi.Imaging.Image>>(this, nameof(this.Out));
        }

        /// <summary>
        /// Gets the emitter for the audio stream.
        /// </summary>
        public Emitter<AudioBuffer> Audio
        {
            get;
            private set;
        }

        /// <summary>
        /// Gets the emitter for the video stream.
        /// </summary>
        public Emitter<Shared<Microsoft.Psi.Imaging.Image>> Video => this.Out;

        /// <summary>
        /// Gets the output stream of images.
        /// </summary>
        public Emitter<Shared<Microsoft.Psi.Imaging.Image>> Out { get; private set; }

        /// <summary>
        /// Gets the original pixel format from ffmpeg.
        /// </summary>
        public FFmpegPixelFormat PixelFormat { get; private set; }

        /// <summary>
        /// Dispose method.
        /// </summary>
        public void Dispose()
        {
            // check for null since it's possible that Start was never called
            if (this.camera != null)
            {
                this.camera.Shutdown();
                this.camera.Dispose();
                this.camera = null;
            }
        }

        /// <inheritdoc/>
        public void Start(Action<DateTime> rtspNotifyCompletionTime)
        {
            // notify that this is an infinite source component
            rtspNotifyCompletionTime(DateTime.MaxValue);

            // RTSP setting
            this.RtspFrameReciver(this.uri, this.credential);

            // Get capture sample
            // if (this.rtspFormat != null)
            if (this.rawFramesSource != null)
            {
                this.rawFramesSource.FrameReceived += this.OnFrameReceived;
            }
            else
            {
                throw new ArgumentException("RawFramesSource is not created properly.");
            }
        }

        /// <inheritdoc/>
        public void Stop(DateTime finalOriginatingTime, Action notifyCompleted)
        {
            if (this.rawFramesSource != null)
            {
                this.rawFramesSource.Stop();
                this.rawFramesSource = null;
            }

            this.Dispose();
            notifyCompleted();
        }

        /// <summary>
        /// This method is to get width,height,timestamp and realtime stream of the rtspframe.
        /// </summary>
        /// <param name="rtspUri">the adderess of RTSP camera with 172.0.0.0.</param>
        /// <param name="rtspCredential">the username and password of RTSP camera.</param>
        private void RtspFrameReciver(Uri rtspUri, NetworkCredential rtspCredential)
        {
            ConnectionParameters connectionParameters;
            if (rtspCredential != null)
            {
                connectionParameters = new ConnectionParameters(rtspUri, rtspCredential);
            }
            else
            {
                connectionParameters = new ConnectionParameters(rtspUri);
            }

            this.rtspClient = new RtspClient(connectionParameters);
            this.cancellationTokenSource = new CancellationTokenSource();
            if (this.rawFramesSource != null)
            {
                return;
            }

            this.rawFramesSource = new RawFramesSource(connectionParameters);

            this.rawFramesSource.Start();
        }

        private void OnFrameReceived(object sender, RawFrame rawFrame)
        {
            try
            {
                if (rawFrame is RawVideoFrame rawVideoFrame)
                {
                    this.ProcessVideoFrame(rawVideoFrame);
                }

                if (rawFrame is RawAudioFrame rawAudioFrame)
                {
                    if (this.configuration.CaptureAudio)
                    {
                        this.ProcessAudioFrame(rawAudioFrame);
                    }
                }
            }
            catch (Exception e)
            {
                Console.WriteLine(e.ToString());
            }
        }

        private WaveFormat ConvertAudioFormat(AudioFrameFormat format)
        {
            return WaveFormat.CreatePcm(format.SampleRate, format.BitPerSample, format.Channels);
        }

        private void ProcessAudioFrame(RawAudioFrame rawAudioFrame)
        {
            FFmpegAudioDecoder decoder = this.GetAudioDecoderForFrame(rawAudioFrame);

            if (!decoder.TryDecode(rawAudioFrame))
            {
                return;
            }

            IDecodedAudioFrame decodedFrame = decoder.GetDecodedFrame(new AudioConversionParameters() { OutBitsPerSample = 16 });
            var originatingTime = decodedFrame.Timestamp;
            this.Audio.Post(new AudioBuffer(decodedFrame.DecodedBytes.Array, this.ConvertAudioFormat(decodedFrame.Format)), originatingTime);
        }

        private FFmpegAudioDecoder GetAudioDecoderForFrame(RawAudioFrame audioFrame)
        {
            FFmpegAudioCodecId codecId = this.DetectAudioCodecId(audioFrame);
            if (!this.audioDecodersMap.TryGetValue(codecId, out FFmpegAudioDecoder decoder))
            {
                int bitsPerCodedSample = 0;
                if (audioFrame is RawG726Frame g726Frame)
                {
                    bitsPerCodedSample = g726Frame.BitsPerCodedSample;
                }

                decoder = FFmpegAudioDecoder.CreateDecoder(codecId, bitsPerCodedSample);
                this.audioDecodersMap.Add(codecId, decoder);
            }

            return decoder;
        }

        private FFmpegAudioCodecId DetectAudioCodecId(RawAudioFrame audioFrame)
        {
            if (audioFrame is RawAACFrame)
            {
                return FFmpegAudioCodecId.AAC;
            }

            if (audioFrame is RawG711AFrame)
            {
                return FFmpegAudioCodecId.G711A;
            }

            if (audioFrame is RawG711UFrame)
            {
                return FFmpegAudioCodecId.G711U;
            }

            if (audioFrame is RawG726Frame)
            {
                return FFmpegAudioCodecId.G726;
            }

            throw new ArgumentOutOfRangeException(nameof(audioFrame));
        }

        private void ProcessVideoFrame(RawVideoFrame rawVideoFrame)
        {
            FFmpegVideoDecoder decoder = this.GetVideoDecoderForFrame(rawVideoFrame);
            DecodedVideoFrameParameters currentParameters;
            IDecodedVideoFrame decodedFrame = decoder.TryDecode(rawVideoFrame, out currentParameters);
            if (currentParameters != null)
            {
                int width = currentParameters.Width;
                int height = currentParameters.Height;
                this.configuration.Width = width;
                this.configuration.Height = height;
                this.PixelFormat = currentParameters.PixelFormat;
                this.transformParameters = new TransformParameters(
                    RectangleF.Empty,
                    new Size(width, height),
                    ScalingPolicy.Stretch,
                    Smartlab.Rtsp.PixelFormat.Bgra32,
                    ScalingQuality.FastBilinear);
            }

            try
            {
                // Manually get stride number:
                System.Drawing.Imaging.PixelFormat format = this.GetSystemPixelFormat(this.PixelFormat);
                Bitmap bitmap = new Bitmap(this.configuration.Width, this.configuration.Height, System.Drawing.Imaging.PixelFormat.Format32bppArgb);
                BitmapData bdata = bitmap.LockBits(
                    new Rectangle(
                        new Point(0, 0),
                        new Size(bitmap.Width, bitmap.Height)),
                    ImageLockMode.ReadWrite,
                    bitmap.PixelFormat);
                decodedFrame.TransformTo(bdata.Scan0, bdata.Stride, this.transformParameters);
                bitmap.UnlockBits(bdata);
                using (var sharedImage = ImagePool.GetOrCreate(bitmap))
                {
                    var originatingTime = rawVideoFrame.Timestamp;
                    this.Out.Post(sharedImage, originatingTime);
                }
            }
            catch (Exception e)
            {
                System.Console.WriteLine(e.Message);
            }
        }

        private PixelFormat GetPsiPixelFormat(FFmpegPixelFormat pixelFormat)
        {
            if (pixelFormat == FFmpegPixelFormat.BGR24)
            {
                return Rtsp.PixelFormat.Bgr24;
            }
            else if (pixelFormat == FFmpegPixelFormat.BGRA)
            {
                return Rtsp.PixelFormat.Bgra32;
            }
            else if (pixelFormat == FFmpegPixelFormat.GRAY8)
            {
                return Rtsp.PixelFormat.Grayscale;
            }
            else
            {
                throw new ArgumentOutOfRangeException("Undefined pixel format " + pixelFormat.ToString());
            }
        }

        private System.Drawing.Imaging.PixelFormat GetSystemPixelFormat(FFmpegPixelFormat pixelFormat)
        {
            if (pixelFormat == FFmpegPixelFormat.BGR24)
            {
                // WARNING:
                // Seems that the order of pixels are not perfectly matched. Not sure yet whether this will cause problems.
                return System.Drawing.Imaging.PixelFormat.Format24bppRgb;
            }
            else if (pixelFormat == FFmpegPixelFormat.BGRA)
            {
                return System.Drawing.Imaging.PixelFormat.Format32bppArgb;
            }
            else if (pixelFormat == FFmpegPixelFormat.GRAY8)
            {
                // WARNING:
                // Here I can't find a corresponding pixel format for GRAY8
                return System.Drawing.Imaging.PixelFormat.Format8bppIndexed;
            }
            else
            {
                return System.Drawing.Imaging.PixelFormat.DontCare;
            }
        }

        private FFmpegVideoDecoder GetVideoDecoderForFrame(RawVideoFrame videoFrame)
        {
            FFmpegVideoCodecId codecId = this.DetectVideoCodecId(videoFrame);
            if (!this.videoDecodersMap.TryGetValue(codecId, out FFmpegVideoDecoder decoder))
            {
                decoder = FFmpegVideoDecoder.CreateDecoder(codecId);
                this.videoDecodersMap.Add(codecId, decoder);
            }

            return decoder;
        }

        private FFmpegVideoCodecId DetectVideoCodecId(RawVideoFrame videoFrame)
        {
            if (videoFrame is RawJpegFrame)
            {
                return FFmpegVideoCodecId.MJPEG;
            }

            if (videoFrame is RawH264Frame)
            {
                return FFmpegVideoCodecId.H264;
            }

            throw new ArgumentOutOfRangeException(nameof(videoFrame));
        }
    }
}

// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.

namespace CMU.Smartlab.Rtsp
{
    using System;
    using System.Security.Authentication;
    using System.Threading;
    using System.Threading.Tasks;
    using Microsoft.Psi.Media;
    using RtspClientSharp;
    using RtspClientSharp.RawFrames;
    using RtspClientSharp.Rtsp;

    /// <summary>
    /// Component that......
    /// </summary>
    public class RawFramesSource : IRawFramesSource
    {
        private static readonly TimeSpan RetryDelay = TimeSpan.FromSeconds(5);
        private readonly ConnectionParameters rtspConnectionParameters;
        private Task workTask = Task.CompletedTask;
        private CancellationTokenSource cancellationTokenSource;

        /// <summary>
        /// Initializes a new instance of the <see cref="RawFramesSource"/> class.
        /// </summary>
        /// <param name="connectionParameters">Indicates...</param>
        public RawFramesSource(ConnectionParameters connectionParameters)
        {
            this.rtspConnectionParameters =
                connectionParameters ?? throw new ArgumentNullException(nameof(connectionParameters));
        }

        /// <summary>
        /// Gets or sets ......
        /// </summary>
        public EventHandler<RawFrame> FrameReceived { get; set; }

        /// <summary>
        /// Gets or sets ......
        /// </summary>
        public EventHandler<string> ConnectionStatusChanged { get; set; }

        /// <summary>
        ///  ......
        /// </summary>
        public void Start()
        {
            this.cancellationTokenSource = new CancellationTokenSource();

            CancellationToken token = this.cancellationTokenSource.Token;

            this.workTask = this.workTask.ContinueWith(
                async p =>
            {
                await this.ReceiveAsync(token);
            }, token);
        }

        /// <summary>
        ///  ......
        /// </summary>
        public void Stop()
        {
            this.cancellationTokenSource.Cancel();
        }

        private async Task ReceiveAsync(CancellationToken token)
        {
            try
            {
                using (var rtspClient = new RtspClient(this.rtspConnectionParameters))
                {
                    rtspClient.FrameReceived += this.RtspClientOnFrameReceived;

                    while (true)
                    {
                        this.OnStatusChanged("Connecting...");

                        try
                        {
                            await rtspClient.ConnectAsync(token);
                        }
                        catch (InvalidCredentialException)
                        {
                            this.OnStatusChanged("Invalid login and/or password");
                            await Task.Delay(RetryDelay, token);
                            continue;
                        }
                        catch (RtspClientException e)
                        {
                            this.OnStatusChanged(e.ToString());
                            await Task.Delay(RetryDelay, token);
                            continue;
                        }

                        this.OnStatusChanged("Receiving frames...");

                        try
                        {
                            await rtspClient.ReceiveAsync(token);
                        }
                        catch (RtspClientException e)
                        {
                            this.OnStatusChanged(e.ToString());
                            await Task.Delay(RetryDelay, token);
                        }
                    }
                }
            }
            catch (OperationCanceledException)
            {
            }
        }

        private void RtspClientOnFrameReceived(object sender, RawFrame rawFrame)
        {
            this.FrameReceived?.Invoke(this, rawFrame);
        }

        private void OnStatusChanged(string status)
        {
            this.ConnectionStatusChanged?.Invoke(this, status);
        }
    }
}
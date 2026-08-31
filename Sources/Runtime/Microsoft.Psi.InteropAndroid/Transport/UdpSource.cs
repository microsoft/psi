// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.

namespace Microsoft.Psi.Interop.Transport
{
    using System;
    using System.Net;
    using System.Net.Sockets;
    using System.Threading;
    using Microsoft.Psi;
    using Microsoft.Psi.Components;
    using Microsoft.Psi.Interop.Serialization;

    /// <summary>
    /// Component that reads and deserializes messages from a UDP socket.
    /// </summary>
    /// <typeparam name="T">The type of the messages.</typeparam>
    public class UdpSource<T> : IProducer<T>, ISourceComponent, IDisposable
    {
        private readonly Pipeline pipeline;
        private readonly bool useSourceOriginatingTimes;
        private readonly int port;
        private readonly IFormatDeserializer<T> deserializer;
        private readonly Action<T> deallocator;
        private readonly string name;
        private UdpClient udpClient;
        private Thread readerThread;
        private Action<DateTime> completed;
        private DateTime endTime = DateTime.MaxValue;

        /// <summary>
        /// Initializes a new instance of the <see cref="UdpSource{T}"/> class.
        /// </summary>
        /// <param name="pipeline">The pipeline to add the component to.</param>
        /// <param name="port">The local port on which to listen.</param>
        /// <param name="deserializer">The deserializer to use to deserialize messages.</param>
        /// <param name="deallocator">An optional deallocator for the data.</param>
        /// <param name="useSourceOriginatingTimes">An optional parameter indicating whether to use originating times received from the source over the network or to re-timestamp with the current pipeline time upon receiving.</param>
        /// <param name="name">An optional name for the component.</param>
        public UdpSource(
            Pipeline pipeline,
            int port,
            IFormatDeserializer<T> deserializer,
            Action<T> deallocator = null,
            bool useSourceOriginatingTimes = true,
            string name = nameof(UdpSource<T>))
        {
            this.port = port;
            this.deserializer = deserializer;
            this.deallocator = deallocator ?? (d =>
            {
                if (d is IDisposable disposable)
                {
                    disposable.Dispose();
                }
            });
            this.name = name;
            this.pipeline = pipeline;
            this.useSourceOriginatingTimes = useSourceOriginatingTimes;
            this.Out = pipeline.CreateEmitter<T>(this, nameof(this.Out));
        }

        /// <inheritdoc/>
        public Emitter<T> Out { get; }

        /// <inheritdoc/>
        public void Start(Action<DateTime> notifyCompletionTime)
        {
            this.SetEndTime(this.Out.Pipeline.ReplayDescriptor.End);
            this.completed = notifyCompletionTime;
            this.udpClient = new UdpClient(this.port);
            this.readerThread = new Thread(this.ReadDatagrams) { IsBackground = true };
            this.readerThread.Start();
        }

        /// <inheritdoc/>
        public void Dispose() => this.udpClient?.Close();

        /// <inheritdoc/>
        public void Stop(DateTime finalOriginatingTime, Action notifyCompleted)
        {
            this.endTime = finalOriginatingTime;
            this.udpClient?.Close();
            this.readerThread?.Join();
            notifyCompleted();
        }

        /// <inheritdoc/>
        public override string ToString() => this.name;

        /// <summary>
        /// Sets the end time for the source component.
        /// </summary>
        /// <param name="time">End time to set.</param>
        protected void SetEndTime(DateTime time)
        {
            this.endTime = time;
        }

        private void MessageHandler(T message, DateTime dateTime)
        {
            if (this.useSourceOriginatingTimes)
            {
                if (!dateTime.Equals(this.Out.LastEnvelope.OriginatingTime))
                {
                    this.Out.Post(message, dateTime);
                }
            }
            else
            {
                this.Out.Post(message, this.pipeline.GetCurrentTime());
            }
        }

        private void ReadDatagrams()
        {
            var remoteEp = new IPEndPoint(IPAddress.Any, 0);
            var lastTimestamp = DateTime.MinValue;

            try
            {
                while (true)
                {
                    var datagram = this.udpClient.Receive(ref remoteEp);

                    if (datagram.Length < sizeof(int))
                    {
                        continue;
                    }

                    var frameLength = BitConverter.ToInt32(datagram, 0);
                    if (datagram.Length < sizeof(int) + frameLength)
                    {
                        continue;
                    }

                    (var message, var originatingTime) = this.deserializer.DeserializeMessage(datagram, sizeof(int), frameLength);

                    if (originatingTime > this.endTime)
                    {
                        break;
                    }

                    lastTimestamp = originatingTime;
                    this.MessageHandler((T)message, originatingTime);
                    this.deallocator((T)message);
                }
            }
            catch (SocketException)
            {
                // Socket closed by Stop()
            }
            catch (ObjectDisposedException)
            {
                // Socket disposed
            }
            finally
            {
                this.completed?.Invoke(lastTimestamp);
            }
        }
    }
}

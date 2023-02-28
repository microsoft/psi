
namespace CMU.Smartlab.Communication
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Text;
    using System.Threading.Tasks;
    using NetMQ;
    using NetMQ.Sockets;
    using Microsoft.Psi;
    using Microsoft.Psi.Components;
    using Microsoft.Psi.Interop.Serialization;
    using Microsoft.Psi.Interop.Transport;

    /// <summary>
    /// NetMQ (ZeroMQ) subscriber component.
    /// </summary>
    /// <typeparam name="T">Message type.</typeparam>
    public class NetMQSubscriber<T> : IProducer<T>, ISourceComponent, IDisposable
    {
        private readonly string topic;
        private readonly string address;
        private readonly IFormatDeserializer deserializer;
        private readonly Pipeline pipeline;
        private readonly string name;
        private readonly bool useSourceOriginatingTimes;

        private SubscriberSocket socket;
        private NetMQPoller poller;

        /// <summary>
        /// Initializes a new instance of the <see cref="NetMQSubscriber{T}"/> class.
        /// </summary>
        /// <param name="pipeline">The pipeline to add the component to.</param>
        /// <param name="topic">Topic name.</param>
        /// <param name="address">Connection string.</param>
        /// <param name="deserializer">Format deserializer with which messages are deserialized.</param>
        /// <param name="useSourceOriginatingTimes">Flag indicating whether or not to post with originating times received over the socket. If false, we ignore them and instead use pipeline's current time.</param>
        /// <param name="name">An optional name for the component.</param>
        // public NetMQSubscriber(Pipeline pipeline, string topic, string address, IFormatDeserializer deserializer, bool useSourceOriginatingTimes = true, string name = nameof(NetMQSubscriber<T>))

        public NetMQSubscriber(Pipeline pipeline, string topic, string address, IFormatDeserializer deserializer, bool useSourceOriginatingTimes = true, string name = nameof(NetMQSubscriber<T>))
        {
            this.pipeline = pipeline;
            this.name = name;
            this.useSourceOriginatingTimes = useSourceOriginatingTimes;
            this.topic = topic;
            this.address = address;
            Console.WriteLine("NetMQSubscriber constructor - TCP address = '{0}'", this.address);
            Console.WriteLine("NetMQSubscriber constructor - topic       = '{0}'", this.topic);
            this.deserializer = deserializer;
            this.Out = pipeline.CreateEmitter<T>(this, topic);
            // this.Out = pipeline.CreateEmitter<string>(this, topic);
        }

        public Receiver<string> StringIn { get; }

        public Emitter<string> StringOut { get; }

        public Emitter<IDictionary<string,object>> IDictionaryOut { get; }


        /// <inheritdoc />
        public Emitter<T> Out { get; }

        /// <inheritdoc />
        public void Dispose()
        {
            this.Stop();
        }

        /// <inheritdoc/>
        public void Start(Action<DateTime> notifyCompletionTime)
        {
            Console.WriteLine("NetMQSubscriber Start - enter");
            // notify that this is an infinite source component
            notifyCompletionTime(DateTime.MaxValue);

            this.socket = new SubscriberSocket();
            Console.WriteLine("NetMQSubscriber.Start - TCP address = '{0}'", this.address);
            Console.WriteLine("NetMQSubscriber.Start - topic       = '{0}'", this.topic);
            this.socket.Connect(this.address);
            this.socket.Subscribe(this.topic);
            this.socket.ReceiveReady += this.ReceiveReady;
            this.poller = new NetMQPoller();
            this.poller.Add(this.socket);
            this.poller.RunAsync();

            // this.socket.SubscribeToAnyTopic(); 

        }

        /// <inheritdoc/>
        public void Stop(DateTime finalOriginatingTime, Action notifyCompleted)
        {
            this.Stop();
            notifyCompleted();
        }

        /// <inheritdoc/>
        public override string ToString() => this.name;

        private void Stop()
        {
            if (this.socket != null)
            {
                this.poller.Dispose();
                this.socket.Dispose();
                this.socket = null;
            }
        }

        private void ReceiveReady(object sender, NetMQSocketEventArgs e)
        {
            Console.WriteLine("NetMQSubscriber ReceiveReady - enter");
            var frames = new List<byte[]>();
            while (this.socket.TryReceiveMultipartBytes(ref frames, 2))
            {
                var receivedTopic = System.Text.Encoding.Default.GetString(frames[0]);
                // if (receivedTopic != this.topic)
                // {
                //     throw new Exception($"Unexpected topic name received in NetMQSubscriber. Expected {this.topic} but received {receivedTopic}");
                // }

                if (frames.Count < 2)
                {
                    throw new Exception($"No payload message received for topic: {this.topic}");
                }

                if (frames.Count > 2)
                {
                    throw new Exception($"Multiple interleaved messages received on topic: {this.topic}. Is the sender on the other side sending messages on multiple threads? You may need to add a lock over there.");
                }

                var (message, originatingTime) = this.deserializer.DeserializeMessage(frames[1], 0, frames[1].Length);

                // Console.WriteLine("NetMQSubscriber ReceiveReady - received message: '{0}'", message);

                // =====================================================================
                // Temp print first key-value pair in deserialized python dict as string

                IDictionary<string,object> messageDictionary = new Dictionary<string,object>(); 
                foreach (KeyValuePair<string,object> kvp in message) {
                    messageDictionary.Add(kvp.Key,kvp.Value); 
                    Console.WriteLine("NetMQSubscriber ReceiveReady: message - key: '{0}'  --  value: '{1}'", kvp.Key,kvp.Value);  
                }

                // int element=0;
                // string firstKey = "null";
                // string firstValue = "null"; 

                // foreach (KeyValuePair<string,object> kvp in message) {
                //     if (element == 0) {
                //         firstKey = kvp.Key;
                //         firstValue = (string)kvp.Value;
                //     }
                //     element += 1; 
                // }
                // Console.WriteLine("Message - first key:   '{0}'", firstKey); 
                // Console.WriteLine("Message - first value: '{0}'", firstValue); 
                // =====================================================================

                this.Out.Post(message, this.useSourceOriginatingTimes ? originatingTime : this.pipeline.GetCurrentTime());
            }
        }

    }
}
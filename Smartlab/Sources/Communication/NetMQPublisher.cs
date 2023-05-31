
namespace CMU.Smartlab.Communication
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using Microsoft.Psi;
    using Microsoft.Psi.Components;
    using Microsoft.Psi.Interop.Serialization;
    using Microsoft.Psi.Interop.Transport;
    using NetMQ;
    using NetMQ.Sockets;

    /// <summary>
    /// NetMQ (ZeroMQ) publisher component.
    /// </summary>
    public class NetMQPublisher : IDisposable
    {
        private readonly Pipeline pipeline;
        private readonly string name;
        private readonly IFormatSerializer serializer;
        private readonly Dictionary<string, Type> topics = new ();
        private readonly IDictionary<string,object> messageDictionary = new Dictionary<string,object>(); 

        // private PublisherSocket socket;
        public PublisherSocket socket;

        /// <summary>
        /// Initializes a new instance of the <see cref="NetMQPublisher"/> class.
        /// </summary>
        /// <param name="pipeline">The pipeline to add the component to.</param>
        /// <param name="address">Connection string.</param>
        /// <param name="serializer">Format serializer with which messages are serialized.</param>
        /// <param name="name">An optional name for the component.</param>
        public NetMQPublisher(Pipeline pipeline, string address, IFormatSerializer serializer, string name = nameof(NetMQPublisher))
        {
            this.pipeline = pipeline;
            this.name = name;
            this.Address = address;
            this.serializer = serializer;
            Console.WriteLine("NetMQPublisher constructor - TCP address = '{0}'", this.Address);
            // this.Out = pipeline.CreateEmitter<T>(this);
            // this.Out = pipeline.CreateReceiver<T>(this);
            // this.ReceiveIDictionary = pipeline.CreateReceiver<IDictionary<string,object>>(this, ReceiveIDictionary, nameof(this.IDictionaryIn));
            this.IDictionaryIn = pipeline.CreateReceiver<IDictionary<string,object>>(this, Receive, nameof(this.IDictionaryIn));
            this.socket = new PublisherSocket();
            pipeline.PipelineRun += (s, e) => this.socket.Bind(this.Address);
        }

        // public ReceiveIDictionary<T> messageIn { get; }

        // Receiver that encapsulates the Dictionary input stream
        public Receiver<IDictionary<string,object>> IDictionaryIn { get; private set; }

        public Receiver<string> StringIn { get; }

        public Emitter<string> StringOut { get; }

        /// <summary>
        /// Gets the output emitter.
        /// </summary>
        // public Emitter<T> Out { get; }

        /// <summary>
        /// Gets the connection address string.
        /// </summary>
        public string Address { get; private set; }

        /// <summary>
        /// Gets the topic names and types being published.
        /// </summary>
        public IEnumerable<(string Name, Type Type)> Topics
        {
            get { return this.topics.Select(x => (x.Key, x.Value)); }
        }

        /// <summary>
        /// Add topic receiver.
        /// </summary>
        /// <param name="topic">Topic name.</param>
        /// <typeparam name="T">Message type.</typeparam>
        /// <returns>Receiver to which to pipe messages.</returns>
        public Receiver<T> AddTopic<T>(string topic)
        {
            this.topics.Add(topic, typeof(T));
            Console.WriteLine("NetMQPublisher.Receiver - topic =   '{0}'", topic);
            return this.pipeline.CreateReceiver<T>(this, (m, e) => this.Receive(m, e), topic);
        }
        /// <inheritdoc />
        public void Dispose()
        {
            if (this.socket != null)
            {
                this.socket.Dispose();
                this.socket = null;
            }
        }

        /// <inheritdoc/>
        public override string ToString() => this.name;

        private static Boolean processIDictionary(object message)
        {

            // Console.WriteLine("NetMQPublisher processIDictionary -  enter");  
            IDictionary<string,object> dictionaryIn = (IDictionary<string,object>)message; 
            foreach (KeyValuePair<string,object> kvp in dictionaryIn) {
                Console.WriteLine("NetMQPublisher.processIDictionary: message - key: '{0}'  --  value: '{1}'", kvp.Key,kvp.Value); 
            }
            return true;
        }

        private static string stringForAgent(object message)
        { 
            IDictionary<string,object> dictionaryIn = (IDictionary<string,object>)message; 
            string response = null;
            string location = null; 
            foreach (KeyValuePair<string,object> kvp in dictionaryIn) {
                Console.WriteLine("NetMQPublisher.processIDictionary: message - key: '{0}'  --  value: '{1}'", kvp.Key,kvp.Value); 
                if (kvp.Key == "location") {
                    location = (string)kvp.Value; 
                }
                if (kvp.Key == "response") {
                    response = (string)kvp.Value; 
                }
            }
            if (response != null) {
                return response;
            } else if (location != null) {
                // return location; 
                if (location == "left") {
                    return "Rachel is looking left";
                } else if (location == "front") {
                    return "Rachel is looking straight ahead";
                } else if (location == "right") {
                    return "Rachel is looking right";
                } else {
                    Console.WriteLine($"NetMQPublisher.stringForAgent - unexpected location value: '{0}'", location);
                    return null;
                }
            } else {
                return null; 
            }
        }

        private static IDictionary<string,string> messageForAgent(object message)
        { 
            IDictionary<string,object> dictionaryIn = (IDictionary<string,object>)message; 
            IDictionary<string,string> dictionaryOut = new Dictionary<string, string>(); 
            Boolean messageFound = false; 
            foreach (KeyValuePair<string,object> kvp in dictionaryIn) {
                Console.WriteLine("NetMQPublisher.processIDictionary: message - key: '{0}'  --  value: '{1}'", kvp.Key,kvp.Value); 
                if (kvp.Key == "location") {
                    dictionaryOut.Add("location",(string)kvp.Value); 
                    messageFound = true; 
                }
                if (kvp.Key == "speech") {
                    dictionaryOut.Add("speech",(string)kvp.Value); 
                    messageFound = true; 
                }
            }
            if (messageFound) {
                return dictionaryOut;
            } else {
                return null; 
            }
        }


        // The receive method for the IDictionaryIn receiver. T
        private void Receive<T>(T messageIn, Envelope envelope)
        {
            string topic = topics.Keys.First(); 
            Console.WriteLine($"NetMQPublisher.ReceiveIDictionary - enter - topic = '{0}'", topic);
            IDictionary<string,string> messageToAgent = messageForAgent(messageIn); 
            if (messageToAgent != null) {      
                // var (bytes, index, length) = this.serializer.SerializeMessage(messageIn, envelope.OriginatingTime);   
                var (bytes, index, length) = this.serializer.SerializeMessage(messageToAgent, envelope.OriginatingTime);   
                if (index != 0)
                {
                    var slice = new byte[length];
                    Array.Copy(bytes, index, slice, 0, length);
                    bytes = slice;
                }
                this.socket.SendMoreFrame(topic).SendFrame(bytes, length);
            }
        }
    }
}

namespace CMU.Smartlab.Communication
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using Microsoft.Psi;
    using Microsoft.Psi.Components;
    using Microsoft.Psi.Interop.Serialization;
    using Microsoft.Psi.Interop.Transport;
    using Apache.NMS;
    using Apache.NMS.ActiveMQ;
    using Apache.NMS.ActiveMQ.Transport.Discovery;

// public class AMQPublisher 
    public class AMQPublisher<T>: IProducer<T>, ISourceComponent, IDisposable
    {
        private readonly Pipeline pipeline;
        private readonly string name;
        private int port = 61616; 
        public bool Occupied = false;

        private IConnectionFactory factory = null;
        private IConnection connection = null;
        private ISession session = null;
    
        private string uri;
        private string activeMQUri;
        private Dictionary<string, IMessageProducer> producerList
            = new Dictionary<string, IMessageProducer>();

        private Dictionary<string, IMessageConsumer> consumerList
            = new Dictionary<string, IMessageConsumer>();
        private string inTopic;
        private string outTopic;
        private string clientID;
        private readonly bool useSourceOriginatingTimes;
        // private Envelope envelope;

        public AMQPublisher(Pipeline pipeline, string inTopic, string outTopic, string clientID, bool useSourceOriginatingTimes = true, string name = nameof(AMQPublisher<T>))
        {
            this.inTopic = inTopic;
            this.outTopic = outTopic; 
            this.uri = string.Format("tcp://localhost:{0}", this.port);
            this.activeMQUri = uri;
            this.clientID = clientID;
            this.useSourceOriginatingTimes = useSourceOriginatingTimes;
            this.StringIn = pipeline.CreateReceiver<string>(this, ReceiveString, nameof(this.StringIn));
            this.IDictionaryIn = pipeline.CreateReceiver<IDictionary<string,object>>(this, ReceiveIDictionary, nameof(this.IDictionaryIn));
            this.Out = pipeline.CreateEmitter<T>(this, outTopic);
            // this.envelope = pipeline.Envelope;
            // subscribe(inTopic,ProcessText);
        }


        // Receiver that encapsulates the string input stream
        public Receiver<string> StringIn { get; private set; }

        // Receiver that encapsulates the Dictionary input stream
        public Receiver<IDictionary<string,object>> IDictionaryIn { get; private set; }


        public Emitter<string> StringOut { get; }


        /// <inheritdoc />
        // public Emitter<T> Out { get; }

        // Emitter that encapsulates the output stream
        // public Emitter<string> Out { get; private set; }
        public Emitter<T> Out { get; }

        // public Emitter<T> Out { get; }


        /// <inheritdoc />
        public void Dispose()
        {
            this.Stop();
        }

        /// <inheritdoc/>
        public void Stop(DateTime finalOriginatingTime, Action notifyCompleted)
        {
            this.Stop();
            notifyCompleted();
        }

        private void Stop()
        {
            // if (this.socket != null)
            // {
            //     this.poller.Dispose();
            //     this.socket.Dispose();
            //     this.socket = null;
            // }
        }

        /// <inheritdoc/>
        public void Start(Action<DateTime> notifyCompletionTime)
        {
            Console.WriteLine("AMQPublisher Start - enter");
            // notify that this is an infinite source component
            notifyCompletionTime(DateTime.MaxValue);
            Console.WriteLine("AMQPublisher.Start - InTopic:  '{0}'", this.inTopic);
            Console.WriteLine("AMQPublisher.Start - OutTopic: '{0}'", this.outTopic);
            InitActiveMQServer();

        }
        private void InitActiveMQServer()
        {
            this.factory = new NMSConnectionFactory(this.activeMQUri);
            try
            {
                this.connection = this.factory.CreateConnection();
                this.connection.ClientId = this.clientID;
                this.connection.Start();
                this.session = this.connection.CreateSession(AcknowledgementMode.AutoAcknowledge);
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.ToString());
            }
        }

        private IMessageProducer GetProducer(string topicName)
        {
            IMessageProducer producer;
            if (!this.producerList.TryGetValue(topicName, out producer))
            {
                IDestination destination = new Apache.NMS.ActiveMQ.Commands.ActiveMQTopic(topicName);
                producer = session.CreateProducer(destination);
                this.producerList.Add(topicName, producer);
            }

            return producer;
        }

        public void subscribe(string topic, Action<IMessage> listener)
        {
            IMessageConsumer consumer = this.GetConsumer(topic);
            consumer.Listener += new MessageListener(listener);
        }

        public void subscribe(string topic, Action<ITextMessage> listener)
        {
            IMessageConsumer consumer = this.GetConsumer(topic);
            consumer.Listener += new MessageListener((message) =>
            {
                if (message is ITextMessage)
                {
                    ITextMessage textMessage = (ITextMessage)message;
                    Console.WriteLine("AMQPublisher.cs: subscribe ITextMessage -- topic: " + topic + "  textMessage: " + textMessage);
                    listener.Invoke(textMessage);
                }
            });
        }

        // public void subscribe(string topic, Action<IBytesMessage> listener)
        // {
        //     IMessageConsumer consumer = this.GetConsumer(topic);
        //     consumer.Listener += new MessageListener((message) =>
        //     {
        //         if (message is IBytesMessage)
        //         {
        //             IBytesMessage bytesMessage = (IBytesMessage)message;
        //             listener.Invoke(bytesMessage);
        //         }
        //     });
        // }

        public void subscribe(string topic, Action<string> listener)
        {
            IMessageConsumer consumer = this.GetConsumer(topic);
            consumer.Listener += new MessageListener((message) =>
            {
                if (message is ITextMessage)
                {
                    string text = ((ITextMessage)message).Text;
                    Console.WriteLine("AMQPublisher.cs: subscribe string -- topic: " + topic + "  textMessage: " + text);
                    listener.Invoke(text);
                }
            });
        }

        // public void subscribe(string topic, Action<byte[]> listener)
        // {
        //     IMessageConsumer consumer = this.GetConsumer(topic);
        //     consumer.Listener += new MessageListener((message) =>
        //     {
        //         if (message is IBytesMessage)
        //         {
        //             IBytesMessage bytesMessage = (IBytesMessage)message;
        //             byte[] bytes = new byte[bytesMessage.BodyLength];
        //             ((IBytesMessage)message).ReadBytes(bytes);
        //             listener.Invoke(bytes);
        //         }
        //     });
        // }

        private IMessageConsumer GetConsumer(string topicName)
        {
            IMessageConsumer consumer;
            if (!this.consumerList.TryGetValue(topicName, out consumer))
            {
                ITopic topic = new Apache.NMS.ActiveMQ.Commands.ActiveMQTopic(topicName);
                consumer = this.session.CreateDurableConsumer(topic, "consumer for " + topicName, null, false);
                this.consumerList.Add(topicName, consumer);
            }
            return consumer;
        }

        private static string processString(String s)
        {
            string delimiter = ":"; 
            Console.WriteLine($"AMQPublisher, Process String - input: {s}");
            if (s != null)
            {
                string [] components = s.Split(delimiter); 
                if (components[0] != "location") {
                    return components[1];
                }
            }
            return null; 
        }

        private static string processIDictionary(IDictionary<string,object> dictionaryIn)
        {
            string messageToBazaar = null; 
            string identityString = null;
            string speechString = null; 
            string locationString = null; 
            string poseString = null;
            string identityValue = "psi";            // default identity; hardcoded for now
            string multimodalPreamble = "multimodal:::true"; 
            string multimodalTagDelimiter = ";%;"; 
            string multimodalValueDelimiter = ":::"; 
            
            foreach (KeyValuePair<string,object> kvp in dictionaryIn) {
                // messageDictionary.Add(kvp.Key,kvp.Value); 
                Console.WriteLine("AMQPublisher processIDictionary: message - key: '{0}'  --  value: '{1}'", kvp.Key,kvp.Value);  
                if (kvp.Value != null) {
                    if (kvp.Key == "identity") {
                        identityValue = (string)kvp.Value;
                    } else if (kvp.Key == "speech") {
                        speechString = multimodalTagDelimiter + "speech" + multimodalValueDelimiter + (string)kvp.Value;
                    } else if (kvp.Key == "location") {
                        locationString = multimodalTagDelimiter + "location" + multimodalValueDelimiter + (string)kvp.Value; 
                    } else if (kvp.Key == "pose") {
                        poseString = multimodalTagDelimiter + "pose" + multimodalValueDelimiter + (string)kvp.Value; 
                    } 
                }   
            }
            if ((speechString != null) || (poseString != null)) {
                identityString = multimodalTagDelimiter + "identity" + multimodalValueDelimiter + identityValue; 
                messageToBazaar = multimodalPreamble + identityString + speechString + locationString + poseString; 
            }
            return messageToBazaar;
        }

        /// <inheritdoc />
        // public void Dispose() {}
        // {
        //     if (this.socket != null)
        //     {
        //         this.socket.Dispose();
        //         this.socket = null;
        //     }
        // }

        /// <inheritdoc/>
        public override string ToString() => this.name;

        // The receive method for the StringIn receiver. This executes every time a message arrives on StringIn.
        private void ReceiveString(string input, Envelope envelope)
        {
            string stringIn = processString(input); 
            if (stringIn != null)
            {
                Console.WriteLine("AMQPublisher.cs, ReceiveString: sending -- outTopic: " + outTopic + "  content: " + stringIn);
                IMessageProducer producer = this.GetProducer(outTopic);
                ITextMessage message = producer.CreateTextMessage(stringIn);
                producer.Send(message, MsgDeliveryMode.Persistent, MsgPriority.Normal, TimeSpan.MaxValue);
            }
        }

        // The receive method for the IDictionaryIn receiver. T
        private void ReceiveIDictionary(IDictionary<string,object> messageIn, Envelope envelope)
        {
            string messageToBazaar = processIDictionary(messageIn); 
            if (messageToBazaar != null)
            {
                Console.WriteLine("AMQPublisher.cs, ReceiveIDictionary: sending -- outTopic: " + outTopic + "  --  message: " + messageToBazaar);
                IMessageProducer producer = this.GetProducer(outTopic);
                ITextMessage message = producer.CreateTextMessage(messageToBazaar);
                producer.Send(message, MsgDeliveryMode.Persistent, MsgPriority.Normal, TimeSpan.MaxValue);
            }
        }
    }
}


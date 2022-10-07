using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Apache.NMS;
using Apache.NMS.ActiveMQ;
using Microsoft.Psi.Imaging;

namespace CMU.Smartlab.Communication
{
    public class CommunicationManager
    {
        public bool Occupied = false;

        private IConnectionFactory factory = null;
        private IConnection connection = null;
        private ISession session = null;
        private string activeMQUri;
        private Dictionary<string, IMessageProducer> producerList
            = new Dictionary<string, IMessageProducer>();

        private Dictionary<string, IMessageConsumer> consumerList
            = new Dictionary<string, IMessageConsumer>();

        public CommunicationManager() :
            this(port: 61616)
        {
        }

        public CommunicationManager(int port) :
            this(uri: string.Format("tcp://localhost:{0}", port))
        {
        }

        public CommunicationManager(string uri)
        {
            this.activeMQUri = uri;
            InitActiveMQServer();
        }

        private void InitActiveMQServer()
        {
            this.factory = new NMSConnectionFactory(this.activeMQUri);
            try
            {
                this.connection = this.factory.CreateConnection();
                this.connection.ClientId = "Smart Office - PSI";
                this.connection.Start();
                this.session = this.connection.CreateSession(AcknowledgementMode.AutoAcknowledge);
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.ToString());
            }
        }

        public void SendText(String topicName, String content)
        {
            // Console.WriteLine("CommunicationManager.cs: SendText -- topicName: " + topicName + "  content: " + content);
            IMessageProducer producer = this.GetProducer(topicName);
            ITextMessage message = producer.CreateTextMessage(content);
            producer.Send(message, MsgDeliveryMode.Persistent, MsgPriority.Normal, TimeSpan.MaxValue);
        }

        public void SendImageAsText(String topicName, String content)
        {
            // Console.WriteLine("CommunicationManager.cs: SendText -- topicName: " + topicName + "  content: Image sent");
            IMessageProducer producer = this.GetProducer(topicName);
            ITextMessage message = producer.CreateTextMessage(content);
            producer.Send(message, MsgDeliveryMode.Persistent, MsgPriority.Normal, TimeSpan.MaxValue);
        }

        public void SendBytes(String topicName, byte[] content)
        {
            IMessageProducer producer = this.GetProducer(topicName);
            IBytesMessage message = producer.CreateBytesMessage();
            message.WriteBytes(content);
            producer.Send(message, MsgDeliveryMode.Persistent, MsgPriority.Normal, TimeSpan.MaxValue);
        }

        public void SendBytes(String topicName, byte[] content, int offset, int length)
        {
            IMessageProducer producer = this.GetProducer(topicName);
            IBytesMessage message = producer.CreateBytesMessage();
            message.WriteBytes(content, offset, length);
            producer.Send(message, MsgDeliveryMode.Persistent, MsgPriority.Normal, TimeSpan.MaxValue);
        }


        public void SendImage(String topic, Image img)
        {
            this.SendImageAsText(topic, Convert.ToBase64String(img.ReadBytes(img.Size)));
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
                    // Console.WriteLine("CommunicationManager.cs: subscribe ITextMessage -- topic: " + topic + "  textMessage: " + textMessage);
                    listener.Invoke(textMessage);
                }
            });
        }

        public void subscribe(string topic, Action<IBytesMessage> listener)
        {
            IMessageConsumer consumer = this.GetConsumer(topic);
            consumer.Listener += new MessageListener((message) =>
            {
                if (message is IBytesMessage)
                {
                    IBytesMessage bytesMessage = (IBytesMessage)message;
                    listener.Invoke(bytesMessage);
                }
            });
        }

        public void subscribe(string topic, Action<string> listener)
        {
            IMessageConsumer consumer = this.GetConsumer(topic);
            consumer.Listener += new MessageListener((message) =>
            {
                if (message is ITextMessage)
                {
                    string text = ((ITextMessage)message).Text;
                    // Console.WriteLine("CommunicationManager.cs: subscribe string -- topic: " + topic + "  textMessage: " + text);
                    listener.Invoke(text);
                }
            });
        }

        public void subscribe(string topic, Action<byte[]> listener)
        {
            IMessageConsumer consumer = this.GetConsumer(topic);
            consumer.Listener += new MessageListener((message) =>
            {
                if (message is IBytesMessage)
                {
                    IBytesMessage bytesMessage = (IBytesMessage)message;
                    byte[] bytes = new byte[bytesMessage.BodyLength];
                    ((IBytesMessage)message).ReadBytes(bytes);
                    listener.Invoke(bytes);
                }
            });
        }

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
    }
}

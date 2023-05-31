using NetMQ;
using NetMQ.Sockets;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CMU.Smartlab.Communication
{
    public class NetMqSubscriber
    {
            private SubscriberSocket _subscriberSocket = null;
            private string _endpoint = @"tcp://127.0.0.1:9876";
        public NetMqSubscriber()
        {
            _subscriberSocket = new SubscriberSocket();
            _endpoint = @"tcp://127.0.0.1:9876";
        }
        public NetMqSubscriber(string endPoint)
            {
                _subscriberSocket = new SubscriberSocket();
                _endpoint = endPoint;
            }


            public void Dispose()
            {
                throw new NotImplementedException();
            }


            public event Action<string, string> Nofity = delegate { };


            public void RegisterSubscriber(List<string> topics)
            {
                InnerRegisterSubscriber(topics);
            }


            public void RegisterSbuscriberAll()
            {
                InnerRegisterSubscriber();
            }


            public void RemoveSbuscriberAll()
            {
                InnerStop();
            }

            /// <summary>
            /// regist subscribe
            /// </summary>
            /// <param name="topics">topic</param>
            public void InnerRegisterSubscriber(List<string> topics = null)
            {
                InnerStop();
                _subscriberSocket = new SubscriberSocket();
                _subscriberSocket.Options.ReceiveHighWatermark = 1000;
                _subscriberSocket.Connect(_endpoint);
                if (null == topics)
                {
                    _subscriberSocket.SubscribeToAnyTopic();
                }
                else
                {
                    topics.ForEach(item => _subscriberSocket.Subscribe(item));
                }
                Task.Factory.StartNew(() =>
                {
                    while (true)
                    {
                        string messageTopicReceived = _subscriberSocket.ReceiveFrameString();
                        string messageReceived = _subscriberSocket.ReceiveFrameString();
                        Nofity(messageTopicReceived, messageReceived);
                    }
                });
            }

        public void RegisterSubscriber(string topic)
        {
            InnerStop();
            _subscriberSocket = new SubscriberSocket();
            _subscriberSocket.Options.ReceiveHighWatermark = 1000;
            _subscriberSocket.Connect(_endpoint);
            _subscriberSocket.Subscribe(topic);
            Task.Factory.StartNew(() =>
            {
                while (true)
                {
                    string messageTopicReceived = _subscriberSocket.ReceiveFrameString();
                    string messageReceived = _subscriberSocket.ReceiveFrameString();
                    Nofity(messageTopicReceived, messageReceived);
                }
            });
        }

        /// <summary>
        /// close subscribe
        /// </summary>
        private void InnerStop()
            {
                _subscriberSocket.Close();
            }

        }
   
}

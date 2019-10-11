using System.Collections.Generic;
using NetMQ;
using NetMQ.Sockets;
using MessagePack;

namespace PupilLabs
{
    public class Publisher
    {
        private PublisherSocket publisherSocket;
        private bool isSetup = false;

        public Publisher(RequestController requestController)
        {
            if (requestController.IsConnected)
            {
                Setup(requestController);
            }
            else
            {
                requestController.OnConnected += 
                (
                    () =>
                    {
                        Setup(requestController);
                    }
                );
            }

        }

        public void Send(string topic, Dictionary<string, object> data, byte [] thirdFrame = null)
        {
            NetMQMessage m = new NetMQMessage();

            m.Append(topic);
            m.Append(MessagePackSerializer.Serialize<Dictionary<string, object>>(data));
            
            if (thirdFrame != null)
            {
                m.Append(thirdFrame);
            }

            publisherSocket.SendMultipartMessage(m);
        }

        private void Setup(RequestController requestController)
        {
            publisherSocket = new PublisherSocket(requestController.GetPubConnectionString());
        }
    }
}

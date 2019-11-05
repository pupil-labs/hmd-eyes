using System.Collections.Generic;
using NetMQ;
using NetMQ.Sockets;
using MessagePack;

namespace PupilLabs
{
    public class Publisher
    {
        private RequestController requestController;
        private PublisherSocket publisherSocket;
        private bool isSetup = false;
        private bool waitingOnConnection = false;

        public Publisher(RequestController requestController)
        {
            this.requestController = requestController;
            
            if (requestController.IsConnected)
            {
                Setup();
            }
            else
            {
                waitingOnConnection = true;
                requestController.OnConnected += DelayedSetup;
            }
        }

        public void Destroy()
        {
            if (waitingOnConnection)
            {
                requestController.OnConnected -= DelayedSetup;
            }

            if (isSetup)
            {
                if (publisherSocket != null)
                {
                    publisherSocket.Close();
                }
            }    
        }

        public void Send(string topic, Dictionary<string, object> data, byte[] thirdFrame = null)
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

        private void DelayedSetup()
        {
            waitingOnConnection = false;
            requestController.OnConnected -= DelayedSetup;
            Setup();
        }

        private void Setup()
        {
            publisherSocket = new PublisherSocket(requestController.GetPubConnectionString());
            isSetup = true;
        }
    }
}

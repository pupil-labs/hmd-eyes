using System.Collections.Generic;
using UnityEngine;
using NetMQ;
using NetMQ.Sockets;
using MessagePack;
using System.IO;

namespace PupilLabs
{

    public partial class SubscriptionsController : MonoBehaviour
    {
        private class Subscription
        {
            public string topic;
            private SubscriberSocket socket;

            public event ReceiveDataDelegate OnReceiveData;

            public bool HasSubscribers
            {
                get { return OnReceiveData != null; }
            }

            public bool ShouldClose { get; set; }

            public Subscription(string connection, string topic)
            {
                this.topic = topic;
                socket = new SubscriberSocket(connection);
                socket.Subscribe(topic);

                socket.ReceiveReady += ParseData;
            }

            public void ParseData(object s, NetMQSocketEventArgs eventArgs)
            {
                NetMQMessage m = new NetMQMessage();

                while (eventArgs.Socket.TryReceiveMultipartMessage(ref m))
                {
                    string msgType = m[0].ConvertToString();
                    MemoryStream mStream = new MemoryStream(m[1].ToByteArray());

                    byte[] thirdFrame = null;
                    if (m.FrameCount >= 3)
                    {
                        thirdFrame = m[2].ToByteArray();
                    }

                    if (OnReceiveData != null)
                    {
                        OnReceiveData(msgType, MessagePackSerializer.Deserialize<Dictionary<string, object>>(mStream), thirdFrame);
                    }
                }
            }

            public void UpdateSocket()
            {
                if (socket.HasIn)
                {
                    socket.Poll();
                }
            }

            public void Close()
            {
                socket.Close();
            }
        }
    }
}

using System;
using System.IO;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using NetMQ;
using NetMQ.Sockets;
using MessagePack;

namespace PupilLabs
{

    public class Stream : MonoBehaviour
    {

        [SerializeField]
        private PupilLabs.Connection connection = new PupilLabs.Connection();
        [SerializeField]
        private bool printMessageTopic = false;
        [SerializeField]
        private bool printMessage = false;

        public delegate void ConnectionDelegate();
        public event ConnectionDelegate OnConnected;
        public event ConnectionDelegate OnDisconnecting;

        public bool IsConnected { get { return connection.IsConnected; } }

        public delegate void ReceiveDataDelegate(string topic, Dictionary<string, object> dictionary, byte[] thirdFrame = null);

        private class Subscription
        {
            public string topic;
            public SubscriberSocket socket;
            public event ReceiveDataDelegate OnReceiveData;

            private MemoryStream mStream; //TODO why as member
            public void ParseData(object s, NetMQSocketEventArgs eventArgs)
            {
                NetMQMessage m = new NetMQMessage();

                while (eventArgs.Socket.TryReceiveMultipartMessage(ref m))
                {
                    string msgType = m[0].ConvertToString();
                    mStream = new MemoryStream(m[1].ToByteArray());
                    byte[] thirdFrame = null;
                    if (m.FrameCount >= 3)
                        thirdFrame = m[2].ToByteArray();

                    if (OnReceiveData != null)
                    {
                        OnReceiveData(msgType, MessagePackSerializer.Deserialize<Dictionary<string, object>>(mStream), thirdFrame);
                    }
                }
            }
        }

        private Dictionary<string, Subscription> subscriptions = new Dictionary<string, Subscription>();
        private List<string> subscriptionSocketToBeClosed = new List<string>();

        void OnEnable()
        {
            if (!connection.IsConnected)
            {
                StartCoroutine(Connect(retry: true, retryDelay: 5f));
            }
        }

        void OnDisable()
        {
            if (connection.IsConnected)
            {
                Disconnect();
            }
        }

        void Update()
        {
            if (connection.IsConnected)
            {
                UpdateSubscriptionSockets();
            }
        }

        public IEnumerator Connect(bool retry = false, float retryDelay = 5f)
        //TODO crash on cancel while trying to connect
        {
            yield return new WaitForSeconds(3f);

            while (!connection.IsConnected)
            {
                connection.InitializeRequestSocket();

                if (!connection.IsConnected)
                {
                    if (retry)
                    {
                        Debug.Log("Could not connect, Re-trying in 5 seconds ! ");
                        yield return new WaitForSeconds(retryDelay);

                    }
                    else
                    {
                        connection.TerminateContext();
                        yield return null;
                    }
                }
            }
            Debug.Log(" Succesfully connected to Pupil! ");

            // RepaintGUI(); //
            if (OnConnected != null)
                OnConnected();
            yield break;
        }

        public void Disconnect()
        {
            foreach (var socketKey in subscriptions.Keys)
                CloseSubscriptionSocket(socketKey);
            UpdateSubscriptionSockets();

            connection.CloseSockets();

            if (OnDisconnecting != null){
                OnDisconnecting();
            }
        }

        public void InitializeSubscriptionSocket(string topic, ReceiveDataDelegate subscriberHandler)
        {
            if (!subscriptions.ContainsKey(topic))
            {
                string connectionStr = connection.GetConnectionString();
                Subscription subscription = new Subscription();
                subscription.socket = new SubscriberSocket(connectionStr);
                subscription.topic = topic;

                subscriptions.Add(topic, subscription);
                subscriptions[topic].socket.Subscribe(topic);

                subscriptions[topic].socket.ReceiveReady += subscriptions[topic].ParseData;
                // subscriptions[topic].OnReceiveData += Logging;
            }

            subscriptions[topic].OnReceiveData += subscriberHandler;
        }

        public void CloseSubscriptionSocket(string topic, ReceiveDataDelegate subscriberHandler = null)
        {
            if (subscriptionSocketToBeClosed == null)
                subscriptionSocketToBeClosed = new List<string>();
            if (!subscriptionSocketToBeClosed.Contains(topic))
                subscriptionSocketToBeClosed.Add(topic);

            if (subscriptions.ContainsKey(topic) && subscriberHandler != null)
            {
                subscriptions[topic].OnReceiveData -= subscriberHandler;
            }
        }

        private void UpdateSubscriptionSockets()
        {
            // Poll all sockets
            foreach (var subscription in subscriptions.Values)
            {
                if (subscription.socket.HasIn)
                {
                    subscription.socket.Poll();
                }
            }

            // Check sockets to be closed
            for (int i = subscriptionSocketToBeClosed.Count - 1; i >= 0; i--)
            {
                var toBeClosed = subscriptionSocketToBeClosed[i];
                if (subscriptions.ContainsKey(toBeClosed))
                {
                    subscriptions[toBeClosed].socket.Close();
                    subscriptions.Remove(toBeClosed);
                }
                subscriptionSocketToBeClosed.Remove(toBeClosed);
            }
        }

        public bool Send(Dictionary<string, object> dictionary)
        {
            if (!connection.IsConnected)
            {
                return false;
            }
            return connection.sendRequestMessage(dictionary);
        }

        private void Logging(string topic, Dictionary<string, object> dictionary, byte[] thirdFrame = null)
        {
            if (printMessageTopic)
            {
                Debug.Log(topic);
            }

            if (printMessage)
            {
                Debug.Log(MessagePackSerializer.Serialize<Dictionary<string, object>>(dictionary));
            }
        }
    }
}
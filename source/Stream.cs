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
        private PupilLabs.Connection connection;
        [SerializeField]
        private bool printMessageType = false;
        [SerializeField]
        private bool printMessage = false;

        public delegate void ConnectionDelegate ();
        public event ConnectionDelegate OnConnected;
	    public event ConnectionDelegate OnDisconnecting;

        public delegate void ReceiveDataDelegate (string topic, Dictionary<string,object> dictionary, byte[] thirdFrame = null);
        public event ReceiveDataDelegate OnReceiveData;

        private Dictionary<string, SubscriberSocket> subscriptionSocketForTopic;
        private Dictionary<string, SubscriberSocket> SubscriptionSocketForTopic
        {
            get
            {
                if (subscriptionSocketForTopic == null)
                    subscriptionSocketForTopic = new Dictionary<string, SubscriberSocket>();
                return subscriptionSocketForTopic;
            }
        }
        private List<string> subscriptionSocketToBeClosed = new List<string>();

        void OnEnable()
        {
            if(!connection.IsConnected)
            {
                StartCoroutine (Connect (retry: true, retryDelay: 5f));
            }
        }

        void OnDisable()
        {
            if(connection.IsConnected)
            {
                Disconnect();
            }
        }

        void Update()
        {
            if(connection.IsConnected)
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
                //yield return null;
            }
            Debug.Log(" Succesfully connected to Pupil! ");

            // RepaintGUI(); //TODO what for?
            if (OnConnected != null)
                OnConnected();
            yield break;
        }

        public void Disconnect()
        {
            foreach (var socketKey in SubscriptionSocketForTopic.Keys)
                CloseSubscriptionSocket(socketKey);
            UpdateSubscriptionSockets();

            connection.CloseSockets();
        }

        private MemoryStream mStream; //TODO why as member
        public void InitializeSubscriptionSocket(string topic) //TODO = Subscribe
        {
            if (!SubscriptionSocketForTopic.ContainsKey(topic))
            {
                string connectionStr = connection.GetConnectionString();
                SubscriptionSocketForTopic.Add(topic, new SubscriberSocket(connectionStr));
                SubscriptionSocketForTopic[topic].Subscribe(topic);

                SubscriptionSocketForTopic[topic].ReceiveReady += (s, a) => //TODO = Receive
                {
                    int i = 0;

                    NetMQMessage m = new NetMQMessage();

                    while (a.Socket.TryReceiveMultipartMessage(ref m))
                    {


                        string msgType = m[0].ConvertToString();
                        mStream = new MemoryStream(m[1].ToByteArray());
                        byte[] thirdFrame = null;
                        if (m.FrameCount >= 3)
                            thirdFrame = m[2].ToByteArray();

                        if (printMessageType)
                        {
                            Debug.Log(msgType);
                        }

                        if (printMessage)
                        {
                            Debug.Log(MessagePackSerializer.ToJson(m[1].ToByteArray()));
                        }

                        if (OnReceiveData!=null)
                        {
                            OnReceiveData(msgType, MessagePackSerializer.Deserialize<Dictionary<string, object>>(mStream), thirdFrame);
                        }

                        // removed parsing message -> should all happen via delegates

                        i++;
                    }
                };
            }
        }

        public void CloseSubscriptionSocket(string topic) //TODO what if we have >= 2 subscribers?
        {
            if (subscriptionSocketToBeClosed == null)
                subscriptionSocketToBeClosed = new List<string>();
            if (!subscriptionSocketToBeClosed.Contains(topic))
                subscriptionSocketToBeClosed.Add(topic);
        }

        private void UpdateSubscriptionSockets()
        //TODO split? 
        //called every frame to A: poll sockets and B: maybe delete some
        //originally only called by PupilGazeTracker 
        //TODO what about Blink Demo without gazetracker? it actually needs one ...
        {
            // Poll all sockets
            //TODO replace by foreach
            string[] keys = new string[SubscriptionSocketForTopic.Count];
            SubscriptionSocketForTopic.Keys.CopyTo(keys, 0);
            for (int i = 0; i < keys.Length; i++)
            {
                if (SubscriptionSocketForTopic[keys[i]].HasIn)
                    SubscriptionSocketForTopic[keys[i]].Poll();
            }
            // Check sockets to be closed
            //TODO handle double unsubscribe (remove from list if nothing to unsubscribe from)
            for (int i = subscriptionSocketToBeClosed.Count - 1; i >= 0; i--)
            {
                var toBeClosed = subscriptionSocketToBeClosed[i];
                if (SubscriptionSocketForTopic.ContainsKey(toBeClosed))
                {
                    SubscriptionSocketForTopic[toBeClosed].Close();
                    SubscriptionSocketForTopic.Remove(toBeClosed);
                }
                subscriptionSocketToBeClosed.Remove(toBeClosed);
            }
        }

        public bool Send(Dictionary<string,object> dictionary)
        {
            if(!connection.IsConnected)
            {
                return false;
            }
            return connection.sendRequestMessage (dictionary);
        }
    }
}


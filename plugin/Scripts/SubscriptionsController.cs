using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using NetMQ.Sockets;
using MessagePack;

namespace PupilLabs
{

    public partial class SubscriptionsController : MonoBehaviour
    {

        [SerializeField]
        private PupilLabs.Connection connection = new PupilLabs.Connection();
        // [SerializeField]
        // private bool printMessageTopic = false;
        // [SerializeField]
        // private bool printMessage = false;

        public delegate void ConnectionDelegate();
        public event ConnectionDelegate OnConnected;
        public event ConnectionDelegate OnDisconnecting;

        public bool IsConnected { get { return connection.IsConnected; } }

        public delegate void ReceiveDataDelegate(string topic, Dictionary<string, object> dictionary, byte[] thirdFrame = null);

        private Dictionary<string, Subscription> subscriptions = new Dictionary<string, Subscription>();
        
        void OnEnable()
        {
            if (!connection.IsConnected)
            {
                RunConnect();
                //RequestController.Connect();
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
            if (connection.IsConnected) //TODO needed?
            {
                UpdateSubscriptionSockets(); 
            }
        }

        public void RunConnect()
        {
            StartCoroutine(Connect(retry: true, retryDelay: 5f));
        }

        private IEnumerator Connect(bool retry = false, float retryDelay = 5f)
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

        public void SubscribeTo(string topic, ReceiveDataDelegate subscriberHandler)
        {
            if (!subscriptions.ContainsKey(topic))
            {
                string connectionStr = connection.GetConnectionString();
                Subscription subscription = new Subscription(connectionStr, topic);
                
                subscriptions.Add(topic, subscription);
                // subscriptions[topic].OnReceiveData += Logging; //TODO would keep the socket open forever
            }

            subscriptions[topic].OnReceiveData += subscriberHandler;
        }

        public void UnsubscribeFrom(string topic, ReceiveDataDelegate subscriberHandler)
        {
            if (subscriptions.ContainsKey(topic) && subscriberHandler != null)
            {
                subscriptions[topic].OnReceiveData -= subscriberHandler;

                if(!subscriptions[topic].HasSubscribers)
                {
                    CloseSubscriptionSocket(topic);
                }
            }
        }

        private void CloseSubscriptionSocket(string topic)
        {
            if (subscriptions.ContainsKey(topic))
            {
                subscriptions[topic].ShouldClose = true;
            }
        }

        private void UpdateSubscriptionSockets()
        {

            List<string> toBeRemoved = new List<string>();
            foreach (var subscription in subscriptions.Values)
            {
                if (!subscription.ShouldClose)
                {
                    subscription.UpdateSocket();
                }
                else
                {
                    subscription.Close();
                    toBeRemoved.Add(subscription.topic);
                }
            }

            foreach (var removeTopic in toBeRemoved)
            {
                if( subscriptions.ContainsKey(removeTopic))
                {
                    subscriptions.Remove(removeTopic);
                }
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

        // private void Logging(string topic, Dictionary<string, object> dictionary, byte[] thirdFrame = null)
        // {
        //     if (printMessageTopic)
        //     {
        //         Debug.Log(topic);
        //     }

        //     if (printMessage)
        //     {
        //         Debug.Log(MessagePackSerializer.Serialize<Dictionary<string, object>>(dictionary));
        //     }
        // }
    }
}
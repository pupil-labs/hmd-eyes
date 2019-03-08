using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace PupilLabs
{
    // [RequireComponent(typeof(RequestController))]
    public partial class SubscriptionsController : MonoBehaviour
    {

        public PupilLabs.RequestController requestCtrl;
        // [SerializeField]
        // private bool printMessageTopic = false;
        // [SerializeField]
        // private bool printMessage = false;

        public bool IsConnected { get { return requestCtrl.IsConnected; } }

        public delegate void ReceiveDataDelegate(string topic, Dictionary<string, object> dictionary, byte[] thirdFrame = null);

        private Dictionary<string, Subscription> subscriptions = new Dictionary<string, Subscription>();

        void OnEnable()
        {
            if (requestCtrl != null)
            {
                requestCtrl.OnDisconnecting += Disconnect;
            }
        }

        void OnDisable()
        {
            Disconnect();
        }

        void Update()
        {
            UpdateSubscriptionSockets();
        }

        public void Disconnect()
        {
            foreach (var socketKey in subscriptions.Keys)
                CloseSubscriptionSocket(socketKey);
            UpdateSubscriptionSockets();
        }

        public void SubscribeTo(string topic, ReceiveDataDelegate subscriberHandler)
        {
            if (!subscriptions.ContainsKey(topic))
            {
                string connectionStr = requestCtrl.GetConnectionString();
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

                if (!subscriptions[topic].HasSubscribers)
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
                if (subscriptions.ContainsKey(removeTopic))
                {
                    subscriptions.Remove(removeTopic);
                }
            }
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
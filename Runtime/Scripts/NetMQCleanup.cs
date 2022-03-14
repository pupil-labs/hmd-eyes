using NetMQ;
using System.Collections.Generic;
using UnityEngine;

namespace PupilLabs
{
    public static class NetMQCleanup
    {
        private static List<RequestController> connections = new List<RequestController>(); 

        public static void MonitorConnection(RequestController connection)
        {
            connections.Add(connection);
        }

        public static void CleanupConnection(RequestController connection)
        {
            connections.Remove(connection);

            if (connections.Count == 0)
            {
                Debug.Log("Terminate Context.");
                NetMQConfig.Cleanup(block: false);
            }
        }

        // static NetMQCleanup()
        // {
        //     Application.quitting += () => {NetMQConfig.Cleanup(block: false);};
        // }
    }
}
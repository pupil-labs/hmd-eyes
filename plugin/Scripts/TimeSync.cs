using System;
using System.Collections.Generic;
using UnityEngine;
using NetMQ;
using NetMQ.Sockets;
using MessagePack;

namespace PupilLabs
{
    public class TimeSync : MonoBehaviour
    {
        [SerializeField] RequestController requestCtrl;
        
        public double UnityToPupilTimeOffset { get; private set; }

        void OnEnable()
        {
            requestCtrl.OnConnected += UpdateTimeSync;
        }
        
        public double GetPupilTimestamp()
        {
            if (!requestCtrl.IsConnected)
            {
                Debug.LogWarning("Not connected");
                return 0;
            }

            string response;
            requestCtrl.SendCommand("t", out response);

            return double.Parse(response, System.Globalization.CultureInfo.InvariantCulture.NumberFormat); ;
        }

        public double ConvertToUnityTime(double pupilTimestamp)
        {
            if (!requestCtrl.IsConnected)
            {
                Debug.LogWarning("Not connected");
                return 0;
            }

            return pupilTimestamp - UnityToPupilTimeOffset;
        }

        public double ConvertToPupilTime(double unityTime)
        {
            if (!requestCtrl.IsConnected)
            {
                Debug.LogWarning("Not connected");
                return 0;
            }

            return unityTime + UnityToPupilTimeOffset;
        }

        [ContextMenu("Update TimeSync")]
        public void UpdateTimeSync()
        {
            if (!requestCtrl.IsConnected)
            {
                Debug.LogWarning("Not connected");
                return;
            }

            double tBefore = Time.realtimeSinceStartup;
            double pupilTime = GetPupilTimestamp();
            double tAfter = Time.realtimeSinceStartup;

            double unityTime = (tBefore + tAfter) / 2.0;
            UnityToPupilTimeOffset = pupilTime - unityTime;
        }

        [System.Obsolete("Setting the pupil timestamp might be in conflict with other plugins.")]
        public void SetPupilTimestamp(double time)
        {
            if (!requestCtrl.IsConnected)
            {
                Debug.LogWarning("Not connected");
                return;
            }

            string response;
            string command = "T " + time.ToString("0.000000", System.Globalization.CultureInfo.InvariantCulture);

            float tBefore = Time.realtimeSinceStartup;
            requestCtrl.SendCommand(command, out response);
            float tAfter = Time.realtimeSinceStartup;

            UnityToPupilTimeOffset = -(tAfter - tBefore) / 2f;
        }

        [ContextMenu("Check Time Sync")]
        public void CheckTimeSync()
        {
            if (!requestCtrl.IsConnected)
            {
                Debug.LogWarning("Check Time Sync: not connected");
                return;
            }
            double pupilTime = GetPupilTimestamp();
            double unityTime = Time.realtimeSinceStartup;
            Debug.Log($"Unity time: {unityTime}");
            Debug.Log($"Pupil Time: {pupilTime}");
            Debug.Log($"Unity to Pupil Offset {UnityToPupilTimeOffset}");
            Debug.Log($"out of sync by {unityTime + UnityToPupilTimeOffset - pupilTime}");
        }

        // [ContextMenu("Sync Pupil Time To Time.now")]
        // void SyncPupilTimeToUnityTime()
        // {
        //     if (requestCtrl.IsConnected)
        //     {
        //         SetPupilTimestamp(Time.realtimeSinceStartup);
        //     }
        // }
    }
}
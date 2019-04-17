using System;
using System.Collections.Generic;
using UnityEngine;
using NetMQ;
using NetMQ.Sockets;
using MessagePack;

namespace PupilLabs
{

    public partial class RequestController
    {

        [System.Serializable]
        private class TimeSync
        {
            public TimeSync(Request request)
            {
                this.request = request;
            }

            public float UnityToPupilTimeOffset { get; private set; }

            private Request request;

            public void SetPupilTimestamp(float time)
            {
                string response;
                string command = "T " + time.ToString("0.000000", System.Globalization.CultureInfo.InvariantCulture);
                
                float tBefore = Time.realtimeSinceStartup;
                request.SendCommand(command, out response);
                float tAfter = Time.realtimeSinceStartup;

                UnityToPupilTimeOffset = -(tAfter - tBefore)/2f;
            }

            public float GetPupilTimestamp()
            {
                string response;
                bool success = request.SendCommand("t", out response);

                if (!success)
                {
                    Debug.LogWarning("GetPupilTimestamp: not connected!");
                    return -1;
                }

                return float.Parse(response,System.Globalization.CultureInfo.InvariantCulture.NumberFormat);;
            }

            public float ConvertToUnityTime(float pupilTimestamp)
            {
                return pupilTimestamp - UnityToPupilTimeOffset;
            }

            public float ConvertToPupilTime(float unityTime)
            {
                return unityTime + UnityToPupilTimeOffset;
            }

            public void UpdateTimeSync()
            {
                float tBefore = Time.realtimeSinceStartup;
                float pupilTime = GetPupilTimestamp();
                float tAfter = Time.realtimeSinceStartup;
        
                float unityTime = (tBefore+tAfter)/2f;
                UnityToPupilTimeOffset = pupilTime - unityTime;
            }

            public void CheckTimeSync()
            {
                float pupilTime = GetPupilTimestamp();
                float unityTime = Time.realtimeSinceStartup;
                Debug.Log($"Unity time: {unityTime}");
                Debug.Log($"Pupil Time: {pupilTime}");
                Debug.Log($"Unity to Pupil Offset {UnityToPupilTimeOffset}");
                Debug.Log($"out of sync by {unityTime+UnityToPupilTimeOffset-pupilTime}");
            }
        }
    }
}
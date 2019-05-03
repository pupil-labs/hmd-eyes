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

            public double UnityToPupilTimeOffset { get; private set; }

            private Request request;

            public void SetPupilTimestamp(double time)
            {
                string response;
                string command = "T " + time.ToString("0.000000", System.Globalization.CultureInfo.InvariantCulture);

                float tBefore = Time.realtimeSinceStartup;
                request.SendCommand(command, out response);
                float tAfter = Time.realtimeSinceStartup;

                UnityToPupilTimeOffset = -(tAfter - tBefore) / 2f;
            }

            public double GetPupilTimestamp()
            {
                string response;
                bool success = request.SendCommand("t", out response);

                if (!success)
                {
                    Debug.LogWarning("GetPupilTimestamp: not connected!");
                    return -1;
                }

                return double.Parse(response, System.Globalization.CultureInfo.InvariantCulture.NumberFormat); ;
            }

            public double ConvertToUnityTime(double pupilTimestamp)
            {
                return pupilTimestamp - UnityToPupilTimeOffset;
            }

            public double ConvertToPupilTime(double unityTime)
            {
                return unityTime + UnityToPupilTimeOffset;
            }

            public void UpdateTimeSync()
            {
                double tBefore = Time.realtimeSinceStartup;
                double pupilTime = GetPupilTimestamp();
                double tAfter = Time.realtimeSinceStartup;

                double unityTime = (tBefore + tAfter) / 2.0;
                UnityToPupilTimeOffset = pupilTime - unityTime;
            }

            public void CheckTimeSync()
            {
                double pupilTime = GetPupilTimestamp();
                double unityTime = Time.realtimeSinceStartup;
                Debug.Log($"Unity time: {unityTime}");
                Debug.Log($"Pupil Time: {pupilTime}");
                Debug.Log($"Unity to Pupil Offset {UnityToPupilTimeOffset}");
                Debug.Log($"out of sync by {unityTime + UnityToPupilTimeOffset - pupilTime}");
            }
        }
    }
}
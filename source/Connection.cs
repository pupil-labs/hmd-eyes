using System;
using System.IO;
using System.Collections.Generic;
using UnityEngine;
using NetMQ;
using NetMQ.Sockets;
using MessagePack;

namespace PupilLabs
{
	[Serializable]
    public class Connection
    {

		public string IP = "127.0.0.1";
		public int PORT = 50020;
		private string IPHeader;
		private string subport = "59485";

		private RequestSocket requestSocket = null;
		private bool contextExists = false;
		private TimeSpan requestTimeout = new System.TimeSpan (0, 0, 1); //= 1sec
        
		private bool isConnected = false;
        public bool IsConnected { get; set; }

		private string PupilVersion;
		[HideInInspector]
		public List<int> PupilVersionNumbers;

		// private bool isLocal = true; //TODO check again: only used via inspector

		public string GetConnectionString()
		{
			return IPHeader + subport;
		}
		
		public void InitializeRequestSocket() //TODO default params useful?
		{
			IPHeader = ">tcp://" + IP + ":";

			Debug.Log ("Attempting to connect to : " + IPHeader + PORT);

			if (!contextExists)
			{
				CreateContext();
			}

			requestSocket = new RequestSocket (IPHeader + PORT);
			requestSocket.SendFrame ("SUB_PORT");
			IsConnected = requestSocket.TryReceiveFrameString (requestTimeout, out subport);
			if (IsConnected)
			{
				UpdatePupilVersion ();
			}
		}

		public void CloseSockets()
		{
			if (requestSocket != null)
				requestSocket.Close ();

			TerminateContext ();

			IsConnected = false;
		}

		public bool sendRequestMessage (Dictionary<string,object> data)
		{
			if (requestSocket != null && IsConnected)
			{
				NetMQMessage m = new NetMQMessage ();

				m.Append ("notify." + data ["subject"]);
				m.Append (MessagePackSerializer.Serialize<Dictionary<string,object>> (data));

				requestSocket.SendMultipartMessage (m);
				return receiveRequestResponse ();
			}
			return false;
		}

		private bool receiveRequestResponse ()
		{
			// we are currently not doing anything with this
			NetMQMessage m = new NetMQMessage ();
			return requestSocket.TryReceiveMultipartMessage (requestTimeout, ref m);
		}

		
		private void UpdatePupilVersion()
		{
			requestSocket.SendFrame ("v");
			if (requestSocket.TryReceiveFrameString (requestTimeout, out PupilVersion))
			{
				if (PupilVersion != null && PupilVersion != "Unknown command.")
				{
					Debug.Log (PupilVersion);
					var split = PupilVersion.Split ('.');
					PupilVersionNumbers = new List<int> ();
					int number;
					foreach (var item in split)
					{
						if (int.TryParse (item, out number))
							PupilVersionNumbers.Add (number);
					}
				}
			}
		}

		public void SetPupilTimestamp(float time)
		{
			if (requestSocket != null)
			{
				requestSocket.SendFrame ("T " + time.ToString ("0.00000000"));
				receiveRequestResponse ();
			}
		}

		private void CreateContext()
		{
			AsyncIO.ForceDotNet.Force ();
			NetMQConfig.ManualTerminationTakeOver ();
			NetMQConfig.ContextCreate (true);
			contextExists = true;
		}

		public void TerminateContext()
		{
			if (contextExists)
			{
				NetMQConfig.ContextTerminate (true);
				contextExists = false;
			}
		}
	}
}
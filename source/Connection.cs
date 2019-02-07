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
        private bool isConnected = false;
        public bool IsConnected { get; set; }

		public string IP = "127.0.0.1";
		public int PORT = 50020;
		private string IPHeader;
		private string subport = "59485";

		[SerializeField]
		private bool isLocal = true; //TODO check again: only used via inspector

		public string GetConnectionString()
		{
			return IPHeader + subport;
		}
		
		private RequestSocket requestSocket = null;

		private bool _contextExists = false;
		private bool contextExists
		{
			get { return _contextExists; }
			set { _contextExists = value; }
		}
		private TimeSpan timeout = new System.TimeSpan (0, 0, 1); //1sec
		public void InitializeRequestSocket()
		{
			IPHeader = ">tcp://" + IP + ":";

			Debug.Log ("Attempting to connect to : " + IPHeader + PORT);

			if (!contextExists)
			{
				AsyncIO.ForceDotNet.Force ();
				NetMQConfig.ManualTerminationTakeOver ();
				NetMQConfig.ContextCreate (true);
				contextExists = true;
			}

			requestSocket = new RequestSocket (IPHeader + PORT);
			requestSocket.SendFrame ("SUB_PORT");
			IsConnected = requestSocket.TryReceiveFrameString (timeout, out subport);
			if (IsConnected)
			{
				CheckPupilVersion ();
			}
		}

		private string PupilVersion;
		public List<int> PupilVersionNumbers;
		private void CheckPupilVersion()
		{
			requestSocket.SendFrame ("v");
			if (requestSocket.TryReceiveFrameString (timeout, out PupilVersion))
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
					// Is3DCalibrationSupported ();
				}
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
			return requestSocket.TryReceiveMultipartMessage (timeout, ref m);
		}

		public void SetPupilTimestamp(float time)
		{
			if (requestSocket != null)
			{
				requestSocket.SendFrame ("T " + time.ToString ("0.00000000"));
				receiveRequestResponse ();//TODO not handling the response yet
			}
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
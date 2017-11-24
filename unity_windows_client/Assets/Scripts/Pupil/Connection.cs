using System;
using System.IO;
using System.Collections.Generic;
using UnityEngine;
using NetMQ;
using NetMQ.Sockets;
using MessagePack;

[Serializable]
public class Connection
{
	public UDPCommunication udpComm;
	public Connection(UDPCommunication comm)
	{
		udpComm = comm;
	}

	private bool _isConnected = false;
	public bool isConnected
	{
		get { return _isConnected; }
		set 
		{
			_isConnected = value;

			if ( _isConnected )
				udpComm.SendUDPData (new byte[] { 0, 1 }); 
			else
				udpComm.SendUDPData (new byte[] { 0, 0 }); 
		}
	}
	public bool isAutorun = true;
	public string IP = "127.0.0.1";
	public string IPHeader = ">tcp://127.0.0.1:";
	public int PORT = 50020;
	public string subport = "59485";
	public bool isLocal = true;

	private Dictionary<string,SubscriberSocket> _subscriptionSocketForTopic;
	private Dictionary<string,SubscriberSocket> subscriptionSocketForTopic
	{
		get
		{
			if ( _subscriptionSocketForTopic == null )
				_subscriptionSocketForTopic = new Dictionary<string, SubscriberSocket>();
			return _subscriptionSocketForTopic;
		}
	}
	public RequestSocket requestSocket = null;

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
		isConnected = requestSocket.TryReceiveFrameString (timeout, out subport);
		if (isConnected)
		{
			CheckPupilVersion ();
		}
	}

	public string PupilVersion;
	public List<int> PupilVersionNumbers;
	public void CheckPupilVersion()
	{
		requestSocket.SendFrame ("v");
		if (requestSocket.TryReceiveFrameString (timeout, out PupilVersion))
		{
			var split = PupilVersion.Split ('.');
			PupilVersionNumbers = new List<int> ();
			int number;
			foreach (var item in split)
			{
				if ( int.TryParse (item, out number) )
					PupilVersionNumbers.Add (number);
			}
			Is3DCalibrationSupported ();
		}
	}
	public bool Is3DCalibrationSupported()
	{
		if (PupilVersionNumbers.Count > 0)
			if (PupilVersionNumbers [0] >= 1)
				return true;

		Debug.Log ("Pupil version below 1 detected. V1 is required for 3D calibration");
		// UDP
//		PupilTools.Settings.calibration.currentMode = Calibration.Mode._2D;
		return false;
	}

	public void CloseSockets()
	{
		if (requestSocket != null && isConnected) 
		{
			requestSocket.Close ();
			isConnected = false;
		}

		foreach (var socketKey in subscriptionSocketForTopic.Keys)
			CloseSubscriptionSocket (socketKey);
		UpdateSubscriptionSockets ();

		TerminateContext ();
	}

	private MemoryStream mStream;
	public void InitializeSubscriptionSocket(string topic)
	{		
		if (!subscriptionSocketForTopic.ContainsKey (topic))
		{
			subscriptionSocketForTopic.Add (topic, new SubscriberSocket (IPHeader + subport));
			subscriptionSocketForTopic [topic].Subscribe (topic);

			//André: Is this necessary??
//			subscriptionSocketForTopic[topic].Options.SendHighWatermark = PupilSettings.numberOfMessages;// 6;

			subscriptionSocketForTopic[topic].ReceiveReady += (s, a) => 
			{
				NetMQMessage m = new NetMQMessage();

				while(a.Socket.TryReceiveMultipartMessage(ref m)) 
				{
					string msgType = m[0].ConvertToString();

					byte subscriptionSocketMessageType;
					switch(msgType)
					{
					case "notify.calibration.successful":
						UnityEngine.Debug.Log(msgType);
						subscriptionSocketMessageType = 21;
						udpComm.ResetCalibrationButton();
						break;
					case "notify.calibration.failed":
						UnityEngine.Debug.Log(msgType);
						subscriptionSocketMessageType = 22;
						udpComm.ResetCalibrationButton();
						break;
					case "gaze":
						subscriptionSocketMessageType = 23;
						break;
					case "pupil.0":
						subscriptionSocketMessageType = 24;
						break;
					case "pupil.1":
						subscriptionSocketMessageType = 25;
						break;
					default: 
						UnityEngine.Debug.Log(msgType);
						subscriptionSocketMessageType = 20;
						break;
					}
					byte[] message = m[1].ToByteArray();
					byte[] data = new byte[message.Length+1];
					data[0] = subscriptionSocketMessageType;
					for (int i = 1; i < data.Length; i++)
						data[i] = message[i-1];
					udpComm.SendUDPData(data);
				}
			};
		}
	}

	public void UpdateSubscriptionSockets()
	{
		foreach (var socket in subscriptionSocketForTopic)
		{
			if (socket.Value.HasIn)
				socket.Value.Poll ();
		}
		for (int i = 0; i < subscriptionSocketToBeClosed.Count; i++)
		{
			var toBeClosed = subscriptionSocketToBeClosed [i];
			if (subscriptionSocketForTopic.ContainsKey (toBeClosed))
			{
				subscriptionSocketForTopic [toBeClosed].Close ();
				subscriptionSocketForTopic.Remove (toBeClosed);
			}
			subscriptionSocketToBeClosed.Remove (toBeClosed);
		}
	}
	private List<string> subscriptionSocketToBeClosed  = new List<string> ();
	public void CloseSubscriptionSocket (string topic)
	{
		if ( subscriptionSocketToBeClosed == null )
			subscriptionSocketToBeClosed = new List<string> ();
		if (!subscriptionSocketToBeClosed.Contains (topic))
			subscriptionSocketToBeClosed.Add (topic);
	}

	public void sendRequestMessage (Dictionary<string,object> data)
	{
		if (requestSocket != null && isConnected)
		{
			NetMQMessage m = new NetMQMessage ();

			m.Append ("notify." + data ["subject"]);
			m.Append (MessagePackSerializer.Serialize<Dictionary<string,object>> (data));

			requestSocket.SendMultipartMessage (m);

			// needs to wait for response for some reason..
			receiveRequestMessage ();
		}
	}
	public void sendRequestMessage (string subject, byte[] data)
	{
		if (requestSocket != null && isConnected)
		{
			NetMQMessage m = new NetMQMessage ();

			m.Append ("notify." + subject);
			m.Append (data);

			requestSocket.SendMultipartMessage (m);

			// needs to wait for response for some reason..
			receiveRequestMessage ();
		}
	}

	public NetMQMessage receiveRequestMessage ()
	{
		return requestSocket.ReceiveMultipartMessage ();
	}
		
	public void SetPupilTimestamp(float time)
	{
		if (requestSocket != null && isConnected)
		{
			requestSocket.SendFrame ("T " + time.ToString ("0.00000000"));
			receiveRequestMessage ();
		} else
		{
			UnityEngine.Debug.Log ("SYNC-TIME NOT SET");
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
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
	private bool _isConnected = false;
	public bool isConnected
	{
		get { return _isConnected; }
		set { _isConnected = value; }
	}
	public string IP = "127.0.0.1";
	public string IPHeader = ">tcp://127.0.0.1:";
	public int PORT = 50020;
	public string subport = "59485";
	public bool isLocal = true;
    public float confidenceThreshold = 0.6f;

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
			SetPupilTimestamp (Time.time);
		}
	}

	public string PupilVersion;
	public List<int> PupilVersionNumbers;
	public void CheckPupilVersion()
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
				Is3DCalibrationSupported ();
			}
		}
	}
	public bool Is3DCalibrationSupported()
	{
		if (PupilVersionNumbers.Count > 0)
			if (PupilVersionNumbers [0] >= 1)
				return true;

		Debug.Log ("Pupil version below 1 detected. V1 is required for 3D calibration");
		PupilTools.CalibrationMode = Calibration.Mode._2D;
		return false;
	}

	public void CloseSockets()
	{
		if (requestSocket != null)
			requestSocket.Close ();
		
		foreach (var socketKey in subscriptionSocketForTopic.Keys)
			CloseSubscriptionSocket (socketKey);
		UpdateSubscriptionSockets ();

		TerminateContext ();

		isConnected = false;
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
				int i = 0;

				NetMQMessage m = new NetMQMessage();

				while(a.Socket.TryReceiveMultipartMessage(ref m)) 
				{
					// We read all the messages from the socket, but disregard the ones after a certain point
	//				if ( i > PupilSettings.numberOfMessages ) // 6)
	//					continue;

					string msgType = m[0].ConvertToString();
					mStream = new MemoryStream(m[1].ToByteArray());
					byte[] thirdFrame = null;
					if (m.FrameCount >= 3)
						thirdFrame = m[2].ToByteArray();
					
					if (PupilSettings.Instance.debug.printMessageType)
						Debug.Log(msgType);

					if (PupilSettings.Instance.debug.printMessage)
						Debug.Log (MessagePackSerializer.ToJson(m[1].ToByteArray()));

					if ( PupilTools.ReceiveDataIsSet )
					{
						PupilTools.ReceiveData( msgType, MessagePackSerializer.Deserialize<Dictionary<string,object>> (mStream), thirdFrame);
						continue;
					}

					switch(msgType)
					{
					case "notify.calibration.successful":
						PupilTools.CalibrationFinished();
						Debug.Log(msgType);
						break;
					case "notify.calibration.failed":
						PupilTools.CalibrationFailed();
						Debug.Log(msgType);
						break;
					case "gaze":
					case "gaze.2d.0.":
					case "gaze.2d.1.":
					case "pupil.0":
					case "pupil.1":
						var dictionary = MessagePackSerializer.Deserialize<Dictionary<string,object>> (mStream);
						var confidence = PupilTools.FloatFromDictionary(dictionary,"confidence");
						if ( PupilTools.IsCalibrating )
						{
							string eyeID = PupilTools.StringFromDictionary(dictionary,"id");
							PupilTools.UpdateCalibrationConfidence(eyeID,confidence);
							break;
						}
						if ((confidence > confidenceThreshold) && msgType.StartsWith("gaze"))
                        {
                            PupilTools.gazeDictionary = dictionary;
                        }
							
						break;
					case "frame.eye.0":
					case "frame.eye.1":
						break;
					default: 
						Debug.Log(msgType);
						break;
					}

					i++;
				}
			};
		}
	}

	public void UpdateSubscriptionSockets()
	{
		string[] keys = new string[subscriptionSocketForTopic.Count];
		subscriptionSocketForTopic.Keys.CopyTo (keys, 0);
		for (int i = 0; i < keys.Length; i++)
		{
			if (subscriptionSocketForTopic [keys [i]].HasIn)
				subscriptionSocketForTopic [keys [i]].Poll ();
		}
		for (int i = subscriptionSocketToBeClosed.Count - 1; i >= 0; i--)
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
	private List<string> subscriptionSocketToBeClosed = new List<string> ();
	public void CloseSubscriptionSocket (string topic)
	{
		if ( subscriptionSocketToBeClosed == null )
			subscriptionSocketToBeClosed = new List<string> ();
		if (!subscriptionSocketToBeClosed.Contains (topic))
			subscriptionSocketToBeClosed.Add (topic);
	}

	public bool sendRequestMessage (Dictionary<string,object> data)
	{
		if (requestSocket != null && isConnected)
		{
			NetMQMessage m = new NetMQMessage ();

			m.Append ("notify." + data ["subject"]);
			m.Append (MessagePackSerializer.Serialize<Dictionary<string,object>> (data));

			requestSocket.SendMultipartMessage (m);
			return receiveRequestResponse ();
		}
		return false;
	}

	public bool receiveRequestResponse ()
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
			receiveRequestResponse ();
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
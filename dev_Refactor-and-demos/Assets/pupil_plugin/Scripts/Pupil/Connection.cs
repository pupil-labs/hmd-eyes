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
	public bool isAutorun = true;
	public string IP = "127.0.0.1";
	public string IPHeader = ">tcp://127.0.0.1:";
	public int PORT = 50020;
	public string subport = "59485";
	public bool isLocal = true;
	public List<string> topicList;

	public SubscriberSocket subscribeSocket = null;
	public RequestSocket requestSocket = null;

	private bool contextExists = false;
	public void TryToConnect()
	{
		IPHeader = ">tcp://" + IP + ":";

		Debug.Log ("Attempting to connect to : " + IPHeader + PORT);

		var timeout = new System.TimeSpan (0, 0, 1); //1sec

		if (!contextExists)
		{
			AsyncIO.ForceDotNet.Force ();
			NetMQConfig.ManualTerminationTakeOver ();
			NetMQConfig.ContextCreate (true);
			contextExists = true;
		}

		requestSocket = new RequestSocket (PupilSettings.Instance.connection.IPHeader + PupilSettings.Instance.connection.PORT);
		requestSocket.SendFrame ("SUB_PORT");

		isConnected = requestSocket.TryReceiveFrameString (timeout, out PupilSettings.Instance.connection.subport);
	}

	public void CloseSockets()
	{
		if (requestSocket != null && isConnected) 
		{
			requestSocket.Close ();
			isConnected = false;
		}

		if (subscribeSocket != null)
			subscribeSocket.Close ();
		if (contextExists)
			NetMQConfig.ContextTerminate ();
	}

	private MemoryStream mStream;
	public void InitializeSubscriptionSocket()
	{
		if (subscribeSocket != null)
		{
			subscribeSocket.Close ();
		}
		if (topicList.Count == 0)
			return;

		subscribeSocket = new SubscriberSocket (IPHeader + subport);

		//André: Is this necessary??
		subscribeSocket.Options.SendHighWatermark = PupilSettings.numberOfMessages;// 6;

		foreach (var topic in topicList)
		{
			subscribeSocket.Subscribe (topic);
		}

		subscribeSocket.ReceiveReady += (s, a) => {

			int i = 0;

			NetMQMessage m = new NetMQMessage();

			while(a.Socket.TryReceiveMultipartMessage(ref m)) 
			{
				// We read all the messages from the socket, but disregard the ones after a certain point
				if ( i > PupilSettings.numberOfMessages ) // 6)
					continue;

				mStream = new MemoryStream(m[1].ToByteArray());

				string msgType = m[0].ConvertToString();

				if (PupilSettings.Instance.debug.printMessageType)
					Debug.Log(msgType);

				if (PupilSettings.Instance.debug.printMessage)
					Debug.Log (MessagePackSerializer.ToJson(m[1].ToByteArray()));

				if ( PupilSettings.Instance.dataProcess.state != PupilSettings.EStatus.ProcessingGaze )
					continue;

				switch(msgType)
				{
				case "gaze":
				case "pupil.0":
				case "pupil.1":
					var dictionary = MessagePackSerializer.Deserialize<Dictionary<string,object>> (mStream);
					if (PupilData.ConfidenceForDictionary(dictionary) > 0.6f) 
					{
						if (msgType == "gaze")
							PupilData.gazeDictionary = dictionary;
						else if (msgType == "pupil.0")
							PupilData.pupil0Dictionary = dictionary;
						else if (msgType == "pupil.1")
							PupilData.pupil1Dictionary = dictionary;
					}
					break;
				default: 
					Debug.Log(msgType);
					foreach (var item in MessagePackSerializer.Deserialize<Dictionary<string,object>> (mStream))
					{
						Debug.Log(item.Key);
						Debug.Log(item.Value.ToString());
					}
					break;
				}

				i++;
			}
		};
	}

	public void sendRequestMessage (Dictionary<string,object> data)
	{
		NetMQMessage m = new NetMQMessage ();

		m.Append ("notify." + data ["subject"]);
		m.Append (MessagePackSerializer.Serialize<Dictionary<string,object>> (data));

		requestSocket.SendMultipartMessage (m);

		// needs to wait for response for some reason..
		recieveRequestMessage ();
	}

	public NetMQMessage recieveRequestMessage ()
	{
		return requestSocket.ReceiveMultipartMessage ();
	}

	public float GetPupilTimestamp ()
	{
		requestSocket.SendFrame ("t");
		NetMQMessage recievedMsg = recieveRequestMessage ();
		return float.Parse (recievedMsg [0].ConvertToString ());
	}

	public void TerminateContext()
	{
		NetMQConfig.ContextTerminate(true);
	}
}
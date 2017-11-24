using UnityEngine;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;

[Serializable]
public class Connection
{
	private byte[] StringToPacket (string functionName)
	{
		byte[] message = Encoding.ASCII.GetBytes (functionName);
		byte[] data = new byte[message.Length + 1];
		for (int i = 1; i < data.Length; i++)
		{
			data [i] = message [i - 1];
		}
		return data;
	}

	private bool _isConnected = false;
	public bool isConnected
	{
		get { return _isConnected; }
		set { _isConnected = value; }
	}

	private bool _contextExists = false;
	private bool contextExists
	{
		get { return _contextExists; }
		set { _contextExists = value; }
	}

	public void InitializeRequestSocket()
	{
        byte[] data = StringToPacket ("InitializeRequestSocket");
		data [0] = 0;
		UDPCommunicator.Instance.SendUDPMessage (data);

		byte[] time = System.BitConverter.GetBytes (Time.time);
		byte[] timeData = new byte[time.Length + 1];
		timeData [0] = 40;
		for (int i = 1; i < timeData.Length; i++)
		{
			timeData [i] = time [i - 1];
		}
		UDPCommunicator.Instance.SendUDPMessage (timeData);
	}

	public string PupilVersion;
	public List<int> PupilVersionNumbers;
	public void CheckPupilVersion()
	{
	}
	public bool Is3DCalibrationSupported()
	{
		return false;
	}

	public void CloseSockets()
	{
		byte[] data = StringToPacket ("CloseSockets");
		data [0] = 0;
		UDPCommunicator.Instance.SendUDPMessage (data);
	}

	public void InitializeSubscriptionSocket(string topic)
	{	
		byte[] data = StringToPacket (topic);
		data [0] = 1;
		UDPCommunicator.Instance.SendUDPMessage (data);
	}

	public void UpdateSubscriptionSockets()
	{
	}
	private List<string> subscriptionSocketToBeClosed;
	public void CloseSubscriptionSocket (string topic)
	{
		byte[] data = StringToPacket (topic);
		data [0] = 2;
		UDPCommunicator.Instance.SendUDPMessage (data);
	}

	public void sendRequestMessage (Dictionary<string,object> dictionary)
	{
		byte[] message = MessagePackSlim.Serialize(dictionary);
		byte[] data = new byte[message.Length + 1];
		for (int i = 1; i < data.Length; i++)
		{
			data [i] = message [i - 1];
		}
		data [0] = 10;
        UnityEngine.Debug.Log(dictionary["subject"]);
		UDPCommunicator.Instance.SendUDPMessage (data);
	}

	public bool updatingPupilTimestamp = false;
	private float _currentPupilTimestamp = 0;
	public float currentPupilTimestamp
	{
		get 
		{ 
			if (!updatingPupilTimestamp)
			{
				updatingPupilTimestamp = true;
				UpdatePupilTimestamp ();
			}
			return _currentPupilTimestamp;
		}
		set
		{
			_currentPupilTimestamp = value;
		}
	}
	private void UpdatePupilTimestamp ()
	{
		UDPCommunicator.Instance.SendUDPMessage (new byte[]{ 30, 1 });
	}

	public void TerminateContext()
	{
		byte[] data = StringToPacket ("TerminateContext");
		data [0] = 0;
		UDPCommunicator.Instance.SendUDPMessage (data);
	}
}

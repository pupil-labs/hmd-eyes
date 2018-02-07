using UnityEngine;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;

[Serializable]
public class Connection
{
	public string pupilRemotePort = "50021";
	public string pupilRemoteIP = "192.168.1.12";

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

	public void Initialize()
	{
		// Initialization command
		sendData ( new byte[] 
			{
				(byte)'I',
				PupilTools.CalibrationMode == Calibration.Mode._2D ? (byte)'2' : (byte)'3'
			}
		);

		// Setting reference time
		byte[] time = System.BitConverter.GetBytes (Time.time);
		byte[] timeData = new byte[time.Length + 1];
		timeData [0] = (byte)'T';
		for (int i = 1; i < timeData.Length; i++)
		{
			timeData [i] = time [i - 1];
		}
		sendData (timeData);
	}

	public void CloseSockets()
	{
		sendCommandKey ('i');

        isConnected = false;
	}

	public void InitializeSubscriptionSocket(string topic)
	{	
		if (topic != "gaze")
		{
			UnityEngine.Debug.Log ("The HoloLens implementation currently only supports gaze data");
			return;
		}
		sendCommandKey ('S');
	}

	public void UpdateSubscriptionSockets()
	{
	}
	private List<string> subscriptionSocketToBeClosed;
	public void CloseSubscriptionSocket (string topic)
	{
		if (topic != "gaze")
		{
			UnityEngine.Debug.Log ("The HoloLens implementation currently only supports gaze data");
			return;
		}
		sendCommandKey ('s');
	}

	public void sendCommandKey( char commandKey)
	{
		sendData (new byte[] { (byte)commandKey });
	}

	public void sendData (byte[] data)
	{
        UDPCommunication.Instance.SendUDPMessage(data);
	}

	public void sendRequestMessage (Dictionary<string,object> dictionary)
	{
		byte[] message = MessagePackSlim.Serialize(dictionary);
        byte[] data = new byte[1 + message.Length];

        data[0] = (byte)'R';

        for (int i = 0; i < message.Length; i++)
        {
            data[1 + i] = message[i];

        }
		if (PupilSettings.Instance.debug.printSampling)
        	UnityEngine.Debug.Log(dictionary["subject"]);
		UDPCommunication.Instance.SendUDPMessage (data);
	}
}

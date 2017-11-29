using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Text;
using MessagePack;
using HoloToolkit.Unity;

#if !UNITY_EDITOR
using System.Linq;
using System.IO;
using Windows.Networking.Sockets;
using Windows.Networking.Connectivity;
using Windows.Networking;
#endif

public class UDPCommunication : Singleton<UDPCommunication>
{
	[Tooltip ("port to listen for incoming data")]
	public string internalPort = "12346";

	[Tooltip("IP-Address for sending")]
	public string externalIP = "192.168.1.12";

	[Tooltip("Port for sending")]
	public string externalPort = "12345";

	[Tooltip("Send a message at Startup")]
	public bool sendPingAtStart = true;

	[Tooltip("Conten of Ping")]
	public string PingMessage = "hello";

	private readonly  Queue<Action> ExecuteOnMainThread = new Queue<Action>();


	#if !UNITY_EDITOR

	//Send an UDP-Packet
	public async void SendUDPMessage(byte[] data)
	{
        UnityEngine.Debug.Log("UDP data head " + data[0]);
	    await _SendUDPMessage(data);
	}

	DatagramSocket socket;

	async void Start()
	{

	Debug.Log("Waiting for a connection...");

	socket = new DatagramSocket();
	socket.MessageReceived += Socket_MessageReceived;

	HostName IP = null;
	try
	{
	    var icp = NetworkInformation.GetInternetConnectionProfile();

	    IP = Windows.Networking.Connectivity.NetworkInformation.GetHostNames()
	    .SingleOrDefault(
	    hn =>
	    hn.IPInformation?.NetworkAdapter != null && hn.IPInformation.NetworkAdapter.NetworkAdapterId
	    == icp.NetworkAdapter.NetworkAdapterId);

	    await socket.BindEndpointAsync(IP, internalPort);
	}
	catch (Exception e)
	{
	    Debug.Log(e.ToString());
	    Debug.Log(SocketError.GetStatus(e.HResult).ToString());
	    return;
	}

	if(sendPingAtStart)
	    SendUDPMessage(Encoding.UTF8.GetBytes(PingMessage));
	}




	private async System.Threading.Tasks.Task _SendUDPMessage(byte[] data)
	{
	    using (var stream = await socket.GetOutputStreamAsync(new Windows.Networking.HostName(externalIP), externalPort))
	    {
	        using (var writer = new Windows.Storage.Streams.DataWriter(stream))
	        {
	            writer.WriteBytes(data);
	            await writer.StoreAsync();

	        }
	    }
	}


	#else
	// to make Unity-Editor happy :-)
	void Start()
	{

	}

	public void SendUDPMessage(byte[] data)
	{

	}

	#endif
	// Update is called once per frame
	void Update()
	{
		while (ExecuteOnMainThread.Count > 0)
		{
			ExecuteOnMainThread.Dequeue().Invoke();
		}
	}
		
	public void InterpreteUDPData(byte[] data)
	{
		switch (data[0])
		{
		// Connection established
		case 0:
			UnityEngine.Debug.Log("Connection established");
			PupilSettings.Instance.connection.isConnected = data[1] == 1;
			break;
		// "notify.calibration.successful":
		case 21:
			UnityEngine.Debug.Log("notify.calibration.successful");
			PupilSettings.Instance.calibration.currentStatus = Calibration.Status.Succeeded;
			PupilTools.CalibrationFinished();
			break;
			// "notify.calibration.failed":
		case 22:
			UnityEngine.Debug.Log("notify.calibration.failed");
			PupilSettings.Instance.calibration.currentStatus = Calibration.Status.NotSet;
			PupilTools.CalibrationFailed();
			break;
		case 52:
            //UnityEngine.Debug.Log("Left eye position received");
            var leftEyePosition = FloatArrayFromPacket (data);
			PupilData._2D.LeftEyePosUDP.x = leftEyePosition [0];
			PupilData._2D.LeftEyePosUDP.y = leftEyePosition [1];
            break;
		case 53:
            //UnityEngine.Debug.Log("Right eye position received");
            var rightEyePosition = FloatArrayFromPacket (data);
			PupilData._2D.RightEyePosUDP.x = rightEyePosition [0];
			PupilData._2D.RightEyePosUDP.y = rightEyePosition [1];
			break;
		case 54:
			var gaze2DPosition = FloatArrayFromPacket (data);
			PupilData._2D.Gaze2DPosUDP.x = gaze2DPosition [0];
			PupilData._2D.Gaze2DPosUDP.y = gaze2DPosition [1];
			break;
		case 55:
			var gaze3DPosition = FloatArrayFromPacket (data);
			PupilData._3D.Gaze3DPosUDP.x = gaze3DPosition [0];
			PupilData._3D.Gaze3DPosUDP.y = gaze3DPosition [1];
			PupilData._3D.Gaze3DPosUDP.z = gaze3DPosition [2];
			break;
		case 90:
			UnityEngine.Debug.Log ("Start/stop calibration command");
			if (data [1] == 1)
				PupilTools.StartCalibration ();
			else
				PupilTools.StopCalibration ();
			break;
		case 91:
			UnityEngine.Debug.Log ("Forcing 2D calibration mode (Pupil version < 1 detected)");
			PupilSettings.Instance.calibration.currentMode = Calibration.Mode._2D;
			break;
		default:
			UnityEngine.Debug.Log(StringFromPacket(data));
			break;
		}
	}

	private float[] FloatArrayFromPacket (byte[] data)
	{
		float[] floats = new float[(data.Length-1)/sizeof(float)];
		for(int i = 0; i < floats.Length; i++)
		{
			floats[i] = BitConverter.ToSingle(data, 1 + i*sizeof(float));
		}
		return floats;
	}

	private string StringFromPacket (byte[] data)
	{
		byte[] message = new byte[data.Length - 1];
		for (int i = 1; i < data.Length; i++)
		{
			message [i-1] = data [i];
		}
		return Encoding.ASCII.GetString (message);
	}

	#if !UNITY_EDITOR

	static MemoryStream ToMemoryStream(Stream input)
	{
	    try
	    {                                         // Read and write in
	        byte[] block = new byte[0x1000];       // blocks of 4K.
	        MemoryStream ms = new MemoryStream();
	        while (true)
	        {
	            int bytesRead = input.Read(block, 0, block.Length);
	            if (bytesRead == 0) return ms;
	            ms.Write(block, 0, bytesRead);
	        }
	    }
	    finally { }
	}

	private void Socket_MessageReceived(Windows.Networking.Sockets.DatagramSocket sender,
	Windows.Networking.Sockets.DatagramSocketMessageReceivedEventArgs args)
	{
	    //Read the message that was received from the UDP  client.
	    Stream streamIn = args.GetDataStream().AsStreamForRead();
	    MemoryStream ms = ToMemoryStream(streamIn);
	    byte[] msgData = ms.ToArray();


	    if (ExecuteOnMainThread.Count == 0)
	    {
	        ExecuteOnMainThread.Enqueue(() => { InterpreteUDPData(msgData); });
	    }
	}

	#endif
}
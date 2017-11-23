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

public class UDPCommunicator : Singleton<UDPCommunicator>
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

	//we've got a message (data[]) from (host) in case of not assigned an event
	void UDPMessageReceived(string host, string port, byte[] data)
	{
	Debug.Log("GOT MESSAGE FROM: " + host + " on port " + port + " " + data.Length.ToString() + " bytes ");
	}

	//Send an UDP-Packet
	public async void SendUDPMessage(byte[] data)
	{
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
			PupilTools.Settings.connection.isConnected = data[1] == 1;
			break;
		// "notify.calibration.successful":
		case 21:
			UnityEngine.Debug.Log("notify.calibration.successful");
			PupilTools.Settings.calibration.currentStatus = Calibration.Status.Succeeded;
			PupilTools.CalibrationFinished();
			break;
			// "notify.calibration.failed":
		case 22:
			UnityEngine.Debug.Log("notify.calibration.failed");
			PupilTools.Settings.calibration.currentStatus = Calibration.Status.NotSet;
			PupilTools.CalibrationFailed();
			break;
		//  "gaze","pupil.0", "pupil.1"
		case 23:
		case 24:
		case 25:
			int readSize;
			var dictionary = MessagePackSlim.Deserialize (data, 1, out readSize); // MessagePackSerializer.Deserialize<Dictionary<string,object>> (mStream);
			if (PupilTools.ConfidenceForDictionary(dictionary) > 0.6f) 
			{
				if (data[0] == 23)
					PupilTools.gazeDictionary = dictionary;
				else if (data[0] == 24)
					PupilTools.pupil0Dictionary = dictionary;
				else if (data[0] == 25)
					PupilTools.pupil1Dictionary = dictionary;
			}
			break;
		case 30:
			UnityEngine.Debug.Log("UpdatePupilTimestamp");
			PupilTools.Settings.connection.currentPupilTimestamp = System.BitConverter.ToSingle (data, 1);
			break;
		default:
			UnityEngine.Debug.Log(StringFromPacket(data));
			break;
		}
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
	Debug.Log("GOT MESSAGE FROM: " + args.RemoteAddress.DisplayName);
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
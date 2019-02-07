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
	private readonly  Queue<Action> ExecuteOnMainThread = new Queue<Action>();

#if !UNITY_EDITOR

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

		await socket.BindEndpointAsync(IP, PupilSettings.Instance.connection.pupilRemotePort);
		}
		catch (Exception e)
		{
		    Debug.Log(e.ToString());
		    Debug.Log(SocketError.GetStatus(e.HResult).ToString());
		    return;
		}
	}

	//Send an UDP-Packet
	public async void SendUDPMessage(byte[] data)
	{
		UnityEngine.Debug.Log("UDP data head " + (char)data[0]);
		await _SendUDPMessage(data);
	}

	private async System.Threading.Tasks.Task _SendUDPMessage(byte[] data)
	{
		using (var stream = await socket.GetOutputStreamAsync(new Windows.Networking.HostName(PupilSettings.Instance.connection.pupilRemoteIP), PupilSettings.Instance.connection.pupilRemotePort))
	    {
	        using (var writer = new Windows.Storage.Streams.DataWriter(stream))
	        {
	            writer.WriteBytes(data);
	            await writer.StoreAsync();

	        }
	    }
	}

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

	private void Socket_MessageReceived(Windows.Networking.Sockets.DatagramSocket sender, Windows.Networking.Sockets.DatagramSocketMessageReceivedEventArgs args)
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

#else

	System.Threading.Thread udpThread;
	System.Net.Sockets.UdpClient udpClient;
	System.Net.IPEndPoint _remoteEndPoint;
	System.Net.IPEndPoint remoteEndPoint
	{
		get 
		{
			if ( _remoteEndPoint == null )
				_remoteEndPoint = new System.Net.IPEndPoint(System.Net.IPAddress.Parse(PupilSettings.Instance.connection.pupilRemoteIP), int.Parse(PupilSettings.Instance.connection.pupilRemotePort));
			return _remoteEndPoint;
		}
	}

	// Needs a separate port if Pupil Capture and Editor are running on the same machine
	public int editorModeUDPPort = 50022;

	// to make Unity-Editor happy :-)
	void Start()
	{
		Debug.Log("Waiting for a connection...");

		// create thread for reading UDP messages
		udpThread = new System.Threading.Thread(new System.Threading.ThreadStart(Listen));
		udpThread.IsBackground = true;
		udpThread.Start();
	}

	private void Listen()
	{
		try
		{
			udpClient = new System.Net.Sockets.UdpClient(editorModeUDPPort);
			print ("Started UDP client on port: " + editorModeUDPPort);

			while (true)
			{
				// receive bytes
				byte[] data = udpClient.Receive(ref _remoteEndPoint);
				if (ExecuteOnMainThread.Count == 0)
				{
					ExecuteOnMainThread.Enqueue(() => { InterpreteUDPData(data); });
				}
			}
		}
		catch (Exception err)
		{
			print(err.ToString());
		}
	}

	public void SendUDPMessage(byte[] data)
	{
		try
		{
			udpClient.Send (data, data.Length, remoteEndPoint);
		}
		catch (Exception e)
		{
			UnityEngine.Debug.Log (e.ToString ());
		}
	}

	private void StopUDPThread()
	{
		if (udpThread != null && udpThread.IsAlive)
		{
			udpThread.Abort();
		}
		if (udpClient != null)
			udpClient.Close();
	}

	void OnDisable()
	{
		StopUDPThread ();
	}

#endif

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
		case (byte) '0':
			switch (data [1])
			{
			case (byte) 'I':
				UnityEngine.Debug.Log ("Connection established");
				PupilTools.IsConnected = true;
				break;
            case (byte)'i':
                UnityEngine.Debug.Log("Connection closed");
                PupilTools.IsConnected = false;
                break;
            case (byte)'S':
                // Start gazing command received
                break;
            case (byte)'s':
                // Stop gazing command received
                break;
            default:
				UnityEngine.Debug.Log ("Unknown response: " + (char) data[1]);
				break;
			}
			break;
		case (byte) 'E':
			switch (data [1])
			{
			case (byte) 'C':
				if (data [2] == (byte) 'S') // "notify.calibration.successful"
				{
					UnityEngine.Debug.Log ("notify.calibration.successful");
					PupilTools.CalibrationFinished ();
				} else if (data [2] == (byte) 'F') // "notify.calibration.failed"
				{
					UnityEngine.Debug.Log("notify.calibration.failed");
					PupilTools.CalibrationFailed();
				}
				else
					UnityEngine.Debug.Log ("Unknown calibration ended event");
				break;
			case (byte) 'G':
				if (data [2] == (byte)'2')
				{
					if (data [3] == (byte)'1')
					{
						//UnityEngine.Debug.Log("Left eye position received");
						PupilTools.UpdateGazePostion (PupilSettings.gaze2DLeftEyeKey, FloatArrayFromPacket (data, 4));
					} else if (data [3] == (byte)'0')
					{
						//UnityEngine.Debug.Log("Right eye position received");
						PupilTools.UpdateGazePostion (PupilSettings.gaze2DRightEyeKey, FloatArrayFromPacket (data, 4));
					} else if (data [3] == (byte)'2')
					{
						PupilTools.UpdateGazePostion (PupilSettings.gaze2DKey, FloatArrayFromPacket (data, 4));
					}
					else
						UnityEngine.Debug.Log ("Unknown gaze 2d data");
				} else if (data [2] == (byte)'3')
				{
					PupilTools.UpdateGazePostion (PupilSettings.gaze3DKey, FloatArrayFromPacket (data, 4));
				} else
					UnityEngine.Debug.Log ("Unknown gaze event");
				break;
			default:
				UnityEngine.Debug.Log ("Unknown event");
				break;
			}
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
			PupilTools.CalibrationMode = Calibration.Mode._2D;
			break;
		case (byte) 'R':
			if (PupilSettings.Instance.debug.printSampling)
				Debug.Log ("Reference points received");
			break;
		default:
			UnityEngine.Debug.Log(StringFromPacket(data));
			break;
		}
	}

	private float[] FloatArrayFromPacket (byte[] data, int offset = 1)
	{
		float[] floats = new float[(data.Length-1)/sizeof(float)];
		for(int i = 0; i < floats.Length; i++)
		{
			floats[i] = BitConverter.ToSingle(data, offset + i*sizeof(float));
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
}
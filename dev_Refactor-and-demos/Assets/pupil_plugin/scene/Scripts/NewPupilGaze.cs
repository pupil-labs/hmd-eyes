using UnityEngine;
using UnityEngine.UI;
//TEMP	
//using UnityEditor;
using System.Runtime.Serialization.Formatters.Binary;
using System.Diagnostics;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel.Design;
using System.Net;
using System.Threading;
using System.IO;
using System;
using NetMQ;
using NetMQ.Sockets;

//using MsgPack.Serialization;
using System.Linq;
using MessagePack;
using MessagePack.Internal;

//using GameDevWare.Serialization;

[RequireComponent(typeof(PupilDataReceiver))]
public class NewPupilGaze : MonoBehaviour {

	RequestSocket _requestSocket;
	SubscriberSocket _subscribeSocket;
	bool _isconnected;
//	string subport;
	bool isConnected = false;




	void OnApplicationQuit(){
		
		if (_requestSocket != null)
			_requestSocket.Close ();

		if (_subscribeSocket != null)
			_subscribeSocket.Close ();

//		NetMQConfig.Cleanup (true);

	}

	// Update is called once per frame
	void Update () {

//		if (Input.GetKeyUp (KeyCode.A)) {
//			Connect ();
//		}
		if (Input.GetKeyUp (KeyCode.B)) {
			PupilTools.StartEyeProcesses ();
		}
		if (Input.GetKeyUp (KeyCode.C)) {

//			string IPHeader = ">tcp://" + "127.0.0.1" + ":";
			var timeout = new System.TimeSpan(0, 0, 1); //1sec

//			_subscribeSocket = new SubscriberSocket (IPHeader + subport);
			_subscribeSocket.SubscribeToAnyTopic ();
			_subscribeSocket.Subscribe ("pupil.");




			var msg = new NetMQMessage ();
			isConnected =  _subscribeSocket.TryReceiveMultipartMessage(timeout,ref(msg));

			string msgType=msg[0].ConvertToString();
			print (msgType);


//
//			var m = MsgPack.Unpacking.UnpackObject(msg[1].ToByteArray());
//			MsgPack.MessagePackObject map = m.Value;
//			print("type : " + msgType + " : " + m);

//			MsgPack.MessagePackObjectDictionary dict = map.AsDictionary ();
//
//			MsgPack.MessagePackObject o = new MsgPack.MessagePackObject ();
//			dict.TryGetValue (, o);
//			print ("as dictionary using MSGPACK : " + dict + ", Dictionary count : " + dict.Keys.Count + ", First key element : " + o);

			MemoryStream ms = new MemoryStream (msg [1].ToByteArray ());
//			string rawMessage = MessagePackSerializer.Deserialize<string> (ms);
			print (msg [1].ToString());

			print (MessagePackSerializer.ToJson (msg [1].ToByteArray ()));

			Dictionary<string, object> dict = MessagePackSerializer.Deserialize<Dictionary<string,object>> (ms);



			object o = new object ();
			dict.TryGetValue ("circle_3d", out o);
			Dictionary<object,object> dict2 = (Dictionary<object,object>)o;


			print (dict2.Keys.Count);

//			print (MessagePackSerializer.ToJson ((byte[])o));
			//MessagePackSerializer.Deserialize<String> (ms);

			//print (o.ToString());

			//MessagePackSerializer.Deserialize

			//MessagePackSerializer.UnpackMessagePackObject (ms);
			//MessagePack.

		}
		if (Input.GetKeyUp (KeyCode.D)) {
			print (PupilTools.GetPupilTimestamp ());
		}

		if (Input.GetKeyUp (KeyCode.E)) {
			_requestSocket.SendFrame ("sphere");
			NetMQMessage recievedMsg=_requestSocket.ReceiveMultipartMessage ();
			print (recievedMsg [0].ConvertToString ());
//			return float.Parse(recievedMsg[0].ConvertToString());
		}

		if (isConnected) {
//			var msg = new NetMQMessage ();
//			var timeout = new System.TimeSpan(0, 0, 0); //1sec
//			_subscribeSocket.TryReceiveMultipartMessage(timeout,ref(msg));
//			string msgType=msg[0].ConvertToString();
//			print (msgType);
//
//			var m = MsgPack.Unpacking.UnpackObject(msg[1].ToByteArray());
//			MsgPack.MessagePackObject map = m.Value;
//			print("type : " + msgType + " : " + m);

		}
	}



}

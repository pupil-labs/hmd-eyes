using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class BlinkDemo : MonoBehaviour 
{
	// Use this for initialization
	void Start () 
	{
		PupilTools.OnConnected += StartBlinkSubscription;
		PupilTools.OnDisconnecting += StopBlinkSubscription;

		PupilTools.OnReceiveData += CustomReceiveData;
	}

	void StartBlinkSubscription()
	{
		PupilTools.SubscribeTo ("blinks");

		PupilTools.Send (new Dictionary<string,object> {
			{ "subject", "start_plugin" }
			,{ "name", "Blink_Detection" }
			,{
				"args", new Dictionary<string,object> { 
					{ "history_length", 0.2f }
					,{ "onset_confidence_threshold", 0.5f }
					,{ "offset_confidence_threshold", 0.5f }
				}
			}
		});
	}

	void StopBlinkSubscription()
	{
		UnityEngine.Debug.Log ("Disconnected");

		PupilTools.Send (new Dictionary<string,object> {
			{ "subject","stop_plugin" }
			,{ "name", "Blink_Detection" }
		});

		PupilTools.UnSubscribeFrom ("blinks");
	}

	void CustomReceiveData(string topic, Dictionary<string,object> dictionary, byte[] thirdFrame = null)
	{
		if (topic == "blinks")
		{
			if (dictionary.ContainsKey ("timestamp"))
			{
				Debug.Log ("Blink detected: " + dictionary ["timestamp"].ToString());
			}
//			foreach (var blink in dictionary)
//			{
//				Debug.Log("Key: " + blink.Key);
//				Debug.Log("Value: " + blink.Value.ToString());
//			}
		}
	}

	void OnDisable()
	{
		PupilTools.OnConnected -= StartBlinkSubscription;
		PupilTools.OnDisconnecting -= StopBlinkSubscription;

		PupilTools.OnReceiveData -= CustomReceiveData;
	}
}

using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PupilDemo : MonoBehaviour 
{
	void Start () 
	{
		PupilTools.OnConnected += StartPupilSubscription;
		PupilTools.OnDisconnecting += StopPupilSubscription;

		PupilTools.OnReceiveData += CustomReceiveData;
	}

	void StartPupilSubscription()
	{
		PupilTools.CalibrationMode = Calibration.Mode._2D;

		PupilTools.SubscribeTo ("pupil.");
	}

	void StopPupilSubscription()
	{
		PupilTools.UnSubscribeFrom ("pupil.");
	}

	void CustomReceiveData(string topic, Dictionary<string,object> dictionary, byte[] thirdFrame = null)
	{
		if (topic.StartsWith ("pupil") )
		{
			foreach (var item in dictionary)
			{
				switch (item.Key)
				{
				case "topic":
				case "method":
				case "id":
					var textForKey = PupilTools.StringFromDictionary (dictionary, item.Key);
					// Do stuff
					break;
				case "confidence":
				case "timestamp":
				case "diameter":
					var valueForKey = PupilTools.FloatFromDictionary (dictionary, item.Key);
					// Do stuff
					break;
				case "norm_pos":
					var positionForKey = PupilTools.VectorFromDictionary (dictionary, item.Key);
					// Do stuff
					break;
				case "ellipse":
					var dictionaryForKey = PupilTools.DictionaryFromDictionary (dictionary, item.Key);
					foreach (var pupilEllipse in dictionaryForKey)
					{
						switch (pupilEllipse.Key.ToString())
						{
						case "angle":
							var angle = (float)(double)pupilEllipse.Value;
							// Do stuff
							break;
						case "center":
						case "axes":
							var vector = PupilTools.ObjectToVector (pupilEllipse.Value);
							// Do stuff
							break;
						default:
							break;
						}
					}
					// Do stuff
					break;
				default:
					break;
				}
			}
		}
	}

	void OnDisable()
	{
		PupilTools.OnConnected -= StartPupilSubscription;
		PupilTools.OnDisconnecting -= StopPupilSubscription;

		PupilTools.OnReceiveData -= CustomReceiveData;
	}
}

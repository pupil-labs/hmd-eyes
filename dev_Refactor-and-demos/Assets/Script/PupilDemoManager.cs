using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class PupilDemoManager : MonoBehaviour {

	public List<GameObject> gameObjectsToEnable;

	public List<Text> GUITexts;

	public PupilGazeTracker pupilTracker;
	public PupilDataReceiver pupilDataReceiver;

	void Start(){
	
		pupilTracker = PupilGazeTracker.Instance;

		pupilDataReceiver = pupilTracker.gameObject.GetComponent<PupilDataReceiver> ();

		pupilDataReceiver.OnConnected += OnConnected;

		PupilTools.OnCalibrationStarted += OnCalibtaionStarted;

		PupilTools.OnCalibrationEnded += OnCalibtaionEnded;
	
		PupilTools.Connect ();

	}

	void OnConnected(){

		GUITexts [1].enabled = false;//connecting text

		GUITexts [2].enabled = true;//success text

		Invoke ("ShowCalibrate", 1f);

	}

	void ShowCalibrate(){
	
		GUITexts [2].enabled = false;//success text

		GUITexts [0].enabled = true;//calibrate text

	}

	void OnCalibtaionStarted(){

		GUITexts [0].enabled = false;

	}
		
	void OnCalibtaionEnded(){

		GUITexts [3].enabled = true;

		Invoke ("StartDemo", 1f);

	}

	void StartDemo(){

		GUITexts [3].enabled = false;

		foreach (GameObject go in gameObjectsToEnable) {

			go.SetActive (true);

		}

		Destroy (gameObject);

	}

	void Update(){
	
		if (Input.GetKeyUp (KeyCode.S)) {
		
			StartDemo ();

		}

	}

}

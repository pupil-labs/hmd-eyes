using UnityEngine;
using System.Collections;
using UnityEngine.UI;

public class PupilCalibObject : MonoBehaviour {

	MeshRenderer _mr;
	bool _started=false;
	float x,y;


	// Use this for initialization
	void Start () {

		_mr = GetComponent<MeshRenderer> ();
		_mr.enabled = false;

		PupilGazeTracker.Instance.OnCalibrationStarted += OnCalibrationStarted;
		PupilGazeTracker.Instance.OnCalibrationDone += OnCalibrationDone;
		PupilGazeTracker.Instance.OnCalibData += OnCalibData;
	}

	void OnCalibrationStarted(PupilGazeTracker m)
	{
		_started = true;
	}

	void OnCalibrationDone(PupilGazeTracker m)
	{
		_started = false;
	}

	void OnCalibData(PupilGazeTracker m, object position)
	{
		Vector2 _v2 = (Vector2)position;
		this.x = _v2.x;
		this.y = _v2.y;
	}

	void _SetLocation(float x,float y)
	{
		Vector3 pos=new Vector3 ((x-0.5f)*PupilGazeTracker.Instance.CanvasWidth,(y-0.5f)*PupilGazeTracker.Instance.CanvasHeight,0);
		transform.localPosition = pos;
	}
	// Update is called once per frame
	void Update () {/*
		if (Input.GetKeyDown (KeyCode.C))
			PupilGazeTracker.Instance.StartCalibration ();
		if (Input.GetKeyDown (KeyCode.S))
			PupilGazeTracker.Instance.StopCalibration ();*/
		_mr.enabled = _started;
		if(_started)
			_SetLocation (x, y);
	}
}

using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PupilCalibMarker3D : MonoBehaviour {

	public float animTotalLength;
	public int animSegmentAmount;

	[Range(0.01f,5.0f)]
	public float pulsateSpeed = 1f;
	[Range(0.1f,1.0f)]
	public float maxScale;
	[Range(0.1f,1.0f)]
	public float minScale;

	public Color StartColor;
	public Color EndColor;

	public Sprite sprite;

	private float animSegmentLenght;
	private SpriteRenderer haloRenderer;

	private int ind = 0;
	public  int tmp = 0;

	private bool _started;

	float x,y,z;
	Vector3 _v3Position;

	private List<GameObject> _goPool = new List<GameObject>();

	void Start () {
		animSegmentAmount = PupilGazeTracker.Instance.DefaultCalibrationCount;
		animSegmentLenght = animTotalLength / animSegmentAmount;
		haloRenderer = transform.FindChild ("PupilLabsLogoHalo").gameObject.GetComponent<SpriteRenderer> ();
		AssignDelegates ();
	}

	public void AssignDelegates(){
		PupilGazeTracker.Instance.NullDelegates ();
		PupilGazeTracker.Instance.OnCalibrationStarted += OnCalibrationStarted;
		PupilGazeTracker.Instance.OnCalibrationDone += OnCalibrationDone;
		PupilGazeTracker.Instance.OnCalibData += OnCalibData;
		PupilGazeTracker.Instance.OnSwitchCalibPoint += OnSwitchCalibPoint;
		if (PupilGazeTracker.Instance.m_status == PupilGazeTracker.EStatus.Calibration)
			Debug.LogWarning ("Switching calibration during calibration. Method to restart calibration in another mode is not yet implemented! This might cause issues");
	}

	void OnCalibrationStarted(PupilGazeTracker m)
	{
		ind = 0;
		_started = true;
	}

	void OnCalibrationDone(PupilGazeTracker m)
	{
		_started = false;
	}

	void OnCalibData(PupilGazeTracker m,object position)
	{

		Vector3 _v3 = (Vector3)position;
		this.x = _v3.x;
		this.y = _v3.y;
		this.z = _v3.z;

		_v3Position = (Vector3)position;

		print("Receiving calib data : " + position);
		ind++;
		object _ind = ind;
		///////////////////////////////solve this threading issue with GameObject pooling!!!!!!!!!!!!!////////////////
		//this.gameObject.GetComponent<MainThread> ().Call (UpdateAnim, _ind);
		//MainThread.Call (UpdateAnim, _ind);
		//UpdateAnim (ind);
		MainThread.Call(UpdateAnim, _ind);
	}

	void OnSwitchCalibPoint(PupilGazeTracker m){
		ind = 0;
		MainThread.Call (RemoveChildren);
	}

	void RemoveChildren(){
		foreach (Transform _childT in transform) {
			if (_childT.gameObject.name != "PupilLabsLogoHalo")
				Destroy (_childT.gameObject);
		}
	}

	void Update(){
		PulsateHalo ();
		if (Input.GetKeyUp (KeyCode.A)) {
			tmp++;
			UpdateAnim(tmp);
			//MainThread.Call (test);
		}
		if (_started)
			SetLocation (_v3Position);
	}

	void PulsateHalo(){
		float _t = Time.time;
		Color _c = new Color (1, 1, 1, Mathf.Abs (Mathf.Sin (Time.time * (pulsateSpeed + Time.deltaTime))));

		haloRenderer.color = _c;
	}

	void SetLocation(Vector3 _v3){
		transform.localPosition = _v3;
	}

	public void UpdateAnim(object index){
		int _i = (int)index;
		print ("test susseccful" + transform.gameObject.name + _i);
		float _p = Mathf.InverseLerp (0.9f, animSegmentAmount, _i);
		//print ("Segment percentage : " + _p);
		float _s = Mathf.Lerp (minScale, maxScale, _p);
		//print ("Segment scale : " + _s);
		//GameObject _go = _goPool[_i];


		if (_i < animSegmentAmount) {
			GameObject _go = new GameObject ();
			_go.transform.parent = this.gameObject.transform;
			_go.transform.localPosition = new Vector3 (0, 0, 0);
			_go.transform.localScale = new Vector3 (0, 0, 0);
			SpriteRenderer _sRenderer = _go.AddComponent<SpriteRenderer> ();
			_sRenderer.sprite = sprite;
			_sRenderer.color = Color.Lerp (StartColor, EndColor, _p);

			StartCoroutine (SegmentAnimation (animSegmentLenght, _s, _go));
		}
	}

	IEnumerator SegmentAnimation(float length, float scale, GameObject _go){
		
		float currTime = 0f;
		float _minS = 0.1f;
		float _maxS = scale;
		float step = scale / length;
		while (currTime<length) {
			try{
				_go.transform.localScale += new Vector3 (step, step, step);
			}
			catch
			{
				yield break;
			}
			currTime++;
			yield return null;
		}
			

	}

}

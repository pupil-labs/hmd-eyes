using UnityEngine;

public class PupilMarker
{
	public string name;
	private Color color = Color.white;
	public Vector3 position;
	private GameObject _gameObject;
	private GameObject gameObject
	{
		get
		{
			if (_gameObject == null)
			{
				_gameObject = GameObject.Instantiate (Resources.Load<GameObject> ("MarkerObject"));
				_gameObject.name = this.name;
				_gameObject.GetComponent<MeshRenderer> ().material = new Material (Resources.Load<Material> ("MarkerMaterial"));
				_gameObject.GetComponent<MeshRenderer> ().material.SetColor ("_EmissionColor", this.color);
				_gameObject.transform.parent = this.camera.transform;
			}
			return _gameObject;
		}
	}
				
	private Camera _camera;
	public Camera camera
	{
		get
		{
			if (_camera == null)
			{
				_camera = Camera.main;
			}
			return _camera;
		}
		set
		{
			_camera = value;
			gameObject.transform.parent = _camera.transform;
		}
	}

	public PupilMarker(string name, Color color)
	{
		this.name = name;
		this.color = color;
		this.camera = PupilSettings.Instance.currentCamera;
	}

	public void UpdatePosition(Vector2 newPosition)
	{		
		position.x = newPosition.x;
		position.y = newPosition.y;
		position.z = PupilSettings.Instance.calibration.currentCalibrationType.vectorDepthRadiusScale[0].x;
		gameObject.transform.position = camera.ViewportToWorldPoint(position);
		UpdateOrientation ();
	}
	public void UpdatePosition(Vector3 newPosition)
	{
		position = newPosition;
		gameObject.transform.localPosition = position;
		UpdateOrientation ();
	}
	public void UpdatePosition(float[] newPosition)
	{
		if (PupilSettings.Instance.calibration.currentMode == Calibration.Mode._2D)
		{
			if (newPosition.Length == 2)
			{
				position.x = newPosition[0];
				position.y = newPosition[1];
				position.z = PupilSettings.Instance.calibration.currentCalibrationType.vectorDepthRadiusScale[0].x;
				gameObject.transform.position = camera.ViewportToWorldPoint(position);
			} 
			else
			{
				Debug.Log ("Length of new position array does not match 2D mode");
			}
		}
		else if (PupilSettings.Instance.calibration.currentMode == Calibration.Mode._3D)
		{
			if (newPosition.Length == 3)
			{
				position.x = newPosition[0];
				position.y = newPosition[1];
				position.z = newPosition[2];
				gameObject.transform.localPosition = position;
			} 
			else
			{
				Debug.Log ("Length of new position array does not match 3D mode");
			}
		}
		UpdateOrientation ();
	}
	private void UpdateOrientation()
	{
		gameObject.transform.LookAt (this.camera.transform.position);
	}

//	public void Initialize(bool isActive)
//	{
//		gameObject = GameObject.Instantiate (Resources.Load<GameObject> ("MarkerObject"));
//		gameObject.name = this.name;
//		gameObject.GetComponent<MeshRenderer> ().material = new Material (Resources.Load<Material> ("MarkerMaterial"));
//		gameObject.GetComponent<MeshRenderer> ().material.SetColor ("_EmissionColor", this.color);
//		gameObject.SetActive (isActive);
//		gameObject.transform.parent = this.camera.transform;
//		//				gameObject.hideFlags = HideFlags.HideInHierarchy;
//	}

	public void SetActive(bool toggle)
	{
		if (_gameObject != null)
			gameObject.SetActive (toggle);
	}

	public void SetScale (float value)
	{
		if (gameObject.transform.localScale.x != value)
			gameObject.transform.localScale = Vector3.one * value;
	}

	public static bool TryToReset (PupilMarker marker)
	{
		if (marker != null)
		{
			marker.camera = PupilSettings.Instance.currentCamera;
			marker.gameObject.SetActive (true);
			return true;
		}
		return false;
	}
}
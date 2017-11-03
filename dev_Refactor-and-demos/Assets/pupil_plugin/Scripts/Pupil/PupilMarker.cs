using UnityEngine;

public class PupilMarker
{
	public string name;
	public Vector3 position;
	public bool calibrationPoint;
	public Calibration.CalibMode calibMode;
	private GameObject gameObject;
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
		}
	}

	public PupilMarker(string name)
	{
		this.name = name;
	}

	public void UpdatePosition(Vector2 newPosition)
	{
		UpdatePosition (PupilConversions.Vector2ToFloatArray(newPosition));
	}

	public void UpdatePosition(float[] newPosition)
	{
		if (gameObject == null)
			InitializeGameObject ();

		if (PupilSettings.Instance.calibration.currentCalibrationMode == Calibration.CalibMode._2D)
		{
			if (newPosition.Length == 2)
			{
				position.x = newPosition[0];
				position.y = newPosition[1];
				position.z = PupilSettings.Instance.calibration.currentCalibrationType.depth;
				gameObject.transform.position = camera.ViewportToWorldPoint(position);
			} 
			else
			{
				Debug.Log ("Length of new position array does not match 2D mode");
			}
		}
		else if (PupilSettings.Instance.calibration.currentCalibrationMode == Calibration.CalibMode._3D)
		{
			if (newPosition.Length == 3)
			{
				position.x = newPosition[0];
				position.y = newPosition[1];
				position.z = -newPosition[2];
				gameObject.transform.position = camera.cameraToWorldMatrix.MultiplyPoint3x4(position);
			} 
			else
			{
				Debug.Log ("Length of new position array does not match 3D mode");
			}
		}

	}

	public void SetMaterialColor(Color color)
	{
		if (gameObject == null)
			InitializeGameObject ();

		var material = gameObject.GetComponent<MeshRenderer> ().sharedMaterial;
		if (material == null)
			material = new Material (Resources.Load<Material> ("MarkerMaterial"));
		material.SetColor("_EmissionColor",color);
	}

	private void InitializeGameObject()
	{
		gameObject = GameObject.Instantiate (Resources.Load<GameObject> ("MarkerObject"));
		gameObject.name = name;
		gameObject.GetComponent<MeshRenderer> ().sharedMaterial = new Material (Resources.Load<Material> ("MarkerMaterial"));
		gameObject.SetActive (false);
		//				gameObject.hideFlags = HideFlags.HideInHierarchy;
	}

	public void SetActive(bool toggle)
	{
		if (gameObject == null)
			InitializeGameObject ();
		gameObject.SetActive (toggle);
	}
}
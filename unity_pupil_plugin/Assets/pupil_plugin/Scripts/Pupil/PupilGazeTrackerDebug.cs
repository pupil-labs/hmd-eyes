using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class PupilGazeTrackerDebug : MonoBehaviour 
{	
	private PupilGazeTracker _pupilGazeTracker;
	private PupilGazeTracker pupilGazeTracker
	{
		get
		{
			if (_pupilGazeTracker == null)
				_pupilGazeTracker = PupilGazeTracker.Instance;
			return _pupilGazeTracker;
		}
	}
	public _Debug.Debug_Vars DebugVariables;
	public DebugView.variables DebugViewVariables;

	private Calibration.data CalibrationData
	{
		get { return PupilData.CalibrationData; }
	}

	private PupilSettings Settings
	{
		get { return pupilGazeTracker.Settings; }
	}

	//FRAME PUBLISHING VARIABLES

	#region frame_publishing_vars


	static Material lineMaterial;
	static Material eyeSphereMaterial;

	#endregion

	public void InitViewLines ()
	{
		if (LineDrawer.Instance != null)
		{
			LineDrawer.Instance.Clear ();
			foreach (Vector3 _v3 in CalibrationData.cal_gaze_points0_3d)
			{
				LineDrawer.Instance.AddLineToMesh (new LineDrawer.param () {
					points = new Vector3[] {
						PupilData._3D.EyeCenters (0),
						_v3
					},
					color = new Color (1f, 0.6f, 0f, 0.1f)
				});
			}
			foreach (Vector3 _v3 in CalibrationData.cal_gaze_points1_3d)
			{
				LineDrawer.Instance.AddLineToMesh (new LineDrawer.param () {
					points = new Vector3[] {
						PupilData._3D.EyeCenters (1),
						_v3
					},
					color = new Color (1f, 1f, 0f, 0.1f)
				});
			}
			LineDrawer.Instance.Draw ();
		}
	}
	void OnUpdateEyeCenter ()
	{//This happens on MainThread
		InitViewLines ();
	}

	//private Mesh mes;
	int debugViewFrameIndex = 0;

	[HideInInspector]
	public DebugView._Transform[] OffsetTransforms;


	[HideInInspector]
	public bool isDrawCalibrationDebugInitialized = false;

	public void InitDrawCalibrationDebug ()
	{

		if (OffsetTransforms == null)
		{
			OffsetTransforms = new DebugView._Transform[]{ new DebugView._Transform () };
		} else
		{
			foreach (DebugView._Transform _t in OffsetTransforms)
			{
				if (_t.GO == null)
				{
					_t.GO = new GameObject (_t.name);
					_t.GO.transform.position = _t.position;
					_t.GO.transform.rotation = Quaternion.Euler (_t.rotation);
					_t.GO.transform.localScale = _t.localScale;
				}
			}
			var a = (from tr in OffsetTransforms
				where tr.name == "Debug View Origin Matrix"
				select tr).FirstOrDefault () as DebugView._Transform;

			//TODO: Initialize the point clouds outside of the drawer script, for example here, as it is with the line drawer
			DebugViewVariables.PointCloudGO = new GameObject ("PointCloudDrawer");
			DebugViewVariables.PointCloudGO.transform.parent = a.GO.transform;
			DebugViewVariables.PointCloudGO.transform.localPosition = Vector3.zero;
			DebugViewVariables.PointCloudGO.transform.localRotation = Quaternion.identity;
			DebugViewVariables.PointCloudGO.AddComponent<PointCloudDrawer> ();

			DebugViewVariables.LineDrawerGO = new GameObject ("LineDrawer");
			DebugViewVariables.LineDrawerGO.transform.parent = a.GO.transform;
			DebugViewVariables.LineDrawerGO.transform.localPosition = Vector3.zero;
			DebugViewVariables.LineDrawerGO.transform.localRotation = Quaternion.identity;
			DebugViewVariables.LineDrawerGO.AddComponent<LineDrawer> ();

			Invoke ("InitViewLines", .7f);
			DebugViewVariables.isDrawLines = true;
			DebugViewVariables.isDrawPoints = true;
		}
		pupilGazeTracker.OnUpdate += CalibrationDebugInteraction;
		isDrawCalibrationDebugInitialized = true;
	}

	public void CalibrationDebugInteraction ()
	{
		#region DebugView.Interactions
		if (Input.anyKey)
		{
			var a = (from tr in OffsetTransforms
				where tr.name == "Debug View Origin Matrix"
				select tr).FirstOrDefault () as DebugView._Transform;
			if (Input.GetKey (KeyCode.Alpha1))
			{
				a.GO.transform.position = new Vector3 (-7, -9, 127);
				a.GO.transform.rotation = Quaternion.Euler (new Vector3 (150, -25, -15));
			}
			if (Input.GetKey (KeyCode.Alpha0))
			{
				a.GO.transform.position = new Vector3 (-56, -4, 237);
				a.GO.transform.rotation = Quaternion.Euler (new Vector3 (62, 73, -57));
			}
			if (Input.GetKey (KeyCode.Alpha2))
			{
				a.GO.transform.position = new Vector3 (27.3f, -25f, 321.2f);
				a.GO.transform.rotation = Quaternion.Euler (new Vector3 (292.6f, 0f, 0f));
			}
			if (Input.GetKey (KeyCode.Alpha3))
			{
				a.GO.transform.position = new Vector3 (42f, -24f, 300f);
				a.GO.transform.rotation = Quaternion.Euler (new Vector3 (0f, 190f, 0f));
			}
			if (Input.GetKey (KeyCode.Alpha4))
			{
				a.GO.transform.position = new Vector3 (42f, 27f, 226f);
				a.GO.transform.rotation = Quaternion.Euler (new Vector3 (0f, 0f, 0f));
			}
			if (Input.GetKey (KeyCode.Alpha5))
			{
				a.GO.transform.position = new Vector3 (99f, 18f, 276f);
				a.GO.transform.rotation = Quaternion.Euler (new Vector3 (24f, 292f, 30f));
			}
			if (Input.GetKey (KeyCode.W))
				a.GO.transform.position += -Camera.main.transform.forward;
			if (Input.GetKey (KeyCode.S))
				a.GO.transform.position += Camera.main.transform.forward;
			if (Input.GetKey (KeyCode.A))
				a.GO.transform.position += Camera.main.transform.right;
			if (Input.GetKey (KeyCode.D))
				a.GO.transform.position += -Camera.main.transform.right;
			if (Input.GetKey (KeyCode.Q))
				a.GO.transform.position += Camera.main.transform.up;
			if (Input.GetKey (KeyCode.E))
				a.GO.transform.position += -Camera.main.transform.up;
			if (Input.GetKeyDown (KeyCode.P))
			{
				if (DebugViewVariables.isDrawLines || DebugViewVariables.isDrawPoints)
				{
					SetDrawCalibrationLinesNPoints (false);
				} else
				{
					SetDrawCalibrationLinesNPoints (true);
				}
			}
			if (Input.GetKeyUp (KeyCode.R))
			{
				LineDrawer.Instance.Clear ();
				foreach (Vector3 _v3 in CalibrationData.cal_gaze_points0_3d)
				{
					LineDrawer.Instance.AddLineToMesh (new LineDrawer.param () {
						points = new Vector3[] {
							PupilData._3D.EyeCenters (0),
							_v3
						},
						color = new Color (1f, 0.6f, 0f, 0.1f)
					});
				}
				foreach (Vector3 _v3 in CalibrationData.cal_gaze_points1_3d)
				{
					LineDrawer.Instance.AddLineToMesh (new LineDrawer.param () {
						points = new Vector3[] {
							PupilData._3D.EyeCenters (1),
							_v3
						},
						color = new Color (1f, 1f, 0f, 0.1f)
					});
				}
				LineDrawer.Instance.Draw ();
			}

		}
		if (Input.GetMouseButton (1))
		{
			var a = (from tr in OffsetTransforms
				where tr.name == "Debug View Origin Matrix"
				select tr).FirstOrDefault () as DebugView._Transform;
			a.GO.transform.Rotate (new Vector3 (Input.GetAxis ("Mouse Y"), Input.GetAxis ("Mouse X"), 0));

		}
		#endregion
	}

	public void CloseCalibrationDebugView ()
	{
		var a = (from tr in OffsetTransforms
			where tr.name == "Debug View Origin Matrix"
			select tr).FirstOrDefault () as DebugView._Transform;
		if (a.GO != null)
			a.GO.SetActive (false);
		if (!PupilSettings.Instance.debugView.active && !pupilGazeTracker.isOperatorMonitor)
		{	
			PupilTools.StopFramePublishing ();
		}
		pupilGazeTracker.OnUpdate -= CalibrationDebugInteraction;
		pupilGazeTracker.OnCalibDebug -= DrawCalibrationDebugView;
		PupilSettings.Instance.debugView.active = false;
	}

	public void StartCalibrationDebugView ()
	{
		if (DebugViewVariables.DebugEyeMesh != null)
		{
			var a = (from tr in OffsetTransforms
				where tr.name == "Debug View Origin Matrix"
				select tr).FirstOrDefault () as DebugView._Transform;
			if (a.GO != null)
				a.GO.SetActive (true);

			if (pupilGazeTracker.OnCalibDebug == null)
				pupilGazeTracker.OnCalibDebug += DrawCalibrationDebugView;
			//			OnCalibDebug -= DrawCalibrationDebugView;
			pupilGazeTracker.OnUpdate -= CalibrationDebugInteraction;
			pupilGazeTracker.OnUpdate += CalibrationDebugInteraction;

			PupilTools.StartFramePublishing ();
		} else
		{
			UnityEngine.Debug.LogWarning ("Please assign a Debug Eye Mesh under the Settings Debug View Variables. Accessable in Developer Mode!");
			PupilSettings.Instance.debugView.active = false;
		}
	}
	//	public Texture2D circleTexture;

	public Material CreateLineMaterial ()
	{
		if (!lineMaterial)
		{
			// Unity has a built-in shader that is useful for drawing
			// simple colored things.
			Shader shader = Shader.Find ("Hidden/Internal-Colored");
			lineMaterial = new Material (shader);
			lineMaterial.hideFlags = HideFlags.HideAndDontSave;
			// Turn on alpha blending
			lineMaterial.SetInt ("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.SrcAlpha);
			lineMaterial.SetInt ("_DstBlend", (int)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
			// Turn backface culling off
			lineMaterial.SetInt ("_Cull", (int)UnityEngine.Rendering.CullMode.Off);
			// Turn off depth writes
			lineMaterial.SetInt ("_ZWrite", 1);
		}
		return lineMaterial;
	}

	static void CreateEyeSphereMaterial ()
	{
		if (!eyeSphereMaterial)
		{
			Shader shader = Shader.Find ("Hidden/Internal-Colored");
			eyeSphereMaterial = new Material (shader);
			eyeSphereMaterial.hideFlags = HideFlags.HideAndDontSave;
			// Turn on alpha blending
			eyeSphereMaterial.SetInt ("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.SrcAlpha);
			eyeSphereMaterial.SetInt ("_DstBlend", (int)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
			// Turn backface culling off
			eyeSphereMaterial.SetInt ("_Cull", (int)UnityEngine.Rendering.CullMode.Off);
			// Turn off depth writes
			eyeSphereMaterial.SetInt ("_ZWrite", 1);
			//eyeSphereMaterial.
		}
	}


	public void SetDrawCalibrationLinesNPoints (bool toggle)
	{
		SetDrawCalibrationLines (toggle);
		SetDrawCalibrationPointCloud (toggle);
	}

	public void SetDrawCalibrationLines (bool toggle)
	{
		DebugViewVariables.isDrawLines = toggle;
		DebugViewVariables.LineDrawerGO.GetComponent<MeshRenderer> ().enabled = toggle;
	}

	public void SetDrawCalibrationPointCloud (bool toggle)
	{
		DebugViewVariables.isDrawPoints = toggle;
		DebugViewVariables.PointCloudGO.GetComponent<MeshRenderer> ().enabled = toggle;
	}

	public void DrawCalibrationDebugView ()
	{
		debugViewFrameIndex++;

		if (!isDrawCalibrationDebugInitialized)
			InitDrawCalibrationDebug ();

		CreateLineMaterial ();
		CreateEyeSphereMaterial ();

		Vector3 eye0Pos = PupilData._3D.EyeCenters (0);
		Vector3 eye0Norm = PupilData._3D.EyeCenters (0);

		Vector3 eye1Pos = PupilData._3D.EyeCenters (1);
		Vector3 eye1Norm = PupilData._3D.EyeCenters (1);

		Vector3 gazePoint = PupilData._3D.Gaze ();

		////////////////Draw 3D pupils////////////////
		Vector3 _pupil0Center = PupilData._3D.Circle.Center (0);
		Vector3 _pupil1Center = PupilData._3D.Circle.Center (1);
		float _pupil0Radius = (float)PupilData._3D.Circle.Radius (0);
		float _pupil1Radius = (float)PupilData._3D.Circle.Radius (1);
		Vector3 _pupil0Normal = PupilData._3D.Circle.Normal (0);
		DrawDebugSphere (originMatrix: OffsetTransforms [1].GO.transform.localToWorldMatrix, offsetMatrix: CalibrationData.eye_camera_to_world_matrix1, position: _pupil0Center, size: _pupil0Radius, sphereColor: Color.black, forward: _pupil0Normal, wired: false);
		DrawDebugSphere (originMatrix: OffsetTransforms [1].GO.transform.localToWorldMatrix, offsetMatrix: CalibrationData.eye_camera_to_world_matrix0, position: _pupil1Center, size: _pupil1Radius, sphereColor: Color.black, forward: eye0Norm, wired: false);
		////////////////Draw 3D pupils////////////////

		////////////////Draw eye camera frustums////////////////
		DrawCameraFrustum (origin: CalibrationData.eye_camera_to_world_matrix0, fieldOfView: 90, aspect: aspectRatios.FOURBYTHREE, minViewDistance: 0.001f, maxViewDistance: 30, frustumColor: Color.black, drawEye: true, eyeID: 1, transformOffset: OffsetTransforms [1].GO.transform, drawCameraImage: true, eyeMaterial: Settings.framePublishing.eye1ImageMaterial, eyeImageRotation: 0);
		DrawCameraFrustum (origin: CalibrationData.eye_camera_to_world_matrix1, fieldOfView: 90, aspect: aspectRatios.FOURBYTHREE, minViewDistance: 0.001f, maxViewDistance: 30, frustumColor: Color.white, drawEye: true, eyeID: 0, transformOffset: OffsetTransforms [1].GO.transform, drawCameraImage: true, eyeMaterial: Settings.framePublishing.eye0ImageMaterial, eyeImageRotation: 0);
		////////////////Draw eye camera frustums/////////////////// 

		////////////////Draw 3D eyeballs////////////////
		DrawDebugSphere (position: eye0Pos, eyeID: 0, forward: eye0Norm, isEye: true, norm_length: gazePoint.magnitude * DebugVariables.value0, sphereColor: Color.white, norm_color: Color.red, size: DebugViewVariables.EyeSize, originMatrix: OffsetTransforms [1].GO.transform.localToWorldMatrix, wired: false);//eye0
		DrawDebugSphere (position: eye1Pos, eyeID: 1, forward: eye1Norm, isEye: true, norm_length: gazePoint.magnitude * DebugVariables.value0, sphereColor: Color.white, norm_color: Color.red, size: DebugViewVariables.EyeSize, originMatrix: OffsetTransforms [1].GO.transform.localToWorldMatrix, wired: false);//eye1
		////////////////Draw 3D eyeballs////////////////

		////////////////Draw HMD camera frustum//////////////// fov 137.7274f
		DrawCameraFrustum (origin: OffsetTransforms [1].GO.transform.localToWorldMatrix, fieldOfView: 111, aspect: aspectRatios.FULLVIVE, minViewDistance: 0.001f, maxViewDistance: 100f, frustumColor: Color.gray, drawEye: false, eyeID: 0);
		////////////////Draw HMD camera frustum////////////////

		////////////////Draw gaze point 3D////////////////
		DrawDebugSphere (position: gazePoint, eyeID: 10, forward: eye1Norm, isEye: false, norm_length: 20, sphereColor: Color.red, norm_color: Color.clear, size: 10, originMatrix: OffsetTransforms [1].GO.transform.localToWorldMatrix);//eye1
		////////////////Draw gaze point 3D////////////////


	}

	#region DebugView.DrawDebugSphere

	public void DrawDebugSphere (Matrix4x4 originMatrix = default(Matrix4x4), Vector3 position = default(Vector3), int eyeID = 10, Matrix4x4 offsetMatrix = default(Matrix4x4), Vector3 forward = default(Vector3), float norm_length = 20, bool isEye = false, Color norm_color = default(Color), Color sphereColor = default(Color), float size = 24.2f, float sizeZ = 1f, bool wired = true)
	{
		eyeSphereMaterial.SetColor ("_Color", sphereColor);
		eyeSphereMaterial.SetPass (0);

		if (originMatrix == default(Matrix4x4))
			originMatrix = Camera.main.transform.localToWorldMatrix;

		Matrix4x4 _m = new Matrix4x4 ();

		//print ("from : " + forward + " to :  " + Quaternion.LookRotation (forward, Vector3.up));

		//TODO: rework this: now Forward vector needed for position assignment, not good!
		if (forward != Vector3.zero)
		{
			_m.SetTRS (position, Quaternion.LookRotation (forward, Vector3.up), new Vector3 (size, size, size));
		} else
		{
			//TODO: store the last known position and assign that here
			_m.SetTRS (new Vector3 (100 * eyeID, 0, 0), Quaternion.identity, new Vector3 (size, size, size));
			forward = Vector3.forward;
		}

		//		if (position == default(Vector3))
		//			print ("default vector 3 as position found");

		if (offsetMatrix != default(Matrix4x4))
			_m = offsetMatrix * _m;
		if (wired)
			GL.wireframe = true;
		Graphics.DrawMeshNow (DebugViewVariables.DebugEyeMesh, originMatrix * _m);
		GL.wireframe = false;

		if (isEye)
		{

			//IRIS//
			//			eyeSphereMaterial.SetColor ("_Color", new Color(0,1f,0,.5f));
			//			eyeSphereMaterial.SetPass (0);
			//			Graphics.DrawMeshNow(DebugViewVariables.DebugEyeMesh, originMatrix*Matrix4x4.TRS (position + (forward * 10.5f), Quaternion.LookRotation (forward, Vector3.up), new Vector3 (10, 10, 3.7f)));
			//IRIS//

			eyeSphereMaterial.SetColor ("_Color", norm_color);
			eyeSphereMaterial.SetPass (0);

			GL.MultMatrix (originMatrix * _m);
			GL.Begin (GL.LINES);
			GL.Vertex (Vector3.zero);
			GL.Vertex (Vector3.forward * norm_length);
			GL.End ();
		}
	}

	#endregion


	#region DebugView.CameraFrustum


	public enum aspectRatios
	{
		FULLVIVE,
		HALFVIVE,
		FULLHD,
		ONEOONE,
		FOURBYTHREE
	};

	public void DrawCameraFrustum (Matrix4x4 origin, float fieldOfView, aspectRatios aspect, float minViewDistance, float maxViewDistance, Color frustumColor = default(Color), Transform transformOffset = null, bool drawEye = false, int eyeID = 0, bool drawCameraImage = false, int eyeImageRotation = 0, Material eyeMaterial = default(Material))
	{

		lineMaterial.SetColor ("_Color", frustumColor);
		lineMaterial.SetPass (0);

		Matrix4x4 offsetMatrix = new Matrix4x4 ();

		if (origin == default(Matrix4x4))
			origin = Camera.main.transform.localToWorldMatrix;

		if (transformOffset == null)
		{
			offsetMatrix.SetTRS (Vector3.zero, Quaternion.identity, Vector3.one);
		} else
		{
			offsetMatrix = transformOffset.localToWorldMatrix;
		}

		GL.PushMatrix ();

		float aspectRatio = 1;

		switch (aspect)
		{
		case aspectRatios.FULLHD:
			aspectRatio = 1.7777f;
			break;
		case aspectRatios.FULLVIVE:
			aspectRatio = 1.8f;
			break;
		case aspectRatios.HALFVIVE:
			aspectRatio = 0.9f;
			break;
		case aspectRatios.ONEOONE:
			aspectRatio = 1f;
			break;
		case aspectRatios.FOURBYTHREE:
			aspectRatio = 1.3333f;
			break;
		}
		//Vector3 up = origin.up;
		Pupil.Rect3D farPlaneRect = new Pupil.Rect3D ();
		Pupil.Rect3D nearPlaneRect = new Pupil.Rect3D ();

		GL.MultMatrix (offsetMatrix * origin);

		GL.Begin (GL.LINES);
		float ratio = Mathf.Sin (((fieldOfView / 2) * Mathf.PI) / 180) / Mathf.Sin (((((180 - fieldOfView) / 2) * Mathf.PI) / 180));

		float widthMinView = (ratio * minViewDistance * 2) * -1;
		float heightMinView = widthMinView / aspectRatio;
		float widthMaxView = (ratio * maxViewDistance * 2) * -1;
		float heightMaxView = widthMaxView / aspectRatio;

		nearPlaneRect.Draw (widthMinView, heightMinView, minViewDistance, 1);
		farPlaneRect.Draw (widthMaxView, heightMaxView, maxViewDistance, 1, true);



		#region DebugView.CameraFrustum.ConnectRectangles
		//ConnectRectangles
		for (int i = 0; i < nearPlaneRect.verticies.Count (); i++)
		{
			GL.Vertex (nearPlaneRect.verticies [i]);
			GL.Vertex (farPlaneRect.verticies [i]);
		}
		GL.End ();
		#endregion



		lineMaterial.SetColor ("_Color", Color.white);
		lineMaterial.SetPass (0);

		#region DebugView.CameraFrustum.Gizmo
		GL.Begin (GL.LINES);
		//Draw Gizmo
		//X
		GL.Color (Color.red);
		GL.Vertex (Vector3.zero);
		GL.Vertex (Vector3.right * 30);
		//Y
		GL.Color (Color.green);
		GL.Vertex (Vector3.zero);
		GL.Vertex (Vector3.up * 30);
		//Z
		GL.Color (Color.blue);
		GL.Vertex (Vector3.zero);
		GL.Vertex (Vector3.forward * 30);
		//Draw Gizmo
		GL.End ();
		#endregion

		if (drawCameraImage)
			DrawCameraImages (eyeMaterial, farPlaneRect.verticies, farPlaneRect.width, eyeImageRotation);
		//		if (drawEye) {
		//			float flipper = 1;
		//			if (eyeID == 1)
		//				flipper = -1;
		//
		//			float scaler = widthMaxView / 640 / 24.2f;//scaling
		//
		//			Matrix4x4 _imageSpaceMatrix = offsetMatrix * origin * Matrix4x4.TRS (new Vector3(flipper*(widthMaxView/2), -flipper*(heightMaxView/2),maxViewDistance), Quaternion.identity, Vector3.one*24.2f);
		//			float eyeCenterX = 0f;
		//			float eyeCenterY = 0f;
		//			eyeCenterX = (float)Pupil.values.BaseData [eyeID].projected_sphere.center [0];
		//			eyeCenterY = (float)Pupil.values.BaseData [eyeID].projected_sphere.center [1];
		//			GL.wireframe = true;
		//			Graphics.DrawMeshNow (DebugViewVariables.DebugEyeMesh, _imageSpaceMatrix * Matrix4x4.Translate (  new Vector3 (-flipper*eyeCenterX* scaler, flipper*eyeCenterY* scaler, 0)   ));
		//			GL.wireframe = false;
		//		}

		GL.PopMatrix ();


	}

	#endregion

	#region DebugView.CameraFrustum.CameraImages

	void DrawCameraImages (Material eyeMaterial, Vector3[] drawPlane, float width, int offset = 0)
	{
		float[] _f = new float[]{ 0, 1, 1, 0, 0, 1, 1, 0, 0 };
		eyeMaterial.SetPass (0);
		GL.Begin (GL.QUADS);
		for (int j = drawPlane.Count () - 1; j > -1; j--)
		{
			int ind = (drawPlane.Count () - 1) - j + offset;
			GL.TexCoord2 (_f [ind], _f [ind + 1]);
			GL.Vertex3 (-drawPlane [j].x, drawPlane [j].y, drawPlane [j].z);
		}
		GL.End ();
	}

	#endregion
}

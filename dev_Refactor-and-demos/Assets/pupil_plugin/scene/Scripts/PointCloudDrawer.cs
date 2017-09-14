using UnityEngine;
using System.Linq;
using System.Collections;
using System.Collections.Generic;

[RequireComponent(typeof(MeshFilter), typeof(MeshRenderer))]
public class PointCloudDrawer : MonoBehaviour {

	private Mesh mesh;
	private MeshRenderer mRenderer;

	PupilGazeTracker pupilTracker;

	static PointCloudDrawer _Instance;
	public static PointCloudDrawer Instance
	{
		get{
			return _Instance;
		}
	}

	void Start () {
		mesh = new Mesh();
		pupilTracker = PupilGazeTracker.Instance;
//		print (pupilTracker.CalibrationData.camera_intrinsics.camera_matrix);

		mRenderer = GetComponent<MeshRenderer> ();
		mRenderer.material = Resources.Load ("Material/Pupil", typeof(Material)) as Material;

		Invoke ("InitializePointClouds", 1f);

		_Instance = this;
	}

	public void InitializePointClouds(){
		mesh = new Mesh();
		GetComponent<MeshFilter>().mesh = mesh;
		mesh.name = "PointCloudMesh";
		AddToMesh(pupilTracker.CalibrationData.cal_ref_points_3d, Color.blue, ref mesh);
		AddToMesh(pupilTracker.CalibrationData.cal_gaze_points0_3d, new Color(1f, 0.6f, 0f, 1f), ref mesh);
		AddToMesh(pupilTracker.CalibrationData.cal_gaze_points1_3d, Color.yellow, ref mesh);
		AddToMesh(pupilTracker.CalibrationData.cal_points_3d, Color.red, ref mesh);
	}

	public void AddToMesh(Vector3[] points, Color color, ref Mesh _mesh) {
		int[] indecies = new int[points.Length];
		Color[] colors = new Color[points.Length];
		int[] oldIndices = _mesh.GetIndices (0);

		for(int i=0;i<points.Length;++i) {
			indecies [i] = i + oldIndices.Length;
			colors[i] = color;
		}
//		print (_mesh.colors.Length + " + " + points.Length + "=" + (_mesh.colors.Length + points.Length));
//		print (_mesh.vertices.Length + " + " + colors.Length + "=" + (_mesh.vertices.Length + colors.Length));
		Vector3[] newPoints = _mesh.vertices.Concat (points).ToArray ();
		Color[] newColors = _mesh.colors.Concat(colors).ToArray();
		_mesh.vertices = newPoints;
		_mesh.colors = newColors;
		_mesh.SetIndices(oldIndices.Concat(indecies).ToArray(), MeshTopology.Points,0);

	}
//	void Update(){
//		if (Input.GetKeyUp (KeyCode.L)) {
//			InitializePointClouds ();
//		}
//	}
}
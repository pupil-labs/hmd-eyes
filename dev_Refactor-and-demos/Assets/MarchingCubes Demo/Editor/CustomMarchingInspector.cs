using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;

[CustomEditor(typeof(MarchingCubes))]
public class CustomMarchingInspector : Editor {

	MarchingCubes mc;



	public override void OnInspectorGUI ()
	{

		mc = (MarchingCubes)target;


		base.OnInspectorGUI ();

		mc.voxelShape = (MarchingCubes.VoxelShape)GUILayout.Toolbar ((int)mc.voxelShape, new string[]{ "Plane", "Box", "Sphere" });

		if (GUILayout.Button ("Save")) {



			if (mc.values.Count != 0) {



//				mc.Level.values = mc.values;

				mc.Level.values.Clear ();

				foreach (float f in mc.values) {
			
					mc.Level.values.Add (f);

				}

			} else {
			
				Debug.Log ("values is zero");

			}

		}

		if (GUILayout.Button ("Load")) {

//			mc.values.Clear ();

			if (mc.Level.values.Count != 0) {

				mc.gameObject.GetComponent<MeshFilter> ().mesh = new Mesh ();
				mc.geometry = new Mesh ();

				mc.values.Clear ();

				foreach (float f in mc.Level.values) {

					mc.values.Add (f);

				}


				Debug.Log (mc.Level.values.Count);
				Debug.Log (mc.values.Count);

				mc.CreateChunk ();

			} else {
			
				Debug.Log ("mc.level.valuse.count is zero on Load()");

			}

		}

		if (GUILayout.Button ("Create Level")) {
			
			VoxelLevel vl = ScriptableObject.CreateInstance<VoxelLevel> ();

			AssetDatabase.CreateAsset (vl, "Assets/level.asset");
			AssetDatabase.SaveAssets ();


		}

	}

}

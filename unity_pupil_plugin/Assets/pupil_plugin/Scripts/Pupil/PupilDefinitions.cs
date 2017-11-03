using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Pupil
{
	//Pupil data types based on Yuta Itoh sample hosted in https://github.com/pupil-labs/hmd-eyes
	[Serializable]
	public class ProjectedSphere
	{
		public double[] axes = new double[] { 0, 0 };
		public double angle;
		public double[] center = new double[] { 0, 0 };
	}

	[Serializable]
	public class Sphere
	{
		public double radius;
		public double[] center = new double[] { 0, 0, 0 };
	}

	[Serializable]
	public class Circle3d
	{
		public double radius;
		public double[] center = new double[] { 0, 0, 0 };
		public double[] normal = new double[] { 0, 0, 0 };
	}

	[Serializable]
	public class Ellipse
	{
		public double[] axes = new double[] { 0, 0 };
		public double angle;
		public double[] center = new double[] { 0, 0 };
	}

	public class Rect3D
	{
		public float width;
		public float height;
		public float zOffset;
		public float scale;
		public Vector3[] verticies = new Vector3[4];

		public void SetPosition ()
		{
			verticies [0] = new Vector3 (-(width / 2) * scale, -(height / 2) * scale, zOffset);
			verticies [1] = new Vector3 ((width / 2) * scale, -(height / 2) * scale, zOffset);
			verticies [2] = new Vector3 ((width / 2) * scale, (height / 2) * scale, zOffset);
			verticies [3] = new Vector3 (-(width / 2) * scale, (height / 2) * scale, zOffset);
		}

		public void Draw (float _width, float _height, float _zOffset, float _scale, bool drawCameraImage = false)
		{
			width = _width;
			height = _height;
			zOffset = _zOffset;
			scale = _scale;

			SetPosition ();
			for (int i = 0; i <= verticies.Length - 1; i++)
			{
				GL.Vertex (verticies [i]);
				if (i != verticies.Length - 1)
				{
					GL.Vertex (verticies [i + 1]);
				} else
				{
					GL.Vertex (verticies [0]);
				}
			}
		}
	}

	[Serializable]
	public class eyes3Ddata
	{
		public double[] zero = new double[]{ 0, 0, 0 };
		public double[] one = new double[]{ 0, 0, 0 };
	}

	public struct  processStatus
	{
		public static bool initialized;
		public static bool eyeProcess0;
		public static bool eyeProcess1;
	}
}
namespace Operator
{
	[Serializable]
	public class properties
	{
		public int id;
		[HideInInspector]
		public Vector3 positionOffset = new Vector3 ();
		[HideInInspector]
		public Vector3 rotationOffset = new Vector3 ();
		[HideInInspector]
		public Vector3 scaleOffset = Vector3.one;
		[HideInInspector]
		public Vector2 graphScale = new Vector2 (1, 1);
		[HideInInspector]
		public float gapSize = 1;
		[HideInInspector]
		public int graphLength = 20;
		public float confidence = 0.2f;
		public float refreshDelay;
		[HideInInspector]
		public long graphTime = DateTime.Now.Ticks;
		[HideInInspector]
		public bool update = false;
		[HideInInspector]
		public List<float> confidenceList = new List<float> ();
		[HideInInspector]
		public Camera OperatorCamera;
		[HideInInspector]
		public static properties[] Properties = default(Operator.properties[]);
	}
}

namespace DebugView
{
	[Serializable]
	public class _Transform
	{
		public string name;
		public Vector3 position;
		public Vector3 rotation;
		public Vector3 localScale;
		public GameObject GO;
	}

	[Serializable]
	public class variables
	{
		public float EyeSize = 24.2f;
		//official approximation of the size of an avarage human eye(mm). However it may vary from 21 to 27 millimeters.
		[HideInInspector]
		public PupilMarker Circle;
		public bool isDrawPoints = false;
		public bool isDrawLines = false;
		[HideInInspector]
		public GameObject PointCloudGO;
		[HideInInspector]
		public GameObject LineDrawerGO;
		public Mesh DebugEyeMesh;
	}
	//	[Serializable]
	//	public class framePublishingVariables{
	//		public int targetFPS = 20;
	//		public Texture2D eye0Image;
	//		public Texture2D eye1Image;
	//		[HideInInspector]
	//		public byte[] raw0;
	//		[HideInInspector]
	//		public byte[] raw1;
	//		[HideInInspector]
	//		public bool StreamCameraImages = false;
	//		public Material eye0ImageMaterial;
	//		public Material eye1ImageMaterial;
	//	}
}

#region DebugVariables
namespace _Debug
{
	[Serializable]
	public class Debug_Vars
	{
		public bool subscribeFrame;
		public bool subscribeGaze;
		public bool subscribeAll;
		public bool subscribeNotify;
		public bool printSampling;
		public bool printMessage;
		public bool printMessageType;
		public float value0;
		public float value1;
		public float value2;
		public float value3;
		public float WorldScaling;
		public bool packetsOnMainThread;
	}
}
#endregion

[Serializable]
public struct floatArray
{
	public float[] axisValues;
}

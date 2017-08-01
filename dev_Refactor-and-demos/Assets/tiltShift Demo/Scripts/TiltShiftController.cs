using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public static class EnumExtension{
	
	public static List<Enum> GetFlags(this Enum _enum){

		Array enums = Enum.GetValues (_enum.GetType());

		List<Enum> enumsWithFlag = new List<Enum>();

		foreach(Enum _e in enums){
		
			if (_enum.GetFlags().Contains(_e))
				enumsWithFlag.Add (_e);

		}

		return enumsWithFlag;

	}

}

public class TiltShiftController : MonoBehaviour {

	public static PostFX.TiltShift tiltShift;

	public GameObject floorModule;

	public int width;
	public int height;

	public float floatThreshold;

	[Range(-1f,1f)]
	public float targetOffset;

	[Range(0f,20f)]
	public float targetArea;

	[Range(0f,20f)]
	public float targetSpread;

	[Range(0f,2f)]
	public float targetRadius;

	public float transitionSpeed;

	public enum TiltShiftProperty
	{
		OFFSET = 1,
		AREA = 2,
		SPREAD = 4,
		RADIUS = 6
	}
			
	[HideInInspector]
	public TiltShiftProperty tiltShiftPropertyTypes;

	public List<Enum> flaggedTiltShiftProperties;

	public Dictionary<TiltShiftProperty, tiltDetails> propertyDictionary;

	public struct tiltDetails{
		
		public string propertyName;
		public string targetPropertyName;

	}

	void Start () {

		tiltShift = GetComponent<PostFX.TiltShift> ();

		flaggedTiltShiftProperties = tiltShiftPropertyTypes.GetFlags ();

		UpdatePropertyDictionary (ref propertyDictionary);

		InitializeFloor (width, height);

//		Instantiate (floorModule);

	}

	public void InitializeFloor(int width, int height){

		GameObject Floor = new GameObject ("Floor");

		for (int i = 0; i<width;i++){
			for (int j = 0; j<height;j++){
			
				GameObject go = Instantiate (floorModule, Floor.transform);
				float x = i * go.transform.localScale.x - ((width * go.transform.localScale.x) / 2);
				float y = UnityEngine.Random.Range (-.7f, .7f);
				float z = j * go.transform.localScale.z - ((height * go.transform.localScale.z) / 2);
				go.transform.Translate (x, y, z);

			}
		}

	}

	public Dictionary<TiltShiftProperty, tiltDetails> UpdatePropertyDictionary(ref Dictionary<TiltShiftProperty, tiltDetails> _propertyDictionary){
	
		_propertyDictionary = new Dictionary<TiltShiftProperty, tiltDetails> ();

		_propertyDictionary.Add (TiltShiftProperty.AREA, new tiltDetails () {
			propertyName = "Area",
			targetPropertyName = "targetArea"
		});

		_propertyDictionary.Add (TiltShiftProperty.OFFSET, new tiltDetails () {
			propertyName = "Offset",
			targetPropertyName = "targetOffset"
		});

		_propertyDictionary.Add (TiltShiftProperty.SPREAD, new tiltDetails () {
			propertyName = "Spread",
			targetPropertyName = "targetSpread"
		});

		_propertyDictionary.Add (TiltShiftProperty.RADIUS, new tiltDetails () {
			propertyName = "Radius",
			targetPropertyName = "targetRadius"
		});

		return _propertyDictionary;

	}

	public float SmoothSetValue(float _o, float _t){
	
		float _r = _o;

		if (Mathf.Abs (_o - _t) > floatThreshold)
			_r = Mathf.Lerp (_o, _t, transitionSpeed * Time.deltaTime);

		return _r;

	}

	void Update(){

		foreach (TiltShiftProperty _prop in flaggedTiltShiftProperties) {

//			propertyDictionary
			string refName = propertyDictionary[_prop].propertyName;
			string targetName = propertyDictionary [_prop].targetPropertyName;

			float oValue = (float)tiltShift.GetType ().GetField (refName).GetValue (tiltShift);
			float tValue = (float)this.GetType ().GetField (targetName).GetValue (this);

//			print ("ref name : " + refName + ". target ref name : " + targetName);
//			print (propertyDictionary);

			tiltShift.GetType ().GetField (refName).SetValue (tiltShift, SmoothSetValue (oValue, tValue));

		}

//		print (tiltShift.GetType ().GetField ("Offset").GetValue (tiltShift));

	}
		
}

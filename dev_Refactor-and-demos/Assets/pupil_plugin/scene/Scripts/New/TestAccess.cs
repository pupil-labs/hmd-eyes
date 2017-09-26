using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class TestAccess : MonoBehaviour {

	// Use this for initialization
	void Start () {
		
	}
	
	// Update is called once per frame
	void Update () {



		if (PupilSettings.Instance.connection.isConnected) {

//			if ( PupilData.gazeDictionary != null && ((object[])PupilData.gazeDictionary ["norm_pos"]).Length > 0 )
//				print (  ((object[])PupilData.gazeDictionary ["norm_pos"])[0] );

//			print (PupilData._2D.Norm_Pos ().ToString());

//			print ("Eye 0 confidence : " + PupilData.Confidence (0));
//
//			print ("Eye 1 confidence : " + PupilData.Confidence (1));



//			object o = new object ();
//
//			print (PupilData.pupil0Dictionary.Count);
//
//			string stuff;
//
//			stuff = "phi";
//			PupilData.pupil0Dictionary.TryGetValue (stuff, out o);
//			print ("Type of : " + stuff + " is : " + o.GetType ());
//
//			stuff = "timestamp";
//			PupilData.pupil0Dictionary.TryGetValue (stuff, out o);
//			print ("Type of : " + stuff + " is : " + o.GetType ());
//
//
//			stuff = "ellipse";
//			PupilData.pupil0Dictionary.TryGetValue (stuff, out o);
//			print ("Type of : " + stuff + " is : " + o.GetType ());
//
//			Dictionary<object, object> ellipse = new Dictionary<object, object> ();
//			ellipse = o as Dictionary<object, object>;
//
//			stuff = "axes";
//			ellipse.TryGetValue (stuff, out o);
//			print ("Type of : " + stuff + " is : " + o.GetType ());
//
//			print ((o as object[])[0]);
//
////			stuff = "center";
////			PupilData.pupil0Dictionary.TryGetValue (stuff, out o);
////			print ("Type of : " + stuff + " is : " + o.GetType ());
//
//			stuff = "phi";
//			PupilData.pupil0Dictionary.TryGetValue (stuff, out o);
//			print ("Type of : " + stuff + " is : " + o.GetType ());
//		
		}
	}
}

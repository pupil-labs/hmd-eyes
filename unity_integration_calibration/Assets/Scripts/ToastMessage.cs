using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class ToastMessage : MonoBehaviour {
	
	static ToastMessage _Instance;
	static Canvas _canvas;
	static List<toastMessage> _messagesList = new List<toastMessage>();

	public class toastParameters{
		public string text = "default toas text";
		public int ID = 0;
		public float delay = 2f;
		public float fadeOutSpeed = 2f;
	}

	public static ToastMessage Instance{
		get{ 
			if (_Instance == null) {
				GameObject _go = new GameObject ("ToastMessages");
				_Instance = _go.AddComponent<ToastMessage> ();
				_go.transform.parent = Camera.main.transform;
				_canvas = _go.AddComponent<Canvas> ();
				_canvas.renderMode = RenderMode.ScreenSpaceOverlay;
				_canvas.worldCamera = Camera.main;
				VerticalLayoutGroup _v = _go.AddComponent<VerticalLayoutGroup> ();
				_v.spacing = -80;
				_v.childControlHeight = false;
				_v.childForceExpandHeight = false;
//				_v.padding.top = 100;
				_go.AddComponent<CanvasScaler> ();
			}
			return _Instance;
		}
	}
	public ToastMessage()
	{
		_Instance = this;
	}
		
	public class toastMessage : MonoBehaviour{
		public toastParameters _params = new toastParameters ();

		private Text _textUI;
		private IEnumerator _lifeTimer;

		void Awake(){
			_textUI = this.gameObject.AddComponent<Text> ();
			_textUI.alignment = TextAnchor.UpperCenter;
			_textUI.font = Resources.GetBuiltinResource<Font> ("Arial.ttf");
			_textUI.fontSize = 20;
			_lifeTimer = lifeTimer ();
			StartCoroutine (_lifeTimer);
		}
		public void Reset(){
			StopCoroutine(_lifeTimer);
			_textUI.CrossFadeAlpha (1.0f, 0.2f, false);
			_lifeTimer = lifeTimer();
			StartCoroutine (_lifeTimer);
		}
		IEnumerator lifeTimer(){
			yield return new WaitForSeconds (0.2f);
			_textUI.text = _params.text;
			yield return new WaitForSeconds (_params.delay);
			_textUI.CrossFadeAlpha (0.0f, _params.fadeOutSpeed, false);
			yield return new WaitForSeconds (_params.fadeOutSpeed);
			Destroy (this.gameObject);
			_messagesList.Remove (this);
			yield break;
		}
	}
//	public void DrawToastMessage(object _parameters){
//		print ("asdasdasd");
//		toastParameters _tp = new toastParameters ();
//		_tp = _parameters as toastParameters;
//		print (_tp.delay);
//		//DrawToastMessage (_tp);
//	}
	public void DrawToastMessage(object _params){
		toastMessage _message;
		toastParameters _p = _params as toastParameters;
		//_message._params = new toastParameters ();

		if (_messagesList.Exists (m => m._params.ID == _p.ID) && _p.ID != 0) {
			_message = _messagesList.Find (m => m._params.ID == _p.ID);
			_message.Reset();
		} else {
			_message = new GameObject ("message").AddComponent<toastMessage> ();
			_message.transform.SetParent (_canvas.transform);
			_messagesList.Add (_message);
		}
		_message._params = new toastParameters ();
		print (_p.delay);
		_message._params.delay = _p.delay;
		_message._params.fadeOutSpeed = _p.fadeOutSpeed;
		_message._params.text = _p.text;
		_message._params.ID = _p.ID;
	}
	public void DrawToastMessageOnMainThread(toastParameters _params){
		print ("on main thread : " + _params);
		object _o = _params;
		MainThread.Call (DrawToastMessage, _o);
	}
}


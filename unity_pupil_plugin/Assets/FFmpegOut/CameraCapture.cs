using System;
using System.IO;
using System.Text;
using System.Threading;
using System.Collections.Generic;
using System.Collections;
using UnityEngine;


namespace FFmpegOut
{
    [RequireComponent(typeof(Camera))]
    [AddComponentMenu("FFmpegOut/Camera Capture")]
    public class CameraCapture : MonoBehaviour
    {
        #region Editable properties

		[SerializeField] bool _setResolution = true;
//		[SerializeField] public int _width = 1280;
//        [SerializeField] public int _height = 720;
        [SerializeField] int _frameRate = 30;
        [SerializeField] FFmpegPipe.Codec _codec;
        [SerializeField] public float _recordLength = 5;

        #endregion

        #region Private members

        [SerializeField] public Shader _shader;

		enum RecorderState {RECORDING,PROCESSING,STOPPING,IDLE}
		RecorderState _recorderState = RecorderState.RECORDING;

        Material _material;

        FFmpegPipe _pipe;
        float _elapsed;

        RenderTexture _tempTarget;
        GameObject _tempBlitter;

		Thread RecorderThread;

		List<byte[]> renderPipeQueue = new List<byte[]>();
		object datalock = new object();

		PupilGazeTracker pupilTracker;

		int renderedFrameCount = 0;
		int writtenFrameCount = 0;

		List<float> timeStampList = new List<float>();
//		StringBuilder strBuilder;

		public Camera RecordedMainCamera;

//		bool renderImage = true;

        #endregion

		public Shader sh;

        #region MonoBehavior functions

        void OnValidate()
        {
            _recordLength = Mathf.Max(_recordLength, 0.01f);
        }

        void OnEnable()
        {
            if (!FFmpegConfig.CheckAvailable)
            {
                Debug.LogError(
                    "ffmpeg.exe is missing. " +
                    "Please refer to the installation instruction. " +
                    "https://github.com/keijiro/FFmpegOut"
                );
                enabled = false;
            }
			RecorderThread = new Thread (RecorderThreadMethod);
			RecorderThread.Start ();
        }

//        void OnDisable()
//        {
//			if (_pipe != null) Stop ();
//			
//        }

        void OnDestroy()
        {
			if (_pipe != null) ClosePipe ();
        }

        void Start()
        {
			_material = new Material (Shader.Find ("Hidden/FFmpegOut/CameraCapture"));
			pupilTracker = PupilGazeTracker.Instance;
        }

        void Update()
        {
            _elapsed += Time.deltaTime;

			if (_elapsed < pupilTracker.recorder.recordingLength)
            {
                if (_pipe == null) OpenPipe();
            }
            else
            {
				if (_pipe != null && pupilTracker.recorder.isFixedRecordingLength && _recorderState == RecorderState.RECORDING) Stop ();
            }

			if (_recorderState == RecorderState.STOPPING) {
				
				pupilTracker.RepaintGUI ();
				GameObject.Destroy (this.gameObject);

			}

        }

		public void Stop(){
			Recorder.isRecording = false;
			PupilGazeTracker.Instance.StopPupilServiceRecording ();
			_recorderState = RecorderState.PROCESSING;
			Recorder.isProcessing = true;
		}

		void RecorderThreadMethod(){
			renderPipeQueue.Clear();
			while (true){
				Thread.Sleep (1);

				if (renderPipeQueue.Count > 0) {
					_pipe.Write (renderPipeQueue [0]);
					writtenFrameCount++;
					renderPipeQueue.RemoveAt (0);
//					print ("writing data. Remaining : " + renderPipeQueue.Count);
				} else {
					if (_recorderState == RecorderState.PROCESSING) {
						Recorder.isProcessing = false;
						_recorderState = RecorderState.STOPPING;
						RecorderThread.Join ();
					}
				}

			}


		}

        void OnRenderImage(RenderTexture source, RenderTexture destination)
        {
			if (_pipe != null && _recorderState == RecorderState.RECORDING)
            {
                var tempRT = RenderTexture.GetTemporary(source.width, source.height);
				var pupilTimeStamp = pupilTracker.GetPupilTimestamp ();


				if (_material != null) {
					Graphics.Blit (source, tempRT, _material, 0);
				} else {
					Debug.LogWarning ("Material for the recorder is null, fix this!");
				}


                var tempTex = new Texture2D(source.width, source.height, TextureFormat.RGB24, false);
                tempTex.ReadPixels(new Rect(0, 0, source.width, source.height), 0, 0, false);
                tempTex.Apply();



				renderPipeQueue.Add (tempTex.GetRawTextureData ());
				renderedFrameCount++;

				timeStampList.Add (pupilTimeStamp);
                //_pipe.Write(tempTex.GetRawTextureData());

                Destroy(tempTex);
                RenderTexture.ReleaseTemporary(tempRT);
            }

            Graphics.Blit(source, destination);
        }

        #endregion

        #region Private methods

        void OpenPipe()
        {
            if (_pipe != null) return;

			renderPipeQueue.Clear ();
			timeStampList = new List<float> ();

            var camera = GetComponent<Camera>();
			var width = pupilTracker.recorder.resolutions [(int)pupilTracker.recorder.resolution] [0];
			var height = pupilTracker.recorder.resolutions [(int)pupilTracker.recorder.resolution] [1];
            // Apply the screen resolution settings.
            if (_setResolution)
            {
                _tempTarget = RenderTexture.GetTemporary(width, height);
                camera.targetTexture = _tempTarget;
                _tempBlitter = Blitter.CreateGameObject(camera);
            }
            else
            {
                width = camera.pixelWidth;
                height = camera.pixelHeight;
            }

            // Open an output stream.
			_pipe = new FFmpegPipe(pupilTracker.recorder.filePath, width, height, _frameRate, pupilTracker.recorder.codec);

            // Change the application frame rate.
            if (Time.captureFramerate == 0)
            {
                Time.captureFramerate = _frameRate;
            }
            else if (Time.captureFramerate != _frameRate)
            {
                Debug.LogWarning(
                    "Frame rate mismatch; the application frame rate has been " +
                    "changed with a different value. Make sure using the same " +
                    "frame rate when capturing multiple cameras."
                );
            }

            Debug.Log("Capture started (" + _pipe.Filename + ")");
        }
        
		void ClosePipe()
        {
			
            var camera = GetComponent<Camera>();

            // Destroy the blitter object.
            if (_tempBlitter != null)
            {
                Destroy(_tempBlitter);
                _tempBlitter = null;
            }

            // Release the temporary render target.
            if (_tempTarget != null && _tempTarget == camera.targetTexture)
            {
                camera.targetTexture = null;
                RenderTexture.ReleaseTemporary(_tempTarget);
                _tempTarget = null;
            }

            // Close the output stream.
            if (_pipe != null)
            {
				Debug.Log ("Capture ended (" + _pipe.Filename + ")" + ". Rendered frame count on MainThread : " + renderedFrameCount + ". Written out frame count on SecondaryThread : " + writtenFrameCount + ". Leftover : " + renderPipeQueue.Count);


				string timeStampFileName = "Unity_" + Camera.main.name;
				byte[] timeStampByteArray = pupilTracker.floatArrayToByteArray (timeStampList.ToArray ());
				File.WriteAllBytes(_pipe.FilePath + "/" + timeStampFileName + ".time", timeStampByteArray);

				//File.WriteAllText (_pipe.FilePath + "/" + csvFileName + ".time", strBuilder.ToString ());

               
				_pipe.Close();

                if (!string.IsNullOrEmpty(_pipe.Error))
                {
                    Debug.LogWarning(
                        "ffmpeg returned with a warning or an error message. " +
                        "See the following lines for details:\n" + _pipe.Error
                    );
                }

                _pipe = null;
            }


        }
//		private string SingleToBinaryString(float f)
//		{
//			byte[] b = BitConverter.GetBytes(f);
//			int i = BitConverter.ToInt32(b, 0);
//			return Convert.ToString(i, 2);
//		}
        #endregion
    }
}


using System.Threading;
using System.Collections.Generic;
using UnityEngine;


namespace FFmpegOut
{
    [RequireComponent(typeof(Camera))]
    [AddComponentMenu("FFmpegOut/Camera Capture")]
    public class CameraCapture : MonoBehaviour
    {
        #region Editable properties

		[SerializeField] bool _setResolution = true;
		[SerializeField] public int _width = 1280;
        [SerializeField] public int _height = 720;
        [SerializeField] int _frameRate = 30;
        [SerializeField] FFmpegPipe.Codec _codec;
        [SerializeField] public float _recordLength = 5;

        #endregion

        #region Private members

        [SerializeField, HideInInspector] Shader _shader;
        Material _material;

        FFmpegPipe _pipe;
        float _elapsed;

        RenderTexture _tempTarget;
        GameObject _tempBlitter;

		Thread RecorderThread;

		List<byte[]> renderPipeQueue = new List<byte[]>();
		object datalock = new object();

        #endregion

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
			//if (!RecorderThread.IsAlive)
			RecorderThread = new Thread (RecorderThreadMethod);
			RecorderThread.Start ();
        }

        void OnDisable()
        {
            if (_pipe != null) ClosePipe();
			RecorderThread.Join ();
        }

        void OnDestroy()
        {
            if (_pipe != null) ClosePipe();
        }

        void Start()
        {
            _material = new Material(_shader);
        }

        void Update()
        {
            _elapsed += Time.deltaTime;

            if (_elapsed < _recordLength)
            {
                if (_pipe == null) OpenPipe();
            }
            else
            {
                if (_pipe != null) ClosePipe();
            }
        }

		void RecorderThreadMethod(){
			while (true){
				Thread.Sleep (10);
				if (renderPipeQueue.Count > 0) {
					_pipe.Write (renderPipeQueue [0]);
					renderPipeQueue.RemoveAt (0);
				}
			}
		}

        void OnRenderImage(RenderTexture source, RenderTexture destination)
        {
            if (_pipe != null)
            {
                var tempRT = RenderTexture.GetTemporary(source.width, source.height);
                Graphics.Blit(source, tempRT, _material, 0);

                var tempTex = new Texture2D(source.width, source.height, TextureFormat.RGB24, false);
                tempTex.ReadPixels(new Rect(0, 0, source.width, source.height), 0, 0, false);
                tempTex.Apply();

				renderPipeQueue.Add (tempTex.GetRawTextureData ());
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

            var camera = GetComponent<Camera>();
            var width = _width;
            var height = _height;

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
			_pipe = new FFmpegPipe(Recorder.FilePath, width, height, _frameRate, _codec);

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
                Debug.Log("Capture ended (" + _pipe.Filename + ")");

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
			//PupilGazeTracker.Instance.StopRecording ();
			Recorder.Stop();
        }

        #endregion
    }
}

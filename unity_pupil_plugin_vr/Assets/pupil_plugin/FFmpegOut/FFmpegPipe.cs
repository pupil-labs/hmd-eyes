using System.Diagnostics;
using System.IO;
using System;
using System.Collections.Generic;
using UnityEngine;
using System.Threading;

#if !UNITY_WSA
namespace FFmpegOut
{
    // A stream pipe class that invokes ffmpeg and connect to it.
    public class FFmpegPipe
    {
        #region Public properties

        public enum Codec { ProRes, H264, VP8 }
		public enum Resolution { _1080p, _720p, Preview }

        public string Filename { get; private set; }
        public string Error { get; private set; }
		public string FilePath;

        #endregion

        #region Public methods

        public FFmpegPipe(string name, int width, int height, int framerate, Codec codec)
        {
			PupilGazeTracker pupilTracker = PupilGazeTracker.Instance;

			string path = PupilSettings.Instance.recorder.GetRecordingPath ();

			Thread.Sleep (200);//Waiting for Pupil Service to create the incremented folder

			path += "/" + GetLastIncrement (path);

			if (!Directory.Exists (path))
				Directory.CreateDirectory (path);
			
			Filename = "\"" + path + "/" + name + GetSuffix (codec) + "\"";
            
            var opt = "-y -f rawvideo -vcodec rawvideo -pixel_format rgb24";
            opt += " -video_size " + width + "x" + height;
            opt += " -framerate " + framerate;
            opt += " -loglevel warning -i - " + GetOptions(codec);
            opt += " " + Filename;

            var info = new ProcessStartInfo(FFmpegConfig.BinaryPath, opt);
            info.UseShellExecute = false;
            info.CreateNoWindow = true;
            info.RedirectStandardInput = true;
            info.RedirectStandardOutput = true;
            info.RedirectStandardError = true;

			FilePath = path;

            _subprocess = Process.Start(info);
            _stdin = new BinaryWriter(_subprocess.StandardInput.BaseStream);
        }

        public void Write(byte[] data)
        {
            if (_subprocess == null) return;

            _stdin.Write(data);
            _stdin.Flush();
        }

        public void Close()
        {
            if (_subprocess == null) return;

            _subprocess.StandardInput.Close();
            _subprocess.WaitForExit();

            var outputReader = _subprocess.StandardError;
            Error = outputReader.ReadToEnd();

            _subprocess.Close();
            _subprocess.Dispose();

            outputReader.Close();
            outputReader.Dispose();

            _subprocess = null;
            _stdin = null;
        }

        #endregion

        #region Private members

        Process _subprocess;
        BinaryWriter _stdin;

        static string [] _suffixes = {
            ".mov",
            ".mp4",
            ".webm"
        };

        static string [] _options = {
            "-c:v prores_ks -pix_fmt yuv422p10le",
            "-pix_fmt yuv420p",
            "-c:v libvpx"
        };

        static string GetSuffix(Codec codec)
        {
            return _suffixes[(int)codec];
        }

        static string GetOptions(Codec codec)
        {
            return _options[(int)codec];
        }
		public string GetLastIncrement(string path)
		{
			string[] directories = Directory.GetDirectories (path);
			List<int> directoryIncrements = new List<int> ();
			foreach (string directory in directories) 
			{
				var folderNameValue = int.Parse (directory.Substring (directory.Length - 3));
				directoryIncrements.Add (folderNameValue);
			}
			int currentIncrement = Mathf.Max (directoryIncrements.ToArray());
//			int newIncrement = currentIncrement + 1;
			return (currentIncrement+1).ToString ("000");
			//directoryIncrements.ToArray()
		}
        #endregion
    }
}
#endif

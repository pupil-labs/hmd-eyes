# hmd-eyes

Building blocks for eye tracking in Augmented Reality `AR` and Virtual Reality `VR` or more generally Head Mounted Displays `HMD`.

## Setup

1. Make sure that you have [Pupil Capture or Pupil Service](https://github.com/pupil-labs/pupil/releases/latest) on a Linux or macOS machine.
2. Open `Assets/scene/Calibration.unity` with the Unity3d Editor.
3. Select the PupilGaze GameObject in Unity Hierarchy.

4. Connection
	* #### Local
		1. In Unity's Inspector select Settings>>Pupil App>>Local
		2. Click on the Browse button and navigate then select the pupil_service.exe/pupil_capture.exe you want to use.
		3. Make sure that your Service Port under the path matches the one you have in your Pupil Capture!
	* #### Remote
		1. In Unity's Inspector select Settings>>Pupil App>>Remote
		2. Make sure that your Pupil Capture application is running on the remote machine.
		3. Take note of the IP address of the active communication device in your remote machine. (Please keep in mind that in some cases the IP address stated in Pupil Capture may not be correct, check the IP address of your system!).
		4. Copy this IP address to the IP address field in Unity.
		
5. Still under Settings>>Pupil App you have the option to toggle Autorun Pupil Service when Play is pressed in Unity, by clicking on the Autorun Pupil Service button. Please note that once Autorun in turned off in Unity's Play mode you will have the option to Start/Stop Pupil Service. These options are right under the above mentioned toggle button.
6. Establish connection . Make sure that the line under the Pupil Labs logo says (connected) and both eye Icons are green( these are indicating that each eye process has started successfully thus both eye camera windows are running in the Pupil App ).

## Calibration

1. Choose your desired calibration method under Settings>>Calibration. Currently you have two options 2D or 3D.
2. Make sure you have followed the steps of the Setup.
3. Press Play in Unity and wait until you have established a working connection with your Pupil App.
4. Navigate to Main and press Calibrate or press "C" on the keyboard.
5. Put your headset on and steadily gaze at the calibration markers. You will have different amount of markers depending on your calibration method.

> The ideal setup for calibration may vary using different headsets. For optimal calibration results I suggest some experimenting with the hardware settings of your HMD device such as the eye distance from the lens, inter eye distance, a steady mount position.

## Data access

1. 2D
	1. Currently you can access the 2D gaze point with ```GetEyeGaze2D (GazeSource.BothEyes)``` .
2. 3D
	
	1. ```Pupil.values.GazePoint3D```
	2. ```Pupil.values.GazeNormals3D```
	3. ```Pupil.values.EyeCenters3D```

## Further Details
Further documentation is on the way, mainly for developers.

## Connect

Chat with the hmd-eyes community on [Discord](https://discord.gg/PahDtSH).

Join the [google group](https://groups.google.com/forum/#!forum/hmd-eyes) to discuss ideas and stay updated. 

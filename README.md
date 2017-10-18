# hmd-eyes

Building blocks for eye tracking in Augmented Reality `AR` and Virtual Reality `VR` or more generally Head Mounted Displays `HMD`.
This repository demonstrates how to implement it with Unity3D.

## Setup

1. Make sure that you have [Pupil Capture or Pupil Service](https://github.com/pupil-labs/pupil/releases/latest) on a Linux, macOS, or Windows 10 machine. 
2. The Unity3D sample project contains two scenes: `pupil_plugin/Calibration.unity` and `Market Scene Demo/Market Scene Demo.unity`
3. The former contains the `PupilGazeTracker` gameobject, which lets you adapt settings, while the latter serves as a starting point on how the eye tracking can be implemented.
4. On Windows 10 and with the standard project settings, the path is set to `C:/Program Files/Pupil Service/pupil_service.exe`. If you have a different setup, please adapt it as follows 

## Chaning standard settings via Calibration.unity
1. Connection
	* **Local** - use this setting if your HMD and your Pupil add-on are connected to the same computer.
		1. In Unity's Inspector select `Settings>Pupil App>Local`
		2. Click on the Browse button. Navigate to Pupil Service - pupil_service.exe - **or** Pupil Capture - pupil_capture.exe.
		3. If you're using Pupil Capture make sure your Service Port matches the Port in Pupil Capture. See the Pupil Remote plugin in Pupil Capture to check.
	* **Remote (still to be tested with the updated version)** - Use the remote mode if your HMD and pupil eye tracking add-on are connected to different computers, but on the same wifi or wired network.
		1. In Unity's Inspector select `Settings>Pupil App>Remote`
		2. Make sure that your Pupil Capture application is running on the remote machine.
		3. Take note of the IP address of the active communication device in your remote machine. (Please keep in mind that in some cases the IP address stated in Pupil Capture may not be correct, check the IP address of your system!).
		4. Copy this IP address to the IP address field in Unity.
		
2. Auto-run Pupil App settings - If auto-run is enabled, Pupil Service will run automatically when your Unity3d scene is in play mode. You can disable this behavior by turning off auto-run in `Settings>Pupil App` and manually Start or Stop Pupil Service.
3. Confirm connection - Make sure that the plugin status (below the Pupil Labs logotype) displays the word "connected" and that both of the eye icons are green. The green eye icons signify that the eye processes are running.
4. Once you press `Play`, Unity will try to connect based on your settings and two `Pupil Service` windows should appear (one for either eye). Please make sure Unity is in focus/the foreground app after this process.

## Calibration

1. With the new version (October 2017), we currently only support 2D calibration.
2. Calibration is the first step for both Calibration and Market Scene Demo Unity scenes, after the connection has been established.
3. This would be a good point to put your headset on.
4. As should be displayed on screen, press the key `c` to start the calbration process
5. Follow the markers as their position changes
6. After a successful calibration, the `Calibration.unity` scene will display three colored markers representing the left (green) and right (blue) eye plus the center point (red).
7. `Market Scene Demo.unity` will load a 3D scene with 3 alterinative visualizations for gaze tracking
	* The one described before with 3 colored markers
	* A laserpointer going to the center of your gaze
	* A shader-based implementation that grays out the area around each of the eyes position
	
Note: The ideal setup for calibration may vary using different headsets. For optimal calibration results I suggest some experimenting with the hardware settings of your HMD device such as the eye distance from the lens, inter eye distance, a steady mount position.

## Data access

1. 2D
	* `PupilData._2D.GetEyeGaze (PupilData.GazeSource s)` will provide the current viewport coordinates needed to get 3d world positions (used e.g. for the three colored markers)
	* `PupilData._2D.GetEyePosition (Camera sceneCamera, GazeSource gazeSource)` applies an additional frustum center offset for each eye (used e.g. for the shader implementation)  
2. 3D
	* Still to be implemented

## Connect

Chat with the hmd-eyes community on [Discord](https://discord.gg/PahDtSH).

Join the [google group](https://groups.google.com/forum/#!forum/hmd-eyes) to discuss ideas and stay updated. 



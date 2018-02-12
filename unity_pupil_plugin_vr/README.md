# hmd-eyes

Building blocks for eye tracking in Augmented Reality `AR` and Virtual Reality `VR` or more generally Head Mounted Displays `HMD`.
The purpose of this repository is do demonstrates how to implement Pupil with Unity3D. For details on Pupil computations, please have a look at the related repositories.

## Main new files/classes with a short description
* Connection -> Handles communication with ZeroMQ/NetMQ including initialization, sending and receiving messages
* PupilData -> Used to save and provide pupil positions
* PupilGazeTracker -> Main class to initialize settings, start the calibration process and implement standard gaze visualization
* PupilSettings -> Home to project variables and properties including calibration and connection instances
* PupilTools -> Handles messages pushed/pulled through the sockets. Basically everything dealing with dictionary objects custom to Pupil

## Setup

1. Make sure that you have [Pupil Capture or Pupil Service](https://github.com/pupil-labs/pupil/releases/latest) on a Linux, macOS, or Windows 10 machine. 
2. The Unity3D sample project contains multiple scenes: `pupil_plugin/Calibration.unity`, `pupil_plugin/Blink.unity`, `Market Scene Demo/2D Calibration Demo.unity`, `Market Scene Demo/3D Calibration Demo.unity`, `Market Scene Demo/Heatmap.unity` and `Market Scene Demo/SphericalVideo.unity`
3. The former two represent barebone examples with an implementation of the calibration process (needed for gaze tracking) and a simpler variant that accesses Pupil to get blink information
4. The latter ones are all based on the market scene assets available for free on the Unity Asset Store (http://u3d.as/DRi) and serve as a starting point on how eye tracking can be implemented with either a 2D or 3D calibration
5. Before you start any scene, make sure you either have Pupil Capture already running or that the path to Pupil Service is set correctly.
6. On Windows 10 and with the standard project settings, the path is set to `C:/Program Files/Pupil Service/pupil_service.exe`. If you have a different setup, please adapt it as follows 

## Changing standard settings via PupilGazeTracker
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
4. Once you press `Play`, Unity will try to connect based on your settings and two Pupil windows should appear (one for either eye). Please make sure Unity is in focus/the foreground app after this process.

## Calibration

1. Starting with versions released after mid November 2017 and in addition to 2D calibration, we also support 3D calibration. This requires a Pupil version >= 1.0
2. Calibration is the first step for both the Calibration and all Market Scene Demos, after the connection has been established.
3. This would be a good point to put your headset on.
4. As should be displayed on screen, press the key `c` to start the calbration process
5. Follow the markers as their position changes
6. If you want to adapt marker positions, have a look at `pupil_plugin/Resources/PupilSettings` and expand `Calibration` in Unity Inspector
	* Markers are currently positioned on a circular pattern. 
	* `Calibration Type 2D/3D` provides a Vector3 array to define the depth, radius and scale for each circular layer.
	* These values will have to be adapted based on the headset you are using. They are currently set for the HTC Vive.
	* For a more in-depth look consult the corresponding Calibration.cs class
7. After a successful calibration, the `Calibration.unity` scene will display three colored markers representing the left (green) and right (blue) eye plus the center point (red).
8. `Market Scene Demo/2D Calibration Demo.unity` will load a 3D scene with 3 alterinative visualizations for gaze tracking
	* The one described before with 3 colored markers
	* A laserpointer going to the center of your gaze
	* A shader-based implementation that grays out the area around each of the eyes position	
9. `Market Scene Demo/3D Calibration Demo.unity` implements a simple 3D calibration visualization
	* a white colored marker positioned directly by the Pupil output
	
Note: The ideal setup for calibration may vary using different headsets. For optimal calibration results I suggest some experimenting with the hardware settings of your HMD device such as the eye distance from the lens, inter eye distance, a steady mount position.

## Data access

Once calibration is done, you need to call PupilTools.Subscribe(string topic) to receive messages for the 'topic' you specify. The standard case for a topic is either 'gaze', 'pupil.' or both.
1. 2D
	* `PupilData._2D.GetEyeGaze (PupilData.GazeSource s)` will provide the current viewport coordinates needed to get 3d world positions (used e.g. for the three colored markers)
	* `PupilData._2D.GetEyePosition (Camera sceneCamera, GazeSource gazeSource)` applies an additional frustum center offset for each eye (used e.g. for the shader implementation)  
2. 3D
	* `PupilData._3D.GazePosition`

## Blink demo

For users who do not need gaze data or want a simple example on how to subscribe to a topic and read data from the socket, please have a look at the Blink demo.
* As suggested by its name, this demo utilizes the Blink_Detection Pupil plugin
* It does not require to run through the calibration process
* While dictionary setup is usually kept within PupilTools, BlinkDemoManager contains all blink-specific variants to give a better overview of what is involved
* This includes starting/stoping the plugin, un-/subscribing to "blinks" and receiving the dictionary packages from Pupil
	
## Spherical Video demo
As many users asked for this feature, we now include a demo that can load and display a 360 degree video. Combined with Pupil, this allows to visualize what the user is looking at.
Starting February 12th, we switched to using Unity's internal solution to play spherical videos. This requires Unity3D v 2017.3. Please revert to the previous implementation if you plan not to update Unity to the newest release.
* To load a video, select the `VideoSphere` gameobject and click the "Set path.." button, shown in Inspector
* Alternatively, you can set the path through the public string variable `FilePath` of `SphericalVideoPlayer.cs`

## Heatmap demo
The heatmap demo allows to export gaze visualization to a spherical video or image. Eye tracking positions are translated to particles on a spherical texture, which is overlayed on the 3d scene. The heatmap is available as Prefab, as well, and can be added to existing scenes by draging it onto the main camera of that scene. 
* Press `h` to start recording the output video or to capture the current view to an image. The output path defined by the settings for PupilGazeTracker recordings
* A few variables can change how the heatmap behaves. To change them select the Heatmap gameobject
* `Mode`
	1. 'Particle' will color the area the user is looking at
	2. 'ParticleDebug' will show it for the user, as well
	3. 'Highlight' will only fill-in the area looked at
	4. 'Image' will keep all particles and color code them based on the time of creation
* `Particle Size`: The size of a single gaze visualising particle
* `Remove Particle After X Seconds`: Set how many seconds a particle should be visualized (not used for Image mode)
* `Particle Color`: The color a particle should have.
* `Particle Color Final`: Color for oldest particle in Image mode. Every color in between will be interpolated


## Connect

Chat with the hmd-eyes community on [Discord](https://discord.gg/PahDtSH).

Join the [google group](https://groups.google.com/forum/#!forum/hmd-eyes) to discuss ideas and stay updated. 



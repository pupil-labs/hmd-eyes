# hmd-eyes

Building blocks for eye tracking in Augmented Reality `AR` and Virtual Reality `VR` or more generally Head Mounted Displays `HMD`.
The purpose of this repository is do demonstrates how to implement Pupil with Unity3D.
For details on Pupil computations, please have a look at the related repositories.

As the underlying NetMQ integration (of the master branch) did not work with the combination of Unity3d and UWP targets, an alternative had to be developed for HoloLens.
Building on the fact that Pupil Capture has to run on a remote computer, an UDP-based solution was chosen to communicate between Pupil and the Unity client running on HoloLens.  

## Altered and new files/classes with a short description
Connection -> Highly customized version for HoloLens. Mainly used to send commands to trigger behavior on the Pupil UDP side.
PupilData -> Pupil gaze positions are now directly received via UDP and made available through this class.
PupilTools -> With the exception of calibration points, dictionaries have been mostly replaced.
UDPCommunication -> UWP implementation of UDP. Sends/Receives data to/from Pupil

## Setup

1. Make sure that you have [Pupil Capture](https://github.com/pupil-labs/pupil/releases/latest) on a Linux, macOS, or Windows 10 machine
2. The Unity3D sample project contains three scenes: `pupil_plugin/Calibration.unity` and `Shark Demo/2D Calibration Demo.unity` respectively `Shark Demo/3D Calibration Demo.unity`
3. The former contains only the `PupilGazeTracker` gameobject - which lets you adapt settings - and can be used to test the calibration process. The latter two serve as a starting point on how the eye tracking can be implemented with either a 2D or 3D calibration
4. The HoloLens solution only works with Pupil running on a remote machine
5. Start Pupil Capture and activate "Hololens relay" in the Plugin Manager. The standard UDP port is 50021
6. Open the Unity project, select `pupil_plugin/Resources/PupilSettings` and expand `Connection`
7. `Pupil Remote Port` needs to be set to the same port set in Pupil Capture and `Pupil Remote IP` needs to point to the remote machine
8. You can now build the project for deployment on HoloLens by selecting `Files/Build Settings..` from the menu
9. Select the scene you want to be included and click `Build`
10. After a successful build, go to chosen folder and double-click `HoloLens Client.sln` to open Visual Studio
11. Deploy to your HoloLens

## Calibration

1. Calibration is the first step for both Calibration and Shark Demo scenes, after the connection has been established
2. This would be a good point to put your headset on
3. Open the `Pupil Comm` menu with the double air-tap gesture
4. Menu buttons can be activated by looking straight at one (which will hightlight it) and performing a single air-tap
5. Tap the left button to connect to Pupil
6. Once connected, tap the right button to start calibration
7. Follow the markers as their position changes
	* If you want to adapt marker positions, have a look at `pupil_plugin/Resources/PupilSettings` and expand `Calibration` in Unity Inspector
	* Markers are currently positioned on a circular pattern. 
	* `Calibration Type 2D/3D` provides a Vector3 array to define the depth, radius and scale for each circular layer.
	* For a more in-depth look consult the corresponding Calibration.cs class
7. After a successful calibration, the `Calibration.unity` scene will display three colored markers representing the left (green) and right (blue) eye plus the center point (red).
8. In addition to the marker visualization (three markers in case of 2D calibration, one in case of 3D), the Shark demos show an exemplary 3D model (a rotating shark)
	* The 2D calibration scene alse includes a shader-based implementation that grays out the area around the gaze position
	
## Data access

The current HoloLens implementation only provides gaze positions, so subscribing to any topic other than 'gaze' will not work. You can get access to these as follows
1. 2D
	* `PupilData._2D.GetEyeGaze (PupilData.GazeSource s)` will provide the current viewport coordinates needed to get 3d world positions (used e.g. for the three colored markers)
2. 3D
	* `PupilData._3D.GazePosition`

## Connect

Chat with the hmd-eyes community on [Discord](https://discord.gg/PahDtSH).

Join the [google group](https://groups.google.com/forum/#!forum/hmd-eyes) to discuss ideas and stay updated. 




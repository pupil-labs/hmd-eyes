# hmd-eyes

Building blocks for eye tracking in Augmented Reality `AR` and Virtual Reality `VR` or more generally Head Mounted Displays `HMD`.

## Setup

1. Make sure that you have [Pupil Capture or Pupil Service](https://github.com/pupil-labs/pupil/releases/latest) on a Linux or macOS machine.
2. Open `Assets/scene/Calibration.unity` with the Unity3d Editor.
3. In Unity3d - configure the `PupilGaze` property `Server IP` to point to the machine running Pupil Remote and set the port based on what is shown in Pupil Remote (default port is: 50020) under the Settings tab.
4. (Windows)Set the Pupil Service App Path to the path where your pupil_service.exe is stored by clicking on the Browse button.
5. _[HMD Hardware Dependent]_ Some headsets like the HTC Vive require the Open VR SDK. This SDK is is not enabled in the project by default. You can add it by going to `Edit > Project Settings > Player > Other Settings` and selecting it using the `+` symbol. Make sure that `Virtual Reality Supported` is also checked
6. Plugin your HMD and start it up.
7. Press the `Play` button in the Unity Editor. (This starts Pupil Eye Processes; check to make sure that the pupil is well detected).
8. Choose the calibration method under 'PupilGaze' GameObject >> Calibration >>2D/3D
9. To calibrate press the `C` key, or click on the Calibrate button

## Develeoper Setup

1. Open `Assets/scene/Calibration.unity` with the Unity3d Editor.
2. Select the GameObject called 'PupilGaze'
3. Under Settings, tick the ShowAll checkbox, this will enable you to see all exposed variables.
4. There are 5 Syles that can be adjusted. Main Tabs, Settings Labels, Settings Values, Settings Browse, Logo.
5. The Default paths and file names for the Pupil Service App are stored under the Platforms variable for each platform. You can add one manually if your current platform is not listed.
6. In the current version the 2D and 3D calibration points are not exposed in the final GUI, but you can find and edit them here until the interface comes out. Relevant variables : Calib Points 2D, Calib Points 3D.

## Connect

Chat with the hmd-eyes community on [Discord](https://discord.gg/PahDtSH).

Join the [google group](https://groups.google.com/forum/#!forum/hmd-eyes) to discuss ideas and stay updated. 

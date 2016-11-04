# hmd-eyes

Building blocks for eye tracking in Augmented Reality `AR` and Virtual Reality `VR` or more generally Head Mounted Displays `HMD`.

## Setup

1. Start [Pupil Capture or Pupil Service](https://github.com/pupil-labs/pupil/releases/latest) on a Linux or macOS machine. Make sure that the Pupil Remote plugin is loaded.
2. Open `Assets/scene/Calibration.unity` with the Unity3d Editor.
3. In Unity3d - configure the `PupilGaze` property `Server IP` to point to the machine running Pupil Remote and set the port based on what is shown in Pupil Remote (default port is: 50020).
4. _[HMD Hardware Dependent]_ Some headsets like the HTC Vive require the Open VR SDK. This SDK is is not enabled in the project by default. You can add it by going to `Edit > Project Settings > Player > Other Settings` and selecting it using the `+` symbol. Make sure that `Virtual Reality Supported` is also checked
5. Plugin your HMD and start it up.
6. Press the `Play` button in the Unity Editor. (This starts Pupil Eye Processes; check to make sure that the pupil is well detected).
7. To calibrate press the `C` key.

## Connect

Chat with the hmd-eyes community here [![Gitter](https://badges.gitter.im/pupil-labs/hmd-eyes.svg)](https://gitter.im/pupil-labs/hmd-eyes?utm_source=badge&utm_medium=badge&utm_campaign=pr-badge)

Join the [google group](https://groups.google.com/forum/#!forum/hmd-eyes) to discuss ideas and stay updated. 

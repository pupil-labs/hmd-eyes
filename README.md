hmd-eyes
========

Building blocks for eye tracking in Augmented Reality `AR` and Virtual Reality `VR` or more generally Head Mounted Displays `HMD`. The purpose of this repository is do demonstrates how to implement Pupil with Unity3D. For details on Pupil computations, please have a look at the related repositories.


## VR Getting Started

This guide walks you through a first time setup for your Pupil Labs VR add-on.

1. [HTC Vive Add-on setup](https://docs.pupil-labs.com/#htc-vive-add-on) - Install your Pupil eye tracking add-on into your HMD and connect the add-on to your computer. 
1. [Download Pupil Software](https://github.com/pupil-labs/pupil/releases/latest). Extract the Pupil Capture app to your Desktop.
1. Start Pupil Capture via the `pupil_capture.exe` in the Pupil Capture App folder. Pupil Capture does pupil detection from eye images.
1. [Download and start hmd-eyes demo app](https://github.com/pupil-labs/hmd-eyes/releases/latest) - This demo runs a VR experience. The demo app talks to Pupil software in  the background. You will use it to calibrate and visualize gaze data within a demo scene. The demo app will start with settings dialog. <!-- insert image of app demo dialog -->
1. Start the demo with default values. You will see a view of the left eye of the hmd.
1. This would be a good point to put said device on your head.
2. Use the displayed realtime videos of your eyes to make sure they are as centered as possible and in focus.
3. Press 'c' on your keyboard to start a calibration and focus your gaze on the displayed marker as it changes position.
4. After a successful calibration, this scene will be loaded 

	![Market Scene Demo](https://github.com/pupil-labs/hmd-eyes/blob/master/GettingStarted/2DMarketScene.png)


## HoloLens Getting Started

1. [HoloLens Add-on setup](https://docs.pupil-labs.com/#hololens-add-on) - Set up the Pupil eye tracking add-on with your HMD and connect it to a PC.
1. [Download Pupil Software](https://github.com/pupil-labs/pupil/releases/latest). Extract the Pupil Capture app to your Desktop.
1. Start Pupil Capture via the `pupil_capture.exe` in the Pupil Capture App folder. A window like this will appear when service is running. Capture does pupil detection from eye images.
1. Select the `Plugin Manager` in `Pupil Capture` and start the `HoloLens Relay` plugin ![Pupil Capture with HoloLens Relay](https://github.com/pupil-labs/hmd-eyes/blob/master/GettingStarted/PupilCaptureWithHoloLensRelay.png)
1. [Download and install Unity 3D](https://store.unity.com/).
1. [Download the hmd-eyes source code](https://github.com/pupil-labs/hmd-eyes/releases/latest). Extract the Unity project sources for HoloLens, located in `unity_pupil_plugin_hololens`.
1. In Unity3d open the `unity_pupil_plugin_hololens` as a project. Double click the `Shark Demo/2D Calibration Demo` to load the scene and open the `Holographic Emulation` tab (`Menu/Window/Holographic Emulation`)
1. In the `Holographic` tab, select `Remote to Device` as `Emulation Mode`
1. Read more about Unity's `Holographic Emulation` [here](https://docs.unity3d.com/550/Documentation/Manual/windowsholographic-emulation.html).
1. Start the `Holographic Remoting Player` on your HoloLens device. Enter the displayed device IP in the `Holographic` tab under `Remote machine`. Click the `Connect` button.
1. Once connected, press `Play` in Unity Editor.
1. Follow the on device instructions to open the menu (double air tap) and select `Connect to Pupil` by looking straight at the button and confirming with a single air tap. ![HoloLens Menu](https://github.com/pupil-labs/hmd-eyes/blob/master/GettingStarted/HoloLensMenu.png)
1. Once the connection between Unity3d and `Pupil Capture` is established two eye windows will open. Use these windows to adjust the eye cameras for good tracking.
1. The `Start Calibration` will now be enabled on the HoloLens display.
1. Calibrate - Start the calibration. Focus your gaze on the displayed marker for each displayed position.
1. After a successful calibration, you should see a rotating 3D model  and a visualization of your gaze ![Shark Demo with Gaze Visualization](https://github.com/pupil-labs/hmd-eyes/blob/master/GettingStarted/2DDemoHoloLens.png)

## Develop, integrate, and extend

Check out the [developer docs](https://github.com/pupil-labs/hmd-eyes/blob/master/Developer.md) to learn  how to set up dev envirmoment, make changes to code, and integrate with your own Unity3D project.

## Community

Chat with the hmd-eyes community on [Discord](https://discord.gg/PahDtSH).

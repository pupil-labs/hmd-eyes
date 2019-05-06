hmd-eyes beta
========

**Work in progress:** This is the **beta version** of the new hmd-eyes Unity integration. We recommend using this version for everyone starting with hmd-eyes or starting a new project. Any level of feedback is highly appreciated.

---------------

Building blocks for eye tracking in Augmented Reality `AR` and Virtual Reality `VR` or more generally Head Mounted Displays `HMD`. The purpose of this repository is do demonstrates how to implement Pupil with Unity3D. For details on Pupil computations, please have a look at the related repositories.

## VR Getting Started

This guide walks you through a first time setup for your Pupil Labs VR add-on.

1. [HTC Vive Add-on setup](https://docs.pupil-labs.com/#htc-vive-add-on) - Install your Pupil eye tracking add-on into your HMD and connect the add-on to your computer. 
2. [Download Pupil Software](https://github.com/pupil-labs/pupil/releases/latest). Extract the Pupil Capture app to your Desktop.
3. Start Pupil Capture via the `pupil_capture.exe` in the Pupil Capture App folder.
4. Please refer to the Pupil [getting started](https://docs.pupil-labs.com/#capture-workflow) and [user docs](https://docs.pupil-labs.com/#pupil-detection) to ensure that eyes are well captured and that the pupil detection runs with high confidence (~0.8).
5. [Download and start hmd-eyes demo app](https://github.com/pupil-labs/hmd-eyes/releases/latest) - This demo runs a VR experience. The demo app talks to Pupil software in  the background. You will use it to calibrate and visualize gaze data within a demo scene. The demo app will start with settings dialog. 
6. Start the demo with default values. You will see a view of the left eye of the hmd.
7. This would be a good point to put said device on your head.
8. Use the displayed realtime videos of your eyes to make sure they are as centered as possible and in focus.
9. Press 'c' on your keyboard to start a calibration and focus your gaze on the displayed marker as it changes position.
10. After a successful calibration, the example scene will appear again and the gaze estimate will be visualized.

## Develop, integrate, and extend

Download the *.unitypackage from the [latest release](https://github.com/pupil-labs/hmd-eyes/releases/latest) and 
check out the [developer docs](./docs/Developer.md) to learn how to set up dev environment, make changes to code, and integrate with your own Unity3D project.

**Dependencies**: 

* Unity 2018.3+
* `ProjectSettings/Player/Configuration/Scripting Runtime Verion` set to **.NET 4.x Equivalent**.
* Due to an issue with MessagePack, the default project setting for `ProjectSettings/Player/Configuration/API Compatibility Level` is not supported and needs to be set to **.NET 4.x**

## Community

Chat with the hmd-eyes community on [Discord](https://discord.gg/PahDtSH).

# hmd-eyes

Building blocks for eye tracking in Augmented Reality `AR` and Virtual Reality `VR` or more generally Head Mounted Displays `HMD`.

## Setup:

1- Run Pupil Capture or Pupil Service in ubuntu/mac. Pupil Remote has to be turned on.

2- Open Assets/scene/Calibration.unity with the Unity Editor

3- Configure PupilGaze's property "Server IP" to point to the PC running Pupil Remote (Port: 50020)

[4- Some headsets like the HTC Vive require the Open VR SDK, which is not enabled in the project by default. You can add it by going to Edit -> Project Settings -> Player -> Other Settings and selecting it using the +-symbol ("Virtual Reality Supported" has to be checked too)]

5- Plugin the HMD and start it if you have not done so yet

6- Hit the Play-Button in the Unity Editor (This starts Pupil Eye Processes, make sure the pupil is detected well)

7- To calibrate hit the C-Key

## Connect

Chat with the hmd-eyes community here [![Gitter](https://badges.gitter.im/pupil-labs/hmd-eyes.svg)](https://gitter.im/pupil-labs/hmd-eyes?utm_source=badge&utm_medium=badge&utm_campaign=pr-badge)

Join the [google group](https://groups.google.com/forum/#!forum/hmd-eyes) to discuss ideas and stay updated. 

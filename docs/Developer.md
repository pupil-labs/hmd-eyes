# Developer Documentation

  - [Getting Started](#getting-started)
    - [Pupil Capture or Service](#pupil-capture-or-service)
    - [Dependencies](#dependencies)
    - [Adding hmd-eyes to Existing Projects](#adding-hmd-eyes-to-existing-projects)
    - [VR Build and Player Settings](#vr-build-and-player-settings)
  - [Plugin Overview](#plugin-overview)
  - [Communicating with Pupil](#communicating-with-pupil)
  - [Display Eye Images](#display-eye-images)
  - [Calibration](#calibration)
    - [Calibration Settings and Targets](#calibration-settings-and-targets)
    - [Calibration Success/Failure](#calibration-successfailure)
  - [Gaze Tracking](#gaze-tracking)
    - [GazeDirection + GazeDistance instead of GazePoint3d](#gazedirection--gazedistance-instead-of-gazepoint3d)
  - [Accessing Data](#accessing-data)
    - [Examples for topics that do not require calibration](#examples-for-topics-that-do-not-require-calibration)
  - [Recording Data](#recording-data)
  - [Demo Scenes](#demo-scenes)

## Getting Started

This section of the docs guide through the setup of the development environment in Unity3D + Pupil Capture/Service. 

*Please follow each step of this section and familiarize yourself with the Pupil Capture/Service before jumping into action.*

### Pupil Capture or Pupil Service

For people new to hmd-eyes, we recommend using the project with [Pupil Capture](https://docs.pupil-labs.com/#pupil-capture). 
It supports recordings and the GUI offers detailed feedback like pupil detection confidence, which is especially important during the initial setup and adjusting the pupil detection settings. 
Be aware that Pupil Capture sends out data packages in small bundles with a slightly higher delay. 

In contrast [Pupil Service](https://docs.pupil-labs.com/#pupil-service) features a simplified GUI and a toolset tailored towards low-latency real-time data access.

Please refer to the Pupil Software [getting started](https://docs.pupil-labs.com/#capture-workflow) and [user docs](https://docs.pupil-labs.com/#pupil-detection) to ensure that eyes are well captured and that the pupil detection runs with high confidence (~0.8).

### Dependencies

* Unity 2018.3+
* `ProjectSettings/Player/Configuration/Scripting Runtime Verion` set to **.NET 4.x Equivalent**.
* Due to an issue with MessagePack, the default project setting for `ProjectSettings/Player/Configuration/API Compatibility Level` is not supported and needs to be set to **.NET 4.x**
  
### VR Build and Player Settings

![VR Build And Player Settings](VRBuildAndPlayerSettings.png)

Hmd-eyes provides a `starter_project_vr`. This project is almost identical to the default Unity 3D project and only contains changes in the settings. You still have to install the hmd-eyes plugin as described below.

If you start from a fresh project, you have to make sure that the following settings are set:

* `ProjectSettings/Player/Configuration/Scripting Runtime Verion` = **.NET 4.x Equivalent**.
* `ProjectSettings/Player/Configuration/API Compatibility Level` = **.NET 4.x**

The software has been tested for both Oculus and OpenVR SDKs. Please make sure to select the correct SDK for your headset. 

### Adding hmd-eyes to Existing Projects

>Please be aware that for Unity plugins in general to work (especially demo scenes) it is not sufficient to clone and copy paste the plugin/source folder!

Instead HMD-Eyes provides what is called `Unity Package` assets
- `Pupil.Import.Package.VR.unitypackage`
- ~~`Pupil.Import.Package.HoloLens.unitypackage`~~ *(TBD)*

To import either one in Unity, select `Assets/Import Package/Custom Package...` and navigate to where you downloaded the file to. You are presented with a dialog to choose from the files included in the package. Press `Import` to import all of them. This will create a new folder named `Plugins/Pupil` in your project structure including all necessary scripts and assets to integrate eye tracking with your own project.

### First Steps

To get started with hmd-eyes, we recommend to have a look at and play with the [Demo Scenes](#demo-scenes). Make sure that the demo scenes are working as described (especially the `GazeDemoScene`).

## Plugin Overview

The plugin architecture consists of three layers, which are building on one another.

- **Network Layer** (low level api): 

    Connection to the network API of Pupil Capture/Service, utilizing `NetMQ` and `MessagePack` to send and receive messages (`RequestController`) and manage subscriptions (`SubscriptionsController`). 

- **Pupil Communicaton** (high level api): 

    High level classes handling the communication (*e.g.* `Calibration`) and data parsing for the main topics like pupil (`PupilListener`) and gaze (`GazeListener`).       

- **Flow Control**:

    Ready-made components for visualizing data (*e.g.* `GazeVisualizer`) and guiding through processes like the calibration (`CalibrationController`). 

![Architecture](architecture.png)

<!-- ### Component Setup

*TBD* -->

## Connection to Pupil Capture/Service

All demos contain an instance of the `Pupil Connection` prefab. 
It combines the *Request- and Subscriptionscontroller*, which are the base for accessing the network API of Pupil Capture/Service and represent the low-level API of hmd-eyes.

![Pupil Connection](PupilConnection.png)

#### RequestController

The `RequestController` is reponsible for connecting to the network API (including access to the sub- and pubport) and handles all request towards Pupil - for example starting a plugin.

**Settings**: 

* `auto connect` - Connect on StartUp (default on)
* `IP` & `Port` - need to match settings under Pupil Remote (changed via the inspector under `Request`)

**Events**:  
    
* `OnConnected()`: after a connection is established.
* `OnDisconnecting()`: right before the connection is closed to allow for freeing resources and ending routines.

#### SubscriptionsController

The `SubscriptionsController` allows to subscribe to data messages based on their topics. It manages all `SubscriptionSockets` and delegate bindings internally (see [Accessing Data](#accessing-data)). 
 
**Events:** The method `SubscribeTo(...)` allows to bind C# event delegates for each topic individually. 

---

Both components provide a property for the connection status via `IsConnected()`.

>All *Listeners* and high level components need acces to the Pupil Connection object, which is done via the Unity inspector.  

## Accessing Data 

> Be aware that all timestamps received via hmd-eyes are in Pupil Time. Have a look the `TimeSync` component discusssed [here](#timesync). 

### Low-level data acess

Demo: [Blink Demo](#Blink)

The `SubscriptionsController` is the main component to get low-level data access. 

> For the topics [pupil](#pupil-data) and [gaze](#gaze-tracker) we provided additional abstraction with listener classes and high level components.

The **BlinkDemo** is a good starting point for understanding how the low-level communication with Pupil works in case you need access to data not provided by the *Pupil Communication* layer.

```csharp
//set via inspector
public SubscriptionsController subscriptionsController; 
public RequestController requestCtrl;
//...

  //binding your custom method to the events containting only "blinks" messages and starting the plugin
  subscriptionsController.SubscribeTo("blinks", CustomReceiveData); 
  requestCtrl.StartPlugin(
    "Blink_Detection",
    //...
  );
  //...

//custom method to handle blink events 
void CustomReceiveData(string topic, Dictionary<string, object> dictionary, byte[] thirdFrame = null)
{
  //...
}
```

### Pupil Data

Demo: [Pupil Demo](#Pupil)

One of the most often asked for examples is getting values for pupil diameter.

Since the hmd-eyes v1.0 we provide a dedicated `PupilListener` dispatching events containing fully preparsed `PupilData` objects.
We highly recommend using the `PupilListener` instead of using low-level access and parsing the data dictionary by hand.

Checkout the public properties of the [PupilData.cs](../plugin/Scripts/PupilData.cs) to see what kind of data is available.

### Gaze Data

The `GazeListener` works identical to the `PupilListener` - dispatching events containing fully parsed `GazeData` objects. But in order to receive meaningful data it is needed to run a calibration routine beforehand. The whole setup is described in the next [section](#gaze-tracker).

## Gaze Tracking

Demo: [GazeDemo](#GazeDemo)

Gaze tracking and mapping gaze data in Unity/VR world space is done via the following steps and components:

 * [Calibration](#calibration) routine to establish mapping between tracked pupils and the 3d gaze points (`Calibration` + `CalibrationController`)
 * [Gaze data access](#accessing-gaze-data) as preparsed `GazeData` objects in local head/camera space (`GazeListener` + `GazeController`) 
 * [Mapping gaze data](#Mapping-Gaze-Data) to VR world space (`GazeVisualizer`)

> For using gaze tracking in your own scene hmd-eyes provides the prefab `Gaze Tracker` containing all needed components including the `Connection` prefab. After adding it to your own scene it is only necessary to assign the main camera to the *CalibrationController* and *GazeVisualizer**.

### Calibration 

In order to know what someone is looking at in the scene, we must establish a mapping between pupil positions and the gaze point. 

Before you calibrate you will need to ensure that eyes are well captured and that the pupil detection runs with high confidence (~0.8).
Please refer to the Pupil [getting started](https://docs.pupil-labs.com/#capture-workflow) and [user docs](https://docs.pupil-labs.com/#pupil-detection).

Use the `FrameVisualizer` component to check that you are capturing a good image in particular of the pupil of the eye. 
You may need to adjust the headset to ensure you can see the eye in all ranges of eye movements.

![Before Starting A Calibration](BeforeStartingCalibration.png)

Once the communication between Unity and Pupil Capture has been setup, you are ready to calibrate. 
We provided the `CalibrationController` component, based on the `Calibration` class. The `CalibrationController` guides through the process and acts as an interface, while the `Calibration` itself handles the communication with Pupil Capture.

As all *Listeners* and other high level components the `CalibrationController` needs access to the Pupil Connection object. Additionally assigning the camera object makes sure that the calibration happens in camera space and is independent of head movements. 

![Calibration Controller](CalibrationController.png)


#### Calibration Settings and Targets

The controller allows to swap different `CalibrationSettings` and `CalibrationTargets`. Both are `ScriptableObjects`, so you can create your own via the *Assets/Create/Pupil* menu or use the default ones inside the *Calibration Resources* folder.

![Calibration Resources](CalibrationResources.png)

The **Calibration Settings** are currently reduced to time and sample amount per calibration targets:

- `Seconds Per Target` defines the duration per marker position.
- `Ignore Initial Seconds` defines how much time the user is expected to need to move from one calibration marker position to the next. During this period, the gaze positions will not be recorded. 
- `Samples Per Target` defines the amount of calibration points recorded for each marker position. 

The real flexibility lies in the **Calibration Targets**. 

The plugin provides `CircleCalibrationTargets` as a default implementation, which allows to define targets as a list of circles with different **center** and **radii** plus the number of **points** for every circle. 

On top `CalibrationTargets` and the `CalibrationController` are setup in a way that you can write your own implementation of `CalibrationTargets` and plug them into the controller. Be aware that the controller expects targets in local/camera space and not world coordinates.

#### Calibration Success/Failure 

When the calibration process is complete, all reference points are sent to Pupil software. Pupil software will respond with a `success` or `failure` message. `CalibrationController` provides C# events for *OnStarted*, *OnSucceeded* and *OnFailed*.

### Accessing Gaze Data

The `GazeListener` class takes care of subscribing to all `gaze.3d*` messages and provides C# events containing already parsed `GazeData`. On top the `GazeController` wraps the listener as a MonoBehavior to simplify the setup in Unity.

Checkout the public properties of the [GazeData.cs](../plugin/Scripts/GazeData.cs) to see what kind of data is available. 

Keep in mind that vectors like the `GazeData.GazeDirection` and `GazeData.EyeCenter0/1` are in local VR camera space.

#### GazeDirection + GazeDistance instead of GazePoint3d

Instead of directly using the data field `GazeData.GazePoint3d` we recommend to use the equivalent representation as `GazeData.GazeDirection` and `GazeData.GazeDistance` as this representation clearly separates the angular error from the depth error. Note that while the depth error increases with growing distance of the gaze target the direction remains accurate (angular error of less than 2 degrees).

Due to access to 3d informations about the VR environment, the gaze estimation can be enhanced by using the `GazeData.GazeDirection` to project into the scene via [raycasting](https://docs.unity3d.com/ScriptReference/Physics.Raycast.html).

### Mapping Gaze Data

The `GazeVisualizer` component already showcases how to access the `GazeDirection`, filter based on confidence and map/project into the VR scene.

![Gaze Visualizer](GazeVisualizer.png)

The `GazeVisualizer` component only needs access to the `GazeController` and the main camera `Transform`.

The visualization is using the `GazeData.GazeDirection` to project into the scene via raycasting.

For your own applications you might not want to visualize gaze at all but access it directly. In this case you might want to utilize the `GazeController` component similar to the `GazeVisualizer`:

```csharp
//set via inspector
public GazeController gazeController;
public Transform gazeOrigin; //transform of main cam
//...

  //bind custom method to gaze events
  gazeController.OnReceive3dGaze += ReceiveGaze;
  //...

//custom method for receiving gaze
void ReceiveGaze(GazeData gazeData)
{
    if (gazeData.Confidence >= confidenceThreshold)
    {
        localGazeDirection = gazeData.GazeDirection;
        gazeDistance = gazeData.GazeDistance;
    }
}
//...

  //map direction to VR world space
  Vector3 directioninWorldSpace = gazeOrigin.TransformDirection(localGazeDirection);
```

## Utilities

### Time Conversion

*TBD*

### Recording Data

Demo: [DataRecordingDemo](#DataRecordingDemo)

The Unity VR plugin allows to trigger recordings in Pupil Capture (Pupil Service does not support this feature). 
The `RecordingController` component offers starting and stoping recordings via the inspector or by code using the methods `RecordingController.StartRecording` and `RecordingController.StopRecording` - in either case only once a connection has been established).

We removed the functionality for recording a screen capture in Unity as this recording was incompatible with Pupil Player. 
(We plan to support to stream a screen capture directly to Pupil Capture in the future.) 

<!-- On the plugin side of things, two additional processes are started
- a screen recording, saving the current view to a video file 

- saving Pupil gaze data and their corresponding Unity positions in CSV format using this structure
    - `Timestamp,Identifier,PupilPositionX,PupilPositionY,PupilPositionZ,UnityWorldPositionX,UnityWorldPositionY,UnityWorldPositionZ`

The resulting files are saved to the path set in `PupilSettings/Recorder/File Path`. You can change it manually, there, or through the `PupilGazeTracker` Inspector GUI under `Settings` by activating `CustomPath` and clicking the `Browse` button.  -->

### Unity Sceen Screen Casting

*TBD*

### Display Eye Images

Demo: [Frame Publishing Demo](#FramePublishingDemo)

To display images of the eyes, we provided a component called `FrameVisualizer`. It is based on the `FrameListener`, which takes care of subscribing to the `frame` topic and provides C# events on receiving eye frame data. The visualizer component displays the eye frames as `Texture2D` as children of the camera. 

![Frame Visualizer](FrameVisualizer.png)

## Demo Scenes 

### Pupil

This example shows you how to subscribe to `pupil.` topics and how to interpret the dictionary that you will receive from Pupil software in Unity.

### Blink 

This demo shows blink events and logs them to the console. This demo does not require calibration as it only uses pupil data (not gaze data).

It demonstrates how to  subscribe to a topic and read data from the socket, start/stop a Pupil plugin, to subscribe and un-subscribe from a topic (e.g. `blinks`), and how to receive a dictionary from Pupil in Unity. 

![Capusle Blink Hero](BlinkHero.png)

### FramePublishingDemo

This demo scene shows how to communicate with Pupil and access eye video frames

![Frame Publishing Demo](FramePublishing.png)

### GazeDemo

The Gaze Demo showcases the full stack needed for gaze estimations. You start in a simple scene with and after an established connection to Pupil Capture, you can see Eye Frames as a reference to adjust your HMD and use the scene to "warm up" your eyes.

Following the instructions you can start the calibration, which will hide everything and only shows the calibration targets. After a successful calibration the `GazeVisualizer` starts, projecting the gaze estimate into the scene.

![Gaze Demo](gazeDemo.png)

### GazeRaysDemo

In contrast to the GazeDemo the GazeRaysDemo is visualizing the gaze vectors of both eyes individually - using `GazeData.GazeNormal0/1` and `GazeData.EyePosition0/1`. 

The demo also takes the `GazeData.GazeMappingContext` into account - to check for which eye the data is available.

### DataRecordingDemo

A simple demo using the `RecordingController` component. The demo script allows starting and stoping the recording by pressing "R".

### SceneManagementDemo

This demo is very similar to GazeDemo but contains of two scenes: the calibration and the application scene. By applying the `DontDestroy` script to our `Pupil Connection` object of the calibration scene, we make sure it is available after switching the scene. 

The scene switch is handled by listening to `CalibrationController.OnCalibrationSucceeded` and the `SetupOnSceneLoad` script in the application scene injects the `Pupil Connection` into the `GazeVisualizer`.

### HoloLens - 2D/3D Calibration Demo 

*TBD*

<!-- As it is not a common use-case for HoloLens to visualize complete scenes, we reduced the market scene to a single object - the `sharkman` - for the user to look at.  -->

### Spherical Video demo

*TBD*

<!-- Load and display a 360 degree video based on Unity's 2017.3 implementation. Combined with Pupil, this allows to visualize what the user is looking at.  -->

### Heatmap demo

*TBD*

<!-- This demo shows how to generate and export spherical videos or still images with heat maps generated from gaze postions.

Gaze positions are visualized as particles on a spherical texture that is overlayed on the 3d scene. 

After calibration, press `h` to start recording the output video or to capture the current view to an image. The output path is the same as the path defined by the settings for PupilGazeTracker recordings. 

![Heatmap Component](https://github.com/pupil-labs/hmd-eyes/blob/master/GettingStarted/HeatmapComponent.png)

- Mode 
    - `Particle` will color the area the user is looking at 
    - `ParticleDebug` will make the hetamap visible for the user wearing the HMD as well as the operator. 
    - `Highlight` will only fill-in the area looked at 
        ![Highlight Mode Heatmap](https://github.com/pupil-labs/hmd-eyes/blob/master/GettingStarted/HighlightModeHeatmap.jpg)
    - `Image` will keep all particles and color code them based on the time of creation 
        ![Image Mode Heatmap](https://github.com/pupil-labs/hmd-eyes/blob/master/GettingStarted/ImageModeHeatmap.jpg)
    - `Particle Size` - The size of a single particle used to visualize each gaze position 
    - `Remove Particle After X Seconds` - Set how many seconds a particle should be visualized (not used for Image mode) 
    - `Particle Color` - The color of the particle in the visualization 
    - `Particle Color Final` - Color for oldest particle in Image mode. Every color in between will be interpolated 
    
The heatmap is available as Prefab, and can be added to existing scenes by dragging it onto the main camera of that scene. -->


## FAQ
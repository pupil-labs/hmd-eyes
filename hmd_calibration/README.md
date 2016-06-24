# Calibrate using Pupil Capture or Service

This guide shows how to calibrate using the HMD_Calibration plugin in Pupil Capture.

Starting with v0.8 Pupil Capture (and the new Pupil Service) will have a fully accesible message bus.

The pupil wiki page [Pupil-Interprocess-and-Network-Communication](https://github.com/pupil-labs/pupil/wiki/Pupil-Interprocess-and-Network-Communication) documents the protocol and gives an example on how listen and talk to the bus.

A new calibration plugin: `HMD_Calibration` and gaze mapper `Dual_Monocular_Gaze_Mapper` have been added.

### Basic Working principle of the `HMD_Calibration`

The calibration plugin samples pupil positions and receives reference postions (positions of a stimulus on the left and right hmd screen) from the client app. The reference positions are detemined by the client and sent in normalized screen coordinates with timestamp and id.

```python
ref_data = [{'norm_pos':pos,'timestamp':t,'id':0},...]
```

The right side is denoted as id 0 , the left side as id 1. The timestamp giving clock needs to be in sync with the Pupil Capture / Service.

When the user stops calibration pupil-ref matches are created through temporal correlation. The matched data is used to parameterize the mapping polynomial.

Once the mapping coefficients are succesfully determined a new gaze mapper becomes active and gaze data is now mapped according to the calibration.

The left and right side are treated seperately during parameter estimation and gaze mapping. (Thus the name `Dual Monocular Gaze Mapper`.)


### Basic workflow of this demo

 1. run `notification_monitor.py` to see what happens on the IPC Backbone
 2. start Pupil Capture app (download v0.8 pre-release for [linux](https://drive.google.com/open?id=0Byap58sXjMVfWG5NQTFWNmhlcE0) and [mac](https://drive.google.com/open?id=0Byap58sXjMVfWGZmT0tXdDFxMG8))
 3. run `hmd_calibration_client.py`


### Going from here...

The files in this dir and [Pupil-Interprocess-and-Network-Communication](https://github.com/pupil-labs/pupil/wiki/Pupil-Interprocess-and-Network-Communication) should give enough insight to build a client in any other language. The only dependecies are zmq and a msgpack serializer. For any qustions please connect on [gitter](https://gitter.im/pupil-labs/hmd-eyes). For corrections and improvement raise an issue or Pull Request.




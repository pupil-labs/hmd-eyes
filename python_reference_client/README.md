# Calibrate using Pupil Capture or Service

This guide shows how to calibrate using the HMD_Calibration plugin in Pupil Capture. This example is only a implementation reference, since it does not render any actual calibration targets it does not work before you implement this part in your code!


### Basic Working principle of the `HMD_Calibration`

The calibration plugin samples pupil positions and receives reference positions (positions of a stimulus on the left and right hmd screen) from the client app. The reference positions are determined by the client and sent in normalised screen coordinates with timestamp and id.

```python
ref_data = [{'norm_pos':pos,'timestamp':t,'id':0},...]
```

The right side is denoted as id 0 , the left side as id 1. The timestamp giving clock needs to be in sync with the Pupil Capture / Service.

When the user stops calibration pupil-ref matches are created through temporal correlation. The matched data is used to parameterize the 2d mapping polynomial.

Once the mapping coefficients are successfully determined a new gaze mapper becomes active and gaze data is now mapped according to the calibration.

The left and right side are treated seperately during parameter estimation and gaze mapping. (Thus the name `Dual Monocular Gaze Mapper`.)


### Basic workflow of this demo

 1. run `notification_monitor.py` to see what happens on the IPC Backbone
 2. start Pupil Capture app (download v0.8 pre-release for [linux](https://drive.google.com/open?id=0Byap58sXjMVfWG5NQTFWNmhlcE0) and [mac](https://drive.google.com/open?id=0Byap58sXjMVfWGZmT0tXdDFxMG8))
 3. run `hmd_calibration_client.py`


### Going from here...

The files in this dir and the other clients in the repo should give enough insight to build a client in any other language. The only dependencies are zmq and a msgpack serializer. For any questions please connect on our [Discord channel](https://discord.gg/PahDtSH). For corrections and improvement raise an issue or Pull Request.




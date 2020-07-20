# Calibrate using Pupil Capture or Service

This guide shows how to calibrate using the HMD3DChoreographyPlugin plugin in Pupil Capture. This example is only a implementation reference, since it does not render any actual calibration targets it does not work before you implement this part in your code!


### Basic Working principle of the `HMD_Calibration`

The calibration plugin samples pupil positions and receives reference positions (positions of a stimulus in the 3D environment) from the client app. The reference positions are determined by the client and sent in 3D coordinates (mm) with timestamp.

```python
ref_data = [{'mm_pos': pos,'timestamp': t},...]
```

The clock producing the timestamps needs to be in sync with Pupil Capture / Service.

When the user stops calibration, pupil-ref matches are created through temporal correlation. The matched data is used to parameterize the 3D mapping model.

Once the mapping coefficients are successfully determined, a new gaze mapper becomes active and gaze data is now mapped according to the calibration.

### Basic workflow of this demo

 1. run `notification_monitor.py` to see what happens on the IPC Backbone
 2. start Pupil Capture app (download from our [GitHub Release Page](https://github.com/pupil-labs/pupil/releases/latest#user-content-downloads))
 3. run `hmd_calibration_client.py`


### Going from here...

The files in this directory should give enough insight to build a client in any other language. The only dependencies are zmq and a msgpack serializer. For any questions please connect on our [Discord channel](https://discord.gg/PahDtSH). For corrections and improvement raise an issue or Pull Request.




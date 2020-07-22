"""
HMD calibration client example.
This script shows how to talk to Pupil Capture or Pupil Service
and run a gaze mapper calibration.
"""
import zmq, msgpack, time

ctx = zmq.Context()


# create a zmq REQ socket to talk to Pupil Service/Capture
req = ctx.socket(zmq.REQ)
req.connect("tcp://127.0.0.1:50020")


# convenience functions
def send_recv_notification(n):
    # REQ REP requirese lock step communication with multipart msg (topic,msgpack_encoded dict)
    req.send_string("notify.%s" % n["subject"], flags=zmq.SNDMORE)
    req.send(msgpack.dumps(n, use_bin_type=True))
    return req.recv_string()


def get_pupil_timestamp():
    req.send_string("t")  # see Pupil Remote Plugin for details
    return float(req.recv_string())


# set start eye windows
n = {"subject": "eye_process.should_start.0", "eye_id": 0, "args": {}}
print(send_recv_notification(n))
n = {"subject": "eye_process.should_start.1", "eye_id": 1, "args": {}}
print(send_recv_notification(n))
time.sleep(2)


# set calibration method to hmd calibration
n = {"subject": "start_plugin", "name": "HMD3DChoreographyPlugin", "args": {}}
print(send_recv_notification(n))

# start caliration routine with params. This will make pupil start sampeling pupil data.
# the eye-translations have to be in mm, these here are default values from Unity XR
n = {
    "subject": "calibration.should_start",
    "translation_eye0": [34.75, 0.0, 0.0],
    "translation_eye1": [-34.75, 0.0, 0.0],
    "record": True,
}
print(send_recv_notification(n))


# Mockup logic for sample movement:
# We sample some reference positions in scene coordinates (mm) relative to the HMD.
# Positions can be freely defined

ref_data = []
for pos in (
    (0.0, 0.0, 600.0),
    (0.0, 0.0, 1000.0),
    (0.0, 0.0, 2000.0),
    (180.0, 0.0, 600.0),
    (240.0, 0.0, 1000.0),
    (420.0, 0.0, 2000.0),
    (55.62306, 195.383, 600.0),
    (74.16407, 260.5106, 1000.0),
    (129.7871, 455.8936, 2000.0),
    (-145.6231, 120.7533, 600.0),
    (-194.1641, 161.0044, 1000.0),
    (-339.7872, 281.7577, 2000.0),
    (-145.6231, -120.7533, 600.0),
    (-194.1641, -161.0044, 1000.0),
    (-339.7872, -281.7577, 2000.0),
    (55.62306, -195.383, 600.0),
    (74.16407, -260.5106, 1000.0),
    (129.7871, -455.8936, 2000.0),
):
    print("subject now looks at position:", pos)
    for s in range(60):
        # you direct screen animation instructions here

        # get the current pupil time (pupil uses CLOCK_MONOTONIC with adjustable timebase).
        # You can set the pupil timebase to another clock and use that.
        t = get_pupil_timestamp()

        # in this mockup  the left and right screen marker positions are identical.
        datum0 = {"mm_pos": pos, "timestamp": t}
        datum1 = {"mm_pos": pos, "timestamp": t}
        ref_data.append(datum0)
        ref_data.append(datum1)
        time.sleep(1 / 60.0)  # simulate animation speed.


# Send ref data to Pupil Capture/Service:
# This notification can be sent once at the end or multiple times.
# During one calibraiton all new data will be appended.
n = {
    "subject": "calibration.add_ref_data",
    "ref_data": ref_data,
    "record": True,
}
print(send_recv_notification(n))

# stop calibration
# Pupil will correlate pupil and ref data based on timestamps,
# compute the gaze mapping params, and start a new gaze mapper.
n = {
    "subject": "calibration.should_stop",
    "record": True,
}
print(send_recv_notification(n))

time.sleep(2)


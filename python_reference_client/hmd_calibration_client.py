'''
HMD calibration client example.
This script shows how to talk to Pupil Capture or Pupil Service
and run a gaze mapper calibration.
'''
import zmq, msgpack, time
ctx = zmq.Context()


#create a zmq REQ socket to talk to Pupil Service/Capture
req = ctx.socket(zmq.REQ)
req.connect('tcp://localhost:50020')


#convenience functions
def send_recv_notification(n):
    # REQ REP requirese lock step communication with multipart msg (topic,msgpack_encoded dict)
    req.send_multipart(('notify.%s'%n['subject'], msgpack.dumps(n)))
    return req.recv()

def get_pupil_timestamp():
    req.send('t') #see Pupil Remote Plugin for details
    return float(req.recv())

# set start eye windows
n = {'subject':'eye_process.should_start.0','eye_id':0, 'args':{}}
print send_recv_notification(n)
n = {'subject':'eye_process.should_start.1','eye_id':1, 'args':{}}
print send_recv_notification(n)
time.sleep(2)


# set calibration method to hmd calibration
n = {'subject':'start_plugin','name':'HMD_Calibration', 'args':{}}
print send_recv_notification(n)

# start caliration routine with params. This will make pupil start sampeling pupil data.
n = {'subject':'calibration.should_start', 'hmd_video_frame_size':(1000,1000), 'outlier_threshold':35}
print send_recv_notification(n)


# Mockup logic for sample movement:
# We sample some reference positions (in normalized screen coords).
# Positions can be freely defined

ref_data = []
for pos in ((0.5,0.5),(0,0),(0,0.5),(0,1),(0.5,1),(1,1),(1,0.5),(1,0),(.5,0)):
    print 'subject now looks at position:',pos
    for s in range(60):
        # you direct screen animation instructions here

        # get the current pupil time (pupil uses CLOCK_MONOTONIC with adjustable timebase).
        # You can set the pupil timebase to another clock and use that.
        t = get_pupil_timestamp()

        # in this mockup  the left and right screen marker positions are identical.
        datum0 = {'norm_pos':pos,'timestamp':t,'id':0}
        datum1 = {'norm_pos':pos,'timestamp':t,'id':1}
        ref_data.append(datum0)
        ref_data.append(datum1)
        time.sleep(1/60.) #simulate animation speed.


# Send ref data to Pupil Capture/Service:
# This notification can be sent once at the end or multiple times.
# During one calibraiton all new data will be appended.
n = {'subject':'calibration.add_ref_data','ref_data':ref_data}
print send_recv_notification(n)

# stop calibration
# Pupil will correlate pupil and ref data based on timestamps,
# compute the gaze mapping params, and start a new gaze mapper.
n = {'subject':'calibration.should_stop'}
print send_recv_notification(n)

time.sleep(2)





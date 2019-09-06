"""
Receive data from Pupil using ZMQ.
"""
import logging

import zmq
from msgpack import loads

handlers = (logging.StreamHandler(), logging.FileHandler("monitor.log", mode="w"))
logging.basicConfig(handlers=handlers, level=logging.DEBUG)
logger = logging.getLogger()

context = zmq.Context()
# open a req port to talk to pupil
addr = "127.0.0.1"  # remote ip or localhost
req_port = "50020"  # same as in the pupil remote gui
req = context.socket(zmq.REQ)
req.connect("tcp://{}:{}".format(addr, req_port))
# ask for the sub port
req.send_string("SUB_PORT")
sub_port = req.recv_string()

# open a sub port to listen to pupil
sub = context.socket(zmq.SUB)
sub.connect("tcp://{}:{}".format(addr, sub_port))

# set subscriptions to topics
# recv just pupil/gaze/notifications
# sub.setsockopt_string(zmq.SUBSCRIBE, "pupil.")
# sub.setsockopt_string(zmq.SUBSCRIBE, 'gaze')
sub.setsockopt_string(zmq.SUBSCRIBE, "notify.")
sub.setsockopt_string(zmq.SUBSCRIBE, "logging.")

# or everything:
# sub.setsockopt_string(zmq.SUBSCRIBE, '')


while True:
    try:
        topic = sub.recv_string()
        msg = sub.recv()
        msg = loads(msg, encoding="utf-8")
        logger.debug("{}: {}".format(topic, msg))
    except KeyboardInterrupt:
        break

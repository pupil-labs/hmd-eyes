import zmq
from zmq_tools import Msg_Receiver

ctx = zmq.Context()
requester = ctx.socket(zmq.REQ)
requester.connect('tcp://localhost:50020')

requester.send_string('SUB_PORT')
ipc_sub_port = requester.recv_string()
monitor = Msg_Receiver(ctx, 'tcp://localhost:{}'.format(ipc_sub_port), topics=('gaze',))

print('connected')

while True:
    topic, g = monitor.recv()
    print(g['norm_pos'], g['confidence'])

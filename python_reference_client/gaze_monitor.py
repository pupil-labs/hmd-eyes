from zmq_tools import *


ctx = zmq.Context()
requester = ctx.socket(zmq.REQ)
requester.connect('tcp://127.0.0.1:50020')
requester.send_string('SUB_PORT')
ipc_sub_port = requester.recv_string()
monitor = Msg_Receiver(ctx,'tcp://127.0.0.1:%s'%ipc_sub_port,topics=('gaze',))
print('connected')

while True:
    try:
        topic,g = monitor.recv()
        print(g)
    except KeyboardInterrupt:
        break

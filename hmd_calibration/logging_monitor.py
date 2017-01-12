import zmq
from zmq_tools import Msg_Receiver

ctx = zmq.Context()
requester = ctx.socket(zmq.REQ)
requester.connect('tcp://localhost:50020')

requester.send_string('SUB_PORT')
ipc_sub_port = requester.recv_string()
monitor = Msg_Receiver(ctx, 'tcp://localhost:{}'.format(ipc_sub_port), topics=('logging.',))

while True:
    topic, log = monitor.recv()
    print('{0[name]}: {0[levelname]} - {0[msg]}'.format(log))

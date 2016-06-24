'''
This file contains convenience classes for communication with
the Pupil IPC Backbone.
'''

import zmq
from zmq.utils.monitor import recv_monitor_message
# import ujson as serializer # uncomment for json seialization
import msgpack as serializer
import logging


class ZMQ_handler(logging.Handler):
    '''
    A handler that send log records as serialized strings via zmq
    '''
    def __init__(self,ctx,ipc_pub_url):
        super(ZMQ_handler, self).__init__()
        self.socket = Msg_Dispatcher(ctx,ipc_pub_url)

    def emit(self, record):
        self.socket.send('logging.%s'%str(record.levelname).lower(),record.__dict__)

class Msg_Receiver(object):
    '''
    Recv messages on a sub port.
    Not threadsave. Make a new one for each thread
    __init__ will block until connection is established.
    '''
    def __init__(self,ctx,url,topics = (),block_until_connected=True):
        self.socket = zmq.Socket(ctx,zmq.SUB)
        assert type(topics) != str

        if block_until_connected:
            #connect node and block until a connecetion has been made
            monitor = self.socket.get_monitor_socket()
            self.socket.connect(url)
            while True:
                status =  recv_monitor_message(monitor)
                if status['event'] == zmq.EVENT_CONNECTED:
                    break
                elif status['event'] == zmq.EVENT_CONNECT_DELAYED:
                    pass
                else:
                    raise Exception("ZMQ connection failed")
            self.socket.disable_monitor()
        else:
            self.socket.connect(url)

        for t in topics:
            self.subscribe(t)

    def subscribe(self,topic):
        self.socket.set(zmq.SUBSCRIBE, topic)

    def unsubscribe(self,topic):
        self.socket.set(zmq.UNSUBSCRIBE, topic)

    def recv(self,*args,**kwargs):
        '''
        recv a generic message with topic, payload
        '''
        topic = self.socket.recv(*args,**kwargs)
        payload = serializer.loads(self.socket.recv(*args,**kwargs))
        return topic,payload

    @property
    def new_data(self):
        return self.socket.get(zmq.EVENTS)

    def __del__(self):
        self.socket.close()

class Msg_Streamer(object):
    '''
    Send messages on a pub port.
    Not threadsave. Make a new one for each thread
    '''
    def __init__(self,ctx,url):
        self.socket = zmq.Socket(ctx,zmq.PUB)
        self.socket.connect(url)

    def send(self,topic,payload):
        '''
        send a generic message with topic, payload
        '''
        self.socket.send(str(topic),flags=zmq.SNDMORE)
        self.socket.send(serializer.dumps(payload))

    def __del__(self):
        self.socket.close()




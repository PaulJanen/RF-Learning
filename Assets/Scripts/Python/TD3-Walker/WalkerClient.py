#import Client
import zmq

class WalkerClient():
    
    def __init__(self, port):
        self.address = "tcp://localhost:" + str(port)
        self.port = port

    def SendAction(self, action):
        self.ctx = zmq.Context()
        self.soc = self.ctx.socket(zmq.REQ)
        self.soc.setsockopt(zmq.LINGER, 0)
        self.soc.connect(self.address)
        
        data = {
        'command': "Step",
        'actions': action.tolist()
        }
        self.soc.send_json(data)
        s = self.soc.recv_json()
        self.soc.close()
        if(s["command"] == "DoneTraining"):
            print("Crash iminent")
            return
        else:
            return s["state"], s["reward"], s["done"]
    
    def SendReset(self): 
        self.ctx = zmq.Context()
        self.soc = self.ctx.socket(zmq.REQ)
        self.soc.setsockopt(zmq.LINGER, 0)
        self.soc.connect(self.address)

        data = {
        'command': "Reset",
        }
        self.soc.send_json(data)
        s = self.soc.recv_json()
        self.soc.close()
        return s["state"]
        

    def SendClose(self):
        self.ctx = zmq.Context()
        self.soc = self.ctx.socket(zmq.REQ)
        self.soc.setsockopt(zmq.LINGER, 0)
        self.soc.connect(self.address)
        data = {
        'command': "DoneTraining",
        }
        self.soc.send_json(data)
        s = self.soc.recv_json()
        self.soc.close()
        




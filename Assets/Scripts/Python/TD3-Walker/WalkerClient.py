#import Client
import zmq

class WalkerClient():



    def SendAction(self, action):
        self.ctx = zmq.Context()
        self.soc = self.ctx.socket(zmq.REQ)
        self.soc.setsockopt(zmq.LINGER, 1)
        self.soc.connect("tcp://localhost:5555")

        data = {
        'command': "Step",
        'actions': action.tolist()
        }

        self.soc.send_json(data)
        s = self.soc.recv_json()

        return s["state"], s["reward"], s["done"]
    
    def SendReset(self): 
        self.ctx = zmq.Context()
        self.soc = self.ctx.socket(zmq.REQ)
        self.soc.setsockopt(zmq.LINGER, 1)
        self.soc.connect("tcp://localhost:5555")

        data = {
        'command': "Reset",
        }
        self.soc.send_json(data)
        s = self.soc.recv_json()
        return s["state"]
        

    def SendClose():
        pass




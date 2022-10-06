from random import sample
from cv2 import repeat
from AgentClient import AgentClient
import numpy as np

class Environment():
    def __init__(self, port):
        self.client = AgentClient(port)

    def step(self, action):
        observation, reward, done = self.client.SendAction(action)
        observation = np.asarray(observation)
        return observation, reward, done

    def reset(self):
        observation = self.client.SendReset()
        observation = np.asarray(observation)
        return observation  

    def close (self):
        self.client.SendClose()
    
    def actionSample(self):
        return np.random.uniform(self.minAction, self.maxAction)
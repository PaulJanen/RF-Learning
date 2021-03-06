from random import sample

from cv2 import repeat
from WalkerClient import WalkerClient
import numpy as np

class WalkerEnv():
    def __init__(self, port):
        self.client = WalkerClient(port)
        self.action_space = np.empty((20,))
        self.observation_space = np.empty((127,))
        self.maxAction = np.array([1,1,1,1,1,1,1,1,1,1,1,1,1,1,1,1,1,1,1,1])
        self.minAction = np.array([-1,-1,-1,-1,-1,-1,-1,-1,-1,-1,-1,-1,-1,-1,-1,-1,-1,-1,-1,-1])
        self.max_episode_steps = 5000

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
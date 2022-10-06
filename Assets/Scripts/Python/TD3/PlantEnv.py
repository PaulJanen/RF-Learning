from random import sample
from cv2 import repeat
from AgentClient import AgentClient
import numpy as np
from Environment import Environment

class PlantEnv(Environment):
    def __init__(self, port, loadModel):
        Environment.__init__(self, port)
        self.action_space = np.empty((15,))
        self.observation_space = np.empty((86,))
        self.maxAction = np.array([1,1,1,1,1,1,1,1,1,1,1,1,1,1,1])
        self.minAction = np.array([-1,-1,-1,-1,-1,-1,-1,-1,-1,-1,-1,-1,-1,-1,-1])
        self.max_episode_steps = 500
        if loadModel == True:
            self.max_episode_steps = 50000
        self.trainAfterSteps = 10_000
        self.explorationSteps = 20_000
        self.env_name = "Plant"
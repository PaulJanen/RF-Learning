from Agent import Agent
from PlantEnv import PlantEnv

class PlantAgent(Agent):
  def __init__(self, port, manager):
    self.env = PlantEnv(port, manager.loadModel)
    Agent.__init__(self, port, manager, self.env)

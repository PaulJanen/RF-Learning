from Agent import Agent
from CalEnv import CalEnv

class CalAgent(Agent):
  def __init__(self, port, manager):
    self.env = CalEnv(port, manager.loadModel)
    Agent.__init__(self, port, manager, self.env)


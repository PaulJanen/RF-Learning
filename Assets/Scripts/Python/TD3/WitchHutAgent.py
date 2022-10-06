from Agent import Agent
from WitchHutEnv import WitchHutEnv

class WitchHutAgent(Agent):
  def __init__(self, port, manager):
    self.env = WitchHutEnv(port, manager.loadModel)
    Agent.__init__(self, port, manager, self.env)


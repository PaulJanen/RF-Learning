from Agent import Agent
from HowlCastlev1Env import HowlCastlev1Env

class HowlCastlev1Agent(Agent):
  def __init__(self, port, manager):
    self.env = HowlCastlev1Env(port, manager.loadModel, manager.save_models)
    Agent.__init__(self, port, manager, self.env)


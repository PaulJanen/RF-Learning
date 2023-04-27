from Agent import Agent
from ArmThingEnv import ArmThingEnv

class ArmThingAgent(Agent):
  def __init__(self, port, manager):
    self.env = ArmThingEnv(port, manager.loadModel, manager.save_models)
    Agent.__init__(self, port, manager, self.env)


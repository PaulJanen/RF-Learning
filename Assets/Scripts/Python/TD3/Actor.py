import torch.nn as nn
import torch.nn.functional as F
import torch

class Actor(nn.Module):
  
  def __init__(self, state_dim, action_dim, max_action):
    super(Actor, self).__init__()
    self.layer_1 = nn.Linear(state_dim, 512)
    self.layer_2 = nn.Linear(512, 300)
    self.layer_3 = nn.Linear(300, action_dim)
    self.max_action = max_action

  def forward(self, x):
    x = F.relu(self.layer_1(x))
    x = F.relu(self.layer_2(x))
    x = torch.tensor(self.max_action) * torch.tanh(self.layer_3(x))
    return x
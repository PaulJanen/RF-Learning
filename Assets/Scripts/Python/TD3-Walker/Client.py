import torch
import Actor as Actor
import Critic as Critic
import ReplayBuffer as ReplayBuffer

device = torch.device("cuda" if torch.cuda.is_available() else "cpu")


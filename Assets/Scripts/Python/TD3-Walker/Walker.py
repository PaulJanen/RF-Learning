from threading import Thread
import torch
from Actor import Actor
from Critic import Critic
from ReplayBuffer import ReplayBuffer
import os
from WalkerEnv import WalkerEnv as walkerEnv
import numpy as np
from TD3 import TD3
import time

def mkdir(base, name):
    path = os.path.join(base, name)
    if not os.path.exists(path):
        os.makedirs(path)
    return path

class Walker(Thread):

  def __init__(self, port, manager):
    Thread.__init__(self)
    self.env_name = "Walker"
    self.seed = 0 # Random seed number
    self.max_timesteps = 1e8 # Total number of iterations/timesteps
    self.start_timesteps = (int)(self.max_timesteps*0.01) # Number of iterations/timesteps before which the model randomly chooses an action, and after which it starts to use the policy network
    #self.eval_freq = 5e3 # How often the evaluation step is performed (after how many timesteps)
    self.save_models = True # Boolean checker whether or not to save the pre-trained model
    self.expl_noise = 0.1 # Exploration noise - STD value of exploration Gaussian noise
    self.batch_size = 100 # Size of the batch
    self.discount = 0.99 # Discount factor gamma, used in the calculation of the total discounted reward
    self.tau = 0.005 # Target network update rate
    self.policy_noise = 0.2 # STD of Gaussian noise added to the actions for the exploration purposes
    self.noise_clip = 0.5 # Maximum value of the Gaussian noise added to the actions (policy)
    self.policy_freq = 2 # Number of iterations to wait before the policy network (Actor model) is updated
    self.manager = manager
    self.env = walkerEnv(port)

    self.state_dim = self.env.observation_space.shape[0]
    self.action_dim = self.env.action_space.shape[0]
    self.max_episode_steps = self.env.max_episode_steps


    self.episode_num = 0
    self.done = True
    self.movingAvgReward = 0

  #evaluations = [policy.evaluate_policy(env)]
  

  def run(self):
  ### TRAINING
    obs = 0
    episode_timesteps = 0
    episode_reward = 0
    while self.manager.total_timesteps < self.manager.max_timesteps:
      # If the episode is done
      if self.done:
        # When the training step is done, we reset the state of the environment
        obs = self.env.reset()
        # Set the Done to False
        self.done = False

        self.movingAvgReward = 0.9 * self.movingAvgReward + 0.1*episode_reward
        # Set rewards and episode timesteps to zero
        episode_reward = 0
        episode_timesteps = 0
        self.episode_num += 1
      
      # Before 10000 timesteps, we play random actions
      if self.manager.total_timesteps < self.start_timesteps:
        action = self.env.actionSample()
      else: # After 10000 timesteps, we switch to the model
        while(self.manager.policy.isTraining):
          time.sleep(1)
        action = self.manager.policy.select_action(np.array(obs))
        # If the explore_noise parameter is not 0, we add noise to the action and we clip it
        if self.expl_noise != 0:
          action = (action + np.random.normal(0, self.expl_noise, size=self.env.action_space.shape[0])).clip(self.env.minAction, self.env.maxAction)

      # The agent performs the action in the environment, then reaches the next state and receives the reward
      new_obs, reward, self.done = self.env.step(action)

      if(episode_timesteps + 1 == self.env.max_episode_steps):
        self.done = True
      # We check if the episode is done
      done_bool = 1 if episode_timesteps + 1 == self.env.max_episode_steps else float(self.done)

      # We increase the total reward
      episode_reward += reward
      
      # We store the new transition into the Experience Replay memory (ReplayBuffer)
      self.manager.replayBuffer.add((obs, new_obs, action, reward, done_bool))
      # We update the state, the episode timestep, the total timesteps, and the timesteps since the evaluation of the policy
      obs = new_obs
      episode_timesteps += 1
      self.manager.AddTimesteps()



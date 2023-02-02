import torch
from Actor import Actor
from Critic import Critic
import torch.nn.functional as F
import numpy as np
import tf2onnx
import onnx

class TD3(object):
  
  def __init__(self, state_dim, action_dim, max_action, min_action):
    self.device = torch.device("cuda" if torch.cuda.is_available() else "cpu")
    self.actor = Actor(state_dim, action_dim, max_action).to(self.device)
    self.actor_target = Actor(state_dim, action_dim, max_action).to(self.device)
    self.actor_target.load_state_dict(self.actor.state_dict())
    self.actor_optimizer = torch.optim.Adam(self.actor.parameters())
    self.critic = Critic(state_dim, action_dim).to(self.device)
    self.critic_target = Critic(state_dim, action_dim).to(self.device)
    self.critic_target.load_state_dict(self.critic.state_dict())
    self.critic_optimizer = torch.optim.Adam(self.critic.parameters())
    self.max_action = max_action
    self.min_action = min_action
    self.isTraining = False
    self.agentsSelectingActionCount = 0
    self.stateDim = state_dim

  def select_action(self, state):
    self.agentsSelectingActionCount += 1
    state = torch.Tensor(state.reshape(1, -1)).to(self.device)
    self.agentsSelectingActionCount -= 1
    return self.actor(state).cpu().data.numpy().flatten()

  def train(self, replay_buffer, iterations, batch_size=100, discount=0.99, tau=0.005, policy_noise=0.2, noise_clip=0.5, policy_freq=2):
    
    self.isTraining = True

    for it in range(iterations):
      
      # Step 4: We sample a batch of transitions (s, s’, a, r) from the memory
      batch_states, batch_next_states, batch_actions, batch_rewards, batch_dones = replay_buffer.sample(batch_size)
      state = torch.Tensor(batch_states).to(self.device)
      next_state = torch.Tensor(batch_next_states).to(self.device)
      action = torch.Tensor(batch_actions).to(self.device)
      reward = torch.Tensor(batch_rewards).to(self.device)
      done = torch.Tensor(batch_dones).to(self.device)
      
      # Step 5: From the next state s’, the Actor target plays the next action a’
      next_action = self.actor_target(next_state)
      
      # Step 6: We add Gaussian noise to this next action a’ and we clamp it in a range of values supported by the environment
      # Creates batch of actions tensor with batch_actions size where each value is sampled from N(0, deviation)
      noise = torch.Tensor(batch_actions).data.normal_(0, policy_noise).to(self.device)
      noise = noise.clamp(-noise_clip, noise_clip)
      next_action = (next_action + noise).clamp(torch.tensor(-self.max_action), torch.tensor(self.max_action))
      
      # Step 7: The two Critic targets take each the couple (s’, a’) as input and return two Q-values Qt1(s’,a’) and Qt2(s’,a’) as outputs
      target_Q1, target_Q2 = self.critic_target(next_state, next_action)
      
      # Step 8: We keep the minimum of these two Q-values: min(Qt1, Qt2)
      target_Q = torch.min(target_Q1, target_Q2)
      
      # Step 9: We get the final target of the two Critic models, which is: Qt = r + γ * min(Qt1, Qt2), where γ is the discount factor
      target_Q = reward + ((1 - done) * discount * target_Q).detach()
      
      # Step 10: The two Critic models take each the couple (s, a) as input and return two Q-values Q1(s,a) and Q2(s,a) as outputs
      current_Q1, current_Q2 = self.critic(state, action)
      
      # Step 11: We compute the loss coming from the two Critic models: Critic Loss = MSE_Loss(Q1(s,a), Qt) + MSE_Loss(Q2(s,a), Qt)
      critic_loss = F.mse_loss(current_Q1, target_Q) + F.mse_loss(current_Q2, target_Q)
      
      # Step 12: We backpropagate this Critic loss and update the parameters of the two Critic models with a SGD optimizer
      self.critic_optimizer.zero_grad()
      critic_loss.backward()
      self.critic_optimizer.step()
      
      # Step 13: Once every two iterations, we update our Actor model by performing gradient ascent on the output of the first Critic model
      if it % policy_freq == 0:
        actor_loss = -self.critic.Q1(state, self.actor(state)).mean()
        self.actor_optimizer.zero_grad()
        actor_loss.backward()
        self.actor_optimizer.step()
        
        # Step 14: Still once every two iterations, we update the weights of the Actor target by polyak averaging
        for param, target_param in zip(self.actor.parameters(), self.actor_target.parameters()):
          target_param.data.copy_(tau * param.data + (1 - tau) * target_param.data)
        
        # Step 15: Still once every two iterations, we update the weights of the Critic target by polyak averaging
        for param, target_param in zip(self.critic.parameters(), self.critic_target.parameters()):
          target_param.data.copy_(tau * param.data + (1 - tau) * target_param.data)
    
    self.isTraining = False
  
  # Making a save method to save a trained model
  def save(self, filename, directory):
    torch.save(self.actor.state_dict(), '%s/%s_actor.pth' % (directory, filename))
    torch.save(self.critic.state_dict(), '%s/%s_critic.pth' % (directory, filename))
  
  # Making a load method to load a pre-trained model
  def load(self, filename, directory):
    self.actor.load_state_dict(torch.load('%s/%s_actor.pth' % (directory, filename)))
    self.critic.load_state_dict(torch.load('%s/%s_critic.pth' % (directory, filename)))

  def evaluate_policy(self, env, eval_episodes=10):
    avg_reward = 0.
    for _ in range(eval_episodes):
      obs = env.reset()
      done = False
      episode_timesteps = 0
      while not done:
        action = self.select_action(np.array(obs))
        obs, reward, done = env.step(action)
        avg_reward += reward
        episode_timesteps += 1
        if(episode_timesteps == env.max_episode_steps):
          done = True
    avg_reward /= eval_episodes
    print ("---------------------------------------")
    print ("Average Reward over the Evaluation Step: %f" % (avg_reward))
    print ("---------------------------------------")
    return avg_reward

  def SaveModelToONNX(self):
    self.actor.train(False)
    dummy_input = torch.randn(1, self.stateDim, requires_grad=True)  
    torch.onnx.export(self.actor, dummy_input, "actor.onnx", verbose=False)
    self.actor.train(True)
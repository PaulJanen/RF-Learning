import numpy as np
import pickle

class ReplayBuffer(object):

  def __init__(self, max_size=1e6):
    self.storage = []
    self.max_size = max_size
    self.ptr = 0

  def add(self, transition):
    if len(self.storage) == self.max_size:
      self.storage[int(self.ptr)] = transition
      self.ptr = (self.ptr + 1) % self.max_size
    else:
      self.storage.append(transition)

  def sample(self, batch_size):
    ind = np.random.randint(0, len(self.storage), size=batch_size)
    batch_states, batch_next_states, batch_actions, batch_rewards, batch_dones = [], [], [], [], []
    for i in ind:
      state, next_state, action, reward, done = self.storage[i]
      batch_states.append(np.array(state, copy=False))
      batch_next_states.append(np.array(next_state, copy=False))
      batch_actions.append(np.array(action, copy=False))
      batch_rewards.append(np.array(reward, copy=False))
      batch_dones.append(np.array(done, copy=False))
    return np.array(batch_states), np.array(batch_next_states), np.array(batch_actions), np.array(batch_rewards).reshape(-1, 1), np.array(batch_dones).reshape(-1, 1)
  
  def save(self, filename, directory):
    allData = []
    allData.append(self.storage)
    allData.append(self.ptr)
    allData.append(self.max_size)

    with open('%s/%s_storage' % (directory, filename), 'wb') as f:
      pickle.dump(self.storage, f)
  
  # Making a load method to load a pre-trained model
  def load(self, filename, directory):
    with open('%s/%s_storage' % (directory, filename), 'rb') as f:
      allData = pickle.load(f)
    
    self.storage = allData[0]
    self.ptr = allData[1]
    self.max_size = allData[2]


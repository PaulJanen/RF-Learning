
import os
import time
from pygame import init
import torch
import numpy as np
from TD3 import TD3
from ReplayBuffer import ReplayBuffer
from Walker import Walker






#for i in range(5555,5569):
#    allWalkers.append(Walker(i))



class WalkerManager():

    def mkdir(self, base, name):
        path = os.path.join(base, name)
        if not os.path.exists(path):
            os.makedirs(path)
        return path


    def __init__(self):     
        self.seed = 0 # Random seed number
        torch.manual_seed(self.seed)
        np.random.seed(self.seed)

        work_dir = self.mkdir('exp', 'brs')
        monitor_dir = self.mkdir(work_dir, 'monitor')
        t0 = time.time()

        self.walker = Walker(1, self)
        self.policy = TD3(self.walker.state_dim, self.walker.action_dim, self.walker.env.maxAction, self.walker.env.minAction)
        self.replayBuffer = ReplayBuffer()
        self.evaluations = [0]

        self.file_name = "%s_%s_%s" % ("TD3", self.walker.env_name, str(self.seed))
        print ("---------------------------------------")
        print ("Settings: %s" % (self.file_name))
        print ("------------------")

        if not os.path.exists("./results"):
            os.makedirs("./results")
        if self.walker.save_models and not os.path.exists("./pytorch_models"):
            os.makedirs("./pytorch_models")

        self.total_timesteps = 0
        self.max_timesteps = self.walker.max_timesteps
        self.trainAfterSteps = 10_000
        self.trainingCycles = 1000
        self.timesteps_since_eval = 0
        self.timesteps_since_train = 0
        self.eval_freq = 1000
        self.allWalkers = []
        for i in range(5555,5565):
            self.allWalkers.append(Walker(i, self))
        
        for i in self.allWalkers:
            i.start()
        

    def Train(self):

        if (self.total_timesteps < self.max_timesteps):
            if(self.timesteps_since_train >= self.trainAfterSteps):
                self.timesteps_since_train = 0
                if self.total_timesteps != 0:
                    avgReward = 0
                    for i in self.allWalkers:
                        avgReward += i.movingAvgReward
                    avgReward /= len(self.allWalkers)

                    self.policy.isTraining = True

                    while self.policy.agentsSelectingActionCount != 0:
                        time.sleep(0.1)
                    print(len(self.replayBuffer.storage))
                    print("Total Timesteps: {} Avg Moving Reward: {}".format(self.total_timesteps, avgReward))
                    self.policy.train(self.replayBuffer, self.trainingCycles, self.walker.batch_size, self.walker.discount, self.walker.tau, self.walker.policy_noise, self.walker.noise_clip, self.walker.policy_freq)
                    # We evaluate the episode and we save the policy
                    if self.timesteps_since_eval >= self.eval_freq:
                        self.timesteps_since_eval %= self.eval_freq
                        #self.evaluations.append(self.policy.evaluate_policy(self.allWalkers[0]))
                        self.evaluations.append(avgReward)
                        self.policy.save(self.file_name, directory="./pytorch_models")
                        np.save("./results/%s" % (self.file_name), self.evaluations)
        else:
            print("training done")


    def AddTimesteps(self):
        self.total_timesteps += 1
        self.timesteps_since_eval += 1
        self.timesteps_since_train += 1
        self.Train()







mng = WalkerManager()
mng.Train()
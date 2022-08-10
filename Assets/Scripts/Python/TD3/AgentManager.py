
import os
import time
from pygame import init
import torch
import numpy as np
from TD3 import TD3
from ReplayBuffer import ReplayBuffer
from Plant import Plant
import tensorflow as tf

class AgentManager():

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

        self.agent = Plant(1, self)
        self.policy = TD3(self.agent.state_dim, self.agent.action_dim, self.agent.env.maxAction, self.agent.env.minAction)
        self.replayBuffer = ReplayBuffer()
        self.evaluations = [0]

        self.loadModel = False
        self.save_models = True
        self.file_name = "%s_%s_%s" % ("TD3", self.agent.env_name, str(self.seed))
        self.modelDirectory = "./pytorch_models"
        print ("---------------------------------------")
        print ("Settings: %s" % (self.file_name))
        print ("------------------")

        if not os.path.exists("./results"):
            os.makedirs("./results")
        if self.save_models and not os.path.exists(self.modelDirectory):
            os.makedirs(self.modelDirectory)

        now = time.localtime()
        subdir = time.strftime("%d-%b-%Y_%H.%M.%S", now)

        self.summary_dir1 = os.path.join("stackoverflow", subdir, "t1")
        self.summary_writer1 = tf.summary.create_file_writer(self.summary_dir1, flush_millis=1)


        self.seed = 0 # Random seed number
        self.total_timesteps = 0
        self.max_timesteps = 1e7 # Total number of iterations/timesteps
        self.trainAfterSteps = self.agent.env.max_episode_steps*2
        self.trainingCycles = self.agent.env.max_episode_steps
        self.timesteps_since_eval = 0
        self.timesteps_since_train = 0
        self.eval_freq = 20_000
        self.batch_size = 100
        self.discount = 0.99 # Discount factor gamma, used in the calculation of the total discounted reward
        self.tau = 0.005 # Target network update rate
        self.policy_noise = 0.1#0.05#0.2 # STD of Gaussian noise added to the actions for the exploration purposes
        self.noise_clip = 0.25#0.1#0.5 # Maximum value of the Gaussian noise added to the actions (policy)
        self.expl_noise = 0.1 # Exploration noise - STD value of exploration Gaussian noise
        self.policy_freq = 2 # Number of iterations to wait before the policy network (Actor model) is updated
        self.allWalkers = []
        self.trainingCrashedDetection = 0
        self.crashedAfterThisManySteps = 60
        self.CreateAndStartWalkers()
        
        
    def CreateAndStartWalkers(self):
        if(len(self.allWalkers) > 0):
            oldWalkers = self.allWalkers
            self.allWalkers.clear()
            for index,i in enumerate(range(5556,5564)):
                self.allWalkers.append(Plant(i, self, oldWalkers[index]))
        else:
            for i in range(5556,5564):
                self.allWalkers.append(Plant(i, self))
        
        for i in self.allWalkers:
            i.name = "Thread - " + str(i.port)
            i.setDaemon(True)  
            i.start()


    def Train(self):

        if(self.loadModel):
            self.policy.load(self.file_name, directory=self.modelDirectory)
            self.replayBuffer.load(self.file_name, directory=self.modelDirectory)

        while(self.total_timesteps < self.max_timesteps):
            time.sleep(1)     
            self.trainingCrashedDetection += 1
            
            '''if(self.trainingCrashedDetection >= self.crashedAfterThisManySteps):
                for i in self.allWalkers:
                    i.raise_exception()
                for i in self.allWalkers:
                    i.join()
                time.sleep(1)
                self.trainingCrashedDetection = 0
                self.CreateAndStartWalkers()
            '''
            if(self.timesteps_since_train >= self.trainAfterSteps and self.agent.env.explorationSteps < self.total_timesteps and self.loadModel == False):
                self.trainingCrashedDetection = 0
                self.timesteps_since_train = 0
                if self.total_timesteps != 0:
                    avgReward = 0
                    for i in self.allWalkers:
                        avgReward += i.movingAvgReward
                    avgReward /= len(self.allWalkers)

                    self.policy.isTraining = True

                    while self.policy.agentsSelectingActionCount != 0:
                        time.sleep(0.1)
                    print("Total Timesteps: {} Avg Moving Reward: {}".format(self.total_timesteps, avgReward))
                    with self.summary_writer1.as_default():      
                        tf.summary.scalar(name="unify/sin_x", data=avgReward ,step=self.total_timesteps)
                    #self.summary_writer1.flush()
                    self.policy.train(self.replayBuffer, min(self.trainingCycles, self.total_timesteps), self.batch_size, self.discount, self.tau, self.policy_noise, self.noise_clip, self.policy_freq)
                    # We evaluate the episode and we save the policy
                    if self.timesteps_since_eval >= self.eval_freq and self.save_models==True:
                        self.timesteps_since_eval %= self.eval_freq
                        #self.evaluations.append(self.policy.evaluate_policy(self.allWalkers[0]))
                        self.evaluations.append(avgReward)
                        self.policy.save(self.file_name, directory=self.modelDirectory)
                        self.replayBuffer.save(self.file_name, directory=self.modelDirectory)
                        np.save("./results/%s" % (self.file_name), self.evaluations)
        else:
            print("training done")


    def AddTimesteps(self):
        self.total_timesteps += 1
        self.timesteps_since_eval += 1
        self.timesteps_since_train += 1


mng = AgentManager()
mng.Train()

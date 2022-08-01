import numpy as np

a = np.empty((5,))
b = np.array([1,2,3,4,10])
(np.random.rand(*a.shape)*b).shape

minAction = np.array([-1,-1,-5])
maxAction = np.array([1,1,5])
np.random.uniform(minAction, maxAction, size=(3,))
(int)(100*0.15)
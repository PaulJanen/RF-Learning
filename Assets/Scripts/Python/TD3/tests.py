import numpy as np
import pickle

a = [1,2,"test"]
b = [3,4,"hehe"]
c = [5,6,"123456"]
storage = []
storage.append(a)
storage.append(b)
storage.append(c)
ptr = 5

alldata = []
alldata.append(storage)
alldata.append(ptr)

with open('%s/%s_storage' % ("./pytorch_models", 'hubabuba'), 'wb') as f:
    pickle.dump(alldata, f)


with open('%s/%s_storage' % ("./pytorch_models", 'hubabuba'), "rb") as f:
    b = pickle.load(f)
b[1]

a = np.empty((5,))
b = np.array([1,2,3,4,10])
(np.random.rand(*a.shape)*b).shape

minAction = np.array([-1,-1,-5])
maxAction = np.array([1,1,5])
np.random.uniform(minAction, maxAction, size=(3,))
(int)(100*0.15)

print("hehe " + str(12))

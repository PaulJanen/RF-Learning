from abc import ABCMeta, abstractmethod

class Client(metaclass = ABCMeta):
    def __init__(self):
        pass
    
    @abstractmethod
    def SendAction(self, action):
        pass

    @abstractmethod
    def SendReset(self):
        pass

    @abstractmethod
    def SendClose(self):
        pass



    
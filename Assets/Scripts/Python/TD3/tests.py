

class Parent(object):
    def __init__(self):
        self.foobar = []

class Child(Parent):
    def __init__(self):
        Parent.__init__(self)
        self.foobar.append('world')
        print("-------")
        print(self.foobar)
        
        vava = [3,1,2]
        print(vava[0])

x = Child()

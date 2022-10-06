

class Parent(object):
    def __init__(self):
        self.foobar = []

class Child(Parent):
    def __init__(self):
        Parent.__init__(self)
        self.foobar.append('world')
        print("-------")
        print(self.foobar)

x = Child()

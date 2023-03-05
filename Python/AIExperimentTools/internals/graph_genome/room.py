class Room:
    def __init__(self, left_col=0, right_col=0, bottom_row=0, top_row=0):
        self.isReal = not(left_col == 0 and right_col == 0 and bottom_row == 0 and top_row == 0)
        self.leftColumn = left_col
        self.rightColumn = right_col
        self.bottomRow = bottom_row
        self.topRow = top_row

    def __eq__(self, other):
        if type(other) is type(self):
            return self.__dict__ == other.__dict__
        else:
            return False

    def __hash__(self):
        return hash(tuple(self.__dict__.items()))

    def get_room_size(self):
        if self.isReal:
            return 0, 0
        return self.rightColumn - self.leftColumn, self.topRow - self.bottomRow

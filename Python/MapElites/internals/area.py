
class Area:
    def __init__(self, left_column=0, bottom_row=0, right_column=0, top_row=0, is_corridor=False):
        self.bottomRow = bottom_row
        self.topRow = top_row
        self.leftColumn = left_column
        self.rightColumn = right_column
        self.isCorridor = is_corridor
        self.bottomRow = bottom_row

    def __eq__(self, other):
        if type(other) is type(self):
            return self.__dict__ == other.__dict__
        else:
            return False

    def __hash__(self):
        return hash(tuple(self.__dict__.items()))


def scale_area(area, scale):
    return Area(
        scale * area.leftColumn,
        scale * area.bottomRow,
        scale * area.rightColumn,
        scale * area.topRow,
        area.isCorridor,
    )

def contains_area(area1, area2):
    return (
        area1.leftColumn <= area2.leftColumn
        and area1.rightColumn >= area2.rightColumn
        and area1.topRow >= area2.topRow
        and area1.bottomRow <= area2.bottomRow
    )

def overlap_area(area1, area2):
    # First check if the corners touch, that case is not considered an overlap
    if (
        (area1.leftColumn == area2.rightColumn
        and area1.rightColumn > area2.leftColumn
        and area1.topRow == area2.bottomRow
        and area1.bottomRow < area2.topRow)
        or
        (area1.leftColumn == area2.rightColumn
        and area1.rightColumn > area2.leftColumn
        and area1.topRow > area2.bottomRow
        and area1.bottomRow == area2.topRow)
        or
        (area1.leftColumn < area2.rightColumn
        and area1.rightColumn == area2.leftColumn
        and area1.topRow > area2.bottomRow
        and area1.bottomRow == area2.topRow)
        or
        (area1.leftColumn < area2.rightColumn
        and area1.rightColumn == area2.leftColumn
        and area1.topRow == area2.bottomRow
        and area1.bottomRow < area2.topRow)
    ):
        return False
    else:
        return (area1.leftColumn <= area2.rightColumn 
                and area1.rightColumn >= area2.leftColumn 
                and area1.topRow >= area2.bottomRow 
                and area1.bottomRow <= area2.topRow)

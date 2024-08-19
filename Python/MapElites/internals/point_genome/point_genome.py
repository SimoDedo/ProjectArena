from math import sqrt

import numpy

from internals.area import Area
from internals.phenotype import Phenotype
from internals.point_genome import mutation, crossover, generation, constants
from internals.point_genome.constants import POINT_SQUARE_SIZE, POINT_NUM_ROOMS, POINT_NUM_POINT_COUPLES, POINT_MAX_MAP_WIDTH, POINT_MAX_MAP_HEIGHT, \
    POINT_CORRIDOR_WIDTH, POINT_MAP_SCALE

class PointGenome:
    def __init__(self, point_couples, rooms):
        self.point_couples = point_couples
        self.rooms = rooms
        self.mapScale = POINT_MAP_SCALE
    
    # --- Generation methods ---
    
    @staticmethod
    def create_random_genome():
        return generation.create_random_genome()

    @staticmethod
    def mutate(genome):
        return mutation.mutate(genome)
    
    @staticmethod
    def crossover(genome1, genome2):
        return crossover.crossover(genome1, genome2)

    def to_array(self):
        array = []
        for point in self.point_couples:
            if point is None:
                array.append(0)
                array.append(0)
                array.append(0)
                array.append(0)
                array.append(0)
            else:
                array.append(point.point_left[0])
                array.append(point.point_left[1])
                array.append(point.point_right[0])
                array.append(point.point_right[1])
                array.append(point.connection)
        for room in self.rooms:
            if room is None:
                array.append(0)
                array.append(0)
                array.append(0)
            else:
                array.append(room.left_col)
                array.append(room.bottom_row)
                array.append(room.size)
        return array

    @staticmethod
    def array_as_genome(array):
        if len(array) != + POINT_NUM_POINT_COUPLES * 5 + POINT_NUM_ROOMS * 3:
            raise ValueError(
                "The array must have a length of " + str(POINT_NUM_POINT_COUPLES * 5 + POINT_NUM_ROOMS * 3) + " but has a length of " + str(len(array))
            )
        
        points = []
        rooms = []
        for i in range(0, POINT_NUM_POINT_COUPLES * 5, 5):
            point_one = (array[i], array[i + 1])
            point_two = (array[i + 2], array[i + 3])
            connection = array[i + 4]
            if point_one == (0, 0) and point_two == (0, 0) and connection == 0:
                points.append(None)
            else:
                points.append(PointPointCouple(point_one, point_two, connection))
        for i in range(POINT_NUM_POINT_COUPLES * 5, POINT_NUM_POINT_COUPLES * 5 + POINT_NUM_ROOMS * 3, 3):
            left_col = array[i]
            bottom_row = array[i + 1]
            size = array[i + 2]
            if left_col == 0 and bottom_row == 0 and size == 0:
                rooms.append(None)
            else:
                rooms.append(PointRoom(array[i], array[i + 1], array[i + 2]))
            
        return PointGenome(points, rooms)


    def phenotype(self):
        # Step 1: iterate through all rooms and find the one closest to center
        rooms = [room for room in self.rooms if room is not None]
        corridors = []
        for point in self.point_couples:
            if point is not None:
                corridors.extend(point.to_corridors())
        all_areas = rooms + corridors

        if len(all_areas) == 0:
            return None
        
        closest_room_index = 0
        best_distance = 99999999
        map_center_col = (POINT_MAX_MAP_WIDTH + 1) // 2
        map_center_row = (POINT_MAX_MAP_HEIGHT + 1) // 2
        for idx, area in enumerate(all_areas):
            if area is None: continue
            col_distance = map_center_col - area.center_col()
            row_distance = map_center_row - area.center_row()
            distance = sqrt(col_distance ** 2 + row_distance ** 2)
            if distance < best_distance:
                best_distance = distance
                closest_room_index = idx

        # Step 2: Loop through everything to compute intersections.
        areas_in_phenotype = [all_areas[closest_room_index]]
        previous_areas = []

        while previous_areas != tuple(areas_in_phenotype):
            previous_areas = tuple(areas_in_phenotype)
            for area in self.rooms:
                if area in areas_in_phenotype: continue
                if area is None: continue
                if any(intersect(area, aaa) for aaa in areas_in_phenotype):
                    areas_in_phenotype.append(area)

            for corridor in corridors:
                if corridor in areas_in_phenotype: continue
                if corridor is None: continue
                if any(intersect(corridor, aaa) for aaa in areas_in_phenotype):
                    areas_in_phenotype.append(corridor)

        areas = [scale_area(x.to_area(), POINT_SQUARE_SIZE) for x in areas_in_phenotype]
        return Phenotype(
            POINT_MAX_MAP_WIDTH * POINT_SQUARE_SIZE,
            POINT_MAX_MAP_HEIGHT * POINT_SQUARE_SIZE,
            self.mapScale,
            areas,
        )

    @staticmethod
    def unscaled_area(phenotype):
        tiles = numpy.zeros([POINT_MAX_MAP_HEIGHT * POINT_SQUARE_SIZE, POINT_MAX_MAP_WIDTH * POINT_SQUARE_SIZE])

        for area in phenotype.areas:
            for row in range(area.bottomRow, area.topRow):
                for col in range(area.leftColumn, area.rightColumn):
                    tiles[row][col] = 1
        return numpy.sum(tiles)


class PointRoom:
    def __init__(self, left_col, bottom_row, size):
        self.left_col = left_col
        self.bottom_row = bottom_row
        self.size = size

    def center_row(self):
        return self.bottom_row + self.size / 2

    def center_col(self):
        return self.left_col + self.size / 2
    
    def right_col(self):
        return self.left_col + self.size 

    def top_row(self):
        return self.bottom_row + self.size

    def to_area(self):
        return Area(self.left_col, self.bottom_row, self.right_col(), self.top_row(), False)

    def __eq__(self, other):
        if type(other) is type(self):
            return self.__dict__ == other.__dict__
        else:
            return False

    def __hash__(self):
        return hash(tuple(self.__dict__.items()))


class PointPointCouple:
    def __init__(self, point_left, point_right, connection):
        self.point_left = point_left
        self.point_right = point_right
        self.connection = connection

    def to_corridors(self):
        corridors = []
        if self.connection == constants.HORIZONTAL_FIRST_CONNECTION:
            intermediate_point = self.point_left
            if self.point_left[0] != self.point_right[0]:
                corridors.append(PointCorridor(self.point_left[0], self.point_left[1], self.point_right[0] - self.point_left[0] + POINT_CORRIDOR_WIDTH))
                intermediate_point = (self.point_right[0], self.point_left[1])
            if self.point_left[1] != self.point_right[1]:
                point_down = intermediate_point if intermediate_point[1] < self.point_right[1] else self.point_right
                point_up = intermediate_point if intermediate_point[1] > self.point_right[1] else self.point_right
                corridors.append(PointCorridor(intermediate_point[0], point_down[1], -(point_up[1] - point_down[1])))
            return corridors
        else:
            intermediate_point = self.point_left
            if self.point_left[1] != self.point_right[1]:
                point_down = self.point_left if self.point_left[1] < self.point_right[1] else self.point_right
                point_up = self.point_left if self.point_left[1] > self.point_right[1] else self.point_right
                corridors.append(PointCorridor(self.point_left[0], point_down[1], -(point_up[1] - point_down[1])))
                intermediate_point = (self.point_left[0], self.point_right[1])
            if self.point_left[0] != self.point_right[0]:
                corridors.append(PointCorridor(intermediate_point[0], intermediate_point[1], self.point_right[0] - intermediate_point[0]))
            return corridors

    def __eq__(self, other):
        if type(other) is type(self):
            return self.__dict__ == other.__dict__
        else:
            return False

    def __hash__(self):
        return hash(tuple(self.__dict__.items()))

class PointCorridor:
    def __init__(self, left_col, bottom_row, length):
        self.left_col = left_col
        self.bottom_row = bottom_row
        self.length = length

    def is_vertical(self):
        return self.length < 0

    def right_col(self):
        if self.is_vertical():
            return self.left_col + POINT_CORRIDOR_WIDTH
        else:
            return self.left_col + self.length

    def top_row(self):
        if self.is_vertical():
            return self.bottom_row - self.length
        else:
            return self.bottom_row + POINT_CORRIDOR_WIDTH
        
    def center_row(self):
        if self.is_vertical():
            return self.bottom_row - self.length / 2
        else:
            return self.bottom_row + POINT_CORRIDOR_WIDTH / 2
        
    def center_col(self):
        if self.is_vertical():
            return self.left_col + POINT_CORRIDOR_WIDTH / 2
        else:
            return self.left_col + self.length / 2

    def to_area(self):
        return Area(self.left_col, self.bottom_row, self.right_col(), self.top_row(), True)

    def __eq__(self, other):
        if type(other) is type(self):
            return self.__dict__ == other.__dict__
        else:
            return False

    def __hash__(self):
        return hash(tuple(self.__dict__.items()))

def intersect(area_a, area_b):
    if not (area_a.left_col <= area_b.right_col() and area_b.left_col <= area_a.right_col()):
        return False

    if not (area_a.bottom_row <= area_b.top_row() and area_b.bottom_row <= area_a.top_row()):
        return False

    if not (area_a.bottom_row == area_b.top_row() or area_b.bottom_row == area_a.top_row()):
        # Intersection, not touching only in the angle
        return True

    if not (area_a.left_col == area_b.right_col() or area_b.left_col == area_a.right_col()):
        # Intersection, not touching only in the angle
        return True

    # No intersection
    return False


def scale_area(area, scale):
    return Area(
        scale * area.leftColumn,
        scale * area.bottomRow,
        scale * area.rightColumn,
        scale * area.topRow,
        area.isCorridor,
    )

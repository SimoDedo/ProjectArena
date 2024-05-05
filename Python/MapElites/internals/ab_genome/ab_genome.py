from math import sqrt

import numpy

from internals.area import Area
from internals.phenotype import Phenotype
from internals.ab_genome import mutation, crossover, generation, constants
from internals.ab_genome.constants import AB_SQUARE_SIZE, AB_NUM_ROOMS, AB_NUM_CORRIDORS, AB_MAX_MAP_WIDTH, AB_MAX_MAP_HEIGHT, \
    AB_CORRIDOR_WIDTH, AB_MAP_SCALE

class ABGenome:
    def __init__(self, rooms, corridors):
        self.rooms = rooms
        self.corridors = corridors
        self.mapScale = AB_MAP_SCALE
    
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
        for room in self.rooms:
            array.append(room.left_col)
            array.append(room.bottom_row)
            array.append(room.size)
        for corridor in self.corridors:
            array.append(corridor.left_col)
            array.append(corridor.bottom_row)
            array.append(corridor.length)
        return array

    @staticmethod
    def array_as_genome(array):
        if len(array) != (AB_NUM_ROOMS + AB_NUM_CORRIDORS) * 3:
            raise ValueError(
                f"Expected genome to have {AB_NUM_ROOMS + AB_NUM_CORRIDORS} rooms and corridors, but it had {array.size // 3}.")
        
        rooms = []
        corridors = []
        for i in range(0, AB_NUM_ROOMS * 3, 3):
            rooms.append(ABRoom(array[i], array[i + 1], array[i + 2]))
        for i in range(AB_NUM_ROOMS * 3, (AB_NUM_ROOMS + AB_NUM_CORRIDORS) * 3, 3):
            corridors.append(ABCorridor(array[i], array[i + 1], array[i + 2]))
            
        return ABGenome(rooms, corridors)


    def phenotype(self):
        # Step 1: iterate through all rooms and find the one closest to center
        closest_room_index = 0
        best_distance = 99999999
        map_center_col = (AB_MAX_MAP_WIDTH + 1) // 2
        map_center_row = (AB_MAX_MAP_HEIGHT + 1) // 2
        for idx, room in enumerate(self.rooms):
            col_distance = map_center_col - room.center_col()
            row_distance = map_center_row - room.center_row()
            distance = sqrt(col_distance ** 2 + row_distance ** 2)
            if distance < best_distance:
                best_distance = distance
                closest_room_index = idx

        # Step 2: Loop through everything to compute intersections.
        areas_in_phenotype = [self.rooms[closest_room_index]]
        previous_areas = []

        while previous_areas != tuple(areas_in_phenotype):
            previous_areas = tuple(areas_in_phenotype)
            for room in self.rooms:
                if room in areas_in_phenotype: continue
                if any(intersect(room, aaa) for aaa in areas_in_phenotype):
                    areas_in_phenotype.append(room)

            for corridor in self.corridors:
                if corridor in areas_in_phenotype: continue
                if any(intersect(corridor, aaa) for aaa in areas_in_phenotype):
                    areas_in_phenotype.append(corridor)

        areas = [scale_area(x.to_area(), AB_SQUARE_SIZE) for x in areas_in_phenotype]
        return Phenotype(
            AB_MAX_MAP_WIDTH * AB_SQUARE_SIZE,
            AB_MAX_MAP_HEIGHT * AB_SQUARE_SIZE,
            self.mapScale,
            areas,
        )

    @staticmethod
    def unscaled_area(phenotype):
        tiles = numpy.zeros([AB_MAX_MAP_HEIGHT * AB_SQUARE_SIZE, AB_MAX_MAP_WIDTH * AB_SQUARE_SIZE])

        for area in phenotype.areas:
            for row in range(area.bottomRow, area.topRow):
                for col in range(area.leftColumn, area.rightColumn):
                    tiles[row][col] = 1
        return numpy.sum(tiles)


class ABRoom:
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


class ABCorridor:
    def __init__(self, left_col, bottom_row, length):
        self.left_col = left_col
        self.bottom_row = bottom_row
        self.length = length

    def is_vertical(self):
        return self.length < 0

    def right_col(self):
        if self.is_vertical():
            return self.left_col + AB_CORRIDOR_WIDTH
        else:
            return self.left_col + self.length

    def top_row(self):
        if self.is_vertical():
            return self.bottom_row - self.length
        else:
            return self.bottom_row + AB_CORRIDOR_WIDTH

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

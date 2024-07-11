from math import sqrt

import numpy

from internals.area import Area
from internals.phenotype import Phenotype
from internals.smt_genome import mutation, crossover, generation, constants
from internals.smt_genome.constants import SMT_SQUARE_SIZE, SMT_ROOMS_NUMBER, SMT_MAX_MAP_WIDTH, SMT_MAX_MAP_HEIGHT, \
    SMT_CORRIDOR_WIDTH, SMT_MAP_SCALE, SMT_LINES_NUMBER, GENOME_SCALE_FOR_SMT_SOLVER, SMT_CORRIDOR_WIDTH
from internals.smt_genome.solver import solve
import bisect

class SMTGenome:
    def __init__(self, rooms, lines, separation):
        self.rooms = rooms
        self.lines = lines
        self.separation = separation
        self.mapScale = SMT_MAP_SCALE
    
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
            if room is None:
                array.append(0)
                array.append(0)
            else:
                array.append(room.width)
                array.append(room.height)
        for line in self.lines:
            if line is None:
                array.append(0)
                array.append(0)
                array.append(0)
                array.append(0)
            else:
                array.append(line.start[0])
                array.append(line.start[1])
                array.append(line.end[0])
                array.append(line.end[1])
        array.append(self.separation)
        return array

    @staticmethod
    def array_as_genome(array):
        if len(array) != (SMT_ROOMS_NUMBER * 2) + (SMT_LINES_NUMBER * 4) + 1:
            raise ValueError(
                f"Expected genome to have {SMT_ROOMS_NUMBER} rooms and {SMT_LINES_NUMBER} lines.")
        
        rooms = []
        lines = []
        for i in range(0, SMT_ROOMS_NUMBER * 2, 2):
            if array[i] == 0 and array[i + 1] == 0:
                rooms.append(None)
            else:
                rooms.append(SMTRoom(array[i], array[i + 1]))
        for i in range(SMT_ROOMS_NUMBER * 2, SMT_ROOMS_NUMBER * 2 + SMT_LINES_NUMBER * 4, 4):
            if array[i] == 0 and array[i + 1] == 0 and array[i + 2] == 0 and array[i + 3] == 0:
                lines.append(None)
            else:
                lines.append(SMTLine((array[i], array[i + 1]), (array[i + 2], array[i + 3])))
        separation = array[SMT_ROOMS_NUMBER * 2 + SMT_LINES_NUMBER * 4]
        return SMTGenome(rooms, lines, separation)


    def phenotype(self):
        # Place rooms accordin to the SMT solver and get the minimum spanning tree connecting them
        rooms_pos ,mst = solve(self)
        rooms_pos = [(int(x/GENOME_SCALE_FOR_SMT_SOLVER), int(y/GENOME_SCALE_FOR_SMT_SOLVER)) for x, y in rooms_pos]
        zipped_mst = list(zip(*mst.nonzero()))
        zipped_mst = [(int(i), int(j)) for i, j in zipped_mst]

        # Create areas corresponding to rooms
        room_areas = []
        j = 0
        for i in range(SMT_ROOMS_NUMBER):
            if self.rooms[i] is not None:
                room_areas.append(Area(rooms_pos[j][0], rooms_pos[j][1], rooms_pos[j][0] + self.rooms[i].width, rooms_pos[j][1] + self.rooms[i].height, False))
                j += 1

        # Create corridors according to the MST
        new_corridors_areas = []
        for i, j in zipped_mst:
            new_corridors_areas += self.__add_corridor(room_areas[i], room_areas[j])
        
        # Create corridors between rooms that are intersected by a line but not already connected by the MST
        # For each line, we calculate intersections with rooms and get them in order of increasing x position. Then, if the rooms are not already connected, 
        # we add a corridor between each two consecutive room from the result.
        rooms_connected = zipped_mst
        for line in self.lines:
            if line is not None:
                ordered_rooms_idxs = self.__check_line_rooms_intersections(room_areas, line)
                for i in range(len(ordered_rooms_idxs) - 1):
                    #print(f"Checking line {line.start} -> {line.end} between rooms {ordered_rooms_idxs[i]}: {(room_areas[ordered_rooms_idxs[i]].leftColumn, room_areas[ordered_rooms_idxs[i]].topRow)} and {ordered_rooms_idxs[i+1]}: {(room_areas[ordered_rooms_idxs[i+1]].leftColumn, room_areas[ordered_rooms_idxs[i+1]].topRow)}")
                    if (ordered_rooms_idxs[i],ordered_rooms_idxs[i+1]) not in zipped_mst and (ordered_rooms_idxs[i+1],ordered_rooms_idxs[i]) not in zipped_mst: 
                        #print("Actual adding corridor")
                        new_corridors_areas += self.__add_corridor(room_areas[ordered_rooms_idxs[i]], room_areas[ordered_rooms_idxs[i+1]])
                        rooms_connected.append((ordered_rooms_idxs[i], ordered_rooms_idxs[i+1]))

        areas = room_areas + new_corridors_areas
        return Phenotype(
            SMT_MAX_MAP_WIDTH * SMT_SQUARE_SIZE,
            SMT_MAX_MAP_HEIGHT * SMT_SQUARE_SIZE,
            self.mapScale,
            areas,
        )
    
    def __add_corridor(self, room_one: Area, room_two: Area):
        # Room two position compared to room one
        onRight = room_one.rightColumn <= room_two.leftColumn
        onLeft = room_one.leftColumn >= room_two.rightColumn
        onTop = room_one.topRow <= room_two.bottomRow
        onBottom = room_one.bottomRow >= room_two.topRow

        horizontal = (onRight or onLeft) and not onTop and not onBottom
        vertical = (onTop or onBottom) and not onRight and not onLeft

        if horizontal:
            left_room = room_one if onRight else room_two
            right_room = room_two if onRight else room_one
            
            top = min(left_room.topRow, right_room.topRow)
            bottom = max(left_room.bottomRow, right_room.bottomRow)
            corridor_center = bottom + (top - bottom) / 2
            extra_length = SMT_CORRIDOR_WIDTH #0 if right_room.leftColumn - left_room.rightColumn >= SMT_CORRIDOR_WIDTH else SMT_CORRIDOR_WIDTH - (right_room.leftColumn - left_room.rightColumn) 
            if left_room.rightColumn != right_room.leftColumn:
                return [
                    Area(
                        int(left_room.rightColumn - extra_length / 2) , 
                        int(corridor_center - SMT_CORRIDOR_WIDTH / 2), 
                        int(right_room.leftColumn + extra_length / 2),
                        int(corridor_center + SMT_CORRIDOR_WIDTH /2), 
                        True)]
            else:
                return []
        elif vertical:
            bottom_room = room_one if onTop else room_two
            top_room = room_two if onTop else room_one

            right = min(bottom_room.rightColumn, top_room.rightColumn)
            left = max(bottom_room.leftColumn, top_room.leftColumn)
            corridor_center = left + (right - left) / 2
            extra_length = SMT_CORRIDOR_WIDTH #0 if top_room.bottomRow - bottom_room.topRow >= SMT_CORRIDOR_WIDTH else SMT_CORRIDOR_WIDTH - (top_room.bottomRow - bottom_room.topRow)
            if bottom_room.topRow != top_room.bottomRow:
                return [
                    Area(
                        int(corridor_center - SMT_CORRIDOR_WIDTH / 2),
                        int(bottom_room.topRow - extra_length / 2),
                        int(corridor_center + SMT_CORRIDOR_WIDTH / 2),
                        int(top_room.bottomRow + extra_length / 2),
                        True)]
            else:
                return []
        else:
            left_room = room_one if onRight else room_two
            right_room = room_two if onRight else room_one

            areas = []
            if left_room.topRow <= right_room.bottomRow: # The right_room is above
                areas.append(Area(
                    int(left_room.rightColumn - SMT_CORRIDOR_WIDTH),
                    int(left_room.topRow),
                    int(left_room.rightColumn),
                    int(right_room.bottomRow),
                    True))
                areas.append(Area(
                    int(left_room.rightColumn - SMT_CORRIDOR_WIDTH),
                    int(right_room.bottomRow),
                    int(right_room.leftColumn),
                    int(right_room.bottomRow + SMT_CORRIDOR_WIDTH),
                    True))
            else: # The right_room is below
                areas.append(Area(
                    int(left_room.rightColumn - SMT_CORRIDOR_WIDTH),
                    int(right_room.topRow),
                    int(left_room.rightColumn),
                    int(left_room.bottomRow),
                    True))
                areas.append(Area(
                    int(left_room.rightColumn - SMT_CORRIDOR_WIDTH),
                    int(right_room.topRow - SMT_CORRIDOR_WIDTH),
                    int(right_room.leftColumn),
                    int(right_room.topRow),
                    True))
            return areas

    def __check_line_rooms_intersections(self, room_areas, line):
        ordered_intersections = []
        for room in room_areas:
            points = []
            points.append(self.__intersect((room.leftColumn, room.topRow), (room.rightColumn, room.topRow), line.start, line.end))
            points.append(self.__intersect((room.rightColumn, room.topRow), (room.rightColumn, room.bottomRow), line.start, line.end))
            points.append(self.__intersect((room.rightColumn, room.bottomRow), (room.leftColumn, room.bottomRow), line.start, line.end))
            points.append(self.__intersect((room.leftColumn, room.bottomRow), (room.leftColumn, room.topRow), line.start, line.end))

            points = [point for point in points if point is not None]
            min_x = min(points, key=lambda x: x[1])[1] if len(points) > 0 else None
            if min_x is not None:
                bisect.insort(ordered_intersections, (min_x, room_areas.index(room)))
        ordered_rooms = [room_idx for _, room_idx in ordered_intersections]
        return ordered_rooms
    
    def __intersect(self, p1, p2, p3, p4):
        x1,y1 = p1
        x2,y2 = p2
        x3,y3 = p3
        x4,y4 = p4
        denom = (y4-y3)*(x2-x1) - (x4-x3)*(y2-y1)
        if denom == 0: # parallel
            return None
        ua = ((x4-x3)*(y1-y3) - (y4-y3)*(x1-x3)) / denom
        if ua < 0 or ua > 1: # out of range
            return None
        ub = ((x2-x1)*(y1-y3) - (y2-y1)*(x1-x3)) / denom
        if ub < 0 or ub > 1: # out of range
            return None
        x = x1 + ua * (x2-x1)
        y = y1 + ua * (y2-y1)
        return (x,y)

    @staticmethod
    def unscaled_area(phenotype):
        tiles = numpy.zeros([SMT_MAX_MAP_HEIGHT * SMT_SQUARE_SIZE, SMT_MAX_MAP_WIDTH * SMT_SQUARE_SIZE])

        for area in phenotype.areas:
            for row in range(area.bottomRow, area.topRow):
                for col in range(area.leftColumn, area.rightColumn):
                    tiles[row][col] = 1
        return numpy.sum(tiles)


class SMTRoom:
    def __init__(self, width, height):
        self.height = height
        self.width = width
        
    def __eq__(self, other):
        if type(other) is type(self):
            return self.__dict__ == other.__dict__
        else:
            return False

    def __hash__(self):
        return hash(tuple(self.__dict__.items()))


class SMTLine:
    def __init__(self, start, end):
        self.start = start
        self.end = end
    
    def __eq__(self, other):
        if type(other) is type(self):
            return self.__dict__ == other.__dict__
        else:
            return False
    
    def __hash__(self):
        return hash(tuple(self.__dict__.items()))
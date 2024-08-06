import random

from internals.point_genome.constants import POINT_NUM_POINT_COUPLES, POINT_NUM_ROOMS, POINT_MIN_ROOM_SIZE, POINT_MAX_ROOM_SIZE, \
    POINT_MAX_MAP_WIDTH, POINT_MAX_MAP_HEIGHT, POINT_CORRIDOR_WIDTH
import internals.point_genome.point_genome as point_genome

NO_POINT_COUPLE_PROBABILITY = 0.4
NO_ROOM_PROBABILITY = 0

def create_random_genome():
    point_couples = []
    rooms = []

    for x in range(POINT_NUM_POINT_COUPLES):
        point_couples.append(create_point_couple())
    for x in range(POINT_NUM_ROOMS):
        rooms.append(create_room())

    return point_genome.PointGenome(point_couples, rooms)


def create_room():
    if random.random() < NO_ROOM_PROBABILITY:
        return None
    size = random.randint(POINT_MIN_ROOM_SIZE, POINT_MAX_ROOM_SIZE - 1)
    left_col = random.randint(1, POINT_MAX_MAP_WIDTH - size - 1)
    bottom_row = random.randint(1, POINT_MAX_MAP_HEIGHT - size - 1)
    return point_genome.PointRoom(left_col, bottom_row, size)


def create_point_couple():
    if random.random() < NO_POINT_COUPLE_PROBABILITY:
        return None
    point_one = (random.randint(1, POINT_MAX_MAP_WIDTH - 1 - POINT_CORRIDOR_WIDTH), random.randint(1, POINT_MAX_MAP_HEIGHT - 1 - POINT_CORRIDOR_WIDTH))
    point_two = (random.randint(1, POINT_MAX_MAP_WIDTH - 1 - POINT_CORRIDOR_WIDTH), random.randint(1, POINT_MAX_MAP_HEIGHT - 1 - POINT_CORRIDOR_WIDTH))
    # Reorder points so that point_one is always the one with the smallest x value and if x is the same, the smallest y value
    if point_one[0] > point_two[0] or (point_one[0] == point_two[0] and point_one[1] > point_two[1]):
        temp = point_one
        point_one = point_two
        point_two = temp

    connection = 1#random.randint(0, 1)
    return point_genome.PointPointCouple(point_one, point_two, connection)
import random

from internals.constants import GENOME_NUM_ROWS, GENOME_NUM_COLUMNS, GENOME_MAX_ROOM_HEIGHT, GENOME_MAX_ROOM_WIDTH, \
    GENOME_MIN_ROOM_HEIGHT, GENOME_MIN_ROOM_WIDTH, GENOME_MAP_SCALE
from .room import Room

FAKE_ROOM_PROBABILITY = 0.2
MAX_ROOM_SIZE_PROBABILITY = 0.08
NO_CORRIDOR_PROBABILITY = 0.3


def create_full_room():
    left_col = 0
    bottom_row = 0
    right_col = GENOME_MAX_ROOM_WIDTH - 1
    top_row = GENOME_MAX_ROOM_HEIGHT - 1
    return Room(left_col, right_col, bottom_row, top_row)


def create_empty_genome(individual_init):
    rooms = []
    for row in range(GENOME_NUM_ROWS):
        rooms.append([])
        for col in range(GENOME_NUM_COLUMNS):
            rooms[row].append(create_full_room())

    vertical_corridors = __create_corridors(GENOME_NUM_ROWS - 1, GENOME_NUM_COLUMNS, 0)
    horizontal_corridors = __create_corridors(GENOME_NUM_ROWS, GENOME_NUM_COLUMNS - 1, 0)
    map_scale = 3.0
    return individual_init(rooms, vertical_corridors, horizontal_corridors, map_scale)


def create_random_genome(individual_init):
    rooms = __create_rooms()
    vertical_corridors = __create_corridors(GENOME_NUM_ROWS - 1, GENOME_NUM_COLUMNS, NO_CORRIDOR_PROBABILITY)
    horizontal_corridors = __create_corridors(GENOME_NUM_ROWS, GENOME_NUM_COLUMNS - 1, NO_CORRIDOR_PROBABILITY)
    map_scale = GENOME_MAP_SCALE
    return individual_init(rooms, vertical_corridors, horizontal_corridors, map_scale)


def __create_rooms():
    rooms = []
    for row in range(GENOME_NUM_ROWS):
        rooms.append([])
        for col in range(GENOME_NUM_COLUMNS):
            rooms[row].append(create_room())
    return rooms


def create_room():
    if random.random() < FAKE_ROOM_PROBABILITY:
        return Room()
    else:
        if random.random() < MAX_ROOM_SIZE_PROBABILITY:
            width = GENOME_MAX_ROOM_WIDTH
            height = GENOME_MAX_ROOM_HEIGHT
        else:
            width = random.randint(GENOME_MIN_ROOM_WIDTH, GENOME_MAX_ROOM_WIDTH)
            height = random.randint(GENOME_MIN_ROOM_HEIGHT, GENOME_MAX_ROOM_HEIGHT)

    left_col = random.randint(0, GENOME_MAX_ROOM_WIDTH - width)
    bottom_row = random.randint(0, GENOME_MAX_ROOM_HEIGHT - height)

    right_col = left_col + width
    top_row = bottom_row + height

    return Room(left_col, right_col, bottom_row, top_row)

# def create_room():
#     if random.random() < FAKE_ROOM_PROBABILITY:
#         return Room()
#     else:
#         if random.random() < MAX_ROOM_SIZE_PROBABILITY:
#             width = GENOME_MAX_ROOM_WIDTH
#             height = GENOME_MAX_ROOM_HEIGHT
#         else:
#             width = random.randint(GENOME_MIN_ROOM_WIDTH, GENOME_MAX_ROOM_WIDTH)
#             height = random.randint(GENOME_MIN_ROOM_HEIGHT, GENOME_MAX_ROOM_HEIGHT)
#
#         left_col = random.randint(0, GENOME_MAX_ROOM_WIDTH - width)
#         bottom_row = random.randint(0, GENOME_MAX_ROOM_HEIGHT - height)
#
#         right_col = left_col + width
#         top_row = bottom_row + height
#
#         return Room(left_col, right_col, bottom_row, top_row)


def __create_corridors(rows, columns, probability):
    corridors = []
    for row in range(rows):
        corridors.append([])
        for col in range(columns):
            corridors[row].append(random.random() > probability)
    return corridors

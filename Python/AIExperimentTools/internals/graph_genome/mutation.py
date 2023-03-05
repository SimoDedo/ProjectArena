import random

from internals.constants import GENOME_NUM_COLUMNS, GENOME_NUM_ROWS, GENOME_MIN_ROOM_WIDTH, GENOME_MAX_ROOM_WIDTH, \
    GENOME_MAX_ROOM_HEIGHT, GENOME_MIN_ROOM_HEIGHT
from .generation import create_room
from .room import Room

__MUTATE_ROW_PROBABILITY = 0.3
__MUTATE_COLUMN_PROBABILITY = 0.3


def __mutated_room(room):
    if room.isReal:
        return Room()
    width = random.randint(GENOME_MIN_ROOM_WIDTH, GENOME_MAX_ROOM_WIDTH)
    height = random.randint(GENOME_MIN_ROOM_HEIGHT, GENOME_MAX_ROOM_HEIGHT)
#
    left_col = random.randint(0, GENOME_MAX_ROOM_WIDTH - width)
    bottom_row = random.randint(0, GENOME_MAX_ROOM_HEIGHT - height)
#
    right_col = left_col + width
    top_row = bottom_row + height
#
    return Room(left_col, right_col, bottom_row, top_row)


def mutate(individual):
    for row in range(0, GENOME_NUM_ROWS - 1):
        for col in range(0, GENOME_NUM_COLUMNS - 1):
            if random.random() < __MUTATE_ROW_PROBABILITY:
                individual.rooms[row][col] = __mutated_room(individual.rooms[row][col])
                # individual.rooms[row][col] = create_room()

    for row in range(0, GENOME_NUM_ROWS - 2):
        for col in range(0, GENOME_NUM_COLUMNS - 1):
            if random.random() < __MUTATE_COLUMN_PROBABILITY:
                individual.verticalCorridors[row][col] = not individual.verticalCorridors[row][col]

    for row in range(0, GENOME_NUM_ROWS - 1):
        for col in range(0, GENOME_NUM_COLUMNS - 2):
            if random.random() < __MUTATE_COLUMN_PROBABILITY:
                individual.horizontalCorridors[row][col] = not individual.horizontalCorridors[row][col]

    return individual,

# # select switch start point
    # start_row = random.randint(0, GENOME_NUM_ROWS - 2)
    # end_row = random.randint(start_row + 1, GENOME_NUM_ROWS - 1)
    # start_column = random.randint(0, GENOME_NUM_COLUMNS - 2)
    # end_column = random.randint(start_column + 1, GENOME_NUM_COLUMNS - 1)
    #
    # # Rooms
    # for row in range(start_row, end_row):
    #     for col in range(start_column, end_column):
    #         individual.rooms[row][col] = create_room()
    #
    # # Horizontal corridors
    # for row in range(start_row, end_row):
    #     for col in range(start_column, min(end_column, GENOME_NUM_COLUMNS - 2)):
    #         individual.horizontalCorridors[row][col] = random.random() > NO_CORRIDOR_PROBABILITY
    #
    # # Vertical corridors
    # for row in range(start_row, min(end_row, GENOME_NUM_ROWS - 2)):
    #     for col in range(start_column, end_column):
    #         individual.verticalCorridors[row][col] = random.random() > NO_CORRIDOR_PROBABILITY
    #
    # return individual,

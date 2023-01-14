import random

from . import generation
from .constants import GENOME_NUM_COLUMNS, GENOME_NUM_ROWS

PROBABILITY_ROOM_RESIZE = 0.25
PROBABILITY_CORRIDOR_FLIP = 0.25


def mutate(individual):
    # Tried to remove if and increase mutation probability overall.
    mutate_resize_rooms(individual)
    mutate_flip_corridors(individual)
    return individual,


def mutate_resize_rooms(individual):
    # Find new way to mutate genome.
    # Might makes sense to have different mutation system with different activation probabilities

    invalidate_fitness = False
    for row in range(GENOME_NUM_ROWS):
        for col in range(GENOME_NUM_COLUMNS):
            if random.random() < PROBABILITY_ROOM_RESIZE:
                invalidate_fitness = True
                individual.rooms[row][col] = generation.create_room()
 
    return invalidate_fitness


def mutate_flip_corridors(individual):
    # Horizontal corridors
    invalidate_fitness = False
    for row in range(GENOME_NUM_ROWS):
        for col in range(GENOME_NUM_COLUMNS - 1):
            if random.random() < PROBABILITY_CORRIDOR_FLIP:
                invalidate_fitness = True
                individual.horizontalCorridors[row][col] = not individual.horizontalCorridors[row][col]

    # Vertical corridors
    for row in range(GENOME_NUM_ROWS - 1):
        for col in range(GENOME_NUM_COLUMNS):
            if random.random() < PROBABILITY_CORRIDOR_FLIP:
                invalidate_fitness = True
                individual.verticalCorridors[row][col] = not individual.verticalCorridors[row][col]
    return invalidate_fitness


def __assign_room_values(room, is_real, left_col, right_col, bottom_row, top_row):
    room.isReal = is_real
    room.leftColumn = left_col
    room.rightColumn = right_col
    room.bottomRow = bottom_row
    room.topRow = top_row


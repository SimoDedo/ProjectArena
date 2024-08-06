import random

from internals.point_genome.constants import POINT_NUM_POINT_COUPLES, POINT_NUM_ROOMS, POINT_MIN_ROOM_SIZE, POINT_MAX_ROOM_SIZE
import internals.point_genome.generation as generation

MUTATE_PROBABILITY = 0.2


def mutate(individual):
    for x in range(POINT_NUM_ROOMS):
        if random.random() < MUTATE_PROBABILITY:
            individual.rooms[x] = generation.create_room()

    for x in range(POINT_NUM_POINT_COUPLES):
        if random.random() < MUTATE_PROBABILITY:
            individual.point_couples[x] = generation.create_point_couple()

    return individual



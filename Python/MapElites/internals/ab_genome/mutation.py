import random

from internals.ab_genome.constants import AB_NUM_ROOMS, AB_NUM_CORRIDORS
import internals.ab_genome.generation as generation

MUTATE_PROBABILITY = 0.2


def mutate(individual):
    for x in range(AB_NUM_ROOMS):
        if random.random() < MUTATE_PROBABILITY:
            individual.rooms[x] = generation.create_room()

    for x in range(AB_NUM_CORRIDORS):
        if random.random() < MUTATE_PROBABILITY:
            individual.corridors[x] = generation.create_corridor()

    return individual

def mutate_array(individual):
    for x in range(AB_NUM_ROOMS):
        if random.random() < MUTATE_PROBABILITY:
            individual.rooms[x] = generation.create_room()

    for x in range(AB_NUM_CORRIDORS):
        if random.random() < MUTATE_PROBABILITY:
            individual.corridors[x] = generation.create_corridor()

    return individual,


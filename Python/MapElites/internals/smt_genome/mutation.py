import random

from internals.smt_genome.constants import SMT_ROOMS_NUMBER, SMT_LINES_NUMBER, SMT_MIN_SEPARATION, SMT_MAX_SEPARATION
import internals.smt_genome.generation as generation

MUTATE_PROBABILITY_ROOM = 0.2
MUTATE_PROBABILITY_LINE = 0.4
MUTATE_PROBABILITY_SEPARATION = 0.2


def mutate(individual):
    for x in range(SMT_ROOMS_NUMBER):
        if random.random() < MUTATE_PROBABILITY_ROOM:
            individual.rooms[x] = generation.create_room()

    for x in range(SMT_LINES_NUMBER):
        if random.random() < MUTATE_PROBABILITY_LINE:
            individual.lines[x] = generation.create_line()

    if random.random() < MUTATE_PROBABILITY_SEPARATION:
        individual.separation = random.randint(SMT_MIN_SEPARATION, SMT_MAX_SEPARATION)

    return individual



import random

import tqdm

from internals.smt_genome.constants import SMT_ROOMS_NUMBER, SMT_LINES_NUMBER


def crossover(ind1, ind2):

    # Two point crossover
    point_1 = random.randint(0, SMT_ROOMS_NUMBER - 2)
    point_2 = random.randint(point_1 + 1, SMT_ROOMS_NUMBER - 1)

    for x in range(point_1, point_2):
        t = ind1.rooms[x]
        ind1.rooms[x] = ind2.rooms[x]
        ind2.rooms[x] = t

    # Two point crossover
    point_1 = random.randint(0, SMT_LINES_NUMBER - 2)
    point_2 = random.randint(point_1 + 1, SMT_LINES_NUMBER - 1)

    for x in range(point_1, point_2):
        t = ind1.lines[x]
        ind1.lines[x] = ind2.lines[x]
        ind2.lines[x] = t

    # Switch separation
    t = ind1.separation
    ind1.separation = ind2.separation
    ind2.separation = t

    return ind1, ind2
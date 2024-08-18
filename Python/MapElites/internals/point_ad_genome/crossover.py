import random

import tqdm

from internals.point_ad_genome.constants import POINT_AD_NUM_POINT_COUPLES


def crossover(ind1, ind2):
    if ind1.phenotype() == ind2.phenotype():
        tqdm.tqdm.write("Breed individuals impossible, they are the same!")
        return ind1, ind2

    # Two point crossover
    point_1 = random.randint(0, POINT_AD_NUM_POINT_COUPLES - 2)
    point_2 = random.randint(point_1 + 1, POINT_AD_NUM_POINT_COUPLES - 1)

    for x in range(point_1, point_2):
        t = ind1.point_couples[x]
        ind1.point_couples[x] = ind2.point_couples[x]
        ind2.point_couples[x] = t

    return ind1, ind2
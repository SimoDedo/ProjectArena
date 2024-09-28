import random

import tqdm

from internals.point_ad_genome.constants import POINT_AD_NUM_POINT_COUPLES
from internals.point_ad_genome.generation import NO_MIN_ROOMS

def crossover(ind1, ind2):
    if ind1.phenotype() == ind2.phenotype():
        tqdm.tqdm.write("Breed individuals impossible, they are the same!")
        return ind1, ind2

    rooms1 = []
    rooms2 = []
    while (len(ind1.point_couples) < 1 or len(rooms1) < NO_MIN_ROOMS) or (len(ind2.point_couples) < 1 or len(rooms2) < NO_MIN_ROOMS):
        # Two point crossover
        point_1 = random.randint(0, POINT_AD_NUM_POINT_COUPLES - 2)
        point_2 = random.randint(point_1 + 1, POINT_AD_NUM_POINT_COUPLES - 1)

        for x in range(point_1, point_2):
            t = ind1.point_couples[x]
            ind1.point_couples[x] = ind2.point_couples[x]
            ind2.point_couples[x] = t
        
        rooms1 = [point_couple.room_left for point_couple in ind1.point_couples if point_couple is not None and point_couple.room_left is not None]
        rooms1 += [point_couple.room_right for point_couple in ind1.point_couples if point_couple is not None and point_couple.room_right is not None]
        rooms2 = [point_couple.room_left for point_couple in ind2.point_couples if point_couple is not None and point_couple.room_left is not None]
        rooms2 += [point_couple.room_right for point_couple in ind2.point_couples if point_couple is not None and point_couple.room_right is not None]
        if (len(ind1.point_couples) < 1 or len(rooms1) < NO_MIN_ROOMS) or (len(ind2.point_couples) < 1 or len(rooms2) < NO_MIN_ROOMS):
            tqdm.tqdm.write("No rooms remain after crossover, trying again")

    return ind1, ind2
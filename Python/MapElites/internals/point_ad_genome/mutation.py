import random

import tqdm

from internals.point_ad_genome.constants import POINT_AD_NUM_POINT_COUPLES, POINT_AD_MIN_ROOM_RADIUS, POINT_AD_MAX_ROOM_RADIUS
import internals.point_ad_genome.generation as generation

MUTATE_PROBABILITY_POINT_AD_COUPLE = 0.3
MUTATE_PROBABILITY_POINT_ROOM = 0.1

def mutate(individual):
    rooms = []
    while len(individual.point_couples) < 1 or len(rooms) < generation.NO_MIN_ROOMS:
        for x in range(POINT_AD_NUM_POINT_COUPLES):
            if random.random() < MUTATE_PROBABILITY_POINT_AD_COUPLE:
                individual.point_couples[x] = generation.create_point_couple()
            if random.random() < MUTATE_PROBABILITY_POINT_ROOM:
                if individual.point_couples[x] is not None:
                    individual.point_couples[x].room_left = generation.create_room_for_point(individual.point_couples[x].point_left)
            if random.random() < MUTATE_PROBABILITY_POINT_ROOM:
                if individual.point_couples[x] is not None:
                    individual.point_couples[x].room_right = generation.create_room_for_point(individual.point_couples[x].point_right)

        rooms = [point_couple.room_left for point_couple in individual.point_couples if point_couple is not None and point_couple.room_left is not None]
        rooms += [point_couple.room_right for point_couple in individual.point_couples if point_couple is not None and point_couple.room_right is not None]
        if len(individual.point_couples) < 1 or len(rooms) < generation.NO_MIN_ROOMS:
            tqdm.tqdm.write("No rooms remain after mutation, trying again")

    return individual



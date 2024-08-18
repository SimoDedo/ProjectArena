import random

from internals.point_ad_genome.constants import POINT_AD_NUM_POINT_COUPLES, POINT_AD_MIN_ROOM_RADIUS, POINT_AD_MAX_ROOM_RADIUS
import internals.point_ad_genome.generation as generation

MUTATE_PROBABILITY_POINT_AD_COUPLE = 0.3
MUTATE_PROBABILITY_POINT_ROOM = 0.1


def mutate(individual):
    for x in range(POINT_AD_NUM_POINT_COUPLES):
        if random.random() < MUTATE_PROBABILITY_POINT_AD_COUPLE:
            individual.point_couples[x] = generation.create_point_couple()
        if random.random() < MUTATE_PROBABILITY_POINT_ROOM:
            if individual.point_couples[x] is not None:
                individual.point_couples[x].room_left = generation.create_room_for_point(individual.point_couples[x].point_left)
        if random.random() < MUTATE_PROBABILITY_POINT_ROOM:
            if individual.point_couples[x] is not None:
                individual.point_couples[x].room_right = generation.create_room_for_point(individual.point_couples[x].point_right)

    return individual



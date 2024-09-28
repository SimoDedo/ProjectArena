import random

import tqdm

from internals.point_ad_genome.constants import POINT_AD_NUM_POINT_COUPLES, POINT_AD_MIN_ROOM_RADIUS, POINT_AD_MAX_ROOM_RADIUS, \
    POINT_AD_MAX_MAP_WIDTH, POINT_AD_MAX_MAP_HEIGHT, POINT_AD_CORRIDOR_WIDTH
import internals.point_ad_genome.point_ad_genome as point_ad_genome

NO_POINT_AD_COUPLE_PROBABILITY = 0.6
NO_ROOM_PROBABILITY = 0.1

NO_MIN_ROOMS = 1

def create_random_genome():
    point_couples = []
    rooms = []
    while len(point_couples) < 1 or len(rooms) < NO_MIN_ROOMS:
        for x in range(POINT_AD_NUM_POINT_COUPLES):
            point_couples.append(create_point_couple())
        rooms = [point_couple.room_left for point_couple in point_couples if point_couple is not None and point_couple.room_left is not None]
        rooms += [point_couple.room_right for point_couple in point_couples if point_couple is not None and point_couple.room_right is not None]
        if len(point_couples) < 1 or len(rooms) < NO_MIN_ROOMS:
            tqdm.tqdm.write("No rooms created at generation, trying again")

    return point_ad_genome.PointAdGenome(point_couples)


def create_point_couple():
    if random.random() < NO_POINT_AD_COUPLE_PROBABILITY:
        return None
    point_left = (random.randint(1, POINT_AD_MAX_MAP_WIDTH - 1 - POINT_AD_CORRIDOR_WIDTH), random.randint(1, POINT_AD_MAX_MAP_HEIGHT - 1 - POINT_AD_CORRIDOR_WIDTH))
    point_right = (random.randint(1, POINT_AD_MAX_MAP_WIDTH - 1 - POINT_AD_CORRIDOR_WIDTH), random.randint(1, POINT_AD_MAX_MAP_HEIGHT - 1 - POINT_AD_CORRIDOR_WIDTH))
    # Reorder points so that point_one is always the one with the smallest x value and if x is the same, the smallest y value
    if point_left[0] > point_right[0] or (point_left[0] == point_right[0] and point_left[1] > point_right[1]):
        temp = point_left
        point_left = point_right
        point_right = temp

    connection = 1#random.randint(0, 1)

    room_left = create_room_for_point(point_left)
    room_right = create_room_for_point(point_right)

    return point_ad_genome.PointAdPointCouple(point_left, point_right, room_left, room_right, connection)

def create_room_for_point(point):
    if random.random() < NO_ROOM_PROBABILITY:
        return None
    radius = __get_radius(point)
    if radius < POINT_AD_MIN_ROOM_RADIUS:
        return None
    return point_ad_genome.PointAdRoom(point[0] - radius, point[1] - radius, 2 * radius)

def __get_radius(point):
    radius = random.randint(POINT_AD_MIN_ROOM_RADIUS, POINT_AD_MAX_ROOM_RADIUS)
    return min(radius ,point[0], point[1], POINT_AD_MAX_MAP_WIDTH - point[0], POINT_AD_MAX_MAP_HEIGHT - point[1])
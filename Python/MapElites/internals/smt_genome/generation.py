import random
import numpy as np

from internals.smt_genome.constants import SMT_ROOMS_NUMBER, SMT_MAX_MAP_WIDTH, SMT_MAX_MAP_HEIGHT, SMT_MAX_ROOM_HEIGHT, SMT_MIN_ROOM_HEIGHT, SMT_LINES_NUMBER \
    , SMT_MAX_ROOM_WIDTH, SMT_MIN_ROOM_WIDTH, SMT_MIN_SEPARATION, SMT_MAX_SEPARATION, SMT_MIN_LINES_LENGTH
import internals.smt_genome.smt_genome as smt_genome

NO_ROOM_PROBABILITY = 0.3
NO_LINE_PROBABILITY = 0.3

def create_random_genome():
    rooms = []
    lines = []

    for x in range(SMT_ROOMS_NUMBER):
        rooms.append(create_room())
    for x in range(SMT_LINES_NUMBER):
        lines.append(create_line())
    separation = random.randint(SMT_MIN_SEPARATION, SMT_MAX_SEPARATION)

    return smt_genome.SMTGenome(rooms, lines, separation)


def create_room():
    if random.random() < NO_ROOM_PROBABILITY:
        return None
    else :
        width = random.randint(SMT_MIN_ROOM_WIDTH, SMT_MAX_ROOM_WIDTH)
        height = random.randint(SMT_MIN_ROOM_HEIGHT, SMT_MAX_ROOM_HEIGHT)
        return smt_genome.SMTRoom(width=width, height=height)

def create_line():
    if random.random() < NO_LINE_PROBABILITY:
        return None
    else:
        start = np.array([0,0])
        end = np.array([0,0])
        while np.linalg.norm(end - start) < SMT_MIN_LINES_LENGTH:
            start = np.array([random.randint(0, SMT_MAX_MAP_WIDTH), random.randint(0, SMT_MAX_MAP_HEIGHT)])
            end = np.array([random.randint(0, SMT_MAX_MAP_WIDTH), random.randint(0, SMT_MAX_MAP_HEIGHT)])
        return smt_genome.SMTLine(start=(start[0], start[1]), end=(end[0], end[1]))

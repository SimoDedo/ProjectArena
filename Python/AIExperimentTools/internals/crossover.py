import random

from .constants import GENOME_NUM_COLUMNS, GENOME_NUM_ROWS


def crossover(ind1, ind2):
    if ind1.rooms == ind2.rooms:
        print("Breed individuals impossible, they are the same!")
        return ind1, ind2

    swapped_successfully = False
    while not swapped_successfully:
        print("Attempt to breed individuals!")
        room1 = ind1.rooms
        room2 = ind2.rooms

        # select switch start point
        start_row = random.randint(0, GENOME_NUM_ROWS - 2)
        end_row = random.randint(start_row + 1, GENOME_NUM_ROWS - 1)
        start_column = random.randint(0, GENOME_NUM_COLUMNS - 2)
        end_column = random.randint(start_column + 1, GENOME_NUM_COLUMNS - 1)

        for row in range(start_row, end_row):
            for col in range(start_column, end_column):
                # swap the two rooms
                if room1[row][col] != room2[row][col]:
                    swapped_successfully = True
                t = room1[row][col]
                room1[row][col] = room2[row][col]
                room2[row][col] = t

    print("Breed individuals success!")
    return ind1, ind2

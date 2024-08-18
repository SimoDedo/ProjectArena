from z3 import *
from internals.ab_genome.ab_genome import ABGenome
from internals.graph_genome.gg_genome import GraphGenome
from internals.smt_genome.smt_genome import SMTGenome
import matplotlib.pyplot as plt
import numpy as np
import time

def analyze_symmetry(map_matrix):
    # In an image, X is the VERTICAL axis and Y is the HORIZONTAL axis

    # Copy the map 
    x_map = np.copy(map_matrix)
    y_map = np.copy(map_matrix)

    x_len = len(map_matrix)
    y_len = len(map_matrix[0])

    # Get minimum and maximum coordinates that hold 1
    min_x, max_x, min_y, max_y = x_len, 0, x_len, 0
    for x in range(len(map_matrix)):
        for y in range(len(map_matrix[0])):
            if map_matrix[x][y] == 1:
                min_x = min(min_x, x)
                max_x = max(max_x, x)
                min_y = min(min_y, y)
                max_y = max(max_y, y)
    x_len = max_x - min_x
    y_len = max_y - min_y

    total_tiles = np.count_nonzero(map_matrix)

    x_symmetry = 0
    y_mid_point = int(np.floor(y_len/2))
    for x in range(min_x, max_x + 1):
        for y in range(min_y, min_y + y_mid_point + 1):
            opposite_y = max_y - (y - min_y)
            if map_matrix[x][y] == map_matrix[x][opposite_y]:
                if map_matrix[x][y] != 0:
                    x_symmetry += 2 if y != opposite_y else 1
                    x_map[x][y] = 2
                    x_map[x][opposite_y] = 2

    y_symmetry = 0
    x_mid_point = int(np.floor(x_len/2))
    for y in range(min_y, max_y + 1):
        for x in range(min_x, min_x + x_mid_point + 1):
            opposite_x = max_x - (x - min_x)
            if map_matrix[x][y] == map_matrix[opposite_x][y]:
                if map_matrix[x][y] != 0:
                    y_symmetry += 2 if x != opposite_x else 1
                    y_map[x][y] = 2
                    y_map[opposite_x][y] = 2



    x_symmetry /= total_tiles
    y_symmetry /= total_tiles
    max_symmetry = max(x_symmetry, y_symmetry)

    return x_symmetry, y_symmetry, max_symmetry, x_map, y_map

if __name__ == "__main__":
    genome = ABGenome.create_random_genome()
    phenotype = genome.phenotype()
    map_matrix = phenotype.map_matrix()
    x_symmetry, y_symmetry, max_symmetry, x_map, y_map = analyze_symmetry(map_matrix)

    # Draw the map matrix
    #fig, ax = plt.subplots()
    #ax.imshow(map_matrix, cmap='binary', interpolation='nearest', zorder = 1)
    
    fig, ax = plt.subplots()
    plt.contour(x_map, alpha=1, cmap='binary', zorder=0)
    plt.gca().invert_yaxis()
    plt.annotate(f"x_symmetry: {x_symmetry}", xy=(.025, .975), xycoords='figure fraction',
            horizontalalignment='left', verticalalignment='top',
            fontsize=10)
    
    fig, ax = plt.subplots()
    plt.contour(y_map, alpha=1, cmap='binary', zorder=0)
    plt.gca().invert_yaxis()
    plt.annotate(f"y_symmetry: {y_symmetry}", xy=(.025, .975), xycoords='figure fraction',
            horizontalalignment='left', verticalalignment='top',
            fontsize=10)

    plt.show()

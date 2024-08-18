from z3 import *
from internals.ab_genome.ab_genome import ABGenome
from internals.graph_genome.gg_genome import GraphGenome
from internals.smt_genome.smt_genome import SMTGenome
from internals.point_genome.point_genome import PointGenome
from internals.point_ad_genome.point_ad_genome import PointAdGenome
import matplotlib.pyplot as plt
import numpy as np
import igraph as ig
from matplotlib import cm

if __name__ == "__main__":
    # Create random SMTGenome
    genome = PointAdGenome.create_random_genome()
    phenotype = genome.phenotype()
    map_matrix = phenotype.map_matrix()

    # Draw the phenotype
    fig, ax = plt.subplots()
    for area in phenotype.areas:
        if area.isCorridor:
            ax.add_patch(plt.Rectangle((area.leftColumn, area.topRow), area.rightColumn-area.leftColumn, area.bottomRow-area.topRow, fill='b', edgecolor='b'))
        else:
            ax.add_patch(plt.Rectangle((area.leftColumn, area.topRow), area.rightColumn-area.leftColumn, area.bottomRow-area.topRow, fill=None, edgecolor='r'))
    ax.set_xlim(0, phenotype.mapWidth)
    ax.set_ylim(0, phenotype.mapHeight)

    for cp in genome.point_couples:
        if cp is not None:
            if cp.connection == 0:
                x1, y1 = [cp.point_left[0], cp.point_right[0]], [cp.point_left[1], cp.point_left[1]]
                plt.plot(x1, y1, 'ro-')
                x1, y1 = [cp.point_right[0], cp.point_right[0]], [cp.point_left[1], cp.point_right[1]]
                plt.plot(x1, y1, 'ro-')
            else:
                x1, y1 = [cp.point_left[0], cp.point_left[0]], [cp.point_left[1], cp.point_right[1]]
                plt.plot(x1, y1, 'ro-')
                x1, y1 = [cp.point_left[0], cp.point_right[0]], [cp.point_right[1], cp.point_right[1]]
                plt.plot(x1, y1, 'ro-')
    plt.axis('off')

    # Draw the map matrix
    fig, ax = plt.subplots()
    ax.imshow(map_matrix, cmap='gray', interpolation='nearest')
    plt.axis('off')
    plt.gca().invert_yaxis()

    plt.show()    

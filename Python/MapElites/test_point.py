from z3 import *
from internals.ab_genome.ab_genome import ABGenome
from internals.graph_genome.gg_genome import GraphGenome
from internals.smt_genome.smt_genome import SMTGenome
from internals.point_genome.point_genome import PointGenome
from internals.point_ad_genome.point_ad_genome import PointAdGenome, PointAdPointCouple, PointAdRoom
import matplotlib.pyplot as plt
import numpy as np
import igraph as ig
from matplotlib import cm

if __name__ == "__main__":
    # Create random SMTGenome
    genome = PointAdGenome.create_random_genome()

    point1 = (10, 10)
    point2 = (40, 40)
    size = 10
    room1 = PointAdRoom(point1[0] - size//2, point1[1]- size//2, size)
    size = 8
    room2 = PointAdRoom(point2[0] - size//2, point2[1]- size//2, size)
    point_couple = PointAdPointCouple(point_left=point1, point_right=point2, room_left=room1, room_right=room2 ,connection=0)

    point3 = (25, 55)
    point4 = (35, 15)
    size = 12
    room3 = PointAdRoom(point3[0] - size//2, point3[1] - size//2, size)
    room4 = PointAdRoom(point4[0] - size//2, point4[1] - size//2, size)
    size = 14
    point_couple2 = PointAdPointCouple(point_left=point3, point_right=point4, room_left=room3, room_right=room4 ,connection=1)

    genome = PointAdGenome(point_couples=[point_couple, point_couple2])
    phenotype = genome.phenotype()
    map_matrix = phenotype.map_matrix()

    # Draw the phenotype
    fig, ax = plt.subplots()
    for area in phenotype.areas:
        if area.isCorridor:
            ax.add_patch(plt.Rectangle((area.leftColumn, area.topRow), area.rightColumn-area.leftColumn, area.bottomRow-area.topRow, fill=None, edgecolor='b'))
        else:
            ax.add_patch(plt.Rectangle((area.leftColumn, area.topRow), area.rightColumn-area.leftColumn, area.bottomRow-area.topRow, fill=None, edgecolor='r'))
    ax.set_xlim(0, phenotype.mapWidth+1)
    ax.set_ylim(0, phenotype.mapHeight+1)

    for cp in genome.point_couples:
        if cp is not None:
            if cp.connection == 0:
                x1, y1 = [cp.point_left[0], cp.point_right[0]], [cp.point_left[1], cp.point_left[1]]
                plt.plot(x1, y1, 'b-')
                x1, y1 = [cp.point_right[0], cp.point_right[0]], [cp.point_left[1], cp.point_right[1]]
                plt.plot(x1, y1, 'b-')
                # plot the points
                plt.plot(cp.point_left[0], cp.point_left[1], 'bo')
                plt.plot(cp.point_right[0], cp.point_right[1], 'bo')
            else:
                x1, y1 = [cp.point_left[0], cp.point_left[0]], [cp.point_left[1], cp.point_right[1]]
                plt.plot(x1, y1, 'b-')
                x1, y1 = [cp.point_left[0], cp.point_right[0]], [cp.point_right[1], cp.point_right[1]]
                plt.plot(x1, y1, 'b-')
                # plot the points
                plt.plot(cp.point_left[0], cp.point_left[1], 'bo')
                plt.plot(cp.point_right[0], cp.point_right[1], 'bo')

    # Draw the map matrix
    fig, ax = plt.subplots()
    ax.imshow(map_matrix, cmap='gray', interpolation='nearest')
    #plt.gca().invert_yaxis()

    plt.show()    

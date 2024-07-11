from z3 import *
from internals.ab_genome.ab_genome import ABGenome
from internals.graph_genome.gg_genome import GraphGenome
from internals.smt_genome.smt_genome import SMTGenome
import matplotlib.pyplot as plt
import numpy as np
import igraph as ig
from matplotlib import cm

if __name__ == "__main__":
    # Create random SMTGenome
    genome = SMTGenome.create_random_genome()
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
    for line in genome.lines:
        if line is not None:
            x1, y1 = [line.start[0], line.end[0]], [line.start[1], line.end[1]]
            plt.plot(x1, y1, 'ro-')
    plt.axis('off')
    plt.show()    

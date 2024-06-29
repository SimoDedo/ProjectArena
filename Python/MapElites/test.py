from internals.ab_genome.ab_genome import ABGenome
from internals.graph_genome.gg_genome import GraphGenome
import matplotlib.pyplot as plt
import numpy as np
import igraph as ig

if __name__ == "__main__":
    # Create random ABGenome
    genome = ABGenome.create_random_genome()
    phenotype = genome.phenotype()
    phenotype.simplify()

    # Use matplotlib to draw each area as a square indipendently
    fig, ax = plt.subplots()
    for area in phenotype.areas:
        if area.isCorridor:
            ax.add_patch(plt.Rectangle((area.leftColumn, area.topRow), area.rightColumn-area.leftColumn, area.bottomRow-area.topRow, fill='b', edgecolor='b'))
        else:
            ax.add_patch(plt.Rectangle((area.leftColumn, area.topRow), area.rightColumn-area.leftColumn, area.bottomRow-area.topRow, fill=None, edgecolor='r'))
    ax.set_xlim(0, phenotype.mapWidth)
    ax.set_ylim(0, phenotype.mapHeight)

    g, layout = phenotype.to_graph()
    fig, ax = plt.subplots()
    ig.plot(g, vertex_size=10, layout=layout,  target=ax)
    plt.show()
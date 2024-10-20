import pandas as pd
from z3 import *
from internals import constants
from internals.ab_genome.ab_genome import ABGenome, ABRoom, ABCorridor
from internals.graph_genome.gg_genome import GraphGenome
from internals.phenotype import Phenotype
from internals.smt_genome.smt_genome import SMTGenome
import matplotlib.pyplot as plt
import numpy as np
import igraph as ig
from matplotlib import cm
import holoviews as hv
import numpy as np
import pandas as pd
hv.extension('bokeh')
from bokeh.plotting import show
import networkx as nx
from karateclub import Graph2Vec
import numpy as np
from internals.graph import to_rooms_only_graph


def ab_plot():
    room = ABRoom(2, 2, 10)
    room2 = ABRoom(18, 2, 8)
    corridorH = ABCorridor(10, 7, 10)
    corridorV = ABCorridor(2, 10, -10)
    genome = ABGenome([room, room2], [corridorH, corridorV])
    phenotype = genome.phenotype()

    mapWidth = 30
    mapHeight = 25
    fig, ax = plt.subplots()

    for area in phenotype.areas:
        if area.isCorridor:
            ax.add_patch(plt.Rectangle((area.leftColumn, area.topRow), area.rightColumn-area.leftColumn, area.bottomRow-area.topRow, fill='b', edgecolor='b'))
        else:
            ax.add_patch(plt.Rectangle((area.leftColumn, area.topRow), area.rightColumn-area.leftColumn, area.bottomRow-area.topRow, fill=None, edgecolor='r'))
    # Annotate the plot with the room's parameter as <,x,y,z>, where x,y are the room's bottom left corner and z is the room's width in color red.
    for room in [room, room2]:
        ax.text(room.left_col, room.bottom_row, f"<{room.left_col},{room.bottom_row},{room.size}>", color='r', bbox=dict(facecolor='white', alpha=0.5))
    # Annotate the plot with the corridor's parameter as <,x,y,z>, where x,y are the corridor's bottom left corner and z is the corridor's width in color blue.
    for corridor in [corridorH, corridorV]:
        ax.text(corridor.left_col, corridor.bottom_row, f"<{corridor.left_col},{corridor.bottom_row},{corridor.length}>", color='b', bbox=dict(facecolor='white', alpha=0.5))
    
    ax.set_xlim(0, mapWidth)
    ax.set_ylim(0, mapHeight)
    #plt.gca().invert_yaxis()
    plt.savefig(os.path.join(constants.GAME_DATA_FOLDER, "ABGenome.png"))
    plt.show()
    plt.clf()
    plt.close()

if __name__ == "__main__":
    ab_plot()






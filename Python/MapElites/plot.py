import json
from pathlib import Path
import pickle
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
from ribs.archives import ArchiveDataFrame, SlidingBoundariesArchive

from ribs.visualize import sliding_boundaries_archive_heatmap
from internals import config as conf

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

def gg_plot():
    genome = GraphGenome.create_random_genome()
    phenotype = genome.phenotype()
    graph = to_rooms_only_graph(phenotype)
    pos = nx.spring_layout(graph)
    nx.draw(graph, pos, with_labels=True, font_weight='bold')
    plt.savefig(os.path.join(constants.GAME_DATA_FOLDER, "GGGenome.png"))
    plt.show()
    plt.clf()
    plt.close()

def merge_plots_me_results():
    outdir = os.path.join(constants.MAP_ELITES_OUTPUT_FOLDER, "Fin_1")
    os.makedirs(outdir, exist_ok=True)

    ab_experiment = "Fin_AB_ABEmitter_SB_entropy_area_maxSymmetry_I400_B1_E10"
    gg_experiment = "Fin_GG_GGEmitter_SB_entropy_area_maxSymmetry_I400_B1_E10"
    pointad_experiment = "Fin_PointAD_PointADEmitter_SB_entropy_area_maxSymmetry_I400_B1_E10"
    smt_experiment = "Fin_SMT_SMTEmitter_SB_entropy_area_maxSymmetry_I400_B1_E10"
    #ab_experiment = "Fin_AB_ABEmitter_SB_entropy_pace_averageEccentricity_I400_B1_E10"
    #gg_experiment = "Fin_GG_GGEmitter_SB_entropy_pace_averageEccentricity_I400_B1_E10"
    #pointad_experiment = "Fin_PointAD_PointADEmitter_SB_entropy_pace_averageEccentricity_I400_B1_E10"
    #smt_experiment = "Fin_SMT_SMTEmitter_SB_entropy_pace_averageEccentricity_I400_B1_E10"
    #ab_experiment = "Fin_AB_ABEmitter_SB_entropy_averageMincut_maxValuePosition_I400_B1_E10"
    #gg_experiment = "Fin_GG_GGEmitter_SB_entropy_averageMincut_maxValuePosition_I400_B1_E10"
    #pointad_experiment = "Fin_PointAD_PointADEmitter_SB_entropy_averageMincut_maxValuePosition_I400_B1_E10"
    #smt_experiment = "Fin_SMT_SMTEmitter_SB_entropy_averageMincut_maxValuePosition_I400_B1_E10"

    measure_names = ["area", "maxSymmetry"]
    
    ab_path = os.path.join(constants.MAP_ELITES_OUTPUT_FOLDER, ab_experiment)
    gg_path = os.path.join(constants.MAP_ELITES_OUTPUT_FOLDER, gg_experiment)
    pointad_path = os.path.join(constants.MAP_ELITES_OUTPUT_FOLDER, pointad_experiment)
    smt_path = os.path.join(constants.MAP_ELITES_OUTPUT_FOLDER, smt_experiment)

    ab_df = ArchiveDataFrame(pd.read_csv(os.path.join(ab_path, "archive.csv")))
    gg_df = ArchiveDataFrame(pd.read_csv(os.path.join(gg_path, "archive.csv")))
    pointad_df = ArchiveDataFrame(pd.read_csv(os.path.join(pointad_path, "archive.csv")))
    smt_df = ArchiveDataFrame(pd.read_csv(os.path.join(smt_path, "archive.csv")))

    #remove all lines with "objective" == 0
    ab_df = ab_df[ab_df["objective"] != 0]
    gg_df = gg_df[gg_df["objective"] != 0]
    pointad_df = pointad_df[pointad_df["objective"] != 0]
    smt_df = smt_df[smt_df["objective"] != 0]

    # Plot heatmaps    
    # TODO
    
    ab_metrics = json.load(open(os.path.join(ab_path, "metrics.json")))
    gg_metrics = json.load(open(os.path.join(gg_path, "metrics.json")))
    pointad_metrics = json.load(open(os.path.join(pointad_path, "metrics.json")))
    smt_metrics = json.load(open(os.path.join(smt_path, "metrics.json")))

    # Plot metrics

    for metric in ab_metrics:
        fig, ax = plt.subplots()
        ax.plot(ab_metrics[metric]["x"], ab_metrics[metric]["y"], label="AB", color='r')
        ax.plot(gg_metrics[metric]["x"], gg_metrics[metric]["y"], label="GG", color='g')
        ax.plot(pointad_metrics[metric]["x"], pointad_metrics[metric]["y"], label="Point", color='b')
        ax.plot(smt_metrics[metric]["x"], smt_metrics[metric]["y"], label="SMT", color='y')
        ax.legend()
        ax.set_title(metric)
        ax.set_xlabel("Iteration")
        fig.savefig(str(Path(outdir) / f"{metric.lower().replace(' ', '_')}.png"))

    # Plot ccdf

    fig, ax = plt.subplots()
    ax.hist(
        ab_df["objective"],
        50,  # Number of cells.
        histtype="step",
        density=True,  # Use density to get the percentage
        cumulative=-1,
        color="r",
        label="AB",
        )  # CCDF rather than CDF.
    ax.yaxis.set_major_formatter(plt.FuncFormatter(lambda y, _: '{:.0%}'.format(y)))
    ax.hist(
        gg_df["objective"],
        50,  # Number of cells.
        histtype="step",
        density=True,
        cumulative=-1,
        color="g",
        label="GG",
        )  # CCDF rather than CDF.
    ax.hist(
        pointad_df["objective"],
        50,  # Number of cells.
        histtype="step",
        density=True,
        cumulative=-1,
        color="b",
        label="Point",
        )  # CCDF rather than CDF.
    ax.hist(
        smt_df["objective"],
        50,  # Number of cells.
        histtype="step",
        density=True,
        cumulative=-1,
        color="y",
        label="SMT",
        )  # CCDF rather than CDF.
    ax.legend()
    ax.set_xlabel("Objectives")
    ax.set_ylabel("Num. Entries")
    ax.set_title("Distribution of Archive Objectives")
    fig.savefig(str(Path(outdir) / "archive_ccdf.png"))
                            
    # Sliding boundaries archive heatmap
    ab_archive = pickle.load(open(os.path.join(ab_path, "archive.pkl"), "rb"))
    gg_archive = pickle.load(open(os.path.join(gg_path, "archive.pkl"), "rb"))
    pointad_archive = pickle.load(open(os.path.join(pointad_path, "archive.pkl"), "rb"))
    smt_archive = pickle.load(open(os.path.join(smt_path, "archive.pkl"), "rb"))

    fig, ax = plt.subplots(figsize=(8, 6))
    sliding_boundaries_archive_heatmap(ab_archive, ax=ax, vmin=conf.OBJECTIVE_RANGE[0], vmax=conf.OBJECTIVE_RANGE[1], boundary_lw=0.5)
    ax.set_xlabel(measure_names[0])
    ax.set_ylabel(measure_names[1])
    fig.savefig(str(Path(outdir) / "heatmap_ab.png"))

    fig, ax = plt.subplots(figsize=(8, 6))
    sliding_boundaries_archive_heatmap(gg_archive, ax=ax, vmin=conf.OBJECTIVE_RANGE[0], vmax=conf.OBJECTIVE_RANGE[1], boundary_lw=0.5)
    ax.set_xlabel(measure_names[0])
    ax.set_ylabel(measure_names[1])
    fig.savefig(str(Path(outdir) / "heatmap_gg.png"))

    fig, ax = plt.subplots(figsize=(8, 6))
    sliding_boundaries_archive_heatmap(pointad_archive, ax=ax, vmin=conf.OBJECTIVE_RANGE[0], vmax=conf.OBJECTIVE_RANGE[1], boundary_lw=0.5)
    ax.set_xlabel(measure_names[0])
    ax.set_ylabel(measure_names[1])
    fig.savefig(str(Path(outdir) / "heatmap_pointad.png"))

    fig, ax = plt.subplots(figsize=(8, 6))
    sliding_boundaries_archive_heatmap(smt_archive, ax=ax, vmin=conf.OBJECTIVE_RANGE[0], vmax=conf.OBJECTIVE_RANGE[1], boundary_lw=0.5)
    ax.set_xlabel(measure_names[0])
    ax.set_ylabel(measure_names[1])
    fig.savefig(str(Path(outdir) / "heatmap_smt.png"))




if __name__ == "__main__":
    #ab_plot()

    #gg_plot()   

    merge_plots_me_results()

    #testname = "Fin_SMT_SMTEmitter_SB_entropy_peripheryPercent_averageRoomBetweenness_I400_B1_E10"
    #test_path = os.path.join(constants.MAP_ELITES_OUTPUT_FOLDER, testname)
#
    #df = ArchiveDataFrame(pd.read_csv(os.path.join(test_path, "archive.csv")))
    #archive = pickle.load(open(os.path.join(test_path, "archive.pkl"), "rb"))
#
    #outdir = os.path.join(constants.MAP_ELITES_OUTPUT_FOLDER, "test")
    #os.makedirs(outdir, exist_ok=True)
#
    #fig, ax = plt.subplots(figsize=(8, 6))
    #sliding_boundaries_archive_heatmap(archive, ax=ax, vmin=conf.OBJECTIVE_RANGE[0], vmax=conf.OBJECTIVE_RANGE[1], boundary_lw=0.5)
    #ax.set_xlabel(conf.MEASURES_NAMES[0])
    #ax.set_ylabel(conf.MEASURES_NAMES[1])
    #fig.savefig(str(Path(outdir) / "heatmap_t.png"))






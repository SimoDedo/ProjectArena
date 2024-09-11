"""
TODO: Add description of the file here.
"""
import argparse
from math import floor, sqrt
import os
from pathlib import Path
import sys

from matplotlib.colors import LinearSegmentedColormap

from internals.result_extractor import extract_bot_positions, extract_death_positions, extract_kill_positions, read_map
from internals.constants import ALL_BLACK_EMITTER_NAME, ALL_BLACK_NAME, ARCHIVE_ANALYSIS_OUTPUT_FOLDER, GAME_DATA_FOLDER, MAP_ELITES_OUTPUT_FOLDER
import internals.constants as constants
import internals.config as conf
from internals.ab_genome.constants import AB_MAP_SCALE
from internals.graph_genome.constants import GG_MAP_SCALE
from internals.smt_genome.constants import SMT_MAP_SCALE
from internals.point_genome.constants import POINT_MAP_SCALE
from internals.point_ad_genome.constants import POINT_AD_MAP_SCALE
from internals.ab_genome.ab_genome import ABGenome
from internals.graph_genome.gg_genome import GraphGenome
from internals.smt_genome.smt_genome import SMTGenome
from internals.point_genome.point_genome import PointGenome
from internals.point_ad_genome.point_ad_genome import PointAdGenome
import matplotlib.pyplot as plt
from matplotlib import cm
import pandas as pd
import numpy as np
from scipy.ndimage import gaussian_filter
import tqdm
import igraph as ig
import pickle
from internals.visibility import WALL_TILE, SPACE_TILE

from ribs.archives import ArchiveDataFrame

# --- UTILS --- #

def get_map_scale(representation):
    match representation:
        case constants.ALL_BLACK_NAME:
            return AB_MAP_SCALE
        case constants.GRID_GRAPH_NAME:
            return GG_MAP_SCALE
        case constants.SMT_NAME:
            return SMT_MAP_SCALE
        case constants.POINT_NAME:
            return POINT_MAP_SCALE
        case constants.POINT_AD_NAME:
            return POINT_AD_MAP_SCALE



def get_phenotype_from_solution(solution, representation):
    match representation:
        case constants.ALL_BLACK_NAME:
            return ABGenome.array_as_genome(list(map(int, solution.tolist()))).phenotype()
        case constants.GRID_GRAPH_NAME:
            return GraphGenome.array_as_genome(list(map(int, solution.tolist()))).phenotype()
        case constants.SMT_NAME:
            return SMTGenome.array_as_genome(list(map(int, solution.tolist()))).phenotype()
        case constants.POINT_NAME:
            return PointGenome.array_as_genome(list(map(int, solution.tolist()))).phenotype()
        case constants.POINT_AD_NAME:
            return PointAdGenome.array_as_genome(list(map(int, solution.tolist()))).phenotype()

# --- SAVE GRAPHS/IMAGES --- #


def __save_map(path, map_matrix, note=None):
    plt.imshow(map_matrix, cmap=cm.Greys, alpha=1.0)
    plt.axis('off')
    plt.annotate(note, xy=(0, 0), xytext=(0, -1), fontsize=12, color='black')
    plt.savefig(path, bbox_inches='tight')
    plt.clf()
    plt.close()

def __save_map_lines(path, phenotype, lines, note=None):
    #plt.imshow(map_matrix, cmap=cm.Greys, alpha=1.0)
    fig, ax = plt.subplots()
    for area in phenotype.areas:
        if area.isCorridor:
            ax.add_patch(plt.Rectangle((area.leftColumn, area.topRow), area.rightColumn-area.leftColumn, area.bottomRow-area.topRow, fill='b', edgecolor='b'))
        else:
            ax.add_patch(plt.Rectangle((area.leftColumn, area.topRow), area.rightColumn-area.leftColumn, area.bottomRow-area.topRow, fill=None, edgecolor='r'))
    for line in lines:
        if line is not None:
            x1, y1 = [line.start[0], line.end[0]], [line.start[1], line.end[1]]
            plt.plot(x1, y1, 'ro-')
    ax.set_xlim(0, phenotype.mapWidth)
    ax.set_ylim(0, phenotype.mapHeight)
    plt.annotate(note, xy=(0, 0), xytext=(0, -1), fontsize=12, color='black')
    plt.gca().invert_yaxis()
    plt.savefig(path, bbox_inches='tight')
    plt.close()

def __save_heatmap(x, y, path, map_matrix, note=None):
    heatmap, xedges, yedges = np.histogram2d(
        x,
        y,
        bins=[[i for i in range(len(map_matrix[0]) + 1)],
              [i for i in range(len(map_matrix) + 1)]]
    )
    heatmap = gaussian_filter(heatmap.T, sigma=3.0)
    plt.axis('off')

    mask = np.matrix(map_matrix)
    mask = np.ma.masked_where(mask == 0, mask)

    cmap = LinearSegmentedColormap.from_list('mycmap',
                                             ['#FFFFFF', '#FFFFFF', '#7777FF', '#0000FF', '#007777', '#00FF00',
                                              '#FFFF00', 'red'])

    plt.contourf(heatmap, cmap=cmap, levels=50, zorder=0)
    plt.imshow(mask, cmap='binary_r', zorder=1)

    # plt.show()
    
    plt.axis('on')  #DEBUG: Add this line to show the axes
    plt.annotate(note, xy=(0, 0), xytext=(0, -1), fontsize=12, color='black')
    plt.savefig(path, bbox_inches='tight')
    plt.clf()
    plt.close()


def __save_traces(start_positions, end_positions, path, map_matrix, note=None):
    plt.axis('off')
    step = max(1, floor(len(start_positions) / 50))
    for idx in range(0, len(start_positions), step):
        start_pos = start_positions[idx]
        end_pos = end_positions[idx]
        x = [start_pos[0], end_pos[0]]
        y = [start_pos[1], end_pos[1]]

        distance = sqrt(pow(x[0] - x[1], 2) + pow(y[0] - y[1], 2))
        distance_mapped = max(0.0, min((distance - 5) / 20, 1.0))
        color = (1.0 - distance_mapped, sqrt(distance_mapped), 0)
        plt.plot(x, y, linewidth=1, linestyle="-",
                 color=color, antialiased=True)
        plt.scatter(end_pos[0], end_pos[1], marker='o',
                    facecolors='none', edgecolors=color, s=12)

    plt.imshow(map_matrix, cmap='binary_r', alpha=1.0)
    plt.annotate(note, xy=(0, 0), xytext=(0, -1), fontsize=10, color='black')
    plt.savefig(path,
                dpi=110, bbox_inches='tight')
    plt.close()

def __save_graph(graph, path, note=None):
    layout = ig.Layout(coords=graph.vs['coords'], dim=2)
    fig, ax = plt.subplots()
    ig.plot(graph, vertex_size=25, layout=layout, vertex_label_color="white",  target=ax)
    plt.annotate(note, xy=(0, 0), xytext=(0, -1), fontsize=12, color='black')
    plt.gca().invert_yaxis()
    plt.savefig(path, bbox_inches='tight')
    plt.close()

def __save_graph_map(phenotype, path, note=None):
    fig, ax = plt.subplots()
    for area in phenotype.areas:
        if area.isCorridor:
            ax.add_patch(plt.Rectangle((area.leftColumn, area.topRow), area.rightColumn-area.leftColumn, area.bottomRow-area.topRow, fill='b', edgecolor='b'))
        else:
            ax.add_patch(plt.Rectangle((area.leftColumn, area.topRow), area.rightColumn-area.leftColumn, area.bottomRow-area.topRow, fill=None, edgecolor='r'))
    ax.set_xlim(0, phenotype.mapWidth)
    ax.set_ylim(0, phenotype.mapHeight)
    plt.annotate(note, xy=(0, 0), xytext=(0, -1), fontsize=12, color='black')
    plt.gca().invert_yaxis()
    plt.savefig(path, bbox_inches='tight')
    plt.close()

def __save_graph_vornoi(outer_shell, obstacles, graph, x_limit, y_limit, path, note=None):
    fig, ax = plt.subplots()

    # Plot the outer wall
    x,y = outer_shell.exterior.xy
    plt.plot(x,y)


    # Plot the obstacles
    for obstacle in obstacles:
        x,y = obstacle.exterior.xy
        plt.plot(x,y, color='darkred')

    # Plot the graph
    #graph.vs['label'] = [str(i) for i in range(len(graph.vs))]
    vertex_attr_names = graph.vertex_attributes()
    color = []
    for i in range(len(graph.vs)):
        if 'chokepoint' in vertex_attr_names and graph.vs[i]['chokepoint']:
            color.append('yellow')
        elif 'dead_end' in vertex_attr_names and graph.vs[i]['dead_end']:
            color.append('green')
        elif graph.vs[i]['region']:
            color.append('red')
        else:
            color.append('blue')
    graph.vs['color'] = color

    layout = ig.Layout(coords=graph.vs['coords'], dim=2)
    ig.plot(graph, vertex_size=5, layout=layout, vertex_label_color="black",  target=ax)
    plt.gca().invert_yaxis()

    ax.set_xlim(0, x_limit)
    ax.set_ylim(0, y_limit)
    plt.gca().invert_yaxis()

    plt.annotate(note, xy=(0, 0), xytext=(0, -1), fontsize=12, color='black')
    plt.savefig(path, bbox_inches='tight')
    plt.close()

def __save_visibility_map(visibility_matrix, map_matrix, path, note=None):
    plt.imshow(visibility_matrix, cmap='inferno', interpolation='nearest', zorder=0)
    mask = np.matrix(map_matrix)
    mask = np.ma.masked_where(mask == SPACE_TILE, mask)
    plt.imshow(mask, cmap='binary', zorder=1)
    #plt.gca().invert_yaxis()

    plt.annotate(note, xy=(0, 0), xytext=(0, -1), fontsize=12, color='black')
    plt.savefig(path, bbox_inches='tight')
    plt.close()

# --- ANALYSIS --- #


def save_image_map(outdir, experiment_name, sol_map_matrix, index, obj, meas_0, meas_1):
    __save_map(
        outdir /
        f"map_{int(index/conf.MEASURES_BINS_NUMBER[0])}_{int(index%conf.MEASURES_BINS_NUMBER[1])}.png",
        map_matrix=sol_map_matrix,
        note=f"Name: {experiment_name}\n {conf.OBJECTIVE_NAME}: {obj:.4f}\n{conf.MEASURES_NAMES[0]}: {meas_0:.4f}\n{conf.MEASURES_NAMES[1]}: {meas_1:.4f}")

def save_image_map_lines(outdir, experiment_name, phenotype, lines, index, obj, meas_0, meas_1):
    __save_map_lines(
        outdir /
        f"map_lines_{int(index/conf.MEASURES_BINS_NUMBER[0])}_{int(index%conf.MEASURES_BINS_NUMBER[1])}.png",
        phenotype=phenotype,
        lines=lines,
        note=f"Name: {experiment_name}\n {conf.OBJECTIVE_NAME}: {obj:.4f}\n{conf.MEASURES_NAMES[0]}: {meas_0:.4f}\n{conf.MEASURES_NAMES[1]}: {meas_1:.4f}")

def save_bot_positions_heatmap(positionsdir, outdir, experiment_name, map_matrix, index, obj, meas_0, meas_1, map_scale):
    for bot_n in range(0, 2):
        (positions_x, positions_y) = extract_bot_positions(
            positionsdir,
            experiment_name,
            bot_n,
        )
        positions_x = [x / map_scale for x in positions_x]
        positions_y = [x / map_scale for x in positions_y]

        __save_heatmap(
            positions_x,
            positions_y,
            outdir /
            f"map_{int(index/conf.MEASURES_BINS_NUMBER[0])}_{int(index%conf.MEASURES_BINS_NUMBER[1])}_positions_bot_{str(bot_n)}.png",
            map_matrix,
            note=f"Name: {experiment_name}\n {conf.OBJECTIVE_NAME}: {obj:.4f}\n{conf.MEASURES_NAMES[0]}: {meas_0:.4f}\n{conf.MEASURES_NAMES[1]}: {meas_1:.4f}"
        )


def save_deaths_and_kills_map(deathsdir, outdir, experiment_name, map_matrix, index, obj, meas_0, meas_1, map_scale):
    note=f"Name: {experiment_name}\n {conf.OBJECTIVE_NAME}: {obj:.4f}\n{conf.MEASURES_NAMES[0]}: {meas_0:.4f}\n{conf.MEASURES_NAMES[1]}: {meas_1:.4f}"
    for bot_n in range(0, 2):
        (deaths_x, deaths_y) = extract_death_positions(
            deathsdir,
            experiment_name,
            bot_n,
        )
        deaths_x = [x / map_scale for x in deaths_x]
        deaths_y = [x / map_scale for x in deaths_y]
        __save_heatmap(
            deaths_x,
            deaths_y,
            outdir /
            f"map_{int(index/conf.MEASURES_BINS_NUMBER[0])}_{int(index%conf.MEASURES_BINS_NUMBER[1])}_deaths_bot_{str(bot_n)}.png",
            map_matrix,
            note=note)

        (kills_x, kills_y) = extract_kill_positions(
            deathsdir,
            experiment_name,
            bot_n,
        )
        kills_x = [x / map_scale for x in kills_x]
        kills_y = [x / map_scale for x in kills_y]
        __save_heatmap(
            kills_x,
            kills_y,
            outdir /
            f"map_{int(index/conf.MEASURES_BINS_NUMBER[0])}_{int(index%conf.MEASURES_BINS_NUMBER[1])}_kills_bot_{str(1-bot_n)}.png",
            map_matrix,
            note=note)

        death_pos = [[x, y] for x, y in zip(deaths_x, deaths_y)]
        kill_pos = [[x, y] for x, y in zip(kills_x, kills_y)]

        __save_traces(
            death_pos,
            kill_pos,
            outdir /
            f"map_{int(index/conf.MEASURES_BINS_NUMBER[0])}_{int(index%conf.MEASURES_BINS_NUMBER[1])}_kill_traces_bot_{str(1-bot_n)}.png",
            map_matrix,
            note=note
            )
        
def save_graphs(graphdir, experiment_name, phenotype, index, obj, meas_0, meas_1):
    graph, _ = phenotype.to_topology_graph_naive()
    graph.vs['label'] = [str(i) for i in range(len(graph.vs))]

    __save_graph(
        graph, 
        graphdir / f"graph_{int(index/conf.MEASURES_BINS_NUMBER[0])}_{int(index%conf.MEASURES_BINS_NUMBER[1])}.png", 
        note=f"Name: {experiment_name}\n {conf.OBJECTIVE_NAME}: {obj:.4f}\n{conf.MEASURES_NAMES[0]}: {meas_0:.4f}\n{conf.MEASURES_NAMES[1]}: {meas_1:.4f}"
    )
    __save_graph_map(
        phenotype,
        graphdir / f"graph_{int(index/conf.MEASURES_BINS_NUMBER[0])}_{int(index%conf.MEASURES_BINS_NUMBER[1])}_map.png",
        note=f"Name: {experiment_name}\n {conf.OBJECTIVE_NAME}: {obj:.4f}\n{conf.MEASURES_NAMES[0]}: {meas_0:.4f}\n{conf.MEASURES_NAMES[1]}: {meas_1:.4f}"
    )

def save_graphs_vornoi(graphdir, experiment_name, phenotype, index, obj, meas_0, meas_1):
    graph, outer_shell, obstacles = phenotype.to_topology_graph_vornoi()

    __save_graph_vornoi(
        outer_shell,
        obstacles,
        graph,
        phenotype.mapWidth,
        phenotype.mapHeight,
        graphdir / f"graph_vornoi_{int(index/conf.MEASURES_BINS_NUMBER[0])}_{int(index%conf.MEASURES_BINS_NUMBER[1])}.png",
        note=f"Name: {experiment_name}\n {conf.OBJECTIVE_NAME}: {obj:.4f}\n{conf.MEASURES_NAMES[0]}: {meas_0:.4f}\n{conf.MEASURES_NAMES[1]}: {meas_1:.4f}"
    )

def save_visibility_maps(visibilitydir, experiment_name, phenotype, index, obj, meas_0, meas_1):
    graph, matrix = phenotype.to_visibility_graph()

    __save_visibility_map(
        matrix,
        phenotype.map_matrix(),
        visibilitydir / f"visibility_map_{int(index/conf.MEASURES_BINS_NUMBER[0])}_{int(index%conf.MEASURES_BINS_NUMBER[1])}.png",
        note=f"Name: {experiment_name}\n {conf.OBJECTIVE_NAME}: {obj:.4f}\n{conf.MEASURES_NAMES[0]}: {meas_0:.4f}\n{conf.MEASURES_NAMES[1]}: {meas_1:.4f}"
    )

def save_results(resultsDir, exportDir, iteration, individual_number, cumulative_dataset):
    name = f"final_results_{iteration}_{individual_number}"
    dataset = pd.read_json(os.path.join(exportDir, name + ".json"))
    dataset.to_json(os.path.join(resultsDir, name + ".json"), orient='records', indent=4)

    if cumulative_dataset is None:
        return dataset
    else:
        return pd.concat([cumulative_dataset, dataset], ignore_index=True)

# --- MAIN --- #


def analyze_archive(
    representation,
    folder_name="test_directory",
):
    # Make parent output directory
    outdir = Path(os.path.join(ARCHIVE_ANALYSIS_OUTPUT_FOLDER, folder_name))
    outdir.mkdir(exist_ok=True)

    # Get existing directiories with data to analyze
    archivedir = Path(os.path.join(MAP_ELITES_OUTPUT_FOLDER, folder_name))
    exportDir = Path(os.path.join(GAME_DATA_FOLDER, "Export", folder_name))

    # Make subdirectories for analysis outputs
    mapsDir = Path(os.path.join(outdir, "Maps"))
    mapsDir.mkdir(exist_ok=True)
    positionDir = Path(os.path.join(outdir, "Positions_Heatmaps"))
    positionDir.mkdir(exist_ok=True)
    deathsDir = Path(os.path.join(outdir, "Deaths_Kills_Heatmaps"))
    deathsDir.mkdir(exist_ok=True)
    graphsDir = Path(os.path.join(outdir, "Graphs"))
    graphsDir.mkdir(exist_ok=True)
    graphsVornoiDir = Path(os.path.join(outdir, "GraphsVornoi"))
    graphsVornoiDir.mkdir(exist_ok=True)
    visibilityDir = Path(os.path.join(outdir, "Visibility"))
    visibilityDir.mkdir(exist_ok=True)
    resultsDir = Path(os.path.join(outdir, "Results"))
    resultsDir.mkdir(exist_ok=True)

    # Load archive data
    df = ArchiveDataFrame(pd.read_csv(archivedir / "archive.csv"))
    df.sort_values(by=["index"], ascending=True, inplace=True)

    # Extract data from archive
    indexes = df.get_field("index")
    solutions = df.get_field("solution")
    obj = df.get_field("objective")
    meas_0 = df.get_field("measures_0")
    meas_1 = df.get_field("measures_1")
    iterations = df.get_field("iterations")
    individual_numbers = df.get_field("individual_numbers")
    
    cumulative_dataset = None

    # Analyze each solution
    for idx in tqdm.trange(0, len(solutions)):
        skip = False
        sol = solutions[idx]
        experiment_name = str(int(iterations[idx])) + "_" + str(int(individual_numbers[idx]))
        if representation == constants.SMT_NAME:
            try:
                phenotype_file = open(os.path.join(exportDir, 'phenotype_' + experiment_name + '.pkl'), 'rb')
                phenotype = pickle.load(phenotype_file)
                phenotype_file.close()
            except:
                skip = True
        else:
            phenotype = get_phenotype_from_solution(sol, representation)

        if not skip:
            sol_map_matrix = read_map(
                str(int(iterations[idx])) + "_" + str(int(individual_numbers[idx])), folder_name)
            map_scale = get_map_scale(representation)
            
            save_image_map(mapsDir, experiment_name, sol_map_matrix,
                           indexes[idx], obj[idx], meas_0[idx], meas_1[idx])
            if representation == constants.SMT_NAME:
                genotype = SMTGenome.array_as_genome(list(map(int, sol.tolist())))
                save_image_map_lines(mapsDir, experiment_name, phenotype, genotype.lines,
                                     indexes[idx], obj[idx], meas_0[idx], meas_1[idx])
            save_bot_positions_heatmap(exportDir, positionDir, experiment_name, sol_map_matrix,
                                      indexes[idx], obj[idx], meas_0[idx], meas_1[idx], map_scale)
            save_deaths_and_kills_map(exportDir, deathsDir, experiment_name, sol_map_matrix,
                                        indexes[idx], obj[idx], meas_0[idx], meas_1[idx], map_scale)
            save_graphs(graphsDir, experiment_name, phenotype, indexes[idx], obj[idx], meas_0[idx], meas_1[idx])
            save_graphs_vornoi(graphsVornoiDir, experiment_name, phenotype, indexes[idx], obj[idx], meas_0[idx], meas_1[idx])
            save_visibility_maps(visibilityDir, experiment_name, phenotype, indexes[idx], obj[idx], meas_0[idx], meas_1[idx])
            cumulative_dataset = save_results(resultsDir, exportDir, int(iterations[idx]), int(individual_numbers[idx]), cumulative_dataset)
    
    cumulative_dataset.to_json(os.path.join(resultsDir, "_final_results.json"), orient='columns', indent=4)
    aggregate_dataset = pd.DataFrame()
    for column in cumulative_dataset.columns:
        aggregate_dataset[f"mean_{column}"] = [cumulative_dataset[column].mean()]
        aggregate_dataset[f"std_{column}"] = [cumulative_dataset[column].std()]
    aggregate_dataset.to_json(os.path.join(resultsDir, "_final_results_aggregate.json"), orient='records', indent=4)
    return


if __name__ == "__main__":
    parser = argparse.ArgumentParser(description='Analyze archive.')

    parser.add_argument("--workers", default=4, type=int, dest="workers")

    # To avoid having to change the config to match a past experiment, the folder name can be passed as an argument.
    parser.add_argument("--folder_name", default="",
                        type=str, dest="folder_name")

    args = parser.parse_args(sys.argv[1:])
    folder_name = args.folder_name if args.folder_name != "" else conf.folder_name()

    analyze_archive(
        representation=conf.REPRESENTATION_NAME,
        folder_name=folder_name
    )

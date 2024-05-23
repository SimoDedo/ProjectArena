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
from internals.ab_genome.ab_genome import ABGenome
from internals.graph_genome.gg_genome import GraphGenome
import matplotlib.pyplot as plt
from matplotlib import cm
import pandas as pd
import numpy as np
from scipy.ndimage import gaussian_filter
import tqdm

from ribs.archives import ArchiveDataFrame

# --- UTILS --- #


def get_map_scale(representation):
    match representation:
        case constants.ALL_BLACK_NAME:
            return AB_MAP_SCALE
        case constants.GRID_GRAPH_NAME:
            return GG_MAP_SCALE


def get_phenotype_from_solution(solution, representation):
    match representation:
        case constants.ALL_BLACK_NAME:
            return ABGenome.array_as_genome(list(map(int, solution.tolist()))).phenotype()
        case constants.GRID_GRAPH_NAME:
            return GraphGenome.array_as_genome(list(map(int, solution.tolist()))).phenotype()

# --- SAVE GRAPHS/IMAGES --- #


def __save_map(path, map_matrix, note=None):
    plt.imshow(map_matrix, cmap=cm.Greys, alpha=1.0)
    plt.axis('off')
    plt.annotate(note, xy=(0, 0), xytext=(0, -1), fontsize=12, color='black')
    plt.savefig(path, bbox_inches='tight')
    plt.clf()
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
    plt.annotate(note, xy=(0, 0), xytext=(0, -1), fontsize=12, color='black')
    plt.savefig(path,
                dpi=110, bbox_inches='tight')
    plt.close()

# --- ANALYSIS --- #


def save_image_map(outdir, sol_map_matrix, index, obj, meas_0, meas_1):
    __save_map(
        outdir /
        f"map_{int(index/conf.MEASURES_BINS_NUMBER[0])}_{int(index%conf.MEASURES_BINS_NUMBER[1])}.png",
        map_matrix=sol_map_matrix,
        note=f"{conf.OBJECTIVE_NAME}: {obj:.2f}\n{conf.MEASURES_NAMES[0]}: {meas_0:.2f}\n{conf.MEASURES_NAMES[1]}: {meas_1:.2f}")


def save_bot_positions_heatmap(positionsdir, outdir, map_matrix, index, obj, meas_0, meas_1, iteration, individual_number, map_scale):
    for bot_n in range(0, 2):
        (positions_x, positions_y) = extract_bot_positions(
            positionsdir,
            str(int(iteration)) + "_" + str(int(individual_number)),
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
            note=f"{conf.OBJECTIVE_NAME}: {obj:.2f}\n{conf.MEASURES_NAMES[0]}: {meas_0:.2f}\n{conf.MEASURES_NAMES[1]}: {meas_1:.2f}"
        )


def save_deaths_and_kills_map(deathsdir, outdir, map_matrix, index, obj, meas_0, meas_1, iteration, individual_number, map_scale):
    experiment_name = str(int(iteration)) + "_" + str(int(individual_number))
    note=f"{conf.OBJECTIVE_NAME}: {obj:.2f}\n{conf.MEASURES_NAMES[0]}: {meas_0:.2f}\n{conf.MEASURES_NAMES[1]}: {meas_1:.2f}"
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

    # Analyze each solution
    for idx in tqdm.trange(0, len(solutions)):
        sol = solutions[idx]
        #phenotype = get_phenotype_from_solution(sol, representation)
        sol_map_matrix = read_map(
            str(int(iterations[idx])) + "_" + str(int(individual_numbers[idx])), folder_name)
        map_scale = get_map_scale(representation)

        save_image_map(mapsDir, sol_map_matrix,
                       indexes[idx], obj[idx], meas_0[idx], meas_1[idx])
        save_bot_positions_heatmap(exportDir, positionDir, sol_map_matrix,
                                   indexes[idx], obj[idx], meas_0[idx], meas_1[idx], iterations[idx], individual_numbers[idx], map_scale)
        save_deaths_and_kills_map(exportDir, deathsDir, sol_map_matrix,
                                    indexes[idx], obj[idx], meas_0[idx], meas_1[idx], iterations[idx], individual_numbers[idx], map_scale)

    return


if __name__ == "__main__":
    parser = argparse.ArgumentParser(description='Evolve population.')

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

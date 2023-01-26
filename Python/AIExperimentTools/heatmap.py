import argparse
import math
import pickle
import sys

import jsonpickle
import matplotlib.pyplot as plt
import numpy
from matplotlib import colors

from internals import evaluation, stats
from internals.constants import GAME_DATA_FOLDER
from internals.phenotype import Phenotype

JSON_GENOME_PATH = "heatmap.json"


def plot(data, name):
    fig, axs = plt.subplots(1, 1, figsize=(2 + 2, 3), constrained_layout=True, squeeze=False)
    for ax in axs.flat:
        psm = ax.pcolormesh(
            data,
            cmap='jet',
            rasterized=True,
            norm=colors.CenteredNorm(),
        )
        fig.colorbar(psm, ax=ax)

    plt.savefig(GAME_DATA_FOLDER + name + ".png", bbox_inches='tight')
    plt.clf()

    fig, axs = plt.subplots(1, 1, figsize=(2 + 2, 3), constrained_layout=True, squeeze=False)
    for ax in axs.flat:
        psm = ax.pcolormesh(
            data,
            cmap='jet',
            rasterized=True,
            norm=colors.CenteredNorm(),
            shading='gouraud',
        )
        fig.colorbar(psm, ax=ax)

    plt.savefig(GAME_DATA_FOLDER + name + "_gouraud.png", bbox_inches='tight')

    with open(GAME_DATA_FOLDER + name + "_heatmap_matrix.pkl", "wb") as cp_file:
        pickle.dump(data, cp_file)


def heatmap(bot1_file, bot2_file, resolution):
    log_ratio_info = numpy.empty((resolution, resolution))
    kill_diff_info = numpy.empty((resolution, resolution))
    with open(JSON_GENOME_PATH, "r") as json_file:
        lines = json_file.readlines()
        genome = jsonpickle.decode(' '.join(lines))

    phenotype = Phenotype(
        genome["width"],
        genome["height"],
        genome["mapScale"],
        genome["areas"],
    )

    bot1_data = {"file": bot1_file}
    bot2_data = {"file": bot2_file}
    for x in range(0, resolution):
        for y in range(0, resolution):
            if y < x and bot1_file == bot2_file:
                log_ratio_info[x][y] = -log_ratio_info[y][x]
                kill_diff_info[x][y] = -kill_diff_info[y][x]
                continue

            bot1_data["skill"] = str(x / (resolution - 1))
            bot2_data["skill"] = str(y / (resolution - 1))
            simulation_results = evaluation.evaluate(phenotype, x, y, bot1_data, bot2_data)

            print("Results for " + str(x) + ", (" + bot1_data["skill"] + "), " + str(y) + "(" + bot2_data["skill"] + "):")

            paces = simulation_results["pace"]
            (min_pace, max_pace, mean_pace, std_dev) = stats.get_statistics(paces)
            rel_std_dev = round(std_dev / mean_pace * 100, 2)
            print("pace mean: " + str(mean_pace) + ", stdDev: " + str(std_dev) + ", relStdDev: " + str(rel_std_dev) +
                  ", min: " + str(min_pace) + ", max: " + str(max_pace))

            ratios = simulation_results["ratio"]
            (min_ratio, max_ratio, mean_ratio, std_dev) = stats.get_statistics(ratios)
            rel_std_dev = round(std_dev / mean_ratio * 100, 2)
            print("ratio mean: " + str(mean_ratio) + ", stdDev: " + str(std_dev) + ", relStdDev: " + str(rel_std_dev) +
                  ", min: " + str(min_ratio) + ", max: " + str(max_ratio))

            kill_diff = simulation_results["killDiff"]
            (min_kill_diff, max_kill_diff, mean_kill_diff, std_dev) = stats.get_statistics(kill_diff)
            rel_std_dev = round(std_dev / mean_ratio * 100, 2)
            print("kill_diff mean: " + str(mean_kill_diff) + ", stdDev: " + str(std_dev) + ", relStdDev: " +
                  str(rel_std_dev) + ", min: " + str(min_kill_diff) + ", max: " + str(max_kill_diff))

            log_ratio_info[x][y] = math.log2(mean_ratio)
            kill_diff_info[x][y] = mean_kill_diff

    plot(log_ratio_info, "heatmap_ratio")
    plot(kill_diff_info, "heatmap_kill_diff")
    print(log_ratio_info)


if __name__ == "__main__":
    parser = argparse.ArgumentParser(description='Evolve population.')

    parser.add_argument("--bot1_file_prefix", required=True, dest="bot1_file")
    parser.add_argument("--bot2_file_prefix", required=True, dest="bot2_file")
    parser.add_argument("--resolution", required=True, type=int, dest="resolution")
    args = parser.parse_args(sys.argv[1:])

    heatmap(args.bot1_file, args.bot2_file, args.resolution)


import argparse
import math
import pickle
import sys

import jsonpickle
import matplotlib.pyplot as plt
import numpy
from matplotlib import colors

from internals import evaluation, stats
from internals.phenotype import Phenotype

JSON_GENOME_PATH = "heatmap.json"


def plot(data):
    fig, axs = plt.subplots(1, 1, figsize=(2 + 2, 3), constrained_layout=True, squeeze=False)
    for ax in axs.flat:
        psm = ax.pcolormesh(
            data,
            cmap='jet',
            rasterized=True,
            norm=colors.CenteredNorm(),
        )
        fig.colorbar(psm, ax=ax)

    plt.savefig("Data/heatmap.png", bbox_inches='tight')
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

    plt.savefig("Data/heatmap_gouraud.png", bbox_inches='tight')

    with open("Data/matrix.pkl", "wb") as cp_file:
        pickle.dump(data, cp_file)


def heatmap(bot1_file, bot2_file, resolution):
    total_info = numpy.empty((resolution, resolution))
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
                total_info[x][y] = -total_info[y][x]
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
            (_, _, mean_ratio, _) = stats.get_statistics(ratios)
            (min_ratio, max_ratio, mean_ratio, std_dev) = stats.get_statistics(ratios)
            rel_std_dev = round(std_dev / mean_ratio * 100, 2)
            print("ratio mean: " + str(mean_ratio) + ", stdDev: " + str(std_dev) + ", relStdDev: " + str(rel_std_dev) +
                  ", min: " + str(min_ratio) + ", max: " + str(max_ratio))

            total_info[x][y] = math.log2(mean_ratio)

    plot(total_info)
    print(total_info)


if __name__ == "__main__":
    parser = argparse.ArgumentParser(description='Evolve population.')

    parser.add_argument("--bot1_file_prefix", required=True, dest="bot1_file")
    parser.add_argument("--bot2_file_prefix", required=True, dest="bot2_file")
    parser.add_argument("--resolution", required=True, type=int, dest="resolution")
    args = parser.parse_args(sys.argv[1:])

    heatmap(args.bot1_file, args.bot2_file, args.resolution)


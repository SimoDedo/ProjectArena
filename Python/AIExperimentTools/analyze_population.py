import argparse
import os
import pickle
import sys

import matplotlib.cm as cm
import matplotlib.pyplot as plt
import numpy

from internals.result_extractor import extract_death_positions, extract_bot_positions
from internals.toolbox import prepare_toolbox


def __save_positions_heatmap(x, y, folder, experiment_name, map_size, map_matrix):
    heatmap, xedges, yedges = numpy.histogram2d(x, y,
                                                bins=[[i for i in range(map_size[0])], [i for i in range(map_size[1])]])
    # heatmap = gaussian_filter(heatmap, sigma=2)
    plt.imshow(heatmap.T, cmap=cm.jet)
    plt.imshow(map_matrix, cmap=cm.Greys, alpha=0.2)
    plt.axis('off')

    if folder is None:
        plt.show()
    else:
        plt.savefig(folder + "/" + experiment_name + ".png", bbox_inches='tight')
    plt.clf()


def __plot_pareto_frontier(x, y, max_x=True, max_y=True):
    """Pareto frontier selection process"""
    sorted_list = sorted([[x[i], y[i]] for i in range(len(x))], reverse=max_y)
    pareto_front = [sorted_list[0]]
    for pair in sorted_list[1:]:
        if max_x:
            if pair[1] >= pareto_front[-1][1]:
                pareto_front.append(pair)
        else:
            if pair[1] <= pareto_front[-1][1]:
                pareto_front.append(pair)

    '''Plotting process'''
    plt.scatter(x, y, marker='x')
    pf_x = [pair[0] for pair in pareto_front]
    pf_y = [pair[1] for pair in pareto_front]
    plt.plot(pf_x, pf_y)
    plt.xlabel("Entropy")
    plt.ylabel("Pace")
    plt.xlim(0.5, 1)
    plt.ylim(0, 1)
    plt.savefig("Data/Analysis/pareto.png", bbox_inches='tight')


def __plot_graph(min, mean, max):
    x_range = [x for x in range(len(min))]
    fig = plt.figure()
    ax = fig.add_axes([0.1, 0.1, 0.6, 0.75])
    ax.plot(x_range, min, label="Fitness min")
    ax.legend(bbox_to_anchor=(1.05, 1), loc='upper left', borderaxespad=0.)
    ax.plot(x_range, mean, label="Fitness mean")
    ax.legend(bbox_to_anchor=(1.05, 1), loc='upper left', borderaxespad=0.)
    ax.plot(x_range, max, label="Fitness max")
    ax.legend(bbox_to_anchor=(1.05, 1), loc='upper left', borderaxespad=0.)
    plt.xlabel("Epoch")
    plt.savefig("Data/Analysis/evolution.png", bbox_inches='tight')


def load_checkpoint(name):
    with open(name, "rb") as cp_file:
        cp = pickle.load(cp_file, errors='none')
        population = cp["population"]
        epoch = cp["epoch"]
    return population, epoch


def __export_checkpoint_results(num_checkpoints, positions_heatmaps, death_heatmaps):
    total_fitnesses = []
    epochs_mean_fitnesses = []
    epochs_max_fitnesses = []
    epochs_min_fitnesses = []
    for checkpoint in range(int(num_checkpoints) + 1):
        epoch_fitnesses = []
        (population, epoch) = load_checkpoint("Data/checkpoints/checkpoint_epoch_" + str(checkpoint) + ".pkl")
        index = 0
        epoch_fenotypes = set()
        for individual in population:
            experiment_name = str(individual.epoch) + "_" + str(individual.number_in_epoch)
            individual_path = "Data/Analysis/" + str(epoch) + "_" + str(index)
            epoch_fitnesses.append(individual.fitness)

            if positions_heatmaps == False and death_heatmaps == False:
                continue

            fenotype = individual.phenotype()
            if fenotype in epoch_fenotypes:
                continue

            epoch_fenotypes.add(fenotype)
            if not os.path.exists(individual_path):
                os.mkdir(individual_path)

            index += 1

            fitness_file_name = "/fitness_" + str(individual.fitness.values[0]) + "_" + str(
                individual.fitness.values[1]) + ".txt"
            open(individual_path + fitness_file_name, "w").close()

            map_matrix, map_width, map_height = extract_map_contents(experiment_name)

            if positions_heatmaps:
                (position_1_x, position_1_y) = extract_bot_positions("genome_evolution/", experiment_name, 1)
                position_1_x = [x / individual.mapScale for x in position_1_x]
                position_1_y = [x / individual.mapScale for x in position_1_y]
                __save_positions_heatmap(position_1_x, position_1_y, individual_path, "_positions_bot1",
                                         [map_width, map_height], map_matrix)
                (position_2_x, position_2_y) = extract_bot_positions("genome_evolution/", experiment_name, 2)
                position_2_x = [x / individual.mapScale for x in position_2_x]
                position_2_y = [x / individual.mapScale for x in position_2_y]
                __save_positions_heatmap(position_2_x, position_2_y, individual_path, "_positions_bot2",
                                         [map_width, map_height], map_matrix)

            if death_heatmaps:
                (death_1_x, death_1_y) = extract_death_positions("genome_evolution/", experiment_name, 1)
                death_1_x = [x / individual.mapScale for x in death_1_x]
                death_1_y = [x / individual.mapScale for x in death_1_y]
                __save_positions_heatmap(death_1_x, death_1_y, individual_path, "_death_bot1", [map_width, map_height],
                                         map_matrix)
                (death_2_x, death_2_y) = extract_death_positions("genome_evolution/", experiment_name, 2)
                death_2_x = [x / individual.mapScale for x in death_2_x]
                death_2_y = [x / individual.mapScale for x in death_2_y]
                __save_positions_heatmap(death_2_x, death_2_y, individual_path, "_death_bot2", [map_width, map_height],
                                         map_matrix)

        total_fitnesses.extend(epoch_fitnesses)
        epoch_fitnesses = numpy.transpose(epoch_fitnesses)
        epochs_mean_fitnesses.append(numpy.mean(epoch_fitnesses[0]))
        epochs_max_fitnesses.append(numpy.max(epoch_fitnesses[0]))
        epochs_min_fitnesses.append(numpy.min(epoch_fitnesses[0]))

    total_fitnesses = numpy.transpose(total_fitnesses)
    __plot_pareto_frontier(total_fitnesses[0], total_fitnesses[1])
    __plot_graph(epochs_min_fitnesses, epochs_mean_fitnesses, epochs_max_fitnesses)


def extract_map_contents(experiment_name):
    with open("Data/Export/genome_evolution/map_" + experiment_name + "_0.txt", "r") as map_file:
        map_contents = map_file.readlines()
    map_height = len(map_contents)
    map_width = len(map_contents[0]) - 1  # Drop newline
    map_matrix = []
    for row in reversed(range(map_height)):
        map_row = []
        for col in range(map_width):
            if map_contents[row][col] == 'w':
                map_row.append(1)
            else:
                map_row.append(0)
        map_matrix.append(map_row)
    return map_matrix, map_width, map_height,


if __name__ == "__main__":
    prepare_toolbox()
    parser = argparse.ArgumentParser(description='Analyze population.')

    parser.add_argument("--num_epochs", default=30, type=int, dest="num_epochs")
    parser.add_argument("--print_positions_heatmap", default=False, type=bool, dest="positions")
    parser.add_argument("--print_death_heatmap", default=False, type=bool, dest="death")
    args = parser.parse_args(sys.argv[1:])

    __export_checkpoint_results(args.num_epochs, args.positions, args.death)

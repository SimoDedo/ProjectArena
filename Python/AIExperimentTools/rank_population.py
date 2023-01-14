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
        plt.savefig(folder + experiment_name + ".png", bbox_inches='tight')
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


def __rank_population(num_checkpoints, num_individuals_per_criteria):
    individuals = []
    known_phenotypes = set()
    for checkpoint in range(int(num_checkpoints) + 1):
        (population, epoch) = load_checkpoint("Data/checkpoints/checkpoint_epoch_" + str(checkpoint) + ".pkl")
        for individual in population:
            phenotype = individual.phenotype()
            if phenotype in known_phenotypes:
                continue
            individuals.append(individual)
            known_phenotypes.add(phenotype)

    sorted_by_fitness = sorted(individuals, key=lambda it: fitness_value(it), reverse=True)
    for i in range(0, num_individuals_per_criteria):
        best_elem = sorted_by_fitness[i]
        print("Fitness best elem " + str(i) + " epoch: " + str(best_elem.epoch) + ", number: " + str(best_elem.number_in_epoch))
        save_elem_info(best_elem, i, "fitness_best")
        worst_elem = sorted_by_fitness[-(i + 1)]
        save_elem_info(worst_elem, i, "fitness_worst")
    sorted_by_entropy = sorted(individuals, key=lambda it: it.fitness.values[0], reverse=True)
    for i in range(0, num_individuals_per_criteria):
        best_elem = sorted_by_entropy[i]
        save_elem_info(best_elem, i, "entropy_best")
        print("Entropy best elem " + str(i) + " epoch: " + str(best_elem.epoch) + ", number: " + str(best_elem.number_in_epoch))
        worst_elem = sorted_by_entropy[-(i + 1)]
        worst_elem.phenotype()
        save_elem_info(worst_elem, i, "entropy_worst")
    sorted_by_pace = sorted(individuals, key=lambda it: it.fitness.values[1], reverse=True)
    for i in range(0, num_individuals_per_criteria):
        best_elem = sorted_by_pace[i]
        save_elem_info(best_elem, i, "pace_best")
        print("Pace best elem " + str(i) + " epoch: " + str(best_elem.epoch) + ", number: " + str(best_elem.number_in_epoch))
        worst_elem = sorted_by_entropy[-(i + 1)]
        save_elem_info(worst_elem, i, "pace_worst")


def save_elem_info(elem, i, folder_name):
    elem_path = "Data/Analysis/" + folder_name + "_" + str(i) + "/"
    if not os.path.exists(elem_path):
        os.mkdir(elem_path)
    experiment_name = str(elem.epoch) + "_" + str(elem.number_in_epoch)
    map_width, map_height, map_matrix = read_map(experiment_name)

    fitness_file_name = "fitness_" + str(fitness_value(elem)) + ".txt"
    open(elem_path + fitness_file_name, "w").close()
    values_file_name = "values_" + str(elem.fitness.values) + ".txt"
    open(elem_path + values_file_name, "w").close()

    (position_1_x, position_1_y) = extract_bot_positions("genome_evolution/", experiment_name, 1)
    position_1_x = [x / elem.mapScale for x in position_1_x]
    position_1_y = [x / elem.mapScale for x in position_1_y]
    __save_positions_heatmap(position_1_x, position_1_y, elem_path, "_positions_bot1",
                             [map_width, map_height], map_matrix)
    (position_2_x, position_2_y) = extract_bot_positions("genome_evolution/", experiment_name, 2)
    position_2_x = [x / elem.mapScale for x in position_2_x]
    position_2_y = [x / elem.mapScale for x in position_2_y]
    __save_positions_heatmap(position_2_x, position_2_y, elem_path, "_positions_bot2",
                             [map_width, map_height], map_matrix)
    (death_1_x, death_1_y) = extract_death_positions("genome_evolution/", experiment_name, 1)
    death_1_x = [x / elem.mapScale for x in death_1_x]
    death_1_y = [x / elem.mapScale for x in death_1_y]
    __save_positions_heatmap(death_1_x, death_1_y, elem_path, "_death_bot1", [map_width, map_height],
                             map_matrix)
    (death_2_x, death_2_y) = extract_death_positions("genome_evolution/", experiment_name, 2)
    death_2_x = [x / elem.mapScale for x in death_2_x]
    death_2_y = [x / elem.mapScale for x in death_2_y]
    __save_positions_heatmap(death_2_x, death_2_y, elem_path, "_death_bot2", [map_width, map_height],
                             map_matrix)


def fitness_value(elem):
    return numpy.dot(elem.fitness.values, elem.fitness.weights)


def read_map(experiment_name):
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
    return map_width, map_height, map_matrix


if __name__ == "__main__":
    prepare_toolbox()
    parser = argparse.ArgumentParser(description='Rank population.')

    parser.add_argument("--num_epochs", default=30, type=int, dest="num_epochs")
    parser.add_argument("--num_individuals_to_print", default=1, type=int, dest="num_individuals_per_criteria")
    args = parser.parse_args(sys.argv[1:])

    __rank_population(args.num_epochs, args.num_individuals_per_criteria)

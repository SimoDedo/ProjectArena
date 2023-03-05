import argparse
import os
import pickle
import sys
from math import sqrt

import matplotlib.pyplot as plt
import numpy
from deap import tools
from matplotlib import cm
from scipy.ndimage import gaussian_filter

from internals.constants import GAME_DATA_FOLDER, NUM_PARALLEL_SIMULATIONS
from internals.result_extractor import extract_bot_positions, extract_death_positions, extract_kill_positions, extract_match_data
from internals.toolbox import prepare_toolbox


def __load_checkpoint(name):
    with open(name, "rb") as cp_file:
        cp = pickle.load(cp_file, errors='none')
        population = cp["population"]
        epoch = cp["epoch"]
        data = cp["data"]
        return epoch, population, data


def __plot_pareto(epoch, all_fitnesses, optimal_front):
    pareto_entropy, pareto_pace, = zip(*[ind.fitness.values for ind in optimal_front])
    non_pareto_data = all_fitnesses  # - optimal_front
    non_pareto_entropy, non_pareto_pace, = zip(*[fitness for fitness in non_pareto_data])
    fig = plt.figure()
    fig.set_size_inches(15, 10)
    axe = plt.subplot2grid((1, 1), (0, 0))
    axe.set_ylabel('Pace', fontsize=15)
    axe.scatter(non_pareto_entropy, non_pareto_pace, c='b', marker='o')
    axe.scatter(pareto_entropy, pareto_pace, c='r', marker='o')
    axe.set_xlabel('Entropy', fontsize=15)
    plt.xlim([0.6, 1.05])
    plt.ylim([0.2, 1.05])
    plt.savefig(GAME_DATA_FOLDER + "Analysis/pareto_front_" + str(epoch) + ".png", bbox_inches='tight')
    plt.close()


def __fitness_similarity(a, b):
    entropy_a = a.fitness.values[0]
    entropy_b = b.fitness.values[0]
    pace_a = a.fitness.values[1]
    pace_b = b.fitness.values[1]
    return abs(entropy_a - entropy_b) < 0.01 and abs(pace_a - pace_b) < 0.01


def __plot_epoch_paretos(num_epochs, num_populations):
    pareto_areas = []
    for checkpoint_idx in range(num_epochs + 1):
        pareto = tools.ParetoFront(__fitness_similarity)
        populations_individuals = []
        populations_fitnesses = []
        for population_idx in range(num_populations):
            (_, population, all_fitnesses) = __load_checkpoint(
                GAME_DATA_FOLDER + str(population_idx) + "/checkpoints/checkpoint_epoch_" + str(checkpoint_idx) + ".pkl")
            populations_individuals.extend(population)
            populations_fitnesses.extend(all_fitnesses)
        if checkpoint_idx == 30:
            print("AAA")
            pass
        pareto.update(populations_individuals)
        __plot_pareto(checkpoint_idx, populations_fitnesses, pareto)
        pareto_areas.append(compute_pareto_area(pareto))
    fig = plt.figure()
    ax = fig.add_axes([0.1, 0.1, 0.6, 0.75])
    ax.plot([x for x in range(num_epochs+1)], pareto_areas, label="Area ander pareto front")
    ax.legend(bbox_to_anchor=(1.05, 1), loc='upper left', borderaxespad=0.)
    plt.xlabel("Epoch")
    plt.savefig(GAME_DATA_FOLDER + "Analysis/pareto_evolution.png", bbox_inches='tight')
    plt.close()


def compute_pareto_area(pareto_front):
    sorted_pareto = sorted([x.fitness.values for x in pareto_front])
    total_area = 0
    sorted_pareto.insert(0, (0, sorted_pareto[0][1]))
    for i in range(len(sorted_pareto)-2):
        # pace_start = sorted_pareto[i][1]
        pace_end = sorted_pareto[i+1][1]
        entropy_start = sorted_pareto[i][0]
        entropy_end = sorted_pareto[i+1][0]
        area = (entropy_end - entropy_start) * pace_end
        if area < 0:
            raise Exception("A")
        total_area += area
    return total_area


def __compute_stats(num_epochs, num_populations, fitness_eval):
    max_values = numpy.zeros([num_populations, num_epochs + 1])
    min_values = numpy.zeros([num_populations, num_epochs + 1])
    avg_values = numpy.zeros([num_populations, num_epochs + 1])

    for checkpoint_idx in range(num_epochs + 1):
        for population_idx in range(num_populations):
            (_, population, _) = __load_checkpoint(
                GAME_DATA_FOLDER + str(population_idx) + "/checkpoints/checkpoint_epoch_" + str(checkpoint_idx) + ".pkl")
            epoch_values = []
            for individual in population:
                epoch_values.append(fitness_eval(individual))
            epoch_values = numpy.transpose(epoch_values)
            avg_values[population_idx][checkpoint_idx] = numpy.mean(epoch_values)
            min_values[population_idx][checkpoint_idx] = numpy.mean(epoch_values)
            max_values[population_idx][checkpoint_idx] = numpy.mean(epoch_values)

    return numpy.mean(min_values, 0), numpy.mean(avg_values, 0), numpy.max(max_values, 0),


def __plot_stats_progression(num_epochs, num_populations):
    total_population = []
    for checkpoint_idx in range(num_epochs + 1):
        for population_idx in range(num_populations):
            (_, population, _) = __load_checkpoint(
                GAME_DATA_FOLDER + str(population_idx) + "/checkpoints/checkpoint_epoch_" + str(checkpoint_idx) + ".pkl")
            for individual in population:
                # TODO not needed with new version
                # data = extract_match_data(
                #     GAME_DATA_FOLDER + str(population_idx) + "/Export/genome_evolution/final_results_" + str(individual.epoch) + '_' + str(individual.number_in_epoch) + '_',
                #     NUM_PARALLEL_SIMULATIONS
                # )
                # individual.data = data
                # end not needed section
                total_population.append(individual)

    # Group by entropy
    groups = {}

    for individual in total_population:
        entropy = individual.fitness.values[0]
        group_idx = int(entropy / 0.025)
        accuracies = groups.get(group_idx, [])
        accuracies.append(individual)
        groups[group_idx] = accuracies

    accuracies_sniper = {}
    accuracies_shotgun = {}

    for key, value in groups.items():
        if key == 0:
            continue
        avg_accuracy_shotgun = numpy.mean([x.data["accuracy1"] for x in value])
        avg_accuracy_sniper = numpy.mean([x.data["accuracy2"] for x in value])
        accuracies_sniper[key] = avg_accuracy_sniper
        accuracies_shotgun[key] = avg_accuracy_shotgun
        pass

    keys, values = zip(*sorted(accuracies_sniper.items()))

    fig = plt.figure()
    ax = fig.add_axes([0.1, 0.1, 0.6, 0.75])
    ax.plot([x * 0.025 for x in keys], values, label="accuracy sniper")
    ax.legend(bbox_to_anchor=(1.05, 1), loc='upper left', borderaxespad=0.)
    keys, values = zip(*sorted(accuracies_shotgun.items()))
    ax.plot([x * 0.025 for x in keys], values, label="accuracy shotgun")
    ax.legend(bbox_to_anchor=(1.05, 1), loc='upper left', borderaxespad=0.)
    plt.xlabel("Entropy")
    plt.savefig(GAME_DATA_FOLDER + "Analysis/entropy.png", bbox_inches='tight')
    plt.close()


def __export_checkpoint_results(num_epochs, num_populations):
    (min_fitness, avg_fitness, max_fitness) = __compute_stats(num_epochs, num_populations, lambda x: __fitness_value(x))
    (min_entropy, avg_entropy, max_entropy) = __compute_stats(num_epochs, num_populations,
                                                              lambda x: x.fitness.values[0])
    (min_pace, avg_pace, max_pace) = __compute_stats(num_epochs, num_populations, lambda x: x.fitness.values[1])
    __plot_graph(
        [min_fitness, avg_fitness, max_fitness],
        [min_entropy, avg_entropy, max_entropy],
        [min_pace, avg_pace, max_pace],
    )


def __fitness_value(elem):
    return numpy.dot(elem.fitness.values, [0.75, 0.25])


def __plot_graph(fitnesses, entropies, paces):
    x_range = numpy.array([x for x in range(len(fitnesses[0]))])
    fig = plt.figure()
    ax = fig.add_axes([0.1, 0.1, 0.6, 0.75])
    __plot(ax, x_range, fitnesses[1], "Fitness avg")
    __plot(ax, x_range, fitnesses[2], "Fitness max")

    __plot(ax, x_range, entropies[1], "Entropy avg")
    __plot(ax, x_range, entropies[2], "Entropy max")

    __plot(ax, x_range, paces[1], "Paces avg")
    __plot(ax, x_range, paces[2], "Paces max")

    plt.xlabel("Epoch")
    plt.savefig(GAME_DATA_FOLDER + "Analysis/evolution.png", bbox_inches='tight')
    plt.close()


def __plot(ax, x, y, label):
    ax.plot(x, y, label=label)
    ax.legend(bbox_to_anchor=(1.05, 1), loc='upper left', borderaxespad=0.)


def __rank_population(num_epochs, num_populations):
    individuals = []
    pareto = tools.ParetoFront()
    for population_idx in range(num_populations):
        (_, population, _) = __load_checkpoint(
            GAME_DATA_FOLDER + str(population_idx) + "/checkpoints/checkpoint_epoch_" + str(num_epochs) + ".pkl"
        )
        for pop in population:
            pop.population = population_idx
        individuals.extend(population)
    pareto.update(individuals)

    pareto = sorted(pareto, key=lambda it: it.fitness.values, reverse=True)

    for idx, front in enumerate(pareto):
        save_elem_info(front, idx)


def __read_map(individual):
    experiment_name = str(individual.epoch) + "_" + str(individual.number_in_epoch)
    with open(GAME_DATA_FOLDER + str(individual.population) + "/Export/genome_evolution/map_" + experiment_name +
              "_0.txt", "r") as map_file:
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
    return map_matrix


def __save_heatmap(x, y, path, map_matrix):
    heatmap, xedges, yedges = numpy.histogram2d(
        x,
        y,
        bins=[[i for i in range(len(map_matrix[0]))], [i for i in range(len(map_matrix))]]
    )
    heatmap = gaussian_filter(heatmap, sigma=2.0)
    plt.imshow(heatmap.T, cmap=cm.jet)
    plt.imshow(map_matrix, cmap=cm.Greys, alpha=0.2)
    plt.axis('off')
    plt.savefig(path, bbox_inches='tight')
    plt.clf()
    plt.close()


def __save_traces(start_positions, end_positions, folder, experiment_name, map_matrix):
    plt.axis('off')
    for idx in range(0, len(start_positions)):
        start_pos = start_positions[idx]
        end_pos = end_positions[idx]
        x = [start_pos[0], end_pos[0]]
        y = [start_pos[1], end_pos[1]]

        distance = sqrt(pow(x[0] - x[1], 2) + pow(y[0] - y[1], 2))
        distance_mapped = min(distance / 50, 1.0)
        color = (distance_mapped, 1.0 - distance_mapped, 0)
        plt.plot(x, y, linewidth=0.3, linestyle="-", color=color)
    plt.imshow(map_matrix, cmap=cm.Greys, alpha=0.2)
    if folder is None:
        plt.show()
    else:
        plt.savefig(folder + experiment_name + ".png", bbox_inches='tight')
    plt.clf()
    plt.close()


def save_elem_info(elem, i):
    elem_path = GAME_DATA_FOLDER + "Analysis/" + str(i) + "/"
    if not os.path.exists(elem_path):
        os.mkdir(elem_path)
    experiment_name = str(elem.epoch) + "_" + str(elem.number_in_epoch)
    map_matrix = __read_map(elem)

    fitness_file_name = "fitness_" + str(__fitness_value(elem)) + ".txt"
    open(elem_path + fitness_file_name, "w").close()
    values_file_name = "values_" + str(elem.fitness.values) + ".txt"
    open(elem_path + values_file_name, "w").close()

    base_folder = GAME_DATA_FOLDER + str(elem.population) + "/Export/genome_evolution/"

    __save_positions(base_folder, elem, elem_path, experiment_name, map_matrix)
    __save_deaths_and_kills(base_folder, elem, elem_path, experiment_name, map_matrix)


def __save_positions(base_folder, elem, elem_path, experiment_name, map_matrix):
    for i in range(0, 2):
        (position_x, position_y) = extract_bot_positions(
            base_folder,
            experiment_name,
            i,
        )
        position_x = [x / elem.mapScale for x in position_x]
        position_y = [x / elem.mapScale for x in position_y]
        __save_heatmap(position_x, position_y, elem_path + "positions_bot_" + str(i), map_matrix)


def __save_deaths_and_kills(base_folder, elem, elem_path, experiment_name, map_matrix):
    for i in range(0, 2):
        (deaths_x, deaths_y) = extract_death_positions(
            base_folder,
            experiment_name,
            i,
        )
        deaths_x = [x / elem.mapScale for x in deaths_x]
        deaths_y = [x / elem.mapScale for x in deaths_y]
        __save_heatmap(deaths_x, deaths_y, elem_path + "deaths_bot_" + str(i), map_matrix)

        (kills_x, kills_y) = extract_kill_positions(
            base_folder,
            experiment_name,
            i,
        )
        kills_x = [x / elem.mapScale for x in kills_x]
        kills_y = [x / elem.mapScale for x in kills_y]
        __save_heatmap(kills_x, kills_y, elem_path + "kills_bot_" + str(1-i), map_matrix)

        death_pos = [[x, y] for x, y in zip(deaths_x, deaths_y)]
        kill_pos = [[x, y] for x, y in zip(kills_x, kills_y)]

        __save_traces(death_pos, kill_pos, elem_path, "kill_traces_bot_" + str(1-i), map_matrix)


def __save_kills(base_folder, elem, elem_path, experiment_name, map_matrix):
    for i in range(0, 2):
        (position_x, position_y) = extract_kill_positions(
            base_folder,
            experiment_name,
            1-i,
        )
        position_x = [x / elem.mapScale for x in position_x]
        position_y = [x / elem.mapScale for x in position_y]
        __save_heatmap(position_x, position_y, elem_path + "kills_bot_" + str(i), map_matrix)


if __name__ == "__main__":
    prepare_toolbox()
    parser = argparse.ArgumentParser(description='Rank populations.')

    parser.add_argument("--num_epochs", default=20, type=int, dest="num_epochs")
    parser.add_argument("--num_populations", default=4, type=int, dest="num_populations")
    args = parser.parse_args(sys.argv[1:])

    __plot_epoch_paretos(args.num_epochs, args.num_populations)
    __export_checkpoint_results(args.num_epochs, args.num_populations)
    __rank_population(args.num_epochs, args.num_populations)
    __plot_stats_progression(args.num_epochs, args.num_populations)

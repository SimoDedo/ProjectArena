import argparse
import os
import pickle
import sys
from math import sqrt, floor

import matplotlib.pyplot as plt
import numpy
from deap import tools
from matplotlib import cm, colors
from matplotlib.colors import ListedColormap, LinearSegmentedColormap
from scipy.ndimage import gaussian_filter

from internals.constants import GAME_DATA_FOLDER, NUM_PARALLEL_SIMULATIONS
from internals.result_extractor import extract_bot_positions, extract_death_positions, extract_kill_positions, \
    extract_match_data
from internals.toolbox import prepare_toolbox


def __load_checkpoint(name):
    with open(name, "rb") as cp_file:
        cp = pickle.load(cp_file, errors='none')
        population = cp["population"]
        epoch = cp["epoch"]
        data = cp["data"]
        return epoch, population, data


def __plot_pareto(population, epoch, all_fitnesses, pareto_front):
    entropies, paces, = zip(*[fitness for fitness in all_fitnesses])
    fig = plt.figure()
    fig.set_size_inches(10, 10)
    axe = plt.subplot2grid((1, 1), (0, 0))
    axe.set_ylabel('Pace', fontsize=15)
    axe.scatter(entropies, paces, c='black', marker='x', s=15)

    sorted_pareto = sorted([x.fitness.values for x in pareto_front])
    pareto_entropy, pareto_pace, = zip(*sorted_pareto)

    axe.scatter(pareto_entropy, pareto_pace, c='blue', marker='o', s=70)
    axe.plot(pareto_entropy, pareto_pace, linewidth=5)
    
    axe.set_xlabel('Entropy', fontsize=15)
    axe.grid(linestyle='-', linewidth=0.4)
    plt.xlim([0.6, 1.05])
    plt.ylim([0.2, 0.92])
    plt.savefig(GAME_DATA_FOLDER + "Analysis/pareto_front_" + str(population) + "_" + str(epoch) + ".png", bbox_inches='tight')
    plt.close()


def __fitness_similarity(a, b):
    entropy_a = a.fitness.values[0]
    entropy_b = b.fitness.values[0]
    pace_a = a.fitness.values[1]
    pace_b = b.fitness.values[1]
    return abs(entropy_a - entropy_b) <= 0.01 and abs(pace_a - pace_b) <= 0.01


def __plot_epoch_paretos_total(num_epochs, num_populations):
    pareto_areas_mean = []
    pareto_areas_std = []
    for checkpoint_idx in range(num_epochs + 1):
        pareto = tools.ParetoFront(__fitness_similarity)
        populations_individuals = []
        populations_fitnesses = []
        epoch_areas = []
        for population_idx in range(num_populations):
            (_, population, all_fitnesses) = __load_checkpoint(
                GAME_DATA_FOLDER + str(population_idx) + "/checkpoints/checkpoint_epoch_" + str(checkpoint_idx) + ".pkl")
            populations_individuals.extend(population)
            populations_fitnesses.extend(all_fitnesses)
            population_pareto = tools.ParetoFront(__fitness_similarity)
            population_pareto.update(population)
            epoch_areas.append(__compute_pareto_area(population_pareto))
        pareto.update(populations_individuals)
        __plot_pareto("total", checkpoint_idx, populations_fitnesses, pareto)
        pareto_areas_mean.append(numpy.mean(epoch_areas))
        pareto_areas_std.append(numpy.std(epoch_areas))
    fig = plt.figure()
    ax = fig.add_axes([0.1, 0.1, 0.6, 0.6])
    ax.plot([x for x in range(num_epochs + 1)], pareto_areas_mean)

    means = numpy.array(pareto_areas_mean, dtype=numpy.float64)
    std = numpy.array(pareto_areas_std, dtype=numpy.float64)

    ax.fill_between(
        [x for x in range( num_epochs + 1)],
        means - std,
        means + std,
        alpha=0.3,
    )
    plt.xlabel("Epoch")
    ax.set_yticks([0.5 + 0.05*x for x in range(11)])
    ax.grid(linestyle='-', linewidth=0.4)
    plt.savefig(GAME_DATA_FOLDER + "Analysis/pareto_evolution.png", bbox_inches='tight')
    plt.close()


def __plot_epoch_paretos_single(num_epochs, population_idx):
    populations_individuals = []
    populations_fitnesses = []
    for checkpoint_idx in range(num_epochs + 1):
        (_, population, all_fitnesses) = __load_checkpoint(
            GAME_DATA_FOLDER + str(population_idx) + "/checkpoints/checkpoint_epoch_" + str(checkpoint_idx) + ".pkl")
        populations_individuals.extend(population)
        populations_fitnesses.extend(all_fitnesses)
    pareto = tools.ParetoFront(__fitness_similarity)
    pareto.update(populations_individuals)
    __plot_pareto("population_" + str(population_idx), num_epochs, populations_fitnesses, pareto)


def __compute_pareto_area(pareto_front):
    sorted_pareto = sorted([x.fitness.values for x in pareto_front])
    total_area = 0
    sorted_pareto.insert(0, [0, sorted_pareto[0][1]])
    for i in range(len(sorted_pareto) - 1):
        pace_end = sorted_pareto[i + 1][1]
        entropy_start = sorted_pareto[i][0]
        entropy_end = sorted_pareto[i + 1][0]
        area = (entropy_end - entropy_start) * pace_end
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


def __plot_stats_entropy_relation(num_epochs, num_populations):
    total_population = []
    for checkpoint_idx in range(num_epochs + 1):
        for population_idx in range(num_populations):
            (_, population, _) = __load_checkpoint(
                GAME_DATA_FOLDER + str(population_idx) + "/checkpoints/checkpoint_epoch_" + str( checkpoint_idx) + ".pkl")
            total_population.extend(population)

    # Group by entropy
    groups = {}
    group_size = 0.025

    for individual in total_population:
        entropy = individual.fitness.values[0]
        if entropy < 0.45: continue
        group_idx = int(entropy / group_size)
        accuracies = groups.get(group_idx, [])
        accuracies.append(individual)
        groups[group_idx] = accuracies

    sight_loss_rate = {}
    accuracies_sniper = {}
    accuracies_shotgun = {}
    paces = {}

    kills_sniper = {}
    kills_shotgun = {}

    for key, value in groups.items():
        if key == 0:
            continue
        sight_loss_rate[key] = __extract_stats(value, "sightLossRate")
        accuracies_sniper[key] = __extract_stats(value, "accuracy2")
        accuracies_shotgun[key] = __extract_stats(value, "accuracy1")
        kills_sniper[key] = __extract_stats(value, "numberOfFrags2")
        kills_shotgun[key] = __extract_stats(value, "numberOfFrags1")
        paces[key] = __extract_stats(value, "pace")

    fig, ax1 = plt.subplots()

    ax1.set_ylim([0.3, 0.9])
    ax1.set_xlabel('Entropy')
    ax1.grid(linestyle='-', linewidth=0.4)

    keys, values = zip(*sorted(accuracies_shotgun.items()))
    y_values, y_errors = numpy.transpose(values)
    x_values = [x * group_size for x in keys]
    p1, = ax1.plot(x_values, y_values, "C0", linestyle='dashed', label="Accuracy shotgun")
    # ax1.fill_between(
    #     x_values,
    #     y_values - y_errors,
    #     y_values + y_errors,
    #     alpha=0.3,
    # )

    keys, values = zip(*sorted(accuracies_sniper.items()))
    y_values, y_errors = numpy.transpose(values)
    x_values = [x * group_size for x in keys]
    p2, = ax1.plot(x_values, y_values, "C1", linestyle='dashed', label="Accuracy sniper")
    # # ax1.fill_between(
    # #     x_values,
    # #     y_values - y_errors,
    # #     y_values + y_errors,
    # #     alpha=0.3,
    # # )

    keys, values = zip(*sorted(paces.items()))
    y_values, y_errors = numpy.transpose(values)
    x_values = [x * group_size for x in keys]
    p3, = ax1.plot(x_values, y_values, "C2", label="Pace")
    # # ax1.fill_between(
    # #     x_values,
    # #     y_values - y_errors,
    # #     y_values + y_errors,
    # #     alpha=0.3,
    # # )
    #
    ax2 = ax1.twinx()  # instantiate a second axes that shares the same x-axis
    ax2.set_ylim([10, 190])
    ax2.set_xlim([0.74, 0.98])

    keys, values = zip(*sorted(kills_shotgun.items()))
    y_values, y_errors = numpy.transpose(values)
    x_values = [x * group_size for x in keys]
    p4, = ax2.plot(x_values, y_values, "C0", label="Frags shotgun")
    # ax2.fill_between(
    #     x_values,
    #     y_values - y_errors,
    #     y_values + y_errors,
    #     alpha=0.3,
    # )

    keys, values = zip(*sorted(kills_sniper.items()))
    y_values, y_errors = numpy.transpose(values)
    x_values = [x * group_size for x in keys]
    p5, = ax2.plot(x_values, y_values, "C1", label="Frags sniper")
    # # ax2.fill_between(
    # #     x_values,
    # #     y_values - y_errors,
    # #     y_values + y_errors,
    # #     alpha=0.3,
    # # )

    ax1.legend(
        handles=[p4, p1, p5, p2, p3],
        loc='upper center',
        bbox_to_anchor=(0.5, 1.20),
        ncol=3,
        fancybox=True,
        shadow=False,
    )
    fig.tight_layout()  # otherwise the right y-label is slightly clipped
    plt.savefig(GAME_DATA_FOLDER + "Analysis/entropy_mix.png", bbox_inches='tight')
    plt.close()


def __extract_stats(individuals, key):
    values = [x.dataset[key] for x in individuals]
    return numpy.mean(values), numpy.std(values)


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


def __rank_population_total(num_epochs, num_populations):
    total_to_print = 2
    individuals = []
    pareto = tools.ParetoFront(__fitness_similarity)
    for population_idx in range(num_populations):
        (_, population, _) = __load_checkpoint(
            GAME_DATA_FOLDER + str(population_idx) + "/checkpoints/checkpoint_epoch_" + str(num_epochs) + ".pkl"
        )
        for pop in population:
            pop.population = population_idx
        individuals.extend(population)
    pareto.update(individuals)

    best_fitness = sorted(pareto, key=__fitness_value, reverse=True)
    for idx, front in enumerate(best_fitness[0:total_to_print]):
        __save_elem_info(front, idx, "total_fitness")

    best_entropy = sorted(pareto, key=lambda it: it.fitness.values[0], reverse=True)
    for idx, front in enumerate(best_entropy[0:total_to_print]):
        __save_elem_info(front, idx, "total_entropy")

    best_pace = sorted(pareto, key=lambda it: it.fitness.values[1], reverse=True)
    for idx, front in enumerate(best_pace[0:total_to_print]):
        __save_elem_info(front, idx, "total_pace")


def __rank_population_single(num_epochs, population_idx):
    total_to_print = 1
    individuals = []
    pareto = tools.ParetoFront(__fitness_similarity)
    (_, population, _) = __load_checkpoint(
        GAME_DATA_FOLDER + str(population_idx) + "/checkpoints/checkpoint_epoch_" + str(num_epochs) + ".pkl"
    )
    for pop in population:
        pop.population = population_idx
    individuals.extend(population)
    pareto.update(individuals)

    best_fitness = sorted(pareto, key=__fitness_value, reverse=True)
    for idx, front in enumerate(best_fitness[0:total_to_print]):
        __save_elem_info(front, idx, str(population_idx) + "_fitness")

    best_entropy = sorted(pareto, key=lambda it: it.fitness.values[0], reverse=True)
    for idx, front in enumerate(best_entropy[0:total_to_print]):
        __save_elem_info(front, idx, str(population_idx) + "_entropy")

    best_pace = sorted(pareto, key=lambda it: it.fitness.values[1], reverse=True)
    for idx, front in enumerate(best_pace[0:total_to_print]):
        __save_elem_info(front, idx, str(population_idx) + "_pace")


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
        bins=[[i for i in range(len(map_matrix[0]) + 1)], [i for i in range(len(map_matrix) + 1)]]
    )
    heatmap = gaussian_filter(heatmap.T, sigma=3.0)
    plt.axis('off')

    mask = numpy.matrix(map_matrix)
    mask = numpy.ma.masked_where(mask == 0, mask)

    cmap = LinearSegmentedColormap.from_list('mycmap',
                                             ['#FFFFFF', '#FFFFFF', '#7777FF', '#0000FF', '#007777', '#00FF00',
                                              '#FFFF00', 'red'])

    plt.contourf(heatmap, cmap=cmap, levels=50, zorder=0)
    plt.imshow(mask, cmap='binary_r', zorder=1)

    # plt.show()
    plt.savefig(path, bbox_inches='tight')
    plt.clf()
    plt.close()


def __save_map(path, map_matrix):
    plt.imshow(map_matrix, cmap=cm.Greys, alpha=1.0)
    plt.axis('off')
    plt.savefig(path, bbox_inches='tight')
    plt.clf()
    plt.close()


def __save_traces(start_positions, end_positions, folder, experiment_name, map_matrix):
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
        plt.plot(x, y, linewidth=1, linestyle="-", color=color, antialiased=True)
        plt.scatter(end_pos[0], end_pos[1], marker='o', facecolors='none', edgecolors=color, s=12)

    plt.imshow(map_matrix, cmap='binary_r', alpha=1.0)
    plt.savefig(folder + experiment_name + ".png", dpi=110, bbox_inches='tight')
    plt.close()


def __save_elem_info(elem, i, population):
    elem_path = GAME_DATA_FOLDER + "Analysis/" + str(population) + "_" + str(i) + "/"
    if not os.path.exists(elem_path):
        os.mkdir(elem_path)
    experiment_name = str(elem.epoch) + "_" + str(elem.number_in_epoch)
    map_matrix = __read_map(elem)

    fitness_file_name = "fitness_" + str(__fitness_value(elem)) + ".txt"
    open(elem_path + fitness_file_name, "w").close()
    values_file_name = "values_" + str(elem.fitness.values) + ".txt"
    open(elem_path + values_file_name, "w").close()

    base_folder = GAME_DATA_FOLDER + str(elem.population) + "/Export/genome_evolution/"

    __save_map(elem_path + "map", map_matrix)
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
        __save_heatmap(kills_x, kills_y, elem_path + "kills_bot_" + str(1 - i), map_matrix)

        death_pos = [[x, y] for x, y in zip(deaths_x, deaths_y)]
        kill_pos = [[x, y] for x, y in zip(kills_x, kills_y)]

        __save_traces(death_pos, kill_pos, elem_path, "kill_traces_bot_" + str(1 - i), map_matrix)


def __save_kills(base_folder, elem, elem_path, experiment_name, map_matrix):
    for i in range(0, 2):
        (position_x, position_y) = extract_kill_positions(
            base_folder,
            experiment_name,
            1 - i,
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

    __plot_stats_entropy_relation(args.num_epochs, args.num_populations)
    
    __plot_epoch_paretos_total(args.num_epochs, args.num_populations)
    for idx in range(args.num_populations):
        __plot_epoch_paretos_single(args.num_epochs, idx)

    __rank_population_total(args.num_epochs, args.num_populations)
    for idx in range(args.num_populations):
        __rank_population_single(args.num_epochs, idx)

    __export_checkpoint_results(args.num_epochs, args.num_populations)

import argparse
import os
import pickle
import random
import sys
import time
from concurrent.futures.thread import ThreadPoolExecutor
from multiprocessing import Lock

import numpy

from internals.constants import EVOLVER_MATE_PROBABILITY, EVOLVER_MUTATE_PROBABILITY, GAME_DATA_FOLDER, NUM_PARALLEL_FITNESS_CALCULATION
from internals.toolbox import prepare_toolbox
from deap import tools


__known_maps = dict()
__known_maps_lock = Lock()


def __evolve_population(num_epochs, population_size, bot1_data, bot2_data, checkpoint):
    toolbox = prepare_toolbox()
    all_fitnesses = []

    if checkpoint is not None:
        with open(checkpoint, "rb") as cp_file:
            checkpoint_data = pickle.load(cp_file)
            pop = checkpoint_data["population"]
            starting_epoch = checkpoint_data["epoch"]
            all_fitnesses = checkpoint_data["data"]
            random.setstate(checkpoint_data['random_state'])
    else:
        starting_epoch = 0
        seed = time.time()
        print("Using seed " + str(seed))
        random.seed(seed)
        pop = []
        current_cycle = 0
        while len(pop) < population_size:
            remaining_individuals = population_size - len(pop)
            print("Remaining " + str(remaining_individuals) + " to test!")
            individuals_to_test = [toolbox.individual() for _ in range(remaining_individuals)]
            __add_epoch_data(individuals_to_test, 0, current_cycle * population_size)
            fitness = __parallel_execute(bot1_data, bot2_data, individuals_to_test, toolbox)
            for x in range(len(fitness)):
                if fitness[x][0] != 0:
                    individuals_to_test[x].fitness.values = fitness[x]
                    pop.append(individuals_to_test[x])
                else:
                    print("Drop individual " + str(x) + " due to invalid fitness")

        fitnesses = [i.fitness.values for i in pop]
        all_fitnesses.extend(fitnesses)
        for ind, fit in zip(pop, fitnesses):
            ind.fitness.values = fit

        # This is just to assign the crowding distance to the individuals
        # no actual selection is done
        pop = toolbox.select(pop, len(pop))
        __save_checkpoint(all_fitnesses, 0, pop)

    for epoch in range(starting_epoch + 1, num_epochs + 1):
        print("-- Generation %i --" % epoch)

        # Vary the population
        offspring = tools.selTournamentDCD(pop, len(pop))
        offspring = [toolbox.clone(ind) for ind in offspring]

        for ind1, ind2 in zip(offspring[::2], offspring[1::2]):
            if random.random() <= EVOLVER_MATE_PROBABILITY:
                toolbox.mate(ind1, ind2)
                del ind1.fitness.values, ind2.fitness.values

        for mutant in offspring:
            if random.random() < EVOLVER_MUTATE_PROBABILITY:
                toolbox.mutate(mutant)
                del mutant.fitness.values

        invalid_individuals = [ind for ind in offspring if not ind.fitness.valid]
        __add_epoch_data(invalid_individuals, epoch)
        print("Recalculate fitness for " + str(len(invalid_individuals)) + " individuals")

        fitnesses = __parallel_execute(bot1_data, bot2_data, invalid_individuals, toolbox)

        all_fitnesses.extend(fitnesses)

        for ind, fit in zip(invalid_individuals, fitnesses):
            ind.fitness.values = fit

        # Select the next generation population
        pop = toolbox.select(pop + offspring, population_size)
        __save_checkpoint(all_fitnesses, epoch, pop)

    return pop, all_fitnesses


def __add_epoch_data(invalid_individuals, epoch, starting_idx=0):
    for idx, invalid_ind in enumerate(invalid_individuals):
        invalid_ind.epoch = epoch
        invalid_ind.number_in_epoch = starting_idx + idx


def __parallel_execute(bot1_data, bot2_data, individuals, toolbox):
    with ThreadPoolExecutor(NUM_PARALLEL_FITNESS_CALCULATION) as executor:
        fitnesses = []
        futures = []
        for i in range(len(individuals)):
            futures.append(
                executor.submit(
                    __print_info_and_evaluate, toolbox, individuals[i], bot1_data, bot2_data,
                )
            )
        for future in futures:
            fitnesses.append(future.result())
    return fitnesses


def __save_checkpoint(all_fitnesses, epoch, pop):
    checkpoint_data = dict(
        population=pop,
        epoch=epoch,
        random_state=random.getstate(),
        data=all_fitnesses,
    )
    with open(os.path.join(GAME_DATA_FOLDER, "checkpoints", "checkpoint_epoch_" + str(epoch) + ".pkl"), "wb") as cp_file:
        pickle.dump(checkpoint_data, cp_file)
        print("Persisted generation for epoch " + str(epoch))


def __print_info_and_evaluate(toolbox, individual, bot1_data, bot2_data):
    print("Calculating fitness of individual num " + str(individual.number_in_epoch) + " for epoch " + str(individual.epoch))
    phenotype = individual.phenotype()
    map_matrix = phenotype.map_matrix()
    with __known_maps_lock:
        if map_matrix in __known_maps:
            print("Used cached fitness value for " + str(individual.epoch) + "_" + str(individual.number_in_epoch))
            cached_value = __known_maps.get(map_matrix)
            individual.epoch = cached_value[1]
            individual.number_in_epoch = cached_value[2]
            individual.dataset = cached_value[3]
            return cached_value[0]

    dataset = toolbox.evaluate(phenotype, individual.epoch, individual.number_in_epoch, bot1_data, bot2_data).to_dict(orient="list")
    __print_dataset_info(dataset)
    fitness = round(numpy.mean(dataset["entropy"]), 3), round(numpy.mean(dataset["pace"]), 3)
    individual.dataset = dataset

    with __known_maps_lock:
        __known_maps[map_matrix] = [fitness, individual.epoch, individual.number_in_epoch, dataset]
        return fitness


def __print_dataset_info(dataset):
    # Add other statistics if useful
    __print_stats(dataset, "entropy")
    __print_stats(dataset, "ratio")
    __print_stats(dataset, "pace")
    # __print_stats(dataset, "killStreakAverage1")
    # __print_stats(dataset, "killStreakAverage2")


def __print_stats(dataset, key):
    values = dataset[key]
    decimals = 5
    min_value = round(numpy.min(values), decimals)
    max_value = round(numpy.max(values), decimals)
    mean_value = round(numpy.mean(values), decimals)
    std_dev = round(numpy.std(values), decimals)
    rel_std_dev = round(std_dev / mean_value * 100, 2)
    print(
        key + " mean: " + str(mean_value) + ", stdDev: " + str(std_dev) + ", relStdDev: " + str(rel_std_dev) +
        ", min: " + str(min_value) + ", max: " + str(max_value)
    )


def __evolver():
    parser = argparse.ArgumentParser(description='Evolve population.')

    parser.add_argument("--num_epochs", default=30, type=int, dest="num_epochs")
    parser.add_argument("--population_size", default=50, type=int, dest="population_size")
    parser.add_argument("--bot1_file_prefix", required=True, dest="bot1_file")
    parser.add_argument("--bot1_skill", required=True, dest="bot1_skill")
    parser.add_argument("--bot2_file_prefix", required=True, dest="bot2_file")
    parser.add_argument("--bot2_skill", required=True, dest="bot2_skill")
    parser.add_argument("--checkpoint_file", default=None, dest="checkpoint")
    args = parser.parse_args(sys.argv[1:])

    bot1_data = {"file": args.bot1_file, "skill": args.bot1_skill}
    bot2_data = {"file": args.bot2_file, "skill": args.bot2_skill}

    __evolve_population(args.num_epochs, args.population_size, bot1_data, bot2_data, args.checkpoint)


if __name__ == "__main__":
    __evolver()

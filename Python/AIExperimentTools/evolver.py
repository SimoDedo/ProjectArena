import argparse
import pickle
import random
import sys
import time

import numpy
import pandas

from internals import stats
from internals.constants import EVOLVER_MATE_PROBABILITY, EVOLVER_MUTATE_PROBABILITY, GAME_DATA_FOLDER
from internals.toolbox import prepare_toolbox
from deap import tools


def __evolve_population(num_epochs, population_size, bot1_data, bot2_data, checkpoint):
    toolbox = prepare_toolbox()
    pareto = tools.ParetoFront()
    all_fitnesses = []

    if checkpoint is not None:
        with open(checkpoint, "rb") as cp_file:
            checkpoint_data = pickle.load(cp_file)
            pop = checkpoint_data["population"]
            starting_epoch = checkpoint_data["epoch"]
            all_fitnesses = checkpoint_data["data"]
            pareto = checkpoint_data["pareto_front"]
            random.setstate(checkpoint_data['random_state'])
    else:
        starting_epoch = 0
        seed = time.time()
        print("Using seed " + str(seed))
        random.seed(seed)
        pop = []
        epoch_phenotypes = dict()
        while len(pop) < population_size:
            individual = toolbox.individual()
            fitness = __print_info_and_evaluate(toolbox, individual, 0, len(pop), bot1_data, bot2_data,
                                                epoch_phenotypes)
            if fitness[0] < 0.9 and fitness[0] != 0:
                individual.fitness.values = fitness
                pop.append(individual)
            else:
                print("Drop individual due to invalid fitness")

        fitnesses = [i.fitness.values for i in pop]
        all_fitnesses.extend(fitnesses)
        for ind, fit in zip(pop, fitnesses):
            ind.fitness.values = fit

        # This is just to assign the crowding distance to the individuals
        # no actual selection is done
        pop = toolbox.select(pop, len(pop))
        pareto.update(pop)
        __save_checkpoint(all_fitnesses, 0, pareto, pop)

    for epoch in range(starting_epoch + 1, num_epochs + 1):
        epoch_phenotypes = dict()
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

        invalid_ind = [ind for ind in offspring if not ind.fitness.valid]

        print("Recalculate fitness for " + str(len(invalid_ind)) + " individuals")

        fitnesses = [
            __print_info_and_evaluate(toolbox, v, epoch, i, bot1_data, bot2_data, epoch_phenotypes)
            for i, v in enumerate(invalid_ind)
        ]

        all_fitnesses.extend(fitnesses)

        for ind, fit in zip(invalid_ind, fitnesses):
            ind.fitness.values = fit

        # Select the next generation population
        pop = toolbox.select(pop + offspring, population_size)
        pareto.update(pop)
        __save_checkpoint(all_fitnesses, epoch, pareto, pop)

    return pop, pareto, all_fitnesses


def __save_checkpoint(all_fitnesses, epoch, pareto_front, pop):
    checkpoint_data = dict(
        population=pop,
        epoch=epoch,
        random_state=random.getstate(),
        data=all_fitnesses,
        pareto_front=pareto_front,
    )
    with open(GAME_DATA_FOLDER + "checkpoints/checkpoint_epoch_" + str(epoch) + ".pkl", "wb") as cp_file:
        pickle.dump(checkpoint_data, cp_file)
        print("Persisted generation for epoch " + str(epoch))


def __print_info_and_evaluate(toolbox, individual, epoch, individual_number, bot1_data, bot2_data, known_phenotypes):
    print("Calculating fitness of individual num " + str(individual_number) + " for epoch " + str(epoch))
    phenotype = individual.phenotype()
    if phenotype in known_phenotypes:
        print("Used cached fitness value for " + str(epoch) + "_" + str(individual_number))
        cached_value = known_phenotypes.get(phenotype)
        individual.epoch = cached_value[1]
        individual.number_in_epoch = cached_value[2]
        individual.dataset = cached_value[3]
        return cached_value[0]
    individual.epoch = epoch
    individual.number_in_epoch = individual_number
    if individual.unscaled_area(phenotype) < 300:
        # penalize small phenotypes
        print("AAA small phenotype!")
        known_phenotypes[phenotype] = [(0, 0), epoch, individual_number, pandas.DataFrame().to_dict()]
        return 0, 0
    dataset = toolbox.evaluate(phenotype, epoch, individual_number, bot1_data, bot2_data).to_dict()
    __print_dataset_info(dataset)
    fitness = numpy.mean(dataset["entropy"]), numpy.mean(dataset["pace"])
    individual.dataset = dataset
    known_phenotypes[phenotype] = [fitness, epoch, individual_number, dataset]
    return fitness


def __print_dataset_info(dataset):
    # Add other statistics if useful
    __print_stats(dataset, "entropy")
    __print_stats(dataset, "ratio")
    __print_stats(dataset, "pace")


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

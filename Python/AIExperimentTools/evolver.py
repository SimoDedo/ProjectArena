import argparse
import pickle
import random
import sys
import time

from deap import algorithms

from internals.constants import EVOLVER_MATE_PROBABILITY, EVOLVER_MUTATE_PROBABILITY
from internals.toolbox import prepare_toolbox

# TODO rename fenotype to phenotype, dum dum

known_fenotypes = dict()


def penalize_repeated_phenotypes(individuals):
    phenotypes = dict()
    for individual in individuals:
        phenotype = individual.phenotype()
        if phenotype in phenotypes:
            phenotypes[phenotype] = phenotypes[phenotype] + 1
        else:
            phenotypes[phenotype] = 1

    # How much to penalize?
    penalization = [(phenotypes[individual.phenotype()] - 1) / (2 * len(individuals)) for individual in individuals]
    return penalization


def small_phenotype_penalization(individual):
    phenotype = individual.phenotype().areas
    room_count = sum(map(lambda x: not x.isCorridor, phenotype))
    if room_count <= 3:
        return 1.0
    elif room_count <= 6:
        return 0.5
    else:
        return 0.0


def evolve_population(num_epochs, population_size, bot1_data, bot2_data, checkpoint):
    toolbox = prepare_toolbox()

    if checkpoint:
        # A file name has been given, then load the data from the file
        with open(checkpoint, "rb") as cp_file:
            checkpoint_data = pickle.load(cp_file)
            population = checkpoint_data["population"]
            epoch = checkpoint_data["epoch"]
            random.setstate(checkpoint_data['random_state'])

    else:
        # Start a new evolution
        epoch = 0
        seed = time.time()
        print("Using seed " + str(seed))
        random.seed(seed)

        population = []
        while len(population) != population_size:
            individual = toolbox.individual()
            (entropy, pace) = print_info_and_evaluate(toolbox, individual, 0, len(population), bot1_data, bot2_data)
            print("Generated individual with fitness " + str(entropy) + ", " + str(pace))
            if entropy != -1 and entropy < 0.90:
                population.append(individual)
                individual.fitness.values = tuple([entropy, pace, 0.0, small_phenotype_penalization(individual)])
            else:
                known_fenotypes.pop(individual.phenotype())
                print("Invalid genome detected! Creating a new random one!")

        checkpoint_data = dict(population=population, epoch=epoch, random_state=random.getstate(),
                               known_fenotypes=known_fenotypes)
        with open("Data/checkpoints/checkpoint_epoch_0.pkl", "wb") as cp_file:
            pickle.dump(checkpoint_data, cp_file)
            print("Persisted generation zero")

    # Begin the evolution
    while epoch < num_epochs:
        # A new generation
        epoch = epoch + 1
        known_fenotypes.clear()
        print("-- Generation %i --" % epoch)

        # Select the next generation individuals
        offspring = toolbox.select(population, len(population))

        offspring = algorithms.varAnd(offspring, toolbox, EVOLVER_MATE_PROBABILITY, EVOLVER_MUTATE_PROBABILITY)

        (entropies, paces) = list(
            zip(*[print_info_and_evaluate(toolbox, v, epoch, i, bot1_data, bot2_data) for i, v in enumerate(offspring)])
        )

        repeated_penalization = penalize_repeated_phenotypes(offspring)

        for ind, entropy, pace, repeated in zip(offspring, entropies, paces, repeated_penalization):
            ind.fitness.values = tuple([entropy, pace, repeated, small_phenotype_penalization(ind)])

        population[:] = offspring

        checkpoint_data = dict(population=population, epoch=epoch, random_state=random.getstate(),
                               known_fenotypes=known_fenotypes)

        with open("Data/checkpoints/checkpoint_epoch_" + str(epoch) + ".pkl", "wb") as cp_file:
            pickle.dump(checkpoint_data, cp_file)
            print("Persisted generation for epoch " + str(epoch))

    cp = dict(population=population, epoch=epoch, random_state=random.getstate(), known_fenotypes=known_fenotypes)
    with open("Data/checkpoints/checkpoint_final.pkl", "wb") as cp_file:
        pickle.dump(cp, cp_file)
        print("Persisted generation for final epoch")


def print_info_and_evaluate(toolbox, individual, epoch, individual_number, bot1_data, bot2_data):
    print("Calculating fitness of individual num " + str(individual_number) + " for epoch " + str(epoch))
    phenotype = individual.phenotype()
    if phenotype in known_fenotypes:
        print("Used cached fitness value for " + str(epoch) + "_" + str(individual_number))
        cached_value = known_fenotypes.get(phenotype)
        individual.epoch = cached_value[1]
        individual.number_in_epoch = cached_value[2]
        return cached_value[0]
    individual.epoch = epoch
    individual.number_in_epoch = individual_number
    result = toolbox.evaluate(phenotype, epoch, individual_number, bot1_data, bot2_data)
    known_fenotypes[phenotype] = [result, epoch, individual_number]
    return result


if __name__ == "__main__":
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

    evolve_population(args.num_epochs, args.population_size, bot1_data, bot2_data, args.checkpoint)

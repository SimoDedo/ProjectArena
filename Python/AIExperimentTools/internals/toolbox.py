from deap import base, creator, tools

from internals import generation, evaluation, mutation, crossover, genome


def prepare_toolbox():
    creator.create("FitnessMulti", base.Fitness, weights=(
        1,  # Entropy
        0.6,  # Pace
        -1.0,  # Penalization for repeated phenotypes
        -0.5,  # Penalization for small phenotypes (too few rooms)
    ))
    creator.create("Individual", genome.Genome, fitness=creator.FitnessMulti)

    toolbox = base.Toolbox()

    toolbox.register("individual", generation.create_random_genome, creator.Individual)

    toolbox.register("evaluate", evaluation.evaluate_fitness)
    toolbox.register("mutate", mutation.mutate)
    toolbox.register("mate", crossover.crossover)
    toolbox.register("select", tools.selTournament, tournsize=3)

    return toolbox

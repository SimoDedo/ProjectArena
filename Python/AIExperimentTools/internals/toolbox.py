from deap import base, creator, tools

from internals import evaluation, ab_genome, graph_genome

import internals.ab_genome.ab_genome
import internals.ab_genome.mutation
import internals.ab_genome.crossover
import internals.ab_genome.generation
import internals.graph_genome.genome
import internals.graph_genome.mutation
import internals.graph_genome.crossover
import internals.graph_genome.generation

IS_AB_GENOME = True


def prepare_toolbox():
    creator.create("FitnessMulti", base.Fitness, weights=(
        1,  # Entropy
        1,  # Pace
    ))

    toolbox = base.Toolbox()

    if IS_AB_GENOME:
        creator.create("Individual", ab_genome.ab_genome.ABGenome, fitness=creator.FitnessMulti)
        toolbox.register("mutate", ab_genome.mutation.mutate)
        toolbox.register("mate", ab_genome.crossover.crossover)
        toolbox.register("individual", ab_genome.generation.create_random_genome, creator.Individual)
    else:
        creator.create("Individual", graph_genome.genome.Genome, fitness=creator.FitnessMulti)
        toolbox.register("mutate", graph_genome.mutation.mutate)
        toolbox.register("mate", graph_genome.crossover.crossover)
        toolbox.register("individual", graph_genome.generation.create_random_genome, creator.Individual)

    toolbox.register("select", tools.selNSGA2)
    toolbox.register("evaluate", evaluation.evaluate)

    return toolbox

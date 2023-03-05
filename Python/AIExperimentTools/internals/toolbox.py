from deap import base, creator, tools

from internals import evaluation
# from internals.ab_genome.ab_genome import ABGenome
# from internals.ab_genome.crossover import crossover
# from internals.ab_genome.generation import create_random_genome
# from internals.ab_genome.mutation import mutate

from internals.graph_genome.genome import Genome
from internals.graph_genome.crossover import crossover
from internals.graph_genome.mutation import mutate
from internals.graph_genome.generation import create_random_genome

__TEST_AB = False


def prepare_toolbox():
    creator.create("FitnessMulti", base.Fitness, weights=(
        1,  # Entropy
        1,  # Pace
    ))

    creator.create("Individual", Genome, fitness=creator.FitnessMulti)
    # creator.create("Individual", ABGenome, fitness=creator.FitnessMulti)
    toolbox = base.Toolbox()
    toolbox.register("mutate", mutate)
    toolbox.register("mate", crossover)
    toolbox.register("individual", create_random_genome, creator.Individual)

    toolbox.register("select", tools.selNSGA2)
    toolbox.register("evaluate", evaluation.evaluate)

    return toolbox

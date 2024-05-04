"""AB Mutation Operator"""
import random
import numpy as np

from ribs.emitters.operators._operator_base import OperatorBase

from internals.config import AB_STANDARD_MUTATION_CHANCE
from internals.ab_genome.mutation import mutate
from internals.ab_genome.crossover import crossover
from internals.ab_genome.ab_genome import ABGenome


class ABMutationOperator(OperatorBase):
    """Adds Gaussian noise to solutions.

    Args:
        sigma (float or array-like): Standard deviation of the Gaussian
            distribution. Note we assume the Gaussian is diagonal, so if this
            argument is an array, it must be 1D.
        lower_bounds (array-like): Upper bounds of the solution space. Passed in
            by emitter
        upper_bounds (array-like): Upper bounds of the solution space. Passed in
            by emitter
        seed (int): Value to seed the random number generator. Set to None to
            avoid a fixed seed.
    """

    def __init__(self, crossover_probability, lower_bounds, upper_bounds, seed=None):
        self._crossover_probability = crossover_probability
        self._lower_bounds = lower_bounds
        self._upper_bounds = upper_bounds

        self._rng = np.random.default_rng(seed)

    @property
    def parent_type(self):
        """int: Parent Type to be used by selector."""
        return 1

    def ask(self, parents):
        """Adds Gaussian noise to parents.

        Args:
            parents (array-like): (batch_size, solution_dim) array of
                solutions to be mutated.

        Returns:
            numpy.ndarray: ``(batch_size, solution_dim)`` array that contains
            ``batch_size`` mutated solutions.
        """
        parents = np.asarray(parents)

        for sol1, sol2 in zip(parents[::2], parents[1::2]):
            if random.random() <= self._crossover_probability:
                mut1, mut2 = crossover(ABGenome.array_as_genome(sol1), ABGenome.array_as_genome(sol2))
                index_sol1 = np.where(parents == sol1)[0][0]
                index_sol2 = np.where(parents == sol1)[0][0]
                parents[index_sol1] = mut1.to_array()
                parents[index_sol2] = mut2.to_array()

        # Get the ABGenome from the array, mutate it and then store the array representation
        mutated_solutions = [mutate(ABGenome.array_as_genome(sol)).to_array() for sol in parents]

        return np.clip(mutated_solutions, self._lower_bounds, self._upper_bounds)

import os

EXPERIMENT_RUNNER_FILE = "Project Arena.exe"
EXPERIMENT_RUNNER_PATH = os.path.join (os.getcwd(), "AIExperiment_Build")
GAME_DATA_FOLDER = os.path.join (os.getcwd(), "Data")

# Number of experiments launched in parallel
NUM_PARALLEL_FITNESS_CALCULATION = 8

# Number of parallel launches of the same experiment
NUM_PARALLEL_SIMULATIONS = 1
NUM_MATCHES_PER_SIMULATION = 1

EVOLVER_MATE_PROBABILITY = 0.3
EVOLVER_MUTATE_PROBABILITY = 0.3

# __MINIMUM_NUMBER_OF_MATCHES = 8
# NUM_PARALLEL_SIMULATIONS = min(os.cpu_count(), __MINIMUM_NUMBER_OF_MATCHES)
# NUM_MATCHES_PER_SIMULATION = max(1, math.ceil((__MINIMUM_NUMBER_OF_MATCHES / NUM_PARALLEL_SIMULATIONS)))

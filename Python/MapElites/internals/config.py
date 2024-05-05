""" Evaluation variables """

# Number of parallel launches of the same experiment
NUM_PARALLEL_SIMULATIONS = 1
NUM_MATCHES_PER_SIMULATION = 1

""" Experiment names """
# This  is used as the basis of the folder name where the results of the experiment will be stored.
EXPERIMENT_NAME = "Exp1"

""" Game variables """
GAME_LENGTH = 300

BOT1_FILE_PREFIX = "sniper"
BOT1_SKILL = "0.85"
BOT2_FILE_PREFIX = "shotgun"
BOT2_SKILL = "0.15"

""" MAP-Elites configuration variables """
# Map representation used in the experiment. See constants.py for possible values
REPRESENTATION_NAME = "AllBlack"
# Emitter type used in the experiment. See constants.py for possible values
EMITTER_TYPE_NAME = "AllBlackEmitter"

ITERATIONS = 100
BATCH_SIZE = 5
N_EMITTERS = 5

NUMBER_OF_INITAL_SOLUTIONS = 10

MEASURES_BINS_NUMBER = [20,20]
MEASURES_RANGES = [(0,1),(0,1)]

OBJECTIVE_RANGE = (None,None) # None if unkwnown

OBJECTIVE_NAME = "Entropy"
MEASURES_NAMES = ["TargetLoss", "Pace"]


AB_STANDARD_CROSSOVER_CHANCE = 0.3
GG_STANDARD_CROSSOVER_CHANCE = 0.3
CMA_ME_SIGMA0 = 0.001

def folder_name(test = False):
    if test:
        print("Test run, folder name will be 'test'")
        return "test"
    else:
        return f"{EXPERIMENT_NAME}_{REPRESENTATION_NAME}_{EMITTER_TYPE_NAME}_I{ITERATIONS}_B{BATCH_SIZE}_E{N_EMITTERS}_{OBJECTIVE_NAME}_{MEASURES_NAMES[0]}_{MEASURES_NAMES[1]}"
import internals.constants as constants
""" Evaluation variables """

# Number of parallel launches of the same experiment
NUM_PARALLEL_SIMULATIONS = 1
NUM_MATCHES_PER_SIMULATION = 1


""" Game variables """
GAME_LENGTH = 600

BOT1_FILE_PREFIX = "sniper"
BOT1_SKILL = "0.85"
BOT2_FILE_PREFIX = "shotgun"
BOT2_SKILL = "0.85"

""" MAP-Elites configuration variables """
# Archive type used in the experiment. See constants.py for possible values
ARCHIVE_TYPE = constants.SLIDING_BOUNDARIES_ARCHIVE_NAME
# Map representation used in the experiment. See constants.py for possible values
REPRESENTATION_NAME = constants.ALL_BLACK_NAME
# Emitter type used in the experiment. See constants.py for possible values
EMITTER_TYPE_NAME = constants.ALL_BLACK_EMITTER_NAME

ITERATIONS = 750
BATCH_SIZE = 1
N_EMITTERS = 10

NUMBER_OF_INITAL_SOLUTIONS = 10

MEASURES_BINS_NUMBER = [10,10]
MEASURES_RANGES = [(0,1),(0,1)]

OBJECTIVE_RANGE = (None,None) # (None, None) if unkwnown

AB_STANDARD_CROSSOVER_CHANCE = 0.3
GG_STANDARD_CROSSOVER_CHANCE = 0.3
SMT_STANDARD_CROSSOVER_CHANCE = 0.3
POINT_STANDARD_CROSSOVER_CHANCE = 0.3
CMA_ME_SIGMA0 = 0.1

# These names are used to generate the folder name and the image captions
OBJECTIVE_NAME = "explorationPlusVisibility"
MEASURES_NAMES = ["averageRoomMinDistance", "roomNumberPlusOneRoomCycle"]
# If set to False, the names above are used to get the categories from the dataset. 
# If set to True, the user is expected to manually choose the features in map_elites.py.
# This is useful to combine different features without saving each one. 
MANUALLY_CHOOSE_FEATURES = True

""" Experiment names """
# This  is used as the basis of the folder name where the results of the experiment will be stored.
EXPERIMENT_NAME = "TestVisibility"

""" Miscellaneous variables """
SAVE_INTERMEDIATE_RESULTS = True

def folder_name(test = False):
    if test:
        print("Test run, folder name will be 'test'")
        return "test"
    else:
        return f"{EXPERIMENT_NAME}_{REPRESENTATION_NAME}_{EMITTER_TYPE_NAME}_{ARCHIVE_TYPE}_{OBJECTIVE_NAME}_{MEASURES_NAMES[0]}_{MEASURES_NAMES[1]}_I{ITERATIONS}_B{BATCH_SIZE}_E{N_EMITTERS}"
import os

""" Path variables """
EXPERIMENT_RUNNER_FILE = "Project Arena.exe"
EXPERIMENT_RUNNER_PATH = os.path.join (os.getcwd(), "ME_Build")
GAME_DATA_FOLDER = os.path.join (os.getcwd(), "Data")
MAP_ELITES_OUTPUT_FOLDER = os.path.join (GAME_DATA_FOLDER, "MapElitesOutput")
ARCHIVE_ANALYSIS_OUTPUT_FOLDER = os.path.join (GAME_DATA_FOLDER, "ArchiveAnalysis")
NOISE_ANALYSIS_OUTPUT_FOLDER = os.path.join (GAME_DATA_FOLDER, "NoiseAnalysisOutput")

""" Evaluation variables """
# Number of experiments launched in parallel
NUM_PARALLEL_FITNESS_CALCULATION = 8

# Number of parallel launches of the same experiment
NUM_PARALLEL_SIMULATIONS = 1
NUM_MATCHES_PER_SIMULATION = 1

""" Configuration variables """
NUMBER_OF_INITAL_SOLUTIONS = 10

AB_STANDARD_MUTATION_CHANCE = 0.3
CMA_ME_SIGMA0 = 0.3

""" Genome names """
ALL_BLACK_NAME = "AllBlack"
GRID_GRAPH_NAME = "GridGraph"

GENOME_NAMES = [ALL_BLACK_NAME, GRID_GRAPH_NAME]

""" Emitter names """
ALL_BLACK_EMITTER_NAME = "AllBlack"
CMA_ME_EMITTER_NAME = "CMA_ME"

EMITTER_NAMES = [ALL_BLACK_EMITTER_NAME, CMA_ME_EMITTER_NAME]

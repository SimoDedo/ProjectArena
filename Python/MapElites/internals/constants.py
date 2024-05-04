import os

""" Path variables """
EXPERIMENT_RUNNER_FILE = "Project Arena.exe"
EXPERIMENT_RUNNER_PATH = os.path.join (os.getcwd(), "ME_Build")
GAME_DATA_FOLDER = os.path.join (os.getcwd(), "Data")
MAP_ELITES_OUTPUT_FOLDER = os.path.join (GAME_DATA_FOLDER, "MapElitesOutput")
ARCHIVE_ANALYSIS_OUTPUT_FOLDER = os.path.join (GAME_DATA_FOLDER, "ArchiveAnalysis")
NOISE_ANALYSIS_OUTPUT_FOLDER = os.path.join (GAME_DATA_FOLDER, "NoiseAnalysisOutput")

""" Genome names """
ALL_BLACK_NAME = "AllBlack"
GRID_GRAPH_NAME = "GridGraph"

GENOME_NAMES = [ALL_BLACK_NAME, GRID_GRAPH_NAME]

""" Emitter names """
ALL_BLACK_EMITTER_NAME = "AllBlackEmitter"
CMA_ME_EMITTER_NAME = "CMA_MEEmitter"

EMITTER_NAMES = [ALL_BLACK_EMITTER_NAME, CMA_ME_EMITTER_NAME]

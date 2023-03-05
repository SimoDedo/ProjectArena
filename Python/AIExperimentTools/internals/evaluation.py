import random
import subprocess
from math import log2

import numpy
import pandas

from internals import stats
from internals.constants import GAME_DATA_FOLDER, NUM_PARALLEL_SIMULATIONS, NUM_MATCHES_PER_SIMULATION, \
    EXPERIMENT_RUNNER_PATH, EXPERIMENT_RUNNER_FILE
from internals.result_extractor import extract_match_data


def evaluate(phenotype, epoch, individual_number, bot1_data, bot2_data, game_length=1200):
    # return __random_dataset()
    folder_name = 'genome_evolution/'
    experiment_name = str(epoch) + '_' + str(individual_number)
    complete_name = folder_name + experiment_name

    # Export genome to file
    phenotype.write_to_file(GAME_DATA_FOLDER + 'Import/Genomes/' + complete_name + '.json')

    rel_std_dev_entropy = 100
    dataset = None
    repeat_count = 0
    while rel_std_dev_entropy > 10 and repeat_count < 1:  # TODO enable again?
        if dataset is not None:
            print("Had to repeat experiment because std dev of entropy is " + str(rel_std_dev_entropy))
        # run the simulation with a proper experiment name (generation_individual)
        processes = []
        received_error = False
        for i in range(NUM_PARALLEL_SIMULATIONS):
            processes.append(__run_simulation(folder_name, experiment_name, experiment_name + '.json', game_length,
                                              repeat_count * NUM_PARALLEL_SIMULATIONS + i,
                                              NUM_MATCHES_PER_SIMULATION, bot1_data, bot2_data, False, i == 0))

        for elem in processes:
            received_error = received_error or (elem.wait() == 255)

        if received_error:
            raise Exception("Genome evaluation encountered error!")

        repeat_count = repeat_count + 1

        dataset = extract_match_data(
            GAME_DATA_FOLDER + 'Export/' + folder_name + 'final_results_' + experiment_name + '_',
            repeat_count * NUM_PARALLEL_SIMULATIONS
        )
        mean_value = round(numpy.mean(dataset["entropy"]), 2)
        std_dev = round(numpy.std(dataset["entropy"]), 2)
        rel_std_dev_entropy = round(std_dev / mean_value * 100, 2)
    return dataset


def __run_simulation(folder_name, experiment_name, map_genome_name, game_length, experiment_part, num_simulations, bot1_data, bot2_data, log=False, save_map=False):
    cmd = [EXPERIMENT_RUNNER_PATH + EXPERIMENT_RUNNER_FILE,
           "-experimentType=BOT_GENOME_TESTER",
           "-batchmode",
           "-nographics",
           "-gameLength=" + str(game_length),
           "-dataFolderPath=" + GAME_DATA_FOLDER,
           "-folderName=" + folder_name,
           "-experimentName=" + experiment_name + "_" + str(experiment_part),
           "-numExperiments=" + str(num_simulations),
           "-areaFilename=" + map_genome_name,
           "-bot1file=" + bot1_data["file"],
           "-bot1skill=" + bot1_data["skill"],
           "-bot2file=" + bot2_data["file"],
           "-bot2skill=" + bot2_data["skill"],
           "-logPositions",
           "-logDeathPositions"
           ]
    if save_map:
        cmd.append("-saveMap"),

    if log:
        cmd.append("-logFile")
        cmd.append(
            GAME_DATA_FOLDER + "Log/" + folder_name + experiment_name + "_" + str(experiment_part) + ".txt")
    else:
        cmd.append("-nolog")

    rtn = subprocess.Popen(cmd,
                           cwd=EXPERIMENT_RUNNER_PATH,
                           stdout=subprocess.DEVNULL,
                           bufsize=0)
    return rtn


# debug use only, in case you need to simulate evolution fast
def __random_dataset():
    dataset = pandas.DataFrame()
    pace = random.random()
    ratio = random.random()
    if random.random() < 0.5:
        ratio = 1/ratio

    entropy = - ((ratio/(1+ratio)) * log2(ratio/(1+ratio)) + (1/(1+ratio)) * log2(1/(1+ratio)))

    dataset["entropy"] = [entropy]
    dataset["pace"] = [pace]
    dataset["ratio"] = [ratio]
    return dataset

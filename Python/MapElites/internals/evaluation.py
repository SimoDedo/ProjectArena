import os
from pathlib import Path
import random
import subprocess
from math import log2

import numpy
import pandas
import tqdm

from internals.constants import GAME_DATA_FOLDER,EXPERIMENT_RUNNER_PATH, EXPERIMENT_RUNNER_FILE
from internals.config import NUM_PARALLEL_SIMULATIONS, NUM_MATCHES_PER_SIMULATION
from internals.result_extractor import extract_match_data



def evaluate(phenotype, iteration, individual_batch_num, bot1_data, bot2_data, game_length=120, folder_name='genome_evolution', experiment_name=None):
    """
    Evaluate the given phenotype by running simulations

    Args:
        phenotype (Phenotype): The phenotype to evaluate
        iteration (int): The iteration number
        individual_batch_num (int): The individual batch number
        bot1_data (dict): The data of the first bot
        bot2_data (dict): The data of the second bot
        game_length (int): The length of the game in seconds
        folder_name (str): The name of the folder to store the results in
        experiment_name (str): The name of the experiment. Defaults to iteration and individual_batch_num. It's used to name the genome file and the simulation results.
    
    Returns:
        pandas.DataFrame: The dataset with the results of the simulation
    """

    # Check if phenotype is valid
    if not phenotype.is_valid():
        tqdm.tqdm.write(f"Invalid phenotype detected, skipping evaluation.")
        return __blank_dataset()

    if experiment_name is None :
        experiment_name = str(iteration) + '_' + str(individual_batch_num)
    complete_name = os.path.join(folder_name, experiment_name)

    # Export genome to file
    phenotype.write_to_file(os.path.join(GAME_DATA_FOLDER, 'Import', 'Genomes', complete_name + '.json'))

    return __run_evaluation(folder_name, experiment_name, bot1_data, bot2_data, game_length)


def evaluate_from_file(folder_name, file_prefix, bot1_data, bot2_data, game_length, num_parallel_simulations = NUM_PARALLEL_SIMULATIONS):
    return __run_evaluation(folder_name, file_prefix, bot1_data, bot2_data, game_length, num_parallel_simulations)


def __run_evaluation(folder_name, experiment_name, bot1_data, bot2_data, game_length, num_parallel_simulations = NUM_PARALLEL_SIMULATIONS):
    rel_std_dev_entropy = 100
    dataset = None
    repeat_count = 0
    while rel_std_dev_entropy > 10 and repeat_count < 1:  # TODO enable again?
        if dataset is not None:
            tqdm.tqdm.write("Had to repeat experiment because std dev of entropy is " + str(rel_std_dev_entropy))
        # run the simulation with a proper experiment name (generation_individual)
        processes = []
        received_error = False
        for i in range(num_parallel_simulations):
            processes.append(__run_simulation(folder_name, experiment_name, experiment_name + '.json', game_length,
                                              repeat_count * num_parallel_simulations + i,
                                              NUM_MATCHES_PER_SIMULATION, bot1_data, bot2_data, False, i == 0))

        # Wait for a timeout for each process before killing all processes
        for elem in processes:
            received_error = received_error or (elem.wait(timeout=(game_length+20)) != 0) # TODO: serially waits for each process to finish, want to do it parallely
            elem.kill()  

        if received_error:
            tqdm.tqdm.write(f"Error in simulation, skipping evaluation.")
            return __blank_dataset()

        repeat_count = repeat_count + 1

        dataset = extract_match_data(
            os.path.join(GAME_DATA_FOLDER, 'Export', folder_name, 'final_results_' + experiment_name + '_'),
            repeat_count * num_parallel_simulations
        )
        mean_value = round(numpy.mean(dataset["entropy"]), 2)
        std_dev = round(numpy.std(dataset["entropy"]), 2)
        rel_std_dev_entropy = round(std_dev / mean_value * 100, 2)
    return dataset


def __run_simulation(folder_name, experiment_name, map_genome_name, game_length, experiment_part, num_simulations, bot1_data, bot2_data, log=False, save_map=False):
    """
    Run a simulation with the given parameters
    
    Args:
        folder_name (str): The folder name to store the results in
        experiment_name (str): The name of the experiment
        map_genome_name (str): The name of the map genome
        game_length (int): The length of the game in seconds
        experiment_part (int): The part of the experiment
        num_simulations (int): The number of simulations to run
        bot1_data (dict): The data of the first bot
        bot2_data (dict): The data of the second bot
        log (bool): Whether to log the results
        save_map (bool): Whether to save the map
    """
    cmd = [os.path.join(EXPERIMENT_RUNNER_PATH, EXPERIMENT_RUNNER_FILE),
           "-experimentType=BOT_GENOME_TESTER",
           "-batchmode",
           #"-nographics",
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
            os.path.join(GAME_DATA_FOLDER, "Log", folder_name, experiment_name + "_" + str(experiment_part) + ".txt"))
    else:
        cmd.append("-nolog")
    # Run the process with a timeout
    rtn = subprocess.Popen(cmd,
                           cwd=EXPERIMENT_RUNNER_PATH,
                           stdout=subprocess.DEVNULL,
                           bufsize=0)
    return rtn

def __blank_dataset():
    """Create a blank dataset with all values set to 0"""

    dataset = pandas.DataFrame()
    dataset["entropy"] = [0]
    dataset["pace"] = [0]
    dataset["ratio"] = [0]
    dataset["fightTime"] = [0]
    dataset["pursueTime"] = [0]
    dataset["numberOfRetreats1"] = [0]
    return dataset

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

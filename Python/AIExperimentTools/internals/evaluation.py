import subprocess

import jsonpickle

from internals import stats
from internals.constants import GAME_DATA_FOLDER, NUM_PARALLEL_SIMULATIONS, NUM_MATCHES_PER_SIMULATION, \
    EXPERIMENT_RUNNER_PATH, EXPERIMENT_RUNNER_FILE
from internals.result_extractor import extract_match_data


def evaluate(phenotype, epoch, individual_number, bot1_data, bot2_data, game_length=1200):
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
            processes.append(run_simulation(folder_name, experiment_name, experiment_name + '.json', game_length,
                                            repeat_count * NUM_PARALLEL_SIMULATIONS + i,
                                            NUM_MATCHES_PER_SIMULATION, bot1_data, bot2_data, True, i == 0))

        for elem in processes:
            received_error = received_error or (elem.wait() == 255)

        if received_error:
            print("Genome evaluation encountered error!")
            return -1,

        repeat_count = repeat_count + 1

        dataset = extract_match_data(folder_name, experiment_name, repeat_count * NUM_PARALLEL_SIMULATIONS)
        (_, _, mean_entropy, std_dev) = stats.get_statistics(dataset["entropy"])
        rel_std_dev_entropy = round(std_dev / mean_entropy * 100, 2)

    return dataset


def evaluate_fitness(individual, epoch, individual_number, bot1_data, bot2_data, game_length=1200):
    dataset = evaluate(individual, epoch, individual_number, bot1_data, bot2_data, game_length)
    # evaluate some info from the dataset, e.g. killRatio
    ratios = dataset["ratio"]
    entropies = dataset["entropy"]
    paces = dataset["pace"]

    (min_entropy, max_entropy, mean_entropy, std_dev) = stats.get_statistics(entropies)
    rel_std_dev = round(std_dev / mean_entropy * 100, 2)
    print("entropy mean: " + str(mean_entropy) + ", stdDev: " + str(std_dev) + ", relStdDev: " + str(
        rel_std_dev) +
          ", min: " + str(min_entropy) + ", max: " + str(max_entropy))

    (min_ratio, max_ratio, mean_ratio, std_dev) = stats.get_statistics(ratios)
    rel_std_dev = round(std_dev / mean_ratio * 100, 2)
    print("ratio mean: " + str(mean_ratio) + ", stdDev: " + str(std_dev) + ", relStdDev: " + str(rel_std_dev) +
          ", min: " + str(min_ratio) + ", max: " + str(max_ratio))

    (min_pace, max_pace, mean_pace, std_dev) = stats.get_statistics(paces)
    rel_std_dev = round(std_dev / mean_pace * 100, 2)
    print("pace mean: " + str(mean_pace) + ", stdDev: " + str(std_dev) + ", relStdDev: " + str(rel_std_dev) +
          ", min: " + str(min_pace) + ", max: " + str(max_pace))

    return [round(mean_entropy, 5), round(mean_pace, 5)]


def run_simulation(folder_name, experiment_name, map_genome_name, game_length, experiment_part, num_simulations,
                   bot1_data,
                   bot2_data, log=False, save_map=False):
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


def __get_mean_and_relative_std_dev(dataset):
    mean = dataset.mean()
    std_dev = dataset.std()

    return mean, std_dev / mean * 100


def __write_to_file(map_data, filepath):
    with open(filepath, 'w') as f:
        cp = dict(
            width=map_data.cellsWidth,
            height=map_data.cellsHeight,
            mapScale=map_data.mapScale,
            areas=map_data.areas,
        )
        genome_json = jsonpickle.encode(cp, unpicklable=False)
        f.write(genome_json)

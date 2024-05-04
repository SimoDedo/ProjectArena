import os
import pandas

from internals.constants import GAME_DATA_FOLDER
from internals.config import NUM_PARALLEL_SIMULATIONS


def extract_match_data(partial_file_name, num_simulations=NUM_PARALLEL_SIMULATIONS):
    frames = []
    for i in range(num_simulations):
        file_name = partial_file_name + str(i) + '.json'

        data = pandas.read_json(file_name)
        frames.append(data)

    dataset = pandas.concat(frames)
    ratios = []
    killDiff = []

    for i in zip(dataset["numberOfFrags1"], dataset["numberOfFrags2"]):
        if i[1] == 0:
            ratios.append(i[0])
        else:
            ratios.append(i[0] / i[1])
        killDiff.append(i[0] - i[1])

    dataset["ratio"] = ratios
    dataset["killDiff"] = killDiff
    return dataset


def extract_kill_positions(initial_path, experiment_name, bot_num, num_simulations=NUM_PARALLEL_SIMULATIONS):
    positions_x = []
    positions_y = []
    for i in range(num_simulations):
        try:
            temp = pandas.read_csv(
                os.path.join(initial_path, "death_positions_" + experiment_name + "_" + str(i) + "_bot" + str(bot_num+1) + ".csv"),
                header=None,
            )
            positions_x.extend(temp[2])
            positions_y.extend(temp[3])
        except pandas.errors.EmptyDataError:
            # Not a single death, file is empty
            pass
    return positions_x, positions_y,


def extract_death_positions(initial_path, experiment_name, bot_num, num_simulations=NUM_PARALLEL_SIMULATIONS):
    positions_x = []
    positions_y = []
    for i in range(num_simulations):
        try:
            temp = pandas.read_csv(
                os.path.join(initial_path, "death_positions_" + experiment_name + "_" + str(i) + "_bot" + str(bot_num+1) + ".csv"),
                header=None,
            )
            positions_x.extend(temp[0])
            positions_y.extend(temp[1])
        except pandas.errors.EmptyDataError:
            # Not a single death, file is empty
            pass
    return positions_x, positions_y,


def extract_bot_positions(initial_path, experiment_name, bot_num, num_simulations=NUM_PARALLEL_SIMULATIONS):
    positions_x = []
    positions_y = []
    for i in range(num_simulations):
        temp = pandas.read_csv(
            os.path.join(initial_path, "position_" + experiment_name + "_" + str(i) + "_bot" + str(bot_num+1) + ".csv"),
            header=None,
        )
        positions_x.extend(temp[0])
        positions_y.extend(temp[1])
    return positions_x, positions_y,


def extract_kill_distance_info(folder_name, experiment_name, bot_num, num_files=NUM_PARALLEL_SIMULATIONS):
    distances = []
    for i in range(num_files):
        temp = pandas.read_csv(
            GAME_DATA_FOLDER + "Export/" + folder_name + "kill_distances_" + experiment_name + "_" + str(i) + "_bot" +
            str(bot_num) + ".csv", header=None)
        distances.extend(temp[0].array)
    return distances

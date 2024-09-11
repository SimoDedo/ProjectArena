from math import floor, sqrt
import os
import numpy as np
import pandas
from scipy.signal import argrelextrema
from scipy.ndimage import gaussian_filter
from skimage.feature import peak_local_max
import igraph as ig

from internals.constants import GAME_DATA_FOLDER
from internals.config import NUM_PARALLEL_SIMULATIONS

import pickle

import warnings
warnings.simplefilter(action='ignore', category=pandas.errors.PerformanceWarning)

BOT_NUM = 2

def extract_match_data(phenotype, folder_name, experiment_name, num_simulations=NUM_PARALLEL_SIMULATIONS):
    # MATCH RESULT READING
    # Read the match results from the files
    frames = []
    for i in range(num_simulations):
        file_name = os.path.join(GAME_DATA_FOLDER, 'Export', folder_name, 'final_results_' + experiment_name + '_' + str(i) + '.json')

        try :
            data = pandas.read_json(file_name)
            frames.append(data)
        except FileNotFoundError:
            return None

    dataset = pandas.concat(frames)

    # Add ratio and killDiff columns
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

    # MATCH RESULT ANALYSIS

    # Read resulting map
    map_matrix = read_map(experiment_name, folder_name)
    mask = np.matrix(map_matrix)
    dataset["area"] = np.count_nonzero(mask == 0) / (len(map_matrix) * len(map_matrix[0]))

    initial_path = os.path.join(GAME_DATA_FOLDER, "Export", folder_name)
    # Analyze positions
    positions_x, positions_y = extract_data(extract_bot_positions, initial_path, experiment_name, BOT_NUM, phenotype.mapScale)
    dataset = __analyze_heatmap(dataset, positions_x, positions_y, "Position", BOT_NUM, map_matrix)
    
    # Analyze kill positions
    kills_x, kills_y = extract_data(extract_kill_positions, initial_path, experiment_name, BOT_NUM, phenotype.mapScale)
    dataset = __analyze_heatmap(dataset, kills_x, kills_y, "Kill", BOT_NUM, map_matrix)
    
    # Analyze death positions
    deaths_x, deaths_y = extract_data(extract_death_positions, initial_path, experiment_name, BOT_NUM, phenotype.mapScale)
    dataset = __analyze_heatmap(dataset, deaths_x, deaths_y, "Death", BOT_NUM, map_matrix)

    # Analyze kill traces
    dataset = __analyze_traces(dataset, kills_x, kills_y, deaths_x, deaths_y, BOT_NUM)

    # GRAPH ANALYSIS

    #graph, _ = phenotype.to_graph_naive()
    #rooms = [v for v in graph.vs if not v['isCorridor']]
    graph, _, _ = phenotype.to_topology_graph_vornoi()
    rooms = [v for v in graph.vs if v['region']]
    chokepoints = [v for v in graph.vs if v['chokepoint']]
    __graph_analysis(graph, rooms, dataset, chokepoints)

    # VISIBILITY GRAPH
    # Remove border from the map
    border = 5
    no_border_map_matrix = map_matrix[border:-border]
    no_border_map_matrix = [row[border:-border] for row in no_border_map_matrix]

    visibility_graph, visibility_matrix = phenotype.to_visibility_graph()

    dataset = __analyze_visibility(visibility_graph, visibility_matrix, no_border_map_matrix, dataset)

    # SYMMETRY
    dataset = __analyze_symmetry(no_border_map_matrix, dataset)


    # TODO: Add graph analysis based on match data?

    # Rewrite the dataset to include the new columns
    dataset.to_json(os.path.join(GAME_DATA_FOLDER, 'Export', folder_name, 'final_results_' + experiment_name + '.json'))

    phenotype_file = open(os.path.join(GAME_DATA_FOLDER, 'Export', folder_name, 'phenotype_' + experiment_name + '.pkl'), 'wb')
    pickle.dump(phenotype, phenotype_file)
    phenotype_file.close()

    return dataset

def extract_data(extract_function, initial_path, experiment_name, bot_num,map_scale):
    p_x = [[] for _ in range(bot_num)]
    p_y = [[] for _ in range(bot_num)]
    for bot_n in range(0, bot_num):
        (positions_x, positions_y) = extract_function(initial_path, experiment_name, bot_n)
        positions_x = [x / map_scale for x in positions_x]
        positions_y = [y / map_scale for y in positions_y]
        p_x[bot_n] = positions_x
        p_y[bot_n] = positions_y
    return p_x, p_y

def __analyze_heatmap(dataset, p_x, p_y, feature_name, bot_num, map_matrix):
    for bot_n in range(0, bot_num):
        positions_x = p_x[bot_n]
        positions_y = p_y[bot_n]

        heatmap = __get_heatmap(positions_x, positions_y, map_matrix)
        filtered_heatmap = gaussian_filter(heatmap, sigma=3.0)
        masked_heatmap = __mask_heatmap(filtered_heatmap, map_matrix)

        max_value = np.max(masked_heatmap.compressed())
        local_maxima = __get_heatmap_local_maxima(masked_heatmap)
        local_maxima_values = [masked_heatmap[local_max[0], local_max[1]] for local_max in local_maxima]
        distances = __get_local_maxima_distances(masked_heatmap, local_maxima)
        q25, q50, q75 = __get_heatmap_quantiles(masked_heatmap)
        coverage = __get_heatmap_coverage(masked_heatmap, map_matrix, 0.1)

        dataset["maxValue" + feature_name + "Bot" + str(bot_n)] = max_value
        dataset["localMaximaNumber" + feature_name + "Bot" + str(bot_n)] = len(local_maxima)
        dataset["localMaximaTopDistance" + feature_name + "Bot" + str(bot_n)] = distances[0]
        dataset["localMaximaAverageDistance" + feature_name + "Bot" + str(bot_n)] = np.mean(distances)
        dataset["averageLocalMaximaValue" + feature_name + "Bot" + str(bot_n)] = np.mean(local_maxima_values) if len(local_maxima) > 0 else 0
        dataset["stdLocalMaximaValue" + feature_name + "Bot" + str(bot_n)] = np.std(local_maxima_values) if len(local_maxima) > 0 else 0
        dataset["quantile25" + feature_name + "Bot" + str(bot_n)] = q25 / max_value
        dataset["quantile50" + feature_name + "Bot" + str(bot_n)] = q50 / max_value
        dataset["quantile75" + feature_name + "Bot" + str(bot_n)] = q75 / max_value
        dataset["coverage" + feature_name + "Bot" + str(bot_n)] = coverage
    
    # Average the values of the bot_num bots
    dataset["maxValue" + feature_name] = sum([dataset["maxValue" + feature_name + "Bot" + str(i)] for i in range(bot_num)]) / bot_num
    dataset["localMaximaNumber" + feature_name] = sum([dataset["localMaximaNumber" + feature_name + "Bot" + str(i)] for i in range(bot_num)]) / bot_num
    dataset["localMaximaTopDistance" + feature_name] = sum([dataset["localMaximaTopDistance" + feature_name + "Bot" + str(i)] for i in range(bot_num)]) / bot_num
    dataset["localMaximaAverageDistance" + feature_name] = sum([dataset["localMaximaAverageDistance" + feature_name + "Bot" + str(i)] for i in range(bot_num)]) / bot_num
    dataset["averageLocalMaximaValue" + feature_name] = sum([dataset["averageLocalMaximaValue" + feature_name + "Bot" + str(i)] for i in range(bot_num)]) / bot_num
    dataset["stdLocalMaximaValue" + feature_name] = sum([dataset["stdLocalMaximaValue" + feature_name + "Bot" + str(i)] for i in range(bot_num)]) / bot_num
    dataset["quantile25" + feature_name] = sum([dataset["quantile25" + feature_name + "Bot" + str(i)] for i in range(bot_num)]) / bot_num
    dataset["quantile50" + feature_name] = sum([dataset["quantile50" + feature_name + "Bot" + str(i)] for i in range(bot_num)]) / bot_num
    dataset["quantile75" + feature_name] = sum([dataset["quantile75" + feature_name + "Bot" + str(i)] for i in range(bot_num)]) / bot_num
    dataset["coverage" + feature_name] = sum([dataset["coverage" + feature_name + "Bot" + str(i)] for i in range(bot_num)]) / bot_num

    return dataset

def __analyze_traces(dataset, kills_x, kills_y, deaths_x, deaths_y, bot_num):
    for bot_n in range(0, bot_num):
        death_pos = [[x, y] for x, y in zip(deaths_x[bot_n], deaths_y[bot_n])]
        kill_pos = [[x, y] for x, y in zip(kills_x[bot_n], kills_y[bot_n])]

        traces = []
        step = max(1, floor(len(death_pos) / 50))
        for idx in range(0, len(death_pos), step):
            start_pos = death_pos[idx]
            end_pos = kill_pos[idx]
            x = [start_pos[0], end_pos[0]]
            y = [start_pos[1], end_pos[1]]

            distance = sqrt(pow(x[0] - x[1], 2) + pow(y[0] - y[1], 2))
            traces.append([distance])
        
        traces = np.array(traces)
        dataset["maxTraces" + "Bot" + str(bot_n)] = np.max(traces)
        dataset["averageTraces" + "Bot" + str(bot_n)] = np.mean(traces)
        dataset["quantile25Traces" + "Bot" + str(bot_n)] = np.percentile(traces, 25)
        dataset["quantile50Traces" + "Bot" + str(bot_n)] = np.percentile(traces, 50)
        dataset["quantile75Traces" + "Bot" + str(bot_n)] = np.percentile(traces, 75)
        
    # Average the values of the bot_num bots
    dataset["maxTraces"] = sum([dataset["maxTraces" + "Bot" + str(i)] for i in range(bot_num)]) / bot_num
    dataset["averageTraces"] = sum([dataset["averageTraces" + "Bot" + str(i)] for i in range(bot_num)]) / bot_num
    dataset["quantile25Traces"] = sum([dataset["quantile25Traces" + "Bot" + str(i)] for i in range(bot_num)]) / bot_num
    dataset["quantile50Traces"] = sum([dataset["quantile50Traces" + "Bot" + str(i)] for i in range(bot_num)]) / bot_num
    dataset["quantile75Traces"] = sum([dataset["quantile75Traces" + "Bot" + str(i)] for i in range(bot_num)]) / bot_num

    return dataset



def __get_heatmap(x, y, map_matrix):
    heatmap, xedges, yedges = np.histogram2d(
        x,
        y,
        bins=[[i for i in range(len(map_matrix[0]) + 1)],
              [i for i in range(len(map_matrix) + 1)]]
    )
    return heatmap.T # Transpose to match map_matrix shape

def __mask_heatmap(heatmap, map_matrix, invert=True):
    # Multiply the heatmap by the inverted map_matrix to mask the walls
    mask = np.matrix(map_matrix)
    if invert:
        mask = np.where(mask == 0, 1, 0)
    else:
        mask = np.where(mask == 0, 0, 1)
    heatmap_zeroed_walls = np.multiply(heatmap, mask)
    return np.ma.masked_where(mask == 0, heatmap_zeroed_walls)

def __get_heatmap_max_index(heatmap):
    return np.unravel_index(np.argmax(heatmap), heatmap.shape)

def __get_heatmap_local_maxima(heatmap):
    return peak_local_max(heatmap, min_distance=5)

def __get_local_maxima_distances(heatmap, local_maxima):
    if(len(local_maxima) < 2):
        return 0, 0
    max_idx = __get_heatmap_max_index(heatmap)
    # Get the distance from max to each other local maxima, sorted by local maxima value
    # They should be already ordered actually, but just in case
    sorted_idxs = np.argsort(heatmap[local_maxima[:, 0], local_maxima[:, 1]])[::-1]
    distances = []
    for local_max in sorted_idxs[1:]:
        distances.append(sqrt(pow(local_maxima[local_max][0] - local_maxima[local_max][1], 2) + pow(max_idx[0] - max_idx[1], 2)))

    return distances

def __get_heatmap_quantiles(heatmap):
    q1 = np.percentile(heatmap.compressed(), 25)
    q2 = np.percentile(heatmap.compressed(), 50)
    q3 = np.percentile(heatmap.compressed(), 75)
    return q1, q2, q3

def __get_heatmap_coverage(heatmap, map_matrix, threshold=0.1):    
    m = np.matrix(map_matrix)
    walkable_spaces = np.count_nonzero(m == 0)
    walked_spaces = np.count_nonzero(heatmap.compressed() >= threshold)
    return walked_spaces / walkable_spaces

def __graph_analysis(graph: ig.Graph, rooms, dataset, chokepoints=None):
    # Rooms number
    dataset["roomNumber"] = len(rooms)

    # Rooms distance
    all_distances = []
    exclusive_distances = []
    for i in range(len(rooms)):
        new_distances = graph.distances(source=rooms[i], target=[rooms[j] for j in range(len(rooms)) if i != j], weights=graph.es['weight'])
        all_distances.extend(new_distances)
        exclusive_distances.extend(new_distances[0][i+1:])

    exclusive_distances = [exclusive_distances[i] if np.isfinite(exclusive_distances[i]) else 0 for i in range(len(exclusive_distances))]
    dataset["averageRoomMinDistance"] = np.mean(exclusive_distances) if len(exclusive_distances) > 0 else 0
    dataset["stdRoomMinDistance"] = np.std(exclusive_distances) if len(exclusive_distances) > 0 else 0

    # Radius
    room_radiuses = [r['radius'] for r in rooms]
    dataset["averageRoomRadius"] = np.mean(room_radiuses) if len(room_radiuses) > 0 else 0
    dataset["stdRoomRadius"] = np.std(room_radiuses) if len(room_radiuses) > 0 else 0
    if chokepoints is not None:
        chokepoint_radiuses = [r['radius'] for r in chokepoints]
        dataset["averageChokepointRadius"] = np.mean(chokepoint_radiuses) if len(chokepoint_radiuses) > 0 else 0
        dataset["stdChokepointRadius"] = np.std(chokepoint_radiuses) if len(chokepoint_radiuses) > 0 else 0


    # Roooms betweenness
    betweenness = graph.betweenness(vertices=rooms, weights=graph.es['weight'])
    betweenness = [betweenness[i] if np.isfinite(betweenness[i]) else 0 for i in range(len(betweenness))]
    dataset["averageRoomBetweenness"] = np.mean(betweenness) if len(betweenness) > 0 else 0
    dataset["stdRoomBetweenness"] = np.std(betweenness) if len(betweenness) > 0 else 0

    # Rooms closeness
    closeness = graph.closeness(vertices=rooms, weights=graph.es['weight'])
    closeness = [closeness[i] if np.isfinite(closeness[i]) else 0 for i in range(len(closeness))]
    dataset["averageRoomCloseness"] = np.mean(closeness) if len(closeness) > 0 else 0
    dataset["stdRoomCloseness"] = np.std(closeness) if len(closeness) > 0 else 0

    # Mincut
    mincut = [len(graph.mincut(source=rooms[i].index, target=rooms[j].index, capacity=None).cut) for i in range(len(rooms)) for j in range(i+1, len(rooms))]
    mincut = [mincut[i] if np.isfinite(mincut[i]) else 0 for i in range(len(mincut))]
    dataset["averageMincut"] = np.mean(mincut) if len(mincut) > 0 else 0
    dataset["stdMincut"] = np.std(mincut) if len(mincut) > 0 else 0
    dataset["maxMincut"] = max(mincut) if len(mincut) > 0 else 0
    dataset["minMincut"] = min(mincut) if len(mincut) > 0 else 0

    # Connectivity
    dataset["vertexConnectivity"] = graph.vertex_connectivity()

    # Ecceentricity, diameter and radius
    eccentricities = [max(d) if len(d) > 0 else 0 for d in all_distances]
    dataset["averageEccentricity"] = np.mean(eccentricities) if len(rooms) > 0 else 0
    dataset["stdEccentricity"] = np.std(eccentricities) if len(rooms) > 0 else 0
    diameter =  max(eccentricities) if len(eccentricities) > 0 else 0
    dataset["diameter"] = diameter
    radius = min(eccentricities) if len(eccentricities) > 0 else 0
    dataset["radius"] = radius
    # Periphery and center should include rooms with the same eccentricity as the diameter and radius,
    # but we use weights to calculate distances, not the number of edges, so we need a threshold within which we consider the eccentricity the same
    threshold = 2
    periphery = [rooms[i] for i in range(len(rooms)) if  np.abs(eccentricities[i] - diameter) < threshold]
    dataset["periphery"] = len(periphery)
    dataset["peripheryPercent"] = len(periphery) / len(rooms) if len(rooms) > 0 else 0
    center = [rooms[i] for i in range(len(rooms)) if np.abs(eccentricities[i] - radius) < threshold]
    dataset["center"] = len(center)
    dataset["centerPercent"] = len(center) / len(rooms) if len(rooms) > 0 else 0

    # Density
    dataset["density"] = graph.density(loops=True)

    # Fundamental cycles. We consider cycles with at least one room and at least two rooms
    cycles = graph.fundamental_cycles()
    vertices_in_cycles = []
    for cycle in cycles:
        vertices_in_cycles.append([])
        for i in cycle:
            if graph.es[i].source not in vertices_in_cycles[-1] and (not graph.vs[graph.es[i].source] in rooms):
                vertices_in_cycles[-1].append(graph.es[i].source)
            if graph.es[i].target not in vertices_in_cycles[-1] and (not graph.vs[graph.es[i].target] in rooms):
                vertices_in_cycles[-1].append(graph.es[i].target)
    cycles_one_room = [cycles[i] for i in range(len(cycles)) if len(vertices_in_cycles[i]) > 0]
    cor_length = [sum([graph.es[i]['weight'] for i in range(len(cycle))]) for cycle in cycles_one_room]
    cycles_two_rooms = [cycles[i] for i in range(len(cycles)) if len(vertices_in_cycles[i]) > 1]
    ctr_length = [sum([graph.es[i]['weight'] for i in range(len(cycle))]) for cycle in cycles_two_rooms]

    dataset["numberCyclesOneRoom"] = len(cycles_one_room)
    dataset["averageLengthCyclesOneRoom"] = np.mean(cor_length) if len(cor_length) > 0 else 0
    dataset["stdLengthCyclesOneRoom"] = np.std(cor_length) if len(cor_length) > 0 else 0
    dataset["numberCyclesTwoRooms"] = len(cycles_two_rooms)
    dataset["averageLengthCyclesTwoRooms"] = np.mean(ctr_length) if len(ctr_length) > 0 else 0
    dataset["stdLengthCyclesTwoRooms"] = np.std(ctr_length) if len(ctr_length) > 0 else 0

def __analyze_visibility(visibility_graph, visibility_matrix, map_matrix, dataset):
    num_tiles = len(visibility_graph.vs)

    masked_heatmap = __mask_heatmap(visibility_matrix, map_matrix)

    max_value = np.max(masked_heatmap.compressed())
    local_maxima = __get_heatmap_local_maxima(masked_heatmap)
    distances = __get_local_maxima_distances(masked_heatmap, local_maxima)
    q25, q50, q75 = __get_heatmap_quantiles(masked_heatmap)
    masked_heatmap_percent = masked_heatmap / num_tiles
    #coverage = __get_heatmap_coverage(masked_heatmap, map_matrix, 0.1) always 1, useless

    dataset["maxValueVisibility"] = max_value
    dataset["maxValuePercentVisibility"] = max_value / num_tiles
    dataset["averageValuePercentVisibility"] = np.mean(masked_heatmap_percent.compressed())
    dataset["stdValuePercentVisibility"] = np.std(masked_heatmap_percent.compressed())
    dataset["localMaximaNumberVisibility"] = len(local_maxima)
    dataset["localMaximaTopDistanceVisibility"] = distances[0]
    dataset["localMaximaAverageDistanceVisibility"] = np.mean(distances)
    averageLocalMaximaValue = [masked_heatmap[local_max[0], local_max[1]] for local_max in local_maxima]
    averageLocalMaximaValuePercent = [v/num_tiles for v in averageLocalMaximaValue]
    dataset["averageLocalMaximaValuePercentVisibility"] = np.mean(averageLocalMaximaValuePercent) if len(local_maxima) > 0 else 0
    dataset["stdLocalMaximaValuePercentVisibility"] = np.std(averageLocalMaximaValuePercent) if len(local_maxima) > 0 else 0
    dataset["quantile25PercentVisibility"] = q25 / num_tiles
    dataset["quantile50PercentVisibility"] = q50 / num_tiles
    dataset["quantile75PercentVisibility"] = q75 / num_tiles

    return dataset

def __analyze_symmetry(map_matrix, dataset):
    x_len = len(map_matrix)
    y_len = len(map_matrix[0])

    # Get minimum and maximum coordinates that hold 1
    min_x, max_x, min_y, max_y = x_len, 0, x_len, 0
    for x in range(len(map_matrix)):
        for y in range(len(map_matrix[0])):
            if map_matrix[x][y] == 1:
                min_x = min(min_x, x)
                max_x = max(max_x, x)
                min_y = min(min_y, y)
                max_y = max(max_y, y)
    x_len = max_x - min_x
    y_len = max_y - min_y

    total_tiles = np.count_nonzero(map_matrix)

    x_symmetry = 0
    y_mid_point = int(np.floor(y_len/2))
    for x in range(min_x, max_x + 1):
        for y in range(min_y, min_y + y_mid_point + 1):
            opposite_y = max_y - (y - min_y)
            if map_matrix[x][y] == map_matrix[x][opposite_y]:
                if map_matrix[x][y] != 0:
                    x_symmetry += 2 if y != opposite_y else 1

    y_symmetry = 0
    x_mid_point = int(np.floor(x_len/2))
    for y in range(min_y, max_y + 1):
        for x in range(min_x, min_x + x_mid_point + 1):
            opposite_x = max_x - (x - min_x)
            if map_matrix[x][y] == map_matrix[opposite_x][y]:
                if map_matrix[x][y] != 0:
                    y_symmetry += 2 if x != opposite_x else 1

    x_symmetry /= total_tiles
    y_symmetry /= total_tiles
    max_symmetry = max(x_symmetry, y_symmetry)
    dataset["xSymmetry"] = x_symmetry
    dataset["ySymmetry"] = y_symmetry
    dataset["maxSymmetry"] = max_symmetry

    return dataset




# --- Extracting data from files ---

# Returns a map represented as a matrix of 0s and 1s, where 1 is a wall and 0 is a free space
def read_map(experiment_name, folder_name):
    path = os.path.join(GAME_DATA_FOLDER, "Export", folder_name, "map_" + experiment_name +
                        "_0.txt")

    with open(path, "r") as map_file:
        map_contents = map_file.readlines()
    map_height = len(map_contents)
    map_width = len(map_contents[0]) - 1  # Drop newline
    map_matrix = []
    for row in reversed(range(map_height)):
        map_row = []
        for col in range(map_width):
            if map_contents[row][col] == 'w':
                map_row.append(1)
            else:
                map_row.append(0)
        map_matrix.append(map_row)
    return map_matrix

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

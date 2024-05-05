"""
Helper script to analyze noise of matches' metrics with different game lengths.
The objective is to understand the ideal match length so as to not have data that is too noisy but also have
computations that are not too long.
"""
import argparse
import os
import sys
import time

import json
import time
from pathlib import Path

import fire
from Python.MapElites.internals.pyribs_ext.genome_emitter import GenomeEmitter
from internals.constants import GAME_DATA_FOLDER, MAP_ELITES_OUTPUT_FOLDER, NOISE_ANALYSIS_OUTPUT_FOLDER
from internals.ab_genome.ab_genome import AB_MAX_CORRIDOR_LENGTH, AB_MAX_MAP_HEIGHT, AB_MAX_MAP_WIDTH, AB_MAX_ROOM_SIZE, AB_MIN_CORRIDOR_LENGTH, AB_MIN_ROOM_SIZE, AB_NUM_CORRIDORS, AB_NUM_ROOMS, ABGenome
from internals.ab_genome.generation import create_random_genome
from internals.evaluation import evaluate, evaluate_from_file
import gymnasium as gym
import matplotlib.pyplot as plt
import numpy as np
import pandas as pd
import tqdm
from dask.distributed import Client, LocalCluster

from ribs.archives import ArchiveDataFrame, GridArchive
from ribs.emitters import EvolutionStrategyEmitter, GeneticAlgorithmEmitter, GaussianEmitter
from ribs.schedulers import Scheduler
from ribs.visualize import grid_archive_heatmap


def simulate_matches(client: Client, folder_name, bot1_data, bot2_data, max_iter, frequency, game_length=600, num_games=10):
    """
    Max iter is the number of iterations done in the map-elites run.
    Frequency is how many of the saved maps we try (one map every "frequency" maps)
    """
    print(
        "> Starting search.\n"
        "  - Open Dask's dashboard at http://localhost:8787 to monitor workers."
    )

    # Generate a list of strings in the format "x_y"
    map_list = [f"{x}_0" for x in range(0, max_iter, frequency)]
    print(map_list)

    result_aggregate = [[] for _ in range(len(map_list))]
    for itr in tqdm.trange(0, num_games):
        # Evaluate the genomes and record the objectives and measures.

        # Ask the Dask client to distribute the simulations among the Dask
        # workers, then gather the results of the simulations.
        futures = []
        
        futures = client.map(
            lambda name: evaluate_from_file(
                folder_name, 
                name,
                bot1_data, 
                bot2_data,
                game_length,
                ), map_list
        )
        results = client.gather(futures)

        # Process the results.
        for i in range(0,len(results)):
            result_aggregate[i].append(results[i])
    

    result_datasets = []
    for i in range(len(result_aggregate)):
        df = pd.concat(result_aggregate[i])
        mean = df.mean()
        std = df.std()
        merged_df = pd.concat([mean, std], axis=1)
        result_datasets.append(merged_df)

    return zip(map_list, result_datasets)


def analyze_matches(
                bot1_data, 
                bot2_data,
                workers=4,
                max_iterations=30,
                frequency = 15,
                game_length=600,
                num_games=10,
                folder_name="test_directory",
                ):
    
    outdir = Path(os.path.join(NOISE_ANALYSIS_OUTPUT_FOLDER, folder_name + "-" + str(game_length)))
    importdir = Path(os.path.join(GAME_DATA_FOLDER, "Import", "Genomes", folder_name))
    exportdir = Path(os.path.join(GAME_DATA_FOLDER, "Export", folder_name))

    outdir.mkdir(exist_ok=True)
    importdir.mkdir(exist_ok=True)
    exportdir.mkdir(exist_ok=True)


    # Setup Dask. The client connects to a "cluster" running on this machine.
    # The cluster simply manages several concurrent worker processes. If using
    # Dask across many workers, we would set up a more complicated cluster and
    # connect the client to it.
    cluster = LocalCluster(
        processes=True,  # Each worker is a process.
        n_workers=workers,  # Create this many worker processes.
        threads_per_worker=1,  # Each worker process is single-threaded.
    )
    client = Client(cluster)

    results = simulate_matches(client, folder_name, bot1_data, bot2_data, max_iterations, frequency, game_length, num_games)

    for name, df in results:
        df.to_csv(os.path.join(outdir, name + ".csv"))




if __name__ == "__main__":
    parser = argparse.ArgumentParser(description='Evolve population.')

    # output directory for saving import data, export data and map elites results data
    # You should put here the name of a map-elites experiment that you run before, so that the phenotypes produced can be exploited in this script.
    parser.add_argument("--folder_name", default="test_noise", type=str, dest="folder_name") 

    parser.add_argument("--workers", default=8, type=int, dest="workers")

    # Argument used to get the results from the export folder. must be less than the number of iterations in the map-elites run.
    parser.add_argument("--max_iterations", default=30, type=int, dest="max_iterations")
    
    parser.add_argument("--frequency", default=5, type=int, dest="frequency")
    parser.add_argument("--num_games", default=10, type=int, dest="num_games")

    parser.add_argument("--bot1_file_prefix", required=True, dest="bot1_file")
    parser.add_argument("--bot1_skill", required=True, dest="bot1_skill")
    parser.add_argument("--bot2_file_prefix", required=True, dest="bot2_file")
    parser.add_argument("--bot2_skill", required=True, dest="bot2_skill")
    parser.add_argument("--game_length", type=int, dest="game_length")

    args = parser.parse_args(sys.argv[1:])

    bot1_data = {"file": args.bot1_file, "skill": args.bot1_skill}
    bot2_data = {"file": args.bot2_file, "skill": args.bot2_skill}

    analyze_matches(
        bot1_data, 
        bot2_data, 
        workers=args.workers, 
        max_iterations=args.max_iterations,
        frequency = args.frequency,
        game_length=args.game_length,
        num_games=args.num_games,
        folder_name=args.folder_name,
        )
    
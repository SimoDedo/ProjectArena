"""
#TODO
"""
import argparse
import json
import os
from pathlib import Path
import sys
import time

import time

from internals.graph_genome.genome import GraphGenome
from internals.pyribs_ext.ab_genome_emitter import AbGenomeEmitter
import internals.constants as c
from internals.constants import AB_STANDARD_MUTATION_CHANCE, ALL_BLACK_EMITTER_NAME, ALL_BLACK_NAME, CMA_ME_SIGMA0, GAME_DATA_FOLDER, MAP_ELITES_OUTPUT_FOLDER
from internals.ab_genome.ab_genome import AB_MAX_CORRIDOR_LENGTH, AB_MAX_MAP_HEIGHT, AB_MAX_MAP_WIDTH, AB_MAX_ROOM_SIZE, AB_MIN_CORRIDOR_LENGTH, AB_MIN_ROOM_SIZE, AB_NUM_CORRIDORS, AB_NUM_ROOMS, ABGenome
import internals.ab_genome.generation as ab_generation
import internals.graph_genome.generation as graph_generation
from internals.evaluation import evaluate
import gymnasium as gym
import matplotlib.pyplot as plt
import numpy as np
import pandas as pd
import tqdm
from dask.distributed import Client, LocalCluster

from ribs.archives import ArchiveDataFrame, GridArchive
from ribs.emitters import EvolutionStrategyEmitter
from ribs.schedulers import Scheduler
from ribs.visualize import grid_archive_heatmap

def create_scheduler(seed, emitter_type, representation, n_emitters, batch_size):
    """Creates the Scheduler based on given configurations.

    Returns:
        A pyribs scheduler.
    """
    archive = GridArchive(
        solution_dim=AB_NUM_ROOMS * 3 + AB_NUM_CORRIDORS * 3,
        dims=[20, 20], 
        ranges=[(0, 1), (0, 1)],
        seed=seed,
        qd_score_offset=0,
    )

    # If we create the emitters with identical seeds, they will all output the
    # same initial solutions. The algorithm should still work -- eventually, the
    # emitters will produce different solutions because they get different
    # responses when inserting into the archive. However, using different seeds
    # avoids this problem altogether.
    seeds = ([None] * n_emitters
             if seed is None else [seed + i for i in range(n_emitters)])
    
    x0 = []
    initial_solutions = []
    match representation:
        case c.ALL_BLACK_NAME:
            x0, initial_solutions = initialize_solutions(representation, ab_generation.create_random_genome, c.NUMBER_OF_INITAL_SOLUTIONS)
        case c.GRID_GRAPH_NAME:
            x0, initial_solutions = initialize_solutions(representation, graph_generation.create_random_genome, c.NUMBER_OF_INITAL_SOLUTIONS)            
    
    emitters = []
    match emitter_type:
        case c.ALL_BLACK_EMITTER_NAME:
            emitters = [
                AbGenomeEmitter(
                    archive,
                    x0=x0,
                    initial_solutions=initial_solutions,
                    crossover_probability=AB_STANDARD_MUTATION_CHANCE,
                    batch_size=batch_size,
                    seed=s,
                    #bounds=bounds,
                ) for s in seeds
            ]
            
        case c.CMA_ME_EMITTER_NAME:
            emitters = [
                EvolutionStrategyEmitter(
                    archive,
                    x0=x0,
                    sigma0=CMA_ME_SIGMA0,
                    ranker="2imp",
                    batch_size=batch_size,
                    seed=s,
                ) for s in seeds
            ]

    scheduler = Scheduler(archive, emitters)
    return scheduler

def initialize_solutions(creation_function, n_solutions):
    x0 = []
    x0 = ab_generation.create_random_genome().to_array()
    
    solutions = []
    for _ in range(n_solutions):
        solutions.append(creation_function().to_array())

    return x0, solutions


def run_search(client: Client, scheduler: Scheduler, representation, iterations, log_freq, folder_name, bot1_data, bot2_data, game_length=600):
    """Runs the QD algorithm for the given number of iterations.

    Args:
        client (Client): A Dask client providing access to workers.
        scheduler (Scheduler): pyribs scheduler.
        env_seed (int): Seed for the environment.
        iterations (int): Iterations to run.
        log_freq (int): Number of iterations to wait before recording metrics.
    Returns:
        dict: A mapping from various metric names to a list of "x" and "y"
        values where x is the iteration and y is the value of the metric. Think
        of each entry as the x's and y's for a matplotlib plot.
    """
    print(
        "> Starting search.\n"
        "  - Open Dask's dashboard at http://localhost:8787 to monitor workers."
    )

    metrics = {
        "Max Score": {
            "x": [],
            "y": [],
        },
        "Archive Size": {
            "x": [0],
            "y": [0],
        },
        "QD Score": {
            "x": [0],
            "y": [0],
        },
    }

    start_time = time.time()
    for itr in tqdm.trange(1, iterations + 1):
        # Request genomes from the scheduler.
        genotypes_sols = scheduler.ask()
        
        match representation:
            case c.ALL_BLACK_NAME:
                phenotypes = list(map(lambda geno: (ABGenome.array_as_genome(list(map(int, geno.tolist())))).phenotype(), genotypes_sols))
            case c.GRID_GRAPH_NAME:
                phenotypes = list(map(lambda geno: (GraphGenome.array_as_genome(list(map(int, geno.tolist())))).phenotype(), genotypes_sols))

        # Evaluate the genomes and record the objectives and measures.
        objs, meas = [], []

        # Ask the Dask client to distribute the simulations among the Dask
        # workers, then gather the results of the simulations.
        futures = []
        
        futures = client.map(
            lambda p: evaluate(
                p, 
                itr -1, 
                phenotypes.index(p), 
                bot1_data, 
                bot2_data,
                game_length,
                folder_name=folder_name
                ), phenotypes
        )
        results = client.gather(futures)

        # Process the results.
        for dataset in results:
            pace = round(np.mean(dataset["pace"]), 2)
            entropy = round(np.mean(dataset["entropy"]), 2)
            target_loss = round(np.mean(dataset["targetLossRate"]), 2)
            pursue_time = round(np.mean(dataset["pursueTime"]), 2)

            objs.append(target_loss)
            meas.append([entropy, pace])
        
        # Send the results back to the scheduler.
        scheduler.tell(objs, meas)

        # Logging.
        if itr % log_freq == 0 or itr == iterations:
            elapsed_time = time.time() - start_time
            metrics["Max Score"]["x"].append(itr)
            metrics["Max Score"]["y"].append(scheduler.archive.stats.obj_max)
            metrics["Archive Size"]["x"].append(itr)
            metrics["Archive Size"]["y"].append(len(scheduler.archive))
            metrics["QD Score"]["x"].append(itr)
            metrics["QD Score"]["y"].append(scheduler.archive.stats.qd_score)
            tqdm.tqdm.write(
                f"> {itr} itrs completed after {elapsed_time:.2f} s\n"
                f"  - Max Score: {metrics['Max Score']['y'][-1]}\n"
                f"  - Archive Size: {metrics['Archive Size']['y'][-1]}\n"
                f"  - QD Score: {metrics['QD Score']['y'][-1]}")

    return metrics


def save_heatmap(archive, filename):
    """Saves a heatmap of the scheduler's archive to the filename.

    Args:
        archive (GridArchive): Archive with results from an experiment.
        filename (str): Path to an image file.
    """
    fig, ax = plt.subplots(figsize=(8, 6))
    grid_archive_heatmap(archive, ax=ax, vmin=0, vmax=1)
    # ax.invert_yaxis()  # Makes more sense if larger velocities are on top.
    ax.set_xlabel("Entropy")
    ax.set_ylabel("Pace")
    fig.savefig(filename)


def save_metrics(outdir, metrics):
    """Saves metrics to png plots and a JSON file.

    Args:
        outdir (Path): output directory for saving files.
        metrics (dict): Metrics as output by run_search.
    """
    # Plots.
    for metric in metrics:
        fig, ax = plt.subplots()
        ax.plot(metrics[metric]["x"], metrics[metric]["y"])
        ax.set_title(metric)
        ax.set_xlabel("Iteration")
        fig.savefig(str(outdir / f"{metric.lower().replace(' ', '_')}.png"))

    # JSON file.
    with (outdir / "metrics.json").open("w") as file:
        json.dump(metrics, file, indent=2)


def save_ccdf(archive, filename):
    """Saves a CCDF showing the distribution of the archive's objectives.

    CCDF = Complementary Cumulative Distribution Function (see
    https://en.wikipedia.org/wiki/Cumulative_distribution_function#Complementary_cumulative_distribution_function_(tail_distribution)).
    The CCDF plotted here is not normalized to the range (0,1). This may help
    when comparing CCDF's among archives with different amounts of coverage
    (i.e. when one archive has more cells filled).

    Args:
        archive (GridArchive): Archive with results from an experiment.
        filename (str): Path to an image file.
    """
    fig, ax = plt.subplots()
    ax.hist(
        archive.data("objective"),
        50,  # Number of cells.
        histtype="step",
        density=False,
        cumulative=-1)  # CCDF rather than CDF.
    ax.set_xlabel("Objectives")
    ax.set_ylabel("Num. Entries")
    ax.set_title("Distribution of Archive Objectives")
    fig.savefig(filename)

def evolve_maps(
                bot1_data, 
                bot2_data,
                representation,
                emitter_type,
                workers=4,
                iterations=500,
                log_freq=25,
                n_emitters=5,
                batch_size=30,
                game_length=600,
                seed=None,
                folder_name="test_directory",
                ):
    """Uses CMA-ME to train linear agents in Lunar Lander.

    Args:
        workers (int): Number of workers to use for simulations.
        env_seed (int): Environment seed. The default gives the flat terrain
            from the tutorial.
        iterations (int): Number of iterations to run the algorithm.
        log_freq (int): Number of iterations to wait before recording metrics
            and saving heatmap.
        n_emitters (int): Number of emitters.
        batch_size (int): Batch size of each emitter.
        seed (seed): Random seed for the pyribs components.
        outdir (str): Directory for Lunar Lander output.
        run_eval (bool): Pass this flag to run an evaluation of 10 random
            solutions selected from the archive in the `outdir`.
    """

    # Create directories.
    outdir = Path(os.path.join(MAP_ELITES_OUTPUT_FOLDER, folder_name))
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

    # CMA-ME.
    scheduler = create_scheduler(seed, n_emitters, batch_size)
    metrics = run_search(client, scheduler, iterations, log_freq, folder_name, bot1_data, bot2_data, game_length)

    # Outputs.
    scheduler.archive.data(return_type="pandas").to_csv(outdir / "archive.csv")
    save_ccdf(scheduler.archive, str(outdir / "archive_ccdf.png"))
    save_heatmap(scheduler.archive, str(outdir / "heatmap.png"))
    save_metrics(outdir, metrics)


if __name__ == "__main__":
    parser = argparse.ArgumentParser(description='Evolve population.')

    # This is the directory where we will store the results of the experiment, the import data and the export data.
    # We can also see this as the name of the experiment.
    parser.add_argument("--folder_name", default="test_directory", type=str, dest="folder_name") 

    # These are the parameters for specifying representation and emitter.
    parser.add_argument("--representation", default=ALL_BLACK_NAME, type=str, dest="representation")
    parser.add_argument("--emitter_type", default=ALL_BLACK_EMITTER_NAME, type=str, dest="emitter_type")

    # These are the parameters for the evolution algorithm.
    parser.add_argument("--iterations", default=50, type=int, dest="iterations")
    parser.add_argument("--batch_size", default=5, type=int, dest="batch_size")
    parser.add_argument("--workers", default=4, type=int, dest="workers")
    parser.add_argument("--n_emitters", default=5, type=int, dest="n_emitters")

    # These are the parameters for the game.
    parser.add_argument("--bot1_file_prefix", required=True, dest="bot1_file")
    parser.add_argument("--bot1_skill", required=True, dest="bot1_skill")
    parser.add_argument("--bot2_file_prefix", required=True, dest="bot2_file")
    parser.add_argument("--bot2_skill", required=True, dest="bot2_skill")
    parser.add_argument("--game_length", type=int, dest="game_length")


    args = parser.parse_args(sys.argv[1:])

    bot1_data = {"file": args.bot1_file, "skill": args.bot1_skill}
    bot2_data = {"file": args.bot2_file, "skill": args.bot2_skill}

    evolve_maps(
        bot1_data, 
        bot2_data, 
        representation=args.representation,
        emitter_type=args.emitter_type,
        batch_size=args.batch_size, 
        iterations=args.iterations, 
        n_emitters=args.n_emitters, 
        workers=args.workers, 
        folder_name=args.folder_name,
        game_length=args.game_length,
        )
    
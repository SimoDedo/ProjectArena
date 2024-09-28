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

from internals.pyribs_ext.genome_emitter import GenomeEmitter
from internals.graph_genome.constants import GG_NUM_ROWS, GG_NUM_COLUMNS
from internals.graph_genome.gg_genome import GraphGenome
import internals.constants as constants
import internals.config as conf
from internals.constants import GAME_DATA_FOLDER, MAP_ELITES_OUTPUT_FOLDER
from internals.ab_genome.constants import AB_NUM_CORRIDORS, AB_NUM_ROOMS
from internals.ab_genome.ab_genome import ABGenome
from internals.smt_genome.smt_genome import SMTGenome
from internals.smt_genome.constants import SMT_ROOMS_NUMBER, SMT_LINES_NUMBER
from internals.point_genome.point_genome import PointGenome
from internals.point_genome.constants import POINT_NUM_POINT_COUPLES, POINT_NUM_ROOMS
from internals.point_ad_genome.point_ad_genome import PointAdGenome
from internals.point_ad_genome.constants import POINT_AD_NUM_POINT_COUPLES
import internals.smt_genome.mutation as smt_mutation
import internals.smt_genome.generation as smt_generation
import internals.graph_genome.generation as graph_generation
import internals.evaluation as eval
import matplotlib
matplotlib.use('Agg')
import matplotlib.pyplot as plt
import numpy as np
import pandas as pd
import tqdm
from dask.distributed import Client, LocalCluster
import pickle

from ribs.archives import ArchiveDataFrame, GridArchive, SlidingBoundariesArchive
from ribs.emitters import EvolutionStrategyEmitter
from ribs.schedulers import Scheduler
from internals.pyribs_ext.scheduler_lineage import SchedulerLineage
from ribs.visualize import grid_archive_heatmap, sliding_boundaries_archive_heatmap

def create_scheduler(seed, emitter_type, representation, n_emitters, batch_size):
    """Creates the Scheduler based on given configurations.

    Returns:
        A pyribs scheduler.
    """
    solution_dim = 0
    match representation:
        case constants.ALL_BLACK_NAME:
            solution_dim = AB_NUM_ROOMS * 3 + AB_NUM_CORRIDORS * 3
        case constants.GRID_GRAPH_NAME:
            solution_dim = GG_NUM_ROWS * GG_NUM_COLUMNS * 4 + (GG_NUM_ROWS - 1) * GG_NUM_COLUMNS + GG_NUM_ROWS * (GG_NUM_COLUMNS - 1)
        case constants.SMT_NAME:
            solution_dim = SMT_ROOMS_NUMBER * 2 + SMT_LINES_NUMBER * 4 + 1
        case constants.POINT_NAME:
            solution_dim =  POINT_NUM_POINT_COUPLES * 5 + POINT_NUM_ROOMS * 3
        case constants.POINT_AD_NAME:
            solution_dim =  POINT_AD_NUM_POINT_COUPLES * 7
    archive = None
    match conf.ARCHIVE_TYPE:
        case constants.GRID_ARCHIVE_NAME:
            archive = GridArchive(
                solution_dim=solution_dim,
                dims=conf.MEASURES_BINS_NUMBER, 
                ranges=conf.MEASURES_RANGES,
                seed=seed,
                qd_score_offset=0,
                extra_fields={"iterations": ((), np.float32), "individual_numbers": ((), np.float32)}
            )
        case constants.SLIDING_BOUNDARIES_ARCHIVE_NAME:
            archive = SlidingBoundariesArchive(
                solution_dim=solution_dim,
                dims=conf.MEASURES_BINS_NUMBER, 
                ranges=conf.MEASURES_RANGES,
                seed=seed,
                qd_score_offset=0,
                extra_fields={"iterations": ((), np.float32), "individual_numbers": ((), np.float32)}
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
        case constants.ALL_BLACK_NAME:
            x0, initial_solutions = initialize_solutions(ABGenome.create_random_genome, conf.NUMBER_OF_INITAL_SOLUTIONS)
        case constants.GRID_GRAPH_NAME:
            x0, initial_solutions = initialize_solutions(GraphGenome.create_random_genome, conf.NUMBER_OF_INITAL_SOLUTIONS)
        case constants.SMT_NAME:
            x0, initial_solutions = initialize_solutions(SMTGenome.create_random_genome, conf.NUMBER_OF_INITAL_SOLUTIONS)
        case constants.POINT_NAME:
            x0, initial_solutions = initialize_solutions(PointGenome.create_random_genome, conf.NUMBER_OF_INITAL_SOLUTIONS)
        case constants.POINT_AD_NAME:
            x0, initial_solutions = initialize_solutions(PointAdGenome.create_random_genome, conf.NUMBER_OF_INITAL_SOLUTIONS)
    emitters = []
    match emitter_type:
        case constants.ALL_BLACK_EMITTER_NAME:
            emitters = [
                GenomeEmitter(
                    archive,
                    genome_type=ABGenome,
                    #x0=x0,
                    initial_solutions=initial_solutions,
                    crossover_probability=conf.AB_STANDARD_CROSSOVER_CHANCE,
                    batch_size=batch_size,
                    seed=s,
                    #bounds=bounds,
                ) for s in seeds
            ]
        case constants.GRID_GRAPH_EMITTER_NAME:
            emitters = [
                GenomeEmitter(
                    archive,
                    genome_type=GraphGenome,
                    #x0=x0,
                    initial_solutions=initial_solutions,
                    crossover_probability=conf.GG_STANDARD_CROSSOVER_CHANCE,
                    batch_size=batch_size,
                    seed=s,
                    #bounds=bounds,
                ) for s in seeds
            ]
        
        case constants.SMT_EMITTER_NAME:
            emitters = [
                GenomeEmitter(
                    archive,
                    genome_type=SMTGenome,
                    #x0=x0,
                    initial_solutions=initial_solutions,
                    crossover_probability=conf.SMT_STANDARD_CROSSOVER_CHANCE,
                    batch_size=batch_size,
                    seed=s,
                    #bounds=bounds,
                ) for s in seeds
            ]

        case constants.POINT_EMITTER_NAME:
            emitters = [
                GenomeEmitter(
                    archive,
                    genome_type=PointGenome,
                    #x0=x0,
                    initial_solutions=initial_solutions,
                    crossover_probability=conf.POINT_STANDARD_CROSSOVER_CHANCE,
                    batch_size=batch_size,
                    seed=s,
                    #bounds=bounds,
                ) for s in seeds
            ]
        
        case constants.POINT_AD_EMITTER_NAME:
            emitters = [
                GenomeEmitter(
                    archive,
                    genome_type=PointAdGenome,
                    #x0=x0,
                    initial_solutions=initial_solutions,
                    crossover_probability=conf.POINT_AD_STANDARD_CROSSOVER_CHANCE,
                    batch_size=batch_size,
                    seed=s,
                    #bounds=bounds,
                ) for s in seeds
            ]

        case constants.CMA_ME_EMITTER_NAME:
            emitters = [
                EvolutionStrategyEmitter(
                    archive,
                    x0=x0,
                    sigma0=conf.CMA_ME_SIGMA0,
                    ranker="2imp",
                    batch_size=batch_size,
                    seed=s,
                ) for s in seeds
            ]

    scheduler = SchedulerLineage(archive, emitters)
    return scheduler

def initialize_solutions(creation_function, n_solutions):
    x0 = []
    x0 = creation_function().to_array()
    
    solutions = []
    for _ in range(n_solutions):
        solutions.append(creation_function().to_array())

    return x0, solutions


def run_search(client: Client, scheduler: SchedulerLineage, representation, iterations, log_freq, folder_name, bot1_data, bot2_data, game_length=600):
    """
    #TODO
    """
    print(
        "> Starting search.\n"
        "  - Open Dask's dashboard at http://localhost:8787 to monitor workers."
    )
    outdir = Path(os.path.join(MAP_ELITES_OUTPUT_FOLDER, folder_name))

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
        "Failed": {
            "x": [0],
            "y": [0],
        },
    }
    num_failed = 0

    start_time = time.time()
    for itr in tqdm.trange(1, iterations + 1):
        # Request genomes from the scheduler.
        genotypes_sols = scheduler.ask()
        
        match representation:
            case constants.ALL_BLACK_NAME:
                phenotypes = list(map(lambda geno: (ABGenome.array_as_genome(list(map(int, geno.tolist())))).phenotype(), genotypes_sols))
            case constants.GRID_GRAPH_NAME:
                phenotypes = list(map(lambda geno: (GraphGenome.array_as_genome(list(map(int, geno.tolist())))).phenotype(), genotypes_sols))
            case constants.SMT_NAME:
                genotypes = list(map(lambda geno: SMTGenome.array_as_genome(list(map(int, geno.tolist()))), genotypes_sols))
                phenotypes = []
                to_skip = []
                for geno in genotypes:
                    try:
                        phenotypes.append(geno.phenotype())
                        to_skip.append(False)
                        #tqdm.tqdm.write("Phenotype created")
                    except Exception as e:
                        #tqdm.tqdm.write(str(e))
                        phenotypes.append(None)
                        to_skip.append(True)
            case constants.POINT_NAME:
                phenotypes = list(map(lambda geno: (PointGenome.array_as_genome(list(map(int, geno.tolist())))).phenotype(), genotypes_sols))
            case constants.POINT_AD_NAME:
                phenotypes = list(map(lambda geno: (PointAdGenome.array_as_genome(list(map(int, geno.tolist())))).phenotype(), genotypes_sols))
        #tqdm.tqdm.write("Finished creating phenotypes")

        # Evaluate the genomes and record the objectives and measures.
        objs, meas = [], []

        # Also remember the iteration and individual number for each solution.
        itrs, inds = [], []

        # Ask the Dask client to distribute the simulations among the Dask
        # workers, then gather the results of the simulations.
        futures = []
        futures = client.map(
            lambda p: 
            #eval.test()
            eval.evaluate(
                p, 
                itr -1, 
                phenotypes.index(p), 
                bot1_data, 
                bot2_data,
                game_length,
                folder_name=folder_name
                )
            , phenotypes
        )
        results = client.gather(futures)

        # Process the results.
        for idx, (dataset, failed) in enumerate(results):
            if not failed:

                if conf.MANUALLY_CHOOSE_FEATURES:
                    # Modify here to use a different/combination of features.
                    entropy = round(np.mean(dataset["entropy"]), 5)

                    balanceTopology = round(np.mean(dataset["balanceTopology"]), 5)
                    peripheryCenterBalance = round(np.mean(dataset["peripheryCenterBalance"]), 5)
                    pursueTime = round(np.mean(dataset["pursueTime"]), 5)
                    entropy = round(np.mean(dataset["entropy"]), 5)

                    objs.append(entropy)
                    meas.append([balanceTopology, pursueTime])
                else:
                    objs.append(round(np.mean(dataset[conf.OBJECTIVE_NAME]), 5))
                    meas.append([round(np.mean(dataset[conf.MEASURES_NAMES[0]]), 5), round(np.mean(dataset[conf.MEASURES_NAMES[1]]), 5)])

                itrs.append(itr-1)
                inds.append(idx)
            else:
                num_failed += 1
                objs.append(0 if conf.OBJECTIVE_RANGE[0] == None else conf.OBJECTIVE_RANGE[0])
                meas.append([0, 0])
                itrs.append(itr-1)
                inds.append(idx)
        
        # Send the results back to the scheduler.
        scheduler.tell(objs, meas, iterations=itrs, individual_numbers=inds)

        # Logging.
        if itr % log_freq == 0 or itr == iterations:
            elapsed_time = time.time() - start_time
            metrics["Max Score"]["x"].append(itr)
            metrics["Max Score"]["y"].append(scheduler.archive.stats.obj_max)
            metrics["Archive Size"]["x"].append(itr)
            metrics["Archive Size"]["y"].append(len(scheduler.archive))
            metrics["QD Score"]["x"].append(itr)
            metrics["QD Score"]["y"].append(scheduler.archive.stats.qd_score)
            metrics["Failed"]["x"].append(itr)
            metrics["Failed"]["y"].append(num_failed)
            tqdm.tqdm.write(
                f"> {itr} itrs completed after {elapsed_time:.2f} s\n"
                f"  - Max Score: {metrics['Max Score']['y'][-1]}\n"
                f"  - Archive Size: {metrics['Archive Size']['y'][-1]}\n"
                f"  - QD Score: {metrics['QD Score']['y'][-1]}\n"
                f"  - Failed: {metrics['Failed']['y'][-1]}")
            if conf.SAVE_INTERMEDIATE_RESULTS:
                scheduler.archive.data(return_type="pandas").to_csv(outdir / "archive.csv")
                save_ccdf(scheduler.archive, str(outdir / "archive_ccdf.png"))
                save_heatmap(scheduler.archive, str(outdir / "heatmap.png"))
                save_metrics(outdir, metrics)
                plt.close('all')
                lineage_table_file = open(os.path.join(outdir, 'lineages.pkl'), 'wb')
                pickle.dump(scheduler.get_lineage_table(), lineage_table_file)

    return metrics


def save_heatmap(archive, filename):
    """Saves a heatmap of the scheduler's archive to the filename.

    Args:
        archive (GridArchive): Archive with results from an experiment.
        filename (str): Path to an image file.
    """
    fig, ax = plt.subplots(figsize=(8, 6))
    match conf.ARCHIVE_TYPE:
        case constants.GRID_ARCHIVE_NAME:
            grid_archive_heatmap(archive, ax=ax, vmin=conf.OBJECTIVE_RANGE[0], vmax=conf.OBJECTIVE_RANGE[1])
        case constants.SLIDING_BOUNDARIES_ARCHIVE_NAME:
            sliding_boundaries_archive_heatmap(archive, ax=ax, vmin=conf.OBJECTIVE_RANGE[0], vmax=conf.OBJECTIVE_RANGE[1])
    # ax.invert_yaxis()  # Makes more sense if larger velocities are on top.
    ax.set_xlabel(conf.MEASURES_NAMES[0])
    ax.set_ylabel(conf.MEASURES_NAMES[1])
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
                log_freq=10,
                n_emitters=5,
                batch_size=30,
                game_length=600,
                seed=None,
                folder_name="test_directory",
                ):
    """
    #TODO
    """

    genome = graph_generation.create_random_genome()
    arr = genome.to_array()
    genome2 = GraphGenome.array_as_genome(arr)
    if genome.phenotype() != genome2.phenotype():
        print("Error")
    
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

    scheduler = create_scheduler(seed, emitter_type, representation, n_emitters, batch_size)
    metrics = run_search(client, scheduler, representation,iterations, log_freq, folder_name, bot1_data, bot2_data, game_length)

    # Outputs.
    scheduler.archive.data(return_type="pandas").to_csv(outdir / "archive.csv")
    save_ccdf(scheduler.archive, str(outdir / "archive_ccdf.png"))
    save_heatmap(scheduler.archive, str(outdir / "heatmap.png"))
    save_metrics(outdir, metrics)


if __name__ == "__main__":
    parser = argparse.ArgumentParser(description='MAP-Elites.')

    parser.add_argument("--workers", default=4, type=int, dest="workers")
    parser.add_argument("--is_test", default=False, type=bool, dest="is_test")

    args = parser.parse_args(sys.argv[1:])

    bot1_data = {"file": conf.BOT1_FILE_PREFIX, "skill": conf.BOT1_SKILL}
    bot2_data = {"file": conf.BOT2_FILE_PREFIX, "skill": conf.BOT2_SKILL}

    evolve_maps(
        bot1_data, 
        bot2_data, 
        representation=conf.REPRESENTATION_NAME,
        emitter_type=conf.EMITTER_TYPE_NAME,
        batch_size=conf.BATCH_SIZE, 
        iterations=conf.ITERATIONS, 
        n_emitters=conf.N_EMITTERS, 
        workers=args.workers, 
        folder_name=conf.folder_name(args.is_test),
        game_length=conf.GAME_LENGTH,
        )
    
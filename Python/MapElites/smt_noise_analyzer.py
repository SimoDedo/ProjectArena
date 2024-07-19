from z3 import *
from internals.ab_genome.ab_genome import ABGenome
from internals.graph_genome.gg_genome import GraphGenome
from internals.smt_genome.smt_genome import SMTGenome
import matplotlib.pyplot as plt
import numpy as np
import igraph as ig
from matplotlib import cm
from os import path
from internals.constants import NOISE_ANALYSIS_OUTPUT_FOLDER, GAME_DATA_FOLDER, MAP_ELITES_OUTPUT_FOLDER
from internals.evaluation import evaluate
from dask.distributed import Client, LocalCluster
from internals import config as conf
import tqdm
import pandas as pd
from archive_analysis import __save_map_lines, __save_map
from pathlib import Path
from internals.constants import SMT_NAME, ALL_BLACK_NAME, GRID_GRAPH_NAME
import matplotlib
matplotlib.use('Agg')
import matplotlib.pyplot as plt

def run_analysis(name, itr, genopme_type):
    importdir = Path(os.path.join(GAME_DATA_FOLDER, "Import", "Genomes", name))
    importdir.mkdir(exist_ok=True)
    exportdir = Path(os.path.join(GAME_DATA_FOLDER, "Export", name))
    exportdir.mkdir(exist_ok=True)
    outdir = Path(path.join(baseoutdir, name))
    outdir.mkdir(exist_ok=True)
    # Create random SMTGenome
    if genopme_type == SMT_NAME:
        genome = SMTGenome.create_random_genome()
        found = False
        while not found:
            try:
                phenotype = genome.phenotype()
                found = True
            except Exception as e:
                genome = SMTGenome.create_random_genome()
    elif genopme_type == ALL_BLACK_NAME:
        genome = ABGenome.create_random_genome()
        phenotype = genome.phenotype()
    elif genopme_type == GRID_GRAPH_NAME:
        genome = GraphGenome.create_random_genome()
        phenotype = genome.phenotype()
    
    phenotypes = [phenotype]
    j = 1
    stop = False
    while not stop and j < 10:
        try:
            phenotypes.append(genome.phenotype())
            j += 1
        except Exception as e:
            stop = True
    print(f"{itr}: {j} phenotypes generated")

    futures = client.map(
        lambda p: 
        #eval.test()
        evaluate(
            p, 
            itr, 
            phenotypes.index(p), 
            bot1_data, 
            bot2_data,
            600,
            folder_name=name
            )
        , phenotypes
    )
    results = client.gather(futures)
    
    datasets = []
    for idx, (dataset, failed) in enumerate(results):
        if failed:
            print(f"Failed to evaluate phenotype {idx}")
        else:
            datasets.append(dataset)
    df = pd.concat(datasets)
    mean = df.mean()
    std = df.std()
    merged_df = pd.concat([mean, std], axis=1)
    merged_df.to_csv(os.path.join(outdir, name + ".csv"))

    if genopme_type == SMT_NAME:
        for p in phenotypes:
            __save_map_lines(os.path.join(outdir, f"phenotype_{name}_{phenotypes.index(p)}.png"), p, genome.lines)
    elif genopme_type == ALL_BLACK_NAME or genopme_type == GRID_GRAPH_NAME:
        __save_map(os.path.join(outdir, f"phenotype_{name}.png"), phenotypes[0].map_matrix())
        
if __name__ == "__main__":
    bot1_data = {"file": conf.BOT1_FILE_PREFIX, "skill": conf.BOT1_SKILL}
    bot2_data = {"file": conf.BOT2_FILE_PREFIX, "skill": conf.BOT2_SKILL}



    cluster = LocalCluster(
        processes=True,  # Each worker is a process.
        n_workers=10,  # Create this many worker processes.
        threads_per_worker=1,  # Each worker process is single-threaded.
    )
    client = Client(cluster)

    baseoutdir = Path(path.join(NOISE_ANALYSIS_OUTPUT_FOLDER, 'smtnoise'))
    baseoutdir.mkdir(exist_ok=True)

    #for i in tqdm.trange(10):
    #    name = f'smtnoise_run{i}'
    #    run_analysis(name, i, SMT_NAME)
    
    name = 'allblack'
    run_analysis(name, 0, ALL_BLACK_NAME)
    name = 'gridgraph'
    run_analysis(name, 0, GRID_GRAPH_NAME)

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
from match_noise_analyzer import get_feature_ranges, heatmap_features, emergent_features, traces_features, topology_features, visibility_features, symmetry_features

def run_analysis(name, itr, genopme_type, feature_ranges, num_matches_per_simulation=1, num_phenotypes=50):
    importdir = Path(os.path.join(GAME_DATA_FOLDER, "Import", "Genomes", name))
    importdir.mkdir(exist_ok=True)
    exportdir = Path(os.path.join(GAME_DATA_FOLDER, "Export", name))
    exportdir.mkdir(exist_ok=True)
    outdir = Path(path.join(baseoutdir, name))
    outdir.mkdir(exist_ok=True)
    mapoutdir = Path(path.join(outdir, "maps"))
    mapoutdir.mkdir(exist_ok=True)
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
    while not stop and j < num_phenotypes:
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
            folder_name=name,
            num_matches_per_simulation=1
            )
        , phenotypes
    )
    results = client.gather(futures)
    
    datasets = []
    for idx, (dataset, failed) in enumerate(results):
        if failed:
            print(f"Failed to evaluate phenotype {idx}")
        else:
            mean = dataset.mean()
            mean_dataset = pd.DataFrame([mean], columns=dataset.columns)
            datasets.append(mean_dataset)

    if genopme_type == SMT_NAME:
        for p in phenotypes:
            __save_map_lines(os.path.join(mapoutdir, f"phenotype_{name}_{phenotypes.index(p)}.png"), p, genome.lines)
    elif genopme_type == ALL_BLACK_NAME or genopme_type == GRID_GRAPH_NAME:
        __save_map(os.path.join(mapoutdir, f"phenotype_{name}.png"), phenotypes[0].map_matrix())
    
    df = pd.concat(datasets)
    df.index = range(len(df))
    df.to_json(os.path.join(outdir, f"final_results_{name}.json"), orient="columns", indent=4)
    min_values = df.min()
    max_values = df.max()

    
    # Normalize all features according to the feature ranges
    for feature in df.columns:
        min_value = feature_ranges.loc["min", feature] if feature_ranges.loc["min", feature] < min_values[feature] else min_values[feature]
        max_value = feature_ranges.loc["max", feature] if feature_ranges.loc["max", feature] > max_values[feature] else max_values[feature]
        df[feature] = (df[feature] - min_value) / (max_value - min_value)


    df.boxplot(column=emergent_features + heatmap_features + traces_features, return_type='dict', fontsize=5)
    plt.xticks(rotation=90)
    plt.tight_layout()
    #Save figure
    plt.savefig(os.path.join(outdir, f"boxplot_emergent_{name}_numsim_{num_matches_per_simulation}.png"), format='png', dpi=300)
    plt.clf()
    plt.close()

    df.boxplot(column=topology_features + visibility_features + symmetry_features, return_type='dict', fontsize=5)
    plt.xticks(rotation=90)
    plt.tight_layout()
    #Save figure
    plt.savefig(os.path.join(outdir, f"boxplot_topology_{name}_numsim_{num_matches_per_simulation}.png"), format='png', dpi=300)
    plt.clf()
    plt.close()


if __name__ == "__main__":
    bot1_data = {"file": 'sniper', "skill": '0.15'}
    bot2_data = {"file": 'shotgun', "skill": '0.85'}


    cluster = LocalCluster(
        processes=True,  # Each worker is a process.
        n_workers=10,  # Create this many worker processes.
        threads_per_worker=1,  # Each worker process is single-threaded.
    )
    client = Client(cluster)

    baseoutdir = Path(path.join(NOISE_ANALYSIS_OUTPUT_FOLDER, 'smtnoise'))
    baseoutdir.mkdir(exist_ok=True)

    experiment_names = [
        "Bari_SMT_SMTEmitter_SB_entropy_balanceTopology_pursueTime_I400_B1_E10",
        "BariInverse_AB_ABEmitter_SB_entropy_balanceTopology_pursueTime_I400_B1_E10",
        "BariInverse_PointAD_PointADEmitter_SB_entropy_balanceTopology_pursueTime_I400_B1_E10",
        "Bari_GG_GGEmitter_SB_entropy_balanceTopology_pursueTime_I400_B1_E10"
    ]

    COMPUTE_FEATURE_RANGES = False
    feature_ranges = get_feature_ranges(experiment_names, baseoutdir, to_compute=COMPUTE_FEATURE_RANGES)
    
    for i in tqdm.trange(3):
        name = f'smtnoise_5_run{i}'
        run_analysis(name, i, SMT_NAME, feature_ranges, num_phenotypes=100)

    #name = 'allblack'
    #run_analysis(name, 0, ALL_BLACK_NAME)
    #name = 'gridgraph'
    #run_analysis(name, 0, GRID_GRAPH_NAME)

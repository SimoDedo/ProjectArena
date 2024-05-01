"""
TODO: Add description of the file here.
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
from internals.constants import AB_STANDARD_MUTATION_CHANCE, ALL_BLACK_EMITTER_NAME, ALL_BLACK_NAME, ARCHIVE_ANALYSIS_OUTPUT_FOLDER, CMA_ME_SIGMA0, GAME_DATA_FOLDER, MAP_ELITES_OUTPUT_FOLDER
from internals.ab_genome.ab_genome import AB_MAX_CORRIDOR_LENGTH, AB_MAX_MAP_HEIGHT, AB_MAX_MAP_WIDTH, AB_MAX_ROOM_SIZE, AB_MIN_CORRIDOR_LENGTH, AB_MIN_ROOM_SIZE, AB_NUM_CORRIDORS, AB_NUM_ROOMS, ABGenome
import internals.ab_genome.generation as ab_generation
import internals.graph_genome.generation as graph_generation
from internals.evaluation import evaluate
import matplotlib.pyplot as plt
from matplotlib import cm
import numpy as np
import pandas as pd
import tqdm
from dask.distributed import Client, LocalCluster

from ribs.archives import ArchiveDataFrame


def __save_map(path, map_matrix, note=None):
    plt.imshow(map_matrix, cmap=cm.Greys, alpha=1.0)
    plt.axis('off')
    plt.annotate(note, xy=(0, 0), xytext=(0, -1), fontsize=12, color='black')
    plt.savefig(path, bbox_inches='tight')
    plt.clf()
    plt.close()

def __save_map_images(outdir, archive: ArchiveDataFrame, representation):
    outdir = Path(os.path.join(outdir, "Maps"))
    outdir.mkdir(exist_ok=True)

    archive.sort_values(["measures_0", "measures_1", "objective"], ascending=True, inplace=True)

    solutions = archive.get_field("solution")
    obj = archive.get_field("objective")
    meas_0 = archive.get_field("measures_0")
    meas_1 = archive.get_field("measures_1")

    for idx in tqdm.trange(0, len(solutions)):
        sol = solutions[idx]
        phenotype = None
        match representation:
          case c.ALL_BLACK_NAME:
            phenotype = ABGenome.array_as_genome(list(map(int, sol.tolist()))).phenotype()
          case c.GRID_GRAPH_NAME:
            phenotype = GraphGenome.array_as_genome(list(map(int, sol.tolist()))).phenotype()
        
        __save_map(outdir / f"map_{idx}.png", phenotype.map_matrix(), note=f"Obj: {obj[idx]:.2f}\nMeas0: {meas_0[idx]:.2f}\nMeas1: {meas_1[idx]:.2f}")

def analyze_archive(
                representation,
                folder_name="test_directory",
                ):
    archivedir = Path(os.path.join(MAP_ELITES_OUTPUT_FOLDER, folder_name))
    outdir = Path(os.path.join(ARCHIVE_ANALYSIS_OUTPUT_FOLDER, folder_name))
    outdir.mkdir(exist_ok=True)

    df = ArchiveDataFrame(pd.read_csv(archivedir / "archive.csv"))

    __save_map_images(outdir, df, representation)

    return


if __name__ == "__main__":
    parser = argparse.ArgumentParser(description='Evolve population.')

    # This is the directory where we will store the results of the experiment, the import data and the export data.
    # We can also see this as the name of the experiment.
    parser.add_argument("--folder_name", default="test_directory", type=str, dest="folder_name") 

    # These are the parameters for specifying representation and emitter.
    parser.add_argument("--representation", default=ALL_BLACK_NAME, type=str, dest="representation")

    args = parser.parse_args(sys.argv[1:])

    analyze_archive(
        representation=args.representation,
        folder_name=args.folder_name,
        )
    
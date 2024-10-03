import pickle
from z3 import *
from internals.ab_genome.ab_genome import ABGenome
from internals.graph_genome.gg_genome import GraphGenome
from internals.smt_genome.smt_genome import SMTGenome
from internals.point_ad_genome.point_ad_genome import PointAdGenome
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
from internals.constants import SMT_NAME, ALL_BLACK_NAME, GRID_GRAPH_NAME, POINT_AD_NAME
import matplotlib
matplotlib.use('Agg')
import seaborn as sns
import networkx as nx
from scipy.cluster.hierarchy import linkage, dendrogram
from sklearn.decomposition import PCA
from sklearn.impute import SimpleImputer
from sklearn.manifold import TSNE


emergent_features = [
    'entropy',
    'pace',
    'fightTime',
    'pursueTime',
    'sightLossRate',
    'targetLossRate',
    'ratio',
    'killDiff',
]
heatmap_features = [
    'maxValuePosition',
    'averageLocalMaximaValuePosition',
    'quantile25Position',
    'quantile50Position',
    'quantile75Position',
    'localMaximaNumberPosition',
    'localMaximaTopDistancePosition',
    'localMaximaAverageDistancePosition',
    'stdLocalMaximaValuePosition',
    'coveragePosition',
    'maxValueKill',
    'averageLocalMaximaValueKill',
    'quantile25Kill',
    'quantile50Kill',
    'quantile75Kill',
    'localMaximaNumberKill',
    'localMaximaTopDistanceKill',
    'localMaximaAverageDistanceKill',
    'stdLocalMaximaValueKill',
    'coverageKill',
    'maxValueDeath',
    'averageLocalMaximaValueDeath',
    'quantile25Death',
    'quantile50Death',
    'quantile75Death',
    'localMaximaNumberDeath',
    'localMaximaTopDistanceDeath',
    'localMaximaAverageDistanceDeath',
    'stdLocalMaximaValueDeath',
    'coverageDeath',
]
traces_features = [
    'maxTraces',
    'averageTraces',
    'quantile25Traces',
    'quantile50Traces',
    'quantile75Traces',
]
topology_features = [
    'area',
    'roomNumber',
    'averageRoomMinDistance',
    'stdRoomMinDistance',
    'averageRoomRadius',
    'stdRoomRadius',
    'averageChokepointRadius',
    'stdChokepointRadius',
    'averageRoomBetweenness',
    'stdRoomBetweenness',
    'averageRoomCloseness',
    'stdRoomCloseness',
    'averageMincut',
    'stdMincut',
    'maxMincut',
    'minMincut',
    #'vertexConnectivity',
    'averageEccentricity',
    'stdEccentricity',
    'diameter',
    'radius',
    'periphery',
    'peripheryPercent',
    'center',
    'centerPercent',
    'density',
    'numberCyclesOneRoom',
    'averageLengthCyclesOneRoom',
    'stdLengthCyclesOneRoom',
    'numberCyclesTwoRooms',
    'averageLengthCyclesTwoRooms',
    'stdLengthCyclesTwoRooms',
]
visibility_features = [
    'maxValueVisibility',
    'maxValuePercentVisibility',
    'averageLocalMaximaValuePercentVisibility',
    'averageValuePercentVisibility',
    'quantile25PercentVisibility',
    'quantile50PercentVisibility',
    'quantile75PercentVisibility',
    'stdValuePercentVisibility',
    'localMaximaNumberVisibility',
    'localMaximaTopDistanceVisibility',
    'localMaximaAverageDistanceVisibility',
    'stdLocalMaximaValuePercentVisibility',
]
symmetry_features = [
    'xSymmetry',
    'ySymmetry',
    'maxSymmetry',
]

aggregate_features = [
    'explorationPlusVisibility',
    'balanceTopology',
    'balanceTopologyPlusPursueTime',
    'peripheryCenterBalance',
]

column_variance = emergent_features + heatmap_features + traces_features
column_covariance = emergent_features + heatmap_features + traces_features + topology_features + visibility_features + symmetry_features + aggregate_features
column_tsne = emergent_features + heatmap_features + traces_features + topology_features + visibility_features + symmetry_features
features_categories = {
    "emergent": emergent_features + heatmap_features + traces_features,
    "topology": topology_features + symmetry_features,
    "visibility": visibility_features,
    "aggregate": aggregate_features,
}

def run_variance_analysis(client: Client, baseoutdir, name, itr, genopme_type, feature_ranges: pd.DataFrame, num_repetitions=60):
    exportdir = Path(os.path.join(GAME_DATA_FOLDER, "Export", name))
    exportdir.mkdir(exist_ok=True)
    importdir = Path(os.path.join(GAME_DATA_FOLDER, "Import", "Genomes", name))
    importdir.mkdir(exist_ok=True)
    outdir = Path(path.join(baseoutdir, name))
    outdir.mkdir(exist_ok=True)
    # Create random Genome
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
    elif genopme_type == POINT_AD_NAME:
        genome = PointAdGenome.create_random_genome()
        phenotype = genome.phenotype()
    
    phenotypes = [phenotype] * num_repetitions
    futures = []
    for idx, phenotype in enumerate(phenotypes):
        futures.append(
            client.submit(
                evaluate,
                phenotype,
                itr,
                idx,
                bot1_data,
                bot2_data,
                1200,
                folder_name=name,
            )
        )
    results = client.gather(futures)
    
    datasets = []
    for idx, (dataset, failed) in enumerate(results):
        if failed:
            print(f"Failed to evaluate phenotype {idx}")
        else:
            datasets.append(dataset)
    df = pd.concat(datasets)
    df.index = range(len(df))
    df.to_json(os.path.join(outdir, f"final_results_{itr}.json"), orient="columns", indent=4)
    min_values = df.min()
    max_values = df.max()

    __save_map(os.path.join(outdir, f"phenotype_{name}.png"), phenotypes[0].map_matrix(inverted=True))
    
    # Normalize all features according to the feature ranges
    for feature in df.columns:
        min_value = feature_ranges.loc["min", feature] if feature_ranges.loc["min", feature] < min_values[feature] else min_values[feature]
        max_value = feature_ranges.loc["max", feature] if feature_ranges.loc["max", feature] > max_values[feature] else max_values[feature]
        df[feature] = (df[feature] - min_value) / (max_value - min_value)


    df.boxplot(column=column_variance, return_type='dict', fontsize=5)
    plt.xticks(rotation=90)
    plt.tight_layout()
    #Save figure
    plt.savefig(os.path.join(outdir, f"boxplot_{name}.png"), format='png', dpi=300)
    plt.clf()
    plt.close()

def run_covariance_analysis(client: Client, baseoutdir, name, itr, genopme_type, feature_ranges: pd.DataFrame, num_phenotypes=30):
    exportdir = Path(os.path.join(GAME_DATA_FOLDER, "Export", name))
    exportdir.mkdir(exist_ok=True)
    importdir = Path(os.path.join(GAME_DATA_FOLDER, "Import", "Genomes", name))
    importdir.mkdir(exist_ok=True)
    outdir = Path(path.join(baseoutdir, name))
    outdir.mkdir(exist_ok=True)
    # Create random Genome
    phenotypes = []
    for i in range(num_phenotypes):
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
        elif genopme_type == POINT_AD_NAME:
            genome = PointAdGenome.create_random_genome()
            phenotype = genome.phenotype()
        phenotypes.append(phenotype)
    
    futures = []
    for idx, phenotype in enumerate(phenotypes):
        futures.append(
            client.submit(
                evaluate,
                phenotype,
                itr,
                idx,
                bot1_data,
                bot2_data,
                1200,
                folder_name=name,
            )
        )
    results = client.gather(futures)
    
    datasets = []
    for idx, (dataset, failed) in enumerate(results):
        if failed:
            print(f"Failed to evaluate phenotype {idx}")
        else:
            datasets.append(dataset)
    df = pd.concat(datasets)
    df.index = range(len(df))
    df.to_json(os.path.join(outdir, f"final_results_{itr}.json"), orient="columns", indent=4)
    min_values = df.min()
    max_values = df.max()

    mapsdir = Path(os.path.join(outdir, "maps"))
    mapsdir.mkdir(exist_ok=True)
    for idx, phenotype in enumerate(phenotypes):
        __save_map(os.path.join(mapsdir, f"phenotype_{name}_{idx}.png"), phenotypes[0].map_matrix())
    
    # Normalize all features according to the feature ranges
    for feature in df.columns:
        min_value = feature_ranges.loc["min", feature] if feature_ranges.loc["min", feature] < min_values[feature] else min_values[feature]
        max_value = feature_ranges.loc["max", feature] if feature_ranges.loc["max", feature] > max_values[feature] else max_values[feature]
        df[feature] = (df[feature] - min_value) / (max_value - min_value)

    # Whole covariance matrix
    df = df[column_covariance]
    var_corr = df.corr()
    fig, ax = plt.subplots(figsize=(24, 16))
    heatmap_ax = sns.heatmap(var_corr, xticklabels=var_corr.columns, yticklabels=var_corr.index, annot=False, annot_kws={"fontsize":6})
    heatmap_ax.tick_params(axis='both', which='major', labelsize=10)
    plt.xticks(rotation=90)
    plt.tight_layout()
    plt.savefig(os.path.join(outdir, f"covariance_map_{name}.png"), format='png', dpi=300) 
    plt.clf()
    plt.close()
    
    items_list = list(features_categories.items())
    for i in range(len(items_list)):
        for j in range(i, len(items_list)):
            features_i = items_list[i][1]
            features_j = items_list[j][1]
            var_corr_partial = var_corr.loc[features_i, features_j]
            fig, ax = plt.subplots(figsize=(24, 16))
            heatmap_ax = sns.heatmap(var_corr_partial, vmin=-1, vmax=1, xticklabels=var_corr_partial.columns, yticklabels=var_corr_partial.index, annot=False, annot_kws={"fontsize":6})
            heatmap_ax.tick_params(axis='both', which='major', labelsize=15)
            plt.xticks(rotation=90)
            plt.tight_layout()
            plt.savefig(os.path.join(outdir, f"covariance_map_{name}_{items_list[i][0]}_{items_list[j][0]}.png"), format='png', dpi=300)
            plt.clf()
            plt.close()

    print("Covariance analysis done for ", name)

def get_feature_ranges(experiment_names, baseoutdir, to_compute=True):
    outdir = Path(path.join(baseoutdir, "feature_ranges"))
    outdir.mkdir(exist_ok=True)
    if to_compute:
        datasets = []
        for experiment_name in experiment_names:
            tqdm.tqdm.write(f"Processing {experiment_name}")
            exportdir = Path(os.path.join(GAME_DATA_FOLDER, "Export", experiment_name))
            exportdir.mkdir(exist_ok=True)
            for iteration in tqdm.trange(0, 400):
                for individual_number in range(0, 10):
                    name = f"final_results_{iteration}_{individual_number}"
                    try:
                        dataset = pd.read_json(os.path.join(exportdir, name + ".json"))
                        datasets.append(dataset)
                    except:
                        pass
        df = pd.concat(datasets)

        df_min = pd.DataFrame([df.min()], columns=df.columns)
        df_max = pd.DataFrame([df.max()], columns=df.columns)
        # Substitute features whose theoretical max and min are known
        #df_min["timeInFight1"] = 0
        #df_max["timeInFight1"] = 1200
        #df_min["timeInFight2"] = 0
        #df_max["timeInFight2"] = 1200
        #df_min["timeToEngage1"] = 0
        #df_max["timeToEngage1"] = 1200
        #df_min["timeToEngage2"] = 0
        #df_max["timeToEngage2"] = 1200
        #df_min["timeBetweenSights1"] = 0
        #df_max["timeBetweenSights1"] = 1200
        #df_min["timeBetweenSights2"] = 0
        #df_max["timeBetweenSights2"] = 1200
        #df_min["timeToSurrender1"] = 0
        #df_max["timeToSurrender1"] = 1200
        #df_min["timeToSurrender2"] = 0
        #df_max["timeToSurrender2"] = 1200
        #df_min["accuracy"] = 0
        #df_max["accuracy"] = 1
        #df_min["entropy"] = 0
        #df_max["entropy"] = 1
        #df_min["pace"] = 0
        #df_max["pace"] = 1
        #df_min["fightTime"] = 0
        #df_max["fightTime"] = 1
        #df_min["pursueTime"] = 0
        #df_max["pursueTime"] = 1
        #df_min["sightLossRate"] = 0
        #df_max["sightLossRate"] = 1
        #df_min["targetLossRate"] = 0
        #df_max["targetLossRate"] = 1
        #df_min["coveragePositionBot1"] = 0
        #df_max["coveragePositionBot1"] = 1
        #df_min["coveragePositionBot2"] = 0
        #df_max["coveragePositionBot2"] = 1
        #df_min["coveragePosition"] = 0
        #df_max["coveragePosition"] = 1
        #df_min["coverageKillBot1"] = 0
        #df_max["coverageKillBot1"] = 1
        #df_min["coverageKillBot2"] = 0
        #df_max["coverageKillBot2"] = 1
        #df_min["coverageKill"] = 0
        #df_max["coverageKill"] = 1
        #df_min["coverageDeathBot1"] = 0
        #df_max["coverageDeathBot1"] = 1
        #df_min["coverageDeathBot2"] = 0
        #df_max["coverageDeathBot2"] = 1
        #df_min["coverageDeath"] = 0
        #df_max["coverageDeath"] = 1

        merged_df = pd.concat([df_min, df_max], ignore_index=True)
        #Rename columns
        merged_df.index = ["min", "max"]
        merged_df.to_json(os.path.join(outdir, "feature_ranges.json"), orient="columns", indent=4)

        return merged_df
    else:
        return pd.read_json(os.path.join(outdir, "feature_ranges.json"))


def tsne_analysis(client: Client, baseoutdir, final_result_name, experiment_names, num_iterations=400):
    exportdir = Path(os.path.join(GAME_DATA_FOLDER, "Export", final_result_name))
    exportdir.mkdir(exist_ok=True)
    outdir = Path(path.join(baseoutdir, final_result_name))
    outdir.mkdir(exist_ok=True)

    datasets = []
    phenotypes = []
    for experiment_name in experiment_names:
        tqdm.tqdm.write(f"Processing {experiment_name}")
        exportdir = Path(os.path.join(GAME_DATA_FOLDER, "Export", experiment_name))
        exportdir.mkdir(exist_ok=True)
        for iteration in tqdm.trange(0, num_iterations):
            for individual_number in range(0, 10):
                final_result_name = f"final_results_{iteration}_{individual_number}"
                phenotype_name = f"phenotype_{iteration}_{individual_number}"
                try:
                    dataset = pd.read_json(os.path.join(exportdir, final_result_name + ".json"))
                    datasets.append(dataset)
                    phenotype_file = open(os.path.join(exportdir, phenotype_name + '.pkl'), 'rb')
                    phenotype = pickle.load(phenotype_file)
                    phenotype_file.close()
                    phenotypes.append(phenotype)
                except Exception as e:
                    #print(e)
                    pass
    df = pd.concat(datasets)
    df = df[column_tsne]
    # Substitute NaN values with the mean of the column
    imputer = SimpleImputer(strategy='mean')
    df = pd.DataFrame(imputer.fit_transform(df), columns=df.columns)
    phenotypes = [phenotype.map_matrix().flatten() for phenotype in phenotypes]
    df_phenotypes = pd.DataFrame(phenotypes)

    tsne = TSNE(n_components=2, random_state=42, max_iter=2000)
    points_feat = tsne.fit_transform(df)
    # get minimum X and Y values and max
    min_x = np.min(points_feat[:, 0])
    min_y = np.min(points_feat[:, 1])
    max_x = np.max(points_feat[:, 0])
    max_y = np.max(points_feat[:, 1])
    # Build 2D color map
    resolution = 500
    x = np.linspace(0, 1, resolution)
    y = np.linspace(0, 1, resolution)
    X, Y = np.meshgrid(x, y)

    color_1 = np.array([1.0, 0.0, 0.0])  # Red
    color_2 = np.array([0.0, 1.0, 0.0])  # Green
    color_3 = np.array([0.0, 0.0, 1.0])  # Blue
    color_4 = np.array([1.0, 1.0, 0.0])  # Yellow

    color = np.zeros((resolution, resolution, 3))
    color += (1 - X)[:, :, None] * (1 - Y)[:, :, None] * color_1[None, None, :]
    color += X[:, :, None] * (1 - Y)[:, :, None] * color_2[None, None, :]
    color += (1 - X)[:, :, None] * Y[:, :, None] * color_3[None, None, :]
    color += X[:, :, None] * Y[:, :, None] * color_4[None, None, :]

    # Assign to each point a color based on the 2D color map
    colors = np.zeros((len(points_feat), 3))
    for i, point in enumerate(points_feat):
        x = int((point[0] - min_x) / (max_x - min_x) * resolution - 1)
        y = int((point[1] - min_y) / (max_y - min_y) * resolution - 1)
        colors[i] = color[x, y]

    plt.scatter(points_feat[:, 0], points_feat[:, 1], s=1, c=colors)
    plt.savefig(os.path.join(outdir, f"tsnefeatures_{name}_.png"), format='png', dpi=300)
    plt.clf()
    plt.close()
    
    tsne = TSNE(n_components=2, random_state=42, max_iter=2000)
    points_img = tsne.fit_transform(df_phenotypes)
    
    #Scatter point by giving them a color corresponding to the 2D color map based on points feat positions
    plt.scatter(points_img[:, 0], points_img[:, 1], s=1, c=colors)
    plt.savefig(os.path.join(outdir, f"tsneimg_{name}.png"), format='png', dpi=300)
    plt.clf()
    plt.close()



if __name__ == "__main__":
    baseoutdir = Path(path.join(NOISE_ANALYSIS_OUTPUT_FOLDER, 'noise'))
    baseoutdir.mkdir(exist_ok=True)

    experiment_names = [
        "Bari_SMT_SMTEmitter_SB_entropy_balanceTopology_pursueTime_I400_B1_E10",
        "BariInverse_AB_ABEmitter_SB_entropy_balanceTopology_pursueTime_I400_B1_E10",
        "BariInverse_PointAD_PointADEmitter_SB_entropy_balanceTopology_pursueTime_I400_B1_E10",
        "Bari_GG_GGEmitter_SB_entropy_balanceTopology_pursueTime_I400_B1_E10"
    ]
    COMPUTE_FEATURE_RANGES = False
    feature_ranges = get_feature_ranges(experiment_names, baseoutdir, to_compute=COMPUTE_FEATURE_RANGES)

    bot1_data = {"file": 'sniper', "skill": '0.15'}
    bot2_data = {"file": 'shotgun', "skill": '0.85'}

    cluster = LocalCluster(
        processes=True,  # Each worker is a process.
        n_workers=10,  # Create this many worker processes.
        threads_per_worker=1,  # Each worker process is single-threaded.
    )
    client = Client(cluster)

    VARIANCE_ANALYSIS = False

    if VARIANCE_ANALYSIS:
        for i in tqdm.trange(0, 5):
            name = f'var_ab_{i}'
            run_variance_analysis(client, baseoutdir, name, i, ALL_BLACK_NAME, feature_ranges)
        for i in tqdm.trange(0, 5):
            name = f'var_grid_{i}'
            run_variance_analysis(client, baseoutdir, name, i, GRID_GRAPH_NAME, feature_ranges)
        for i in tqdm.trange(0, 5):
            name = f'var_smt_{i}'
            run_variance_analysis(client, baseoutdir, name, i, SMT_NAME, feature_ranges)
        for i in tqdm.trange(0, 5):
            name = f'var_pointad_{i}'
            run_variance_analysis(client, baseoutdir, name, i, POINT_AD_NAME, feature_ranges)

    COVARIANCE_ANALYSIS = False
    if COVARIANCE_ANALYSIS:
        name = f'cov_ab'
        run_covariance_analysis(client, baseoutdir, name, 0, ALL_BLACK_NAME, feature_ranges)
        name = f'cov_grid'
        run_covariance_analysis(client, baseoutdir, name, 0, GRID_GRAPH_NAME, feature_ranges)
        name = f'cov_smt'
        run_covariance_analysis(client, baseoutdir, name, 0, SMT_NAME, feature_ranges)
        name = f'cov_pointad'
        run_covariance_analysis(client, baseoutdir, name, 0, POINT_AD_NAME, feature_ranges)

        bot1_data = {"file": "sniper", "skill": "0.15"}
        bot2_data = {"file": "shotgun", "skill": "0.85"}
        name = f'cov_ab_weak'
        run_covariance_analysis(client, baseoutdir, name, 0, ALL_BLACK_NAME, feature_ranges)

    TSNE_ANALYSIS = True
    if TSNE_ANALYSIS:
        experiment_names = [
            "Bari_AB_ABEmitter_SB_entropy_balanceTopology_pursueTime_I400_B1_E10",
        ]        
        name = f'tsne_ab'
        tsne_analysis(client, baseoutdir, name, experiment_names) 

        experiment_names = [
            "BariInverse_PointAD_PointADEmitter_SB_entropy_balanceTopology_pursueTime_I400_B1_E10",
        ]
        name = f'tsne_pointad'
        tsne_analysis(client, baseoutdir, name, experiment_names)

        experiment_names = [
            "Bari_GG_GGEmitter_SB_entropy_balanceTopology_pursueTime_I400_B1_E10"
        ]
        name = f'tsne_gg'
        tsne_analysis(client, baseoutdir, name, experiment_names)

        experiment_names = [
            "Bari_SMT_SMTEmitter_SB_entropy_balanceTopology_pursueTime_I400_B1_E10"
        ]
        name = f'tsne_smt'
        tsne_analysis(client, baseoutdir, name, experiment_names)
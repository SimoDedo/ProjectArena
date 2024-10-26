import pickle
import PIL
import PIL.Image
from matplotlib.image import BboxImage
from matplotlib.offsetbox import AnnotationBbox, OffsetImage
from matplotlib.transforms import Bbox, TransformedBbox
from z3 import *
from internals.ab_genome.ab_genome import ABGenome
from internals.graph import to_rooms_only_graph
from internals.graph_genome.gg_genome import GraphGenome
from internals.phenotype import Phenotype
from internals.smt_genome.smt_genome import SMTGenome
from internals.point_ad_genome.point_ad_genome import PointAdGenome
import matplotlib.pyplot as plt
import numpy as np
import igraph as ig
from matplotlib import cm
from os import path
from internals.constants import ANALYSIS_OUTPUT_FOLDER, GAME_DATA_FOLDER, MAP_ELITES_OUTPUT_FOLDER
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
from adjustText import adjust_text
import networkx as nx
from karateclub import Graph2Vec, GL2Vec
matplotlib.use('Qt5Agg')

emergent_features = [
    'entropy',
    'ratio',
    'pace',
    'fightTime',
    'pursueTime',
    'sightLossRate',
    'targetLossRate',
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
    #'averageChokepointRadius',
    #'stdChokepointRadius',
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
    'center',
    'peripheryPercent',
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
    #'balanceTopologyPlusPursueTime',
    #'peripheryCenterBalance',
]

emergent_features_reduced = [
    'entropy',
    #'ratio',
    'pace',
    #'fightTime',
    #'pursueTime',
    'sightLossRate',
    'targetLossRate',
    'killDiff',
]
heatmap_features_reduced = [
    'maxValuePosition',
    #'averageLocalMaximaValuePosition',
    #'quantile25Position',
    #'quantile50Position',
    #'quantile75Position',
    'localMaximaNumberPosition',
    #'localMaximaTopDistancePosition',
    #'localMaximaAverageDistancePosition',
    #'stdLocalMaximaValuePosition',
    'coveragePosition',
    'maxValueKill',
    #'averageLocalMaximaValueKill',
    #'quantile25Kill',
    #'quantile50Kill',
    #'quantile75Kill',
    'localMaximaNumberKill',
    #'localMaximaTopDistanceKill',
    #'localMaximaAverageDistanceKill',
    #'stdLocalMaximaValueKill',
    'coverageKill',
    'maxValueDeath',
    #'averageLocalMaximaValueDeath',
    #'quantile25Death',
    #'quantile50Death',
    #'quantile75Death',
    'localMaximaNumberDeath',
    #'localMaximaTopDistanceDeath',
    #'localMaximaAverageDistanceDeath',
    #'stdLocalMaximaValueDeath',
    'coverageDeath',
]
traces_features_reduced = [
    'maxTraces',
    #'averageTraces',
    #'quantile25Traces',
    #'quantile50Traces',
    #'quantile75Traces',
]
topology_features_reduced = [
    'area',
    #'roomNumber',
    #'averageRoomMinDistance',
    #'stdRoomMinDistance',
    'averageRoomRadius',
    #'stdRoomRadius',
    #'averageChokepointRadius',
    #'stdChokepointRadius',
    'averageRoomBetweenness',
    #'stdRoomBetweenness',
    'averageRoomCloseness',
    #'stdRoomCloseness',
    'averageMincut',
    #'stdMincut',
    #'maxMincut',
    #'minMincut',
    #'vertexConnectivity',
    'averageEccentricity',
    #'stdEccentricity',
    #'diameter',
    #'radius',
    'periphery',
    #'center',
    'peripheryPercent',
    #'centerPercent',
    'density',
    'numberCyclesOneRoom',
    #'averageLengthCyclesOneRoom',
    #'stdLengthCyclesOneRoom',
    #'numberCyclesTwoRooms',
    #'averageLengthCyclesTwoRooms',
    #'stdLengthCyclesTwoRooms',
]
visibility_features_reduced = [
    'maxValueVisibility',
    'maxValuePercentVisibility',
    'averageLocalMaximaValuePercentVisibility',
    #'averageValuePercentVisibility',
    #'quantile25PercentVisibility',
    #'quantile50PercentVisibility',
    #'quantile75PercentVisibility',
    'stdValuePercentVisibility',
    'localMaximaNumberVisibility',
    #'localMaximaTopDistanceVisibility',
    #'localMaximaAverageDistanceVisibility',
    'stdLocalMaximaValuePercentVisibility',
]
symmetry_features_reduced = [
    #'xSymmetry',
    #'ySymmetry',
    'maxSymmetry',
]
aggregate_features_reduced = [
    'explorationPlusVisibility',
    'balanceTopology',
    #'balanceTopologyPlusPursueTime',
    #'peripheryCenterBalance',
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
column_covariance_reduced = emergent_features_reduced + heatmap_features_reduced + traces_features_reduced + topology_features_reduced + visibility_features_reduced + symmetry_features_reduced + aggregate_features_reduced
column_tsne_reduced = emergent_features_reduced + heatmap_features_reduced + traces_features_reduced + topology_features_reduced + visibility_features_reduced + symmetry_features_reduced
features_categories_reduced = {
    "emergent": emergent_features_reduced + heatmap_features_reduced + traces_features_reduced,
    "topology": topology_features_reduced + symmetry_features_reduced,
    "visibility": visibility_features_reduced,
    "aggregate": aggregate_features_reduced,
}

features_final = [
    "coveragePosition",
    "stdValuePercentVisibility",
    "density",
    #"averageRoomCloseness",
    "maxValuePosition",
    #"maxValueKill",
    #"maxValueDeath",
    "peripheryPercent",
    #averageLocalMaximaValuePercentVisibility
    "maxSymmetry",
    "pace",
    "maxValuePercentVisibility",
    #"coverageKill",
    #"coverageDeath",
    #"peripheryCenterBalance",
    "numberCyclesOneRoom",
    "localMaximaNumberVisibility",
    #"targetLossRate",
    #"averageRoomBetweenness",
    #"localMaximaNumberPosition",
    #"localMaximaNumberKill",
    #"localMaximaNumberDeath",
    "area",
    #"averageEccentricity",
    #"killDiff",
    "sightLossRate",
    "averageMincut",
    #explorationPlusVisibility,
    "maxTraces",
    #maxValueVisibility,
    "stdLocalMaximaValuePercentVisibility",
    "averageRoomRadius",
    "periphery",
    "entropy",
    "balanceTopology",
    #"balanceTopologyPlusPursueTime",
]

features_final = [
    "maxSymmetry",

    "maxValuePosition",
    #"coverageKill",
    #"coverageDeath",
    #"maxValueKill",
    #"maxValueDeath",

    "coveragePosition",

    "peripheryPercent",

    "pace",
    #"maxValuePercentVisibility", #
    
    "stdValuePercentVisibility",
    
    "averageLocalMaximaValuePercentVisibility", #

    "density",
    #"averageRoomCloseness",

    "averageEccentricity", #
    
    "area",
    #"killDiff",
    #"targetLossRate",
    #"localMaximaNumberPosition",
    #"localMaximaNumberKill",
    #"localMaximaNumberDeath",

    "averageRoomBetweenness", #
    
    "localMaximaNumberVisibility",

    "balanceTopology",
    #"balanceTopologyPlusPursueTime",
    
    "entropy",
    "averageRoomRadius",
    "stdLocalMaximaValuePercentVisibility",
    "sightLossRate",
    "periphery",
    
    "maxTraces",
    "maxValueVisibility", #

    "numberCyclesOneRoom",
    "averageMincut",
    #explorationPlusVisibility,
    
]

def plot_graph_vornoi(phenotype: Phenotype, rooms_only=False, show=True):
    graph, outer_shell, obstacles = phenotype.to_topology_graph_vornoi()
    if rooms_only:
        graph = to_rooms_only_graph(graph)
    
    fig, ax = plt.subplots(frameon=False)
    map_matrix = phenotype.map_matrix(inverted=True)

    # Plot the outer wall
    x,y = outer_shell.exterior.xy
    plt.plot(x,y)
    plt.fill([0, 0, map_matrix.shape[1], map_matrix.shape[1]], 
             [0, map_matrix.shape[0], map_matrix.shape[0], 0], 
             color='lightgray', zorder=-10)
    plt.fill(x, y, color='white', zorder=-10)


    # Plot the obstacles
    for obstacle in obstacles:
        x,y = obstacle.exterior.xy
        plt.plot(x,y, color='darkred', zorder=-10)
        plt.fill(x, y, color='darkred', alpha=0.5, zorder=-10)

    # Plot the graph
    #graph.vs['label'] = [str(i) for i in range(len(graph.vs))]
    vertex_attr_names = graph.vertex_attributes()
    color = []
    for i in range(len(graph.vs)):
        if 'chokepoint' in vertex_attr_names and graph.vs[i]['chokepoint']:
            color.append('yellow')
        elif 'dead_end' in vertex_attr_names and graph.vs[i]['dead_end']:
            color.append('green')
        elif graph.vs[i]['region']:
            color.append('red')
        else:
            color.append('blue')
    graph.vs['color'] = color

    layout = ig.Layout(coords=graph.vs['coords'], dim=2)
    ig.plot(graph, vertex_size=5, layout=layout, vertex_label_color="black",  target=ax)
    plt.gca().invert_yaxis()

    ax.set_xlim(0, phenotype.mapWidth)
    ax.set_ylim(0, phenotype.mapHeight)
    plt.gca().invert_yaxis()
    #Remove white space 
    plt.gca().set_aspect('equal', adjustable='box')
    plt.axis('off')

    if show:
        plt.show()
    
    return fig, ax

def run_variance_analysis(
        client: Client, 
        baseoutdir, 
        name, 
        itr, 
        genopme_type, 
        feature_ranges: pd.DataFrame, 
        num_repetitions=100, 
        num_parallel_simulations=1, 
        phenotype_path=None,
        use_cache=False
        ):
    exportdir = Path(os.path.join(GAME_DATA_FOLDER, "Export", name))
    exportdir.mkdir(exist_ok=True)
    importdir = Path(os.path.join(GAME_DATA_FOLDER, "Import", "Genomes", name))
    importdir.mkdir(exist_ok=True)
    outdir = Path(path.join(baseoutdir, name))
    outdir.mkdir(exist_ok=True)
    if not use_cache:
        if phenotype_path is not None:
            with open(phenotype_path, "rb") as f:
                phenotype = pickle.load(f)
        else:
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

        # Save the phenotype object
        with open(os.path.join(outdir, f"phenotype_{name}.pkl"), "wb") as f:
            pickle.dump(phenotype, f)
        __save_map(os.path.join(outdir, f"phenotype_{name}.png"), phenotype.map_matrix(inverted=True))


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
                    num_parallel_simulations=num_parallel_simulations
                )
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
    else:
        datasets = []
        for num in tqdm.trange(0, num_repetitions):
            final_result_name = f"final_results_{itr}_{num}"
            try:
                dataset = pd.read_json(os.path.join(exportdir, final_result_name + ".json"))
                mean = dataset.mean()
                mean_dataset = pd.DataFrame([mean], columns=dataset.columns)
                datasets.append(mean_dataset)
            except Exception as e:
                #print(e)
                continue
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


    df.boxplot(column=column_variance, return_type='dict', fontsize=7.5, flierprops=dict(marker='o', markersize=4))
    plt.ylim(0, 1)  # Set the y-axis limits
    plt.gcf().set_size_inches(9, 5)  # Adjust the figure size to make columns larger
    plt.xticks(rotation=45, ha='right', fontsize=8, rotation_mode="anchor")
    plt.yticks(fontsize=8)
    plt.tight_layout()
    #Save figure
    plt.savefig(os.path.join(outdir, f"boxplot_{name}_numsim_{num_parallel_simulations}.png"), format='png', dpi=300)
    plt.clf()
    plt.close()

def run_covariance_analysis(
        client: Client, 
        baseoutdir, 
        name, 
        itr, 
        genopme_type, 
        feature_ranges: pd.DataFrame, 
        num_phenotypes=100, 
        num_parallel_simulations=1,
        use_cache=False
        ):
    exportdir = Path(os.path.join(GAME_DATA_FOLDER, "Export", name))
    exportdir.mkdir(exist_ok=True)
    importdir = Path(os.path.join(GAME_DATA_FOLDER, "Import", "Genomes", name))
    importdir.mkdir(exist_ok=True)
    outdir = Path(path.join(baseoutdir, name))
    outdir.mkdir(exist_ok=True)
    # Create random Genome
    if not use_cache:
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
                    600,
                    folder_name=name,
                    num_parallel_simulations=num_parallel_simulations
                )
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
        
        mapsdir = Path(os.path.join(outdir, "maps"))
        mapsdir.mkdir(exist_ok=True)
        for idx, phenotype in enumerate(phenotypes):
            __save_map(os.path.join(mapsdir, f"phenotype_{name}_{idx}.png"), phenotypes[0].map_matrix())
    else:
        datasets = []
        for num in tqdm.trange(0, num_phenotypes):
            final_result_name = f"final_results_{itr}_{num}"
            try:
                dataset = pd.read_json(os.path.join(exportdir, final_result_name + ".json"))
                mean = dataset.mean()
                mean_dataset = pd.DataFrame([mean], columns=dataset.columns)
                datasets.append(mean_dataset)
            except Exception as e:
                #print(e)
                continue
    df = pd.concat(datasets)
    imputer = SimpleImputer(strategy='mean')
    df = pd.DataFrame(imputer.fit_transform(df), columns=df.columns)
    df.index = range(len(df))
    df.to_json(os.path.join(outdir, f"final_results_{itr}.json"), orient="columns", indent=4)
    min_values = df.min()
    max_values = df.max()


    
    # Normalize all features according to the feature ranges
    for feature in df.columns:
        min_value = feature_ranges.loc["min", feature] if feature_ranges.loc["min", feature] < min_values[feature] else min_values[feature]
        max_value = feature_ranges.loc["max", feature] if feature_ranges.loc["max", feature] > max_values[feature] else max_values[feature]
        df[feature] = (df[feature] - min_value) / (max_value - min_value)

    # Whole covariance matrix
    df = df[column_covariance]
    var_corr = df.corr()
    # Drop all rows and columns with NaN values
    var_corr = var_corr.dropna(axis=0, how='any')
    var_corr = var_corr.dropna(axis=1, how='any')
    fig, ax = plt.subplots(figsize=(24, 16))
    heatmap_ax = sns.heatmap(var_corr, xticklabels=var_corr.columns, yticklabels=var_corr.index, annot=False, annot_kws={"fontsize":6})
    heatmap_ax.tick_params(axis='both', which='major', labelsize=10)
    plt.xticks(rotation=90)
    plt.tight_layout()
    plt.savefig(os.path.join(outdir, f"covariance_map_{name}.png"), format='png', dpi=300) 
    plt.clf()
    plt.close()

    # Whole covariance clustermap
    clustermap_ax = sns.clustermap(var_corr, xticklabels=var_corr.columns, yticklabels=var_corr.index, annot=False, annot_kws={"fontsize":3})
    plt.setp(clustermap_ax.ax_heatmap.get_yticklabels(), rotation=0, fontsize=5)
    plt.setp(clustermap_ax.ax_heatmap.get_xticklabels(), rotation=45, ha='right', fontsize=5, rotation_mode="anchor")
    plt.savefig(os.path.join(outdir, f"covariance_clustermap_{name}.png"), format='png', dpi=300)
    plt.clf()
    plt.close()

    # Reduced covariance matrix
    df_reduced = df[column_covariance_reduced]
    var_corr_reduced = df_reduced.corr()
    # Drop all rows and columns with NaN values
    var_corr_reduced = var_corr_reduced.dropna(axis=0, how='any')
    var_corr_reduced = var_corr_reduced.dropna(axis=1, how='any')
    fig, ax = plt.subplots(figsize=(24, 16))
    heatmap_ax = sns.heatmap(var_corr_reduced, xticklabels=var_corr_reduced.columns, yticklabels=var_corr_reduced.index, annot=True, annot_kws={"fontsize":6})
    heatmap_ax.tick_params(axis='both', which='major', labelsize=10)
    plt.tight_layout()
    plt.savefig(os.path.join(outdir, f"covariance_map_{name}_reduced.png"), format='png', dpi=300)
    plt.clf()
    plt.close()

    # Reduced covariance clustermap
    clustermap_ax = sns.clustermap(var_corr_reduced, xticklabels=var_corr_reduced.columns, yticklabels=var_corr_reduced.index, annot=True, annot_kws={"fontsize":4})
    plt.setp(clustermap_ax.ax_heatmap.get_yticklabels(), rotation=0)
    plt.setp(clustermap_ax.ax_heatmap.get_xticklabels(), rotation=45, ha='right', fontsize=8, rotation_mode="anchor")
    plt.savefig(os.path.join(outdir, f"covariance_clustermap_{name}_reduced.png"), format='png', dpi=300)
    plt.clf()
    plt.close()

    # Final covariance matrix
    df_final = df[features_final]
    var_corr_final = df_final.corr()
    # Drop all rows and columns with NaN values
    var_corr_final = var_corr_final.dropna(axis=0, how='any')
    var_corr_final = var_corr_final.dropna(axis=1, how='any')
    fig, ax = plt.subplots(figsize=(24, 16))
    heatmap_ax = sns.heatmap(var_corr_final, xticklabels=var_corr_final.columns, yticklabels=var_corr_final.index, annot=True, annot_kws={"fontsize":6})
    heatmap_ax.tick_params(axis='both', which='major', labelsize=10)
    plt.tight_layout()
    plt.savefig(os.path.join(outdir, f"covariance_map_{name}_final.png"), format='png', dpi=300)
    plt.clf()
    plt.close()
    
    # Final covariance clustermap
    clustermap_ax = sns.clustermap(var_corr_final, xticklabels=var_corr_final.columns, yticklabels=var_corr_final.index, annot=True, annot_kws={"fontsize":4})
    plt.setp(clustermap_ax.ax_heatmap.get_yticklabels(), rotation=0)
    plt.setp(clustermap_ax.ax_heatmap.get_xticklabels(), rotation=45, ha='right', fontsize=8, rotation_mode="anchor")
    plt.savefig(os.path.join(outdir, f"covariance_clustermap_{name}_final.png"), format='png', dpi=300)
    plt.clf()
    plt.close()

    # Transform it in a links data frame (3 columns only):
    links = var_corr_reduced.stack().reset_index()
    links.columns = ['var1', 'var2', 'value']
    # Keep only correlation over a threshold and remove self correlation (cor(A,A)=1)
    links_filtered=links.loc[ (abs(links['value']) >= 0.8) & (links['var1'] != links['var2']) ]
    # Build your graph
    G=nx.from_pandas_edgelist(links_filtered, 'var1', 'var2', edge_attr='value')
    # Plot the network:
    edges = G.edges(data=True)
    edge_colors = [edge[2]['value'] for edge in edges]
    # Avoid labels overlapping by using a different method
    pos = nx.spring_layout(G, weight='value', seed=42)
    nx.draw_networkx_nodes(G, pos, node_color='orange', node_size=2)
    edges = nx.draw_networkx_edges(G, pos, edge_color=edge_colors, edge_cmap=plt.cm.viridis, edge_vmin=-1.0, edge_vmax=1.0, width=0.4)
    
    # Use a different method to avoid label overlapping
    labels = {node: node for node in G.nodes()}
    texts = [plt.text(pos[node][0], pos[node][1], labels[node], fontsize=6, bbox=dict(facecolor='white', edgecolor='none', boxstyle='round,pad=0.1', alpha=0.5)) for node in G.nodes()]
    plt.colorbar(edges, ax=plt.gca(), orientation='vertical', label='Correlation Value')
    adjust_text(texts, force_pull=0.2)
    #Save figure
    plt.savefig(os.path.join(outdir, f"covariance_network_{name}.png"), format='png', dpi=300)
    plt.clf()
    plt.close()

    # Covariance maps for each category
    #items_list = list(features_categories.items())
    #for i in range(len(items_list)):
    #    for j in range(i, len(items_list)):
    #        features_i = items_list[i][1]
    #        features_j = items_list[j][1]
    #        var_corr_partial = var_corr.loc[features_i, features_j]
    #        fig, ax = plt.subplots(figsize=(24, 16))
    #        heatmap_ax = sns.heatmap(var_corr_partial, vmin=-1, vmax=1, xticklabels=var_corr_partial.columns, yticklabels=var_corr_partial.index, annot=False, annot_kws={"fontsize":6})
    #        heatmap_ax.tick_params(axis='both', which='major', labelsize=15)
    #        plt.setp(clustermap_ax.ax_heatmap.get_yticklabels(), rotation=0)
    #        plt.setp(clustermap_ax.ax_heatmap.get_xticklabels(), rotation=45, ha='right', rotation_mode="anchor")
    #        plt.tight_layout()
    #        plt.savefig(os.path.join(outdir, f"covariance_map_{name}_{items_list[i][0]}_{items_list[j][0]}.png"), format='png', dpi=300)
    #        plt.clf()
    #        plt.close()

    print("Covariance analysis done for ", name)
    return var_corr, var_corr_reduced, var_corr_final

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

def get_color_map(resolution = 500):
    # Build 2D color map
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
    return color

def get_graph(phenotype: Phenotype, rooms_only=False):
    vornoi_graph, _, _ = phenotype.to_topology_graph_vornoi()
    if rooms_only:
        vornoi_graph = to_rooms_only_graph(vornoi_graph)
    graph = nx.Graph()
    for node in vornoi_graph.vs:
        graph.add_node(node.index, feature=node['coords'])
    for edge in vornoi_graph.es:
        graph.add_edge(edge.source, edge.target, feature=edge['weight'])
    
    return graph

def tsne_analysis(
        client: Client, 
        baseoutdir, 
        final_result_name, 
        experiment_names, 
        num_experiment_iterations=400, 
        tsne_iterations=2000, 
        perplexities=[30],
        visualize_tsne_img=False,
        visualize_tsne_graph=False,
        use_stored_graphs=True,
        rooms_only=False
        ):
    exportdir = Path(os.path.join(GAME_DATA_FOLDER, "Export", final_result_name))
    exportdir.mkdir(exist_ok=True)
    outdir = Path(path.join(baseoutdir, final_result_name))
    outdir.mkdir(exist_ok=True)
    outdirsub = Path(path.join(outdir, "specific_features"))
    outdirsub.mkdir(exist_ok=True)

    # Load all datasets
    datasets = []
    phenotypes = []
    graphs = []
    graph_images = []
    for experiment_name in experiment_names:
        tqdm.tqdm.write(f"Processing {experiment_name}")
        exportdir = Path(os.path.join(GAME_DATA_FOLDER, "Export", experiment_name))
        exportdir.mkdir(exist_ok=True)
        for iteration in tqdm.trange(0, num_experiment_iterations):
            for individual_number in range(0, 10):
                final_result_name = f"final_results_{iteration}_{individual_number}"
                phenotype_name = f"phenotype_{iteration}_{individual_number}"
                graph_name = f"graph_{iteration}_{individual_number}"
                try:
                    dataset = pd.read_json(os.path.join(exportdir, final_result_name + ".json"))
                    mean = dataset.mean()
                    mean_dataset = pd.DataFrame([mean], columns=dataset.columns)
                    phenotype_file = open(os.path.join(exportdir, phenotype_name + '.pkl'), 'rb')
                    phenotype = pickle.load(phenotype_file)
                    phenotype_file.close()
                    datasets.append(mean_dataset)
                    phenotypes.append(phenotype)
                except Exception as e:
                    #print(e)
                    continue
                try:
                    if not use_stored_graphs:
                        raise Exception("Force recomputation")
                    graph_file = open(os.path.join(exportdir, graph_name + '.pkl'), 'rb')
                    graph = pickle.load(graph_file)
                    graph_file.close()
                    graphs.append(graph)
                except Exception as e:
                    graph = get_graph(phenotype, rooms_only)
                    graphs.append(graph)
                    # Save pickle
                    with open(os.path.join(exportdir, graph_name + '.pkl'), 'wb') as f:
                        pickle.dump(graph, f)
                #try:
                #    if not use_stored_graphs:
                #        raise Exception("Force recomputation")
                #    graph_image_file = os.path.join(exportdir, graph_name + '_image.png')
                #    graph_image = PIL.Image.open(graph_image_file)
                #    graph_images.append(graph_image)
                #except Exception as e:
                #    fig, ax = plot_graph_vornoi(phenotype, rooms_only, show=False)
                #    graph_image_file = os.path.join(exportdir, graph_name + '_image.png')
                #    fig.savefig(graph_image_file, format='png', dpi=300)
                #    image = PIL.Image.frombytes('RGB', fig.canvas.get_width_height(), fig.canvas.buffer_rgba())
                #    graph_images.append(image)

                # If last inserted graph has no node, remove phenotype
                #if len(graph.nodes) == 0:
                #    phenotypes.pop()
                #    datasets.pop()
                #    graphs.pop()
    df = pd.concat(datasets)
    df = df[column_tsne]
    # Substitute NaN values with the mean of the column
    imputer = SimpleImputer(strategy='mean')
    df = pd.DataFrame(imputer.fit_transform(df), columns=df.columns)
    phenotypes_flat = [phenotype.map_matrix().flatten() for phenotype in phenotypes]
    df_phenotypes = pd.DataFrame(phenotypes_flat)

    graph2vec = Graph2Vec(dimensions=128*5, wl_iterations=2, attributed=True, seed=42, min_count=1)  
    graph2vec.fit(graphs)
    graph2vec_embeddings = graph2vec.get_embedding()
    df_graph_embeddings = pd.DataFrame(graph2vec_embeddings)

    #gl2vec = GL2Vec(wl_iterations=2, dimensions=128*5, epochs=10, seed=42, min_count=1, attributed=True)
    #gl2vec.fit(graphs)
    #gl2vec_embeddings = gl2vec.get_embedding()
    #df_graph_embeddings = pd.DataFrame(gl2vec_embeddings)

    # Get color map for 2D visualization
    color_map_resolution = 500
    color_map = get_color_map(color_map_resolution)

    for perplexity in perplexities:
        # Compute t-SNE for features
        tsne = TSNE(n_components=2, random_state=42, perplexity=perplexity, max_iter=tsne_iterations)
        points_feat = tsne.fit_transform(df)
        
        # get minimum X and Y values and max
        min_x = np.min(points_feat[:, 0])
        min_y = np.min(points_feat[:, 1])
        max_x = np.max(points_feat[:, 0])
        max_y = np.max(points_feat[:, 1])

        # Assign to each point a color based on the 2D color map
        colors = np.zeros((len(points_feat), 3))
        for i, point in enumerate(points_feat):
            x = int((point[0] - min_x) / (max_x - min_x) * color_map_resolution - 1)
            y = int((point[1] - min_y) / (max_y - min_y) * color_map_resolution - 1)
            colors[i] = color_map[x, y]

        plt.scatter(points_feat[:, 0], points_feat[:, 1], s=1, c=colors)
        plt.annotate(f"t-SNE with features. Perplexity: {perplexity}", (0.5, 1.05), xycoords='axes fraction', ha='center', va='bottom')
        plt.savefig(os.path.join(outdir, f"{name}_features_p{perplexity}.png"), format='png', dpi=300)
        plt.clf()
        plt.close()

        # Compute t-SNE for image similarity
        tsne = TSNE(n_components=2, random_state=42, perplexity=perplexity, max_iter=tsne_iterations)
        points_img = tsne.fit_transform(df_phenotypes)

        #Scatter point by giving them a color corresponding to the 2D color map based on points feat positions
        fig, ax = plt.subplots()
        plt.scatter(points_img[:, 0], points_img[:, 1], s=1, c=colors)
        plt.annotate(f"t-SNE with images. Color represents position in t-SNE with features. Perplexity: {perplexity}", (0.5, 1.05), xycoords='axes fraction', ha='center', va='bottom')
        if visualize_tsne_img:
            def onclick(event):
                x = event.xdata
                y = event.ydata
                print("Closest point: ", x, y)
                min_dist = 10000
                min_idx = -1
                for idx, point in enumerate(points_img):
                    dist = np.linalg.norm([point[0] - x, point[1] - y])
                    if dist < min_dist:
                        min_dist = dist
                        min_idx = idx
                # Plot show map of closest point
                phenotype = phenotypes[min_idx]
                fig, ax = plt.subplots()
                plt.imshow(phenotype.map_matrix(inverted=True), cmap='gray')
                plt.show()
            cid = fig.canvas.mpl_connect('button_press_event', onclick)
            plt.show()
            fig.canvas.mpl_disconnect(cid)
        plt.savefig(os.path.join(outdir, f"{name}_img_p{perplexity}.png"), format='png', dpi=300)
        plt.clf()
        plt.close()

        # Compute t-SNE for graph similarity
        fig, ax = plt.subplots()
        tsne = TSNE(n_components=2, random_state=42, perplexity=perplexity, max_iter=tsne_iterations)
        
        #Scatter point by giving them a color corresponding to the 2D color map based on points feat positions
        points_graph = tsne.fit_transform(df_graph_embeddings)
        plt.scatter(points_graph[:, 0], points_graph[:, 1], s=1, c=colors)
        plt.annotate(f"t-SNE with graphs. Color represents position in t-SNE with features. Perplexity: {perplexity}", (0.5, 1.05), xycoords='axes fraction', ha='center', va='bottom')
        plt.savefig(os.path.join(outdir, f"{name}_graph_p{perplexity}.png"), format='png', dpi=300)
        if visualize_tsne_graph:
            def onclick(event):
                x = event.xdata
                y = event.ydata
                print("Closest point: ", x, y)
                min_dist = 10000
                min_idx = -1
                for idx, point in enumerate(points_graph):
                    dist = np.linalg.norm([point[0] - x, point[1] - y])
                    if dist < min_dist:
                        min_dist = dist
                        min_idx = idx
                # Plot show map of closest point
                phenotype = phenotypes[min_idx]
                plot_graph_vornoi(phenotype, rooms_only)
            cid = fig.canvas.mpl_connect('button_press_event', onclick)
            plt.show()
            fig.canvas.mpl_disconnect(cid)
        plt.clf()
        plt.close()

        # Scatter points as graph images
        #fig = plt.figure()
        #ax = fig.add_subplot(111)
        ## Set the limits of the plot
        #plt.xlim(min(points_graph[:, 0]), max(points_graph[:, 0]))
        #plt.ylim(min(points_graph[:, 1]), max(points_graph[:, 1]))
        #for (x0, y0, img) in zip(points_graph[:, 0], points_graph[:, 1], graph_images):
        #    bb = Bbox.from_bounds(x0,y0,0.3,0.3)  
        #    bb2 = TransformedBbox(bb,ax.transData)
        #    bbox_image = BboxImage(bb2,
        #                        norm = None,
        #                        origin=None,
        #                        clip_on=False)
        #    bbox_image.set_data(img)
        #    ax.add_artist(bbox_image)
        #plt.annotate(f"t-SNE with graphs. Images represent the graphs. Perplexity: {perplexity}", (0.5, 1.05), xycoords='axes fraction', ha='center', va='bottom')
        #plt.savefig(os.path.join(outdir, f"{name}_graph_wimg_p{perplexity}.png"), format='png', dpi=300)
        #if visualize_tsne_graph:
        #    plt.show()
        #plt.clf()
        #plt.close()

        for feature in column_tsne_reduced:
            plt.scatter(points_img[:, 0], points_img[:, 1], s=1, c=df[feature], cmap='viridis')
            plt.colorbar()
            plt.annotate(f"t-SNE with images. Color representing {feature}", (0.5, 1.05), xycoords='axes fraction', ha='center', va='bottom')
            plt.savefig(os.path.join(outdirsub, f"{name}_img_{feature}_p{perplexity}.png"), format='png', dpi=300)
            plt.clf()
            plt.close()
        
        for feature in column_tsne_reduced:
            plt.scatter(points_graph[:, 0], points_graph[:, 1], s=1, c=df[feature], cmap='viridis')
            plt.colorbar()
            plt.annotate(f"t-SNE with graphs. Color representing {feature}", (0.5, 1.05), xycoords='axes fraction', ha='center', va='bottom')
            plt.savefig(os.path.join(outdirsub, f"{name}_graph_{feature}_p{perplexity}.png"), format='png', dpi=300)
            plt.clf()
            plt.close()
        
        # Visualize difference in distance for the feature and graph tsne
        distances_tsne_features = []
        distances_tsne_graph = []
        for i in range(len(df)):
            for j in range(i+1, len(df)):
                distances_tsne_features.append(np.linalg.norm(points_feat[i] - points_feat[j]))
                distances_tsne_graph.append(np.linalg.norm(points_graph[i] - points_graph[j]))
        plt.gcf().set_size_inches(18, 14) 
        plt.scatter(distances_tsne_features, distances_tsne_graph, s=0.1)
        plt.xlabel("Distance in Feature t-SNE")
        plt.ylabel("Distance in Graph t-SNE")
        plt.savefig(os.path.join(outdirsub, f"{name}_disttsne_feat_graph_p{perplexity}.png"), format='png', dpi=300)
        plt.clf()
        plt.close()

        # Visualize difference in distance for the feature and img tsne
        distances_tsne_img = []
        for i in range(len(df)):
            for j in range(i+1, len(df)):
                distances_tsne_img.append(np.linalg.norm(points_img[i] - points_img[j]))
        plt.gcf().set_size_inches(18, 14) 
        plt.scatter(distances_tsne_features, distances_tsne_img, s=0.1)
        plt.xlabel("Distance in Feature t-SNE")
        plt.ylabel("Distance in Image t-SNE")
        plt.savefig(os.path.join(outdirsub, f"{name}_disttsne_feat_img_p{perplexity}.png"), format='png', dpi=300)
        plt.clf()
        plt.close()

        # Visualize difference in distance for the img and graph tsne
        distances_tsne_img = []
        distances_tsne_graph = []
        for i in range(len(df)):
            for j in range(i+1, len(df)):
                distances_tsne_img.append(np.linalg.norm(points_img[i] - points_img[j]))
                distances_tsne_graph.append(np.linalg.norm(points_graph[i] - points_graph[j]))
        plt.gcf().set_size_inches(18, 14) 
        plt.scatter(distances_tsne_img, distances_tsne_graph, s=0.1)
        plt.xlabel("Distance in Image t-SNE")
        plt.ylabel("Distance in Graph t-SNE")
        plt.savefig(os.path.join(outdirsub, f"{name}_disttsne_img_graph_{name}_p{perplexity}.png"), format='png', dpi=300)
        plt.clf()
        plt.close()


        # Visualize difference in distance for features
        column_main = [
            'entropy',
            'maxValuePosition',
            'coveragePosition',
            'localMaximaNumberPosition',
            'pace',
            'area',
            'stdValuePercentVisibility',
            'averageRoomBetweenness',
            'averageMincut',
            'maxSymmetry',
        ]
        column_main = features_final.copy()
        column_main.remove('balanceTopology')
        for feature in column_main:
            differences_feature = []
            for i in range(len(df)):
                for j in range(i+1, len(df)):
                    differences_feature.append(df[feature][i] - df[feature][j])

            plt.gcf().set_size_inches(18, 14)
            plt.scatter(differences_feature, distances_tsne_graph, s=0.02)
            plt.xlabel(f"Difference in {feature}")
            plt.ylabel("Distance in Graph t-SNE")
            plt.savefig(os.path.join(outdirsub, f"{name}_distfeat_graph_{feature}_p{perplexity}.png"), format='png', dpi=300)
            plt.clf()
            plt.close()

            plt.gcf().set_size_inches(18, 14) 
            plt.scatter(differences_feature, distances_tsne_img, s=0.02)
            plt.xlabel(f"Difference in {feature}")
            plt.ylabel("Distance in Image t-SNE")
            plt.savefig(os.path.join(outdirsub, f"{name}_distfeat_img_{feature}_p{perplexity}.png"), format='png', dpi=300)
            plt.clf()
            plt.close()

            print(f"Distance analysis done for {feature}")

if __name__ == "__main__":
    baseoutdir = Path(path.join(ANALYSIS_OUTPUT_FOLDER, 'analysis'))
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
        for i in tqdm.trange(0, 1):
            name = f'var_ab_{i}_nm1'
            phenotype_path = os.path.join(baseoutdir, name, f"phenotype_{name}.pkl")
            name = f'var_ab_{i}_nm5'
            run_variance_analysis(client, baseoutdir, name, i, ALL_BLACK_NAME, feature_ranges, num_parallel_simulations=5, phenotype_path=phenotype_path, use_cache=True)
        for i in tqdm.trange(0, 1):
            name = f'var_grid_{i}_nm1'
            phenotype_path = os.path.join(baseoutdir, name, f"phenotype_{name}.pkl")
            name = f'var_grid_{i}_nm5'
            run_variance_analysis(client, baseoutdir, name, i, GRID_GRAPH_NAME, feature_ranges, num_parallel_simulations=5, phenotype_path=phenotype_path, use_cache=True)
        for i in tqdm.trange(0, 1):
            name = f'var_smt_{i}_nm1'
            phenotype_path = os.path.join(baseoutdir, name, f"phenotype_{name}.pkl")
            name = f'var_smt_{i}_nm5'
            run_variance_analysis(client, baseoutdir, name, i, SMT_NAME, feature_ranges, num_parallel_simulations=5, phenotype_path=phenotype_path, use_cache=True)
        for i in tqdm.trange(0, 1):
            name = f'var_pointad_{i}_nm1'
            phenotype_path = os.path.join(baseoutdir, name, f"phenotype_{name}.pkl")
            name = f'var_pointad_{i}_nm5'
            run_variance_analysis(client, baseoutdir, name, i, POINT_AD_NAME, feature_ranges, num_parallel_simulations=5, phenotype_path=phenotype_path, use_cache=True)

    COVARIANCE_ANALYSIS = True
    if COVARIANCE_ANALYSIS:
        name = f'cov_ab'
        var_corr_ab, var_corr_reduced_ab, var_corr_final_ab = run_covariance_analysis(client, baseoutdir, name, 0, ALL_BLACK_NAME, feature_ranges, num_parallel_simulations=5, use_cache=True)
        name = f'cov_grid'
        var_corr_grid, var_corr_reduced_grid, var_corr_final_grid = run_covariance_analysis(client, baseoutdir, name, 0, GRID_GRAPH_NAME, feature_ranges, num_parallel_simulations=5, use_cache=True)
        name = f'cov_smt'
        var_corr_smt, var_corr_reduced_smt, var_corr_final_smt = run_covariance_analysis(client, baseoutdir, name, 0, SMT_NAME, feature_ranges, num_parallel_simulations=5, use_cache=True)
        name = f'cov_pointad'
        var_corr_pointad, var_corr_reduced_pointad, var_corr_final_pointad = run_covariance_analysis(client, baseoutdir, name, 0, POINT_AD_NAME, feature_ranges, num_parallel_simulations=5, use_cache=True)

        
        # Given the correletion matrixes, we compute a median correlation matrix
        var_corr = (var_corr_ab + var_corr_grid + var_corr_smt + var_corr_pointad) / 4
        var_corr_reduced = (var_corr_reduced_ab + var_corr_reduced_grid + var_corr_reduced_smt + var_corr_reduced_pointad) / 4
        var_corr_final = (var_corr_final_ab + var_corr_final_grid + var_corr_final_smt + var_corr_final_pointad) / 4

        vcs = [var_corr, var_corr_reduced, var_corr_final]
        names = ['_full', '_reduced', '_final']
        annots = [False, True, True]
        # Save the final correlation matrix
        for i, var in enumerate(vcs):

            fig, ax = plt.subplots(figsize=(24, 16))
            heatmap_ax = sns.heatmap(var, xticklabels=var.columns, yticklabels=var.index, annot=annots[i], annot_kws={"fontsize":6})
            heatmap_ax.tick_params(axis='both', which='major', labelsize=10)
            plt.setp(heatmap_ax.get_xticklabels(), rotation=45, ha='right', rotation_mode="anchor")
            plt.tight_layout()
            plt.savefig(os.path.join(baseoutdir, f"covariance_map{names[i]}.png"), format='png', dpi=300) 
            plt.clf()
            plt.close()

            clustermap_ax = sns.clustermap(var, xticklabels=var.columns, yticklabels=var.index, annot=annots[i], annot_kws={"fontsize":4})
            plt.setp(clustermap_ax.ax_heatmap.get_yticklabels(), rotation=0)
            plt.setp(clustermap_ax.ax_heatmap.get_xticklabels(), rotation=45, ha='right', fontsize=8, rotation_mode="anchor")
            plt.savefig(os.path.join(baseoutdir, f"covariance_clustermap{names[i]}.png"), format='png', dpi=300)
            plt.clf()
            plt.close()



    TSNE_ANALYSIS = False
    if TSNE_ANALYSIS:
        experiment_names = [
            "Bari_AB_ABEmitter_SB_entropy_balanceTopology_pursueTime_I400_B1_E10",
            #"Cov_AB_ABEmitter_SB_entropy_maxValuePercentVisibility_area_I400_B1_E10",
        ]        
        name = f'tsne_ab_graph2vecAttr'

        tsne_analysis(client, baseoutdir, name, experiment_names, num_experiment_iterations=200, use_stored_graphs=True) 
        experiment_names = [
            "BariInverse_PointAD_PointADEmitter_SB_entropy_balanceTopology_pursueTime_I400_B1_E10",
        ]
        name = f'tsne_pointad_graph2vecAttr'
        tsne_analysis(client, baseoutdir, name, experiment_names, num_experiment_iterations=200, use_stored_graphs=True)

        experiment_names = [
            "Bari_GG_GGEmitter_SB_entropy_balanceTopology_pursueTime_I400_B1_E10"
        ]
        name = f'tsne_gg_graph2vecAttr'
        tsne_analysis(client, baseoutdir, name, experiment_names, num_experiment_iterations=200, use_stored_graphs=True)

        experiment_names = [
            "EpV_SMT_SMTEmitter_SB_explorationPlusVisibility3_balanceTopology_averageRoomRadius_I400_B1_E10"
        ]
        name = f'tsne_smt_graph2vecAttr'
        tsne_analysis(client, baseoutdir, name, experiment_names, num_experiment_iterations=200, use_stored_graphs=True)
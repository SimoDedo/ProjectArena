import pandas as pd
from z3 import *
from internals.ab_genome.ab_genome import ABGenome
from internals.graph_genome.gg_genome import GraphGenome
from internals.phenotype import Phenotype
from internals.smt_genome.smt_genome import SMTGenome
import matplotlib.pyplot as plt
import numpy as np
import igraph as ig
from matplotlib import cm
import holoviews as hv
import numpy as np
import pandas as pd
hv.extension('bokeh')
from bokeh.plotting import show
import networkx as nx
from karateclub import Graph2Vec
import numpy as np
from internals.graph import to_rooms_only_graph

def plot_graph_vornoi(phenotype: Phenotype):
    graph, outer_shell, obstacles = phenotype.to_topology_graph_vornoi()
    graph = to_rooms_only_graph(graph)
    fig, ax = plt.subplots()

    # Plot the outer wall
    x,y = outer_shell.exterior.xy
    plt.plot(x,y)


    # Plot the obstacles
    for obstacle in obstacles:
        x,y = obstacle.exterior.xy
        plt.plot(x,y, color='darkred')

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

    graph.es['label'] = [str(w) for w in graph.es['weight']]

    layout = ig.Layout(coords=graph.vs['coords'], dim=2)
    ig.plot(graph, vertex_size=5, layout=layout, vertex_label_color="black",  target=ax, edge_label = graph.es['label'], edge_label_color="black")
    plt.gca().invert_yaxis()

    ax.set_xlim(0, phenotype.mapWidth)
    ax.set_ylim(0, phenotype.mapHeight)
    plt.gca().invert_yaxis()

    plt.show()

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

if __name__ == "__main__":
    genome = ABGenome.create_random_genome()
    phenotype = genome.phenotype()
    graph = get_graph(phenotype)

    print(graph.edges(data=True))

    graph_l = nx.line_graph(graph)

    # Transfer edge features from the original graph to the nodes of the line graph
    for edge in graph.edges(data=True):
        u, v, data = edge
        print(data)
        graph_l.nodes[(u, v)]['feature'] = data['feature']
    print(graph_l.nodes(data=True))



    node_mapper = {node: i for i, node in enumerate(graph_l.nodes())}
    edges = [[node_mapper[edge[0]], node_mapper[edge[1]]] for edge in graph_l.edges()]

    line_graph = nx.from_edgelist(edges)
    # Maintain node data from graph_l
    for node in graph_l.nodes(data=True):
        print(node)
        line_graph.nodes[node_mapper[node[0]]].update(node[1])

    

    print(nx.get_node_attributes(line_graph, "feature"))




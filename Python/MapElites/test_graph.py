from internals.ab_genome.ab_genome import ABGenome
from internals.graph_genome.gg_genome import GraphGenome
import matplotlib.pyplot as plt
import numpy as np
import igraph as ig
import pyvoronoi
from matplotlib import collections  as mc

if __name__ == "__main__":
    # Create random ABGenome
    genome = ABGenome.create_random_genome()
    phenotype = genome.phenotype()
    phenotype.simplify()

    fig, ax = plt.subplots()
    for area in phenotype.areas:
        if area.isCorridor:
            ax.add_patch(plt.Rectangle((area.leftColumn, area.topRow), area.rightColumn-area.leftColumn, area.bottomRow-area.topRow, fill='b', edgecolor='b'))
        else:
            ax.add_patch(plt.Rectangle((area.leftColumn, area.topRow), area.rightColumn-area.leftColumn, area.bottomRow-area.topRow, fill=None, edgecolor='r'))
    ax.set_xlim(0, phenotype.mapWidth)
    ax.set_ylim(0, phenotype.mapHeight)

    graph, layout = phenotype.to_topology_graph_naive()
    graph.vs['label'] = [str(i) for i in range(len(graph.vs))]

    #rooms = graph.vs.select(isCorridor_eq=False)
    
    #print(graph.distances(source=rooms, target=rooms, weights=graph.es['weight'])) 
        
    #distances = graph.distances(source=rooms[0], target=[rooms[i] for i in range(len(rooms)) if i != 0], weights=graph.es['weight']) 
    #print(np.mean(distances))
    #print(np.std(distances))
        
    #print(np.std(graph.betweenness(vertices=rooms, weights=graph.es['weight'])))
    #betweenness = graph.betweenness(weights=graph.es['weight'])
    #betweenness = [betweenness[i] if np.isfinite(betweenness[i]) else 0 for i in range(len(betweenness))]
    #print(betweenness)
    #graph.vs['label'] = betweenness

    #closeness = graph.closeness(vertices=rooms, weights=graph.es['weight'])
    #closeness = [np.round(closeness[i], 3) if np.isfinite(closeness[i]) else 0 for i in range(len(closeness))]
    #print(closeness)
    #graph.vs['label'] = closeness    

    #mincut = [len(graph.mincut(source=i, target=j, capacity=None).cut) for i in range(len(rooms)) for j in range(i+1, len(rooms))]
    #print(mincut)
    #mincut = np.mean(mincut)
    #print(mincut)
    #Print rooms indexes
    #print(rooms['label'])

    #cycles = graph.fundamental_cycles()
    #vertices_in_cycles = []
    #for cycle in cycles:
    #    vertices_in_cycles.append([])
    #    for i in cycle:
    #        if graph.es[i].source not in vertices_in_cycles[-1] and (not graph.vs[graph.es[i].source]['isCorridor']):
    #            vertices_in_cycles[-1].append(graph.es[i].source)
    #        if graph.es[i].target not in vertices_in_cycles[-1] and (not graph.vs[graph.es[i].target]['isCorridor']):
    #            vertices_in_cycles[-1].append(graph.es[i].target)
    #        
    #cycles_one_room = [cycles[i] for i in range(len(cycles)) if len(vertices_in_cycles[i]) > 0]
    #cor_length = [sum([graph.es[i]['weight'] for i in range(len(cycle))]) for cycle in cycles_one_room]
    #cycles_two_rooms = [cycles[i] for i in range(len(cycles)) if len(vertices_in_cycles[i]) > 0]
    #ctr_length = [sum([graph.es[i]['weight'] for i in range(len(cycle))]) for cycle in cycles_two_rooms]

    layout = ig.Layout(coords=graph.vs['coords'], dim=2)
    fig, ax = plt.subplots()
    ig.plot(graph, vertex_size=45, layout=layout, vertex_label_color="white", edge_label=[str(i) for i in range(len(graph.es))],  target=ax)


    plt.show()

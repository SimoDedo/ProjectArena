import jsonpickle
import numpy
from internals import area
from internals.area import overlap_area, contains_area
import igraph as ig
import numpy as np
import matplotlib.pyplot as plt


class Phenotype:
    def __init__(self, map_width, map_height, map_scale, areas):
        self.mapWidth = map_width
        self.mapHeight = map_height
        self.mapScale = map_scale
        self.areas = tuple(areas)

    def __eq__(self, other):
        if type(other) is type(self):
            return self.__dict__ == other.__dict__
        else:
            return False

    def __hash__(self):
        return hash(tuple(self.__dict__.items()))

    def write_to_file(self, file_path):
        with open(file_path, 'w') as f:
            cp = dict(
                width=self.mapWidth,
                height=self.mapHeight,
                mapScale=self.mapScale,
                areas=self.areas,
            )
            genome_json = jsonpickle.encode(cp, unpicklable=False)
            f.write(genome_json)

    def map_matrix(self):
        map_matrix = numpy.zeros([self.mapHeight, self.mapWidth], dtype=numpy.int8)
        for area in self.areas:
            for row in range(area.bottomRow, area.topRow):
                for col in range(area.leftColumn, area.rightColumn):
                    map_matrix[row][col] = 1
        return map_matrix

    def simplify(self):
        to_be_removed = []
        for i in range(len(self.areas)):
            for j in range(i + 1, len(self.areas)):
                if contains_area(self.areas[i], self.areas[j]):
                    to_be_removed.append(j)
        # Remove the areas that are contained in another area
        self.areas = tuple([self.areas[i] for i in range(len(self.areas)) if not i in to_be_removed])
            
    def to_graph(self):
        vertices = []
        isCorridor = []
        v_color = []
        coords = []
        edges = []
        e_color = []
        a: area.Area
        v: area.Area
        for a in self.areas:
            for (v, i) in vertices:
                if overlap_area(a, v):
                    edges.append((i, len(vertices)))
                    if a.isCorridor and v.isCorridor:
                        e_color.append("blue")
                    elif a.isCorridor or v.isCorridor:
                        e_color.append("purple")
                    else:
                        e_color.append("red")
            vertices.append((a, len(vertices)))
            isCorridor.append(a.isCorridor)
            v_color.append("red" if not a.isCorridor else "blue")
            coords.append(((a.rightColumn + a.leftColumn)/2, (a.topRow + a.bottomRow)/2))

        
        graph = ig.Graph(n=len(vertices), edges=edges)
        graph.vs['isCorridor'] = isCorridor
        graph.vs['coords'] = coords
        graph.vs['color'] = v_color
        graph.es['color'] = e_color
        #TODO: Add size to the vertices and edges and merge. Consider adding areas as a vertex attribute and merging
        #graph.vs['label'] = [str(i) for i in range(len(graph.vs))]

        
        def combine_edges(args):
            if len(args) == 0:
                return None
            if len(args) > 0:
                if type (args[0]) is tuple:
                    unzipped = [[i for i, j in args],
                            [j for i, j in args]]
                    return (np.mean(unzipped[0]), np.mean(unzipped[1]))
                elif type(args[0]) is str:
                    if any([a == 'purple' for a in args]):
                        return 'purple'
                    else:
                        return args[0]
                else:
                    return args[0]

        #Filter out cliques of corridors
        cliques = graph.cliques(min=3)
        to_remove = []
        for i in range(len(cliques)):  # Filter out cliques of corridors
            if not all(graph.vs[cliques[i]]['isCorridor']):
                to_remove.append(i)
        cliques = [cliques[i] for i in range(len(cliques)) if not i in to_remove]
        #Contract the cliques
        new_vertices = [i for i in range(len(graph.vs))]
        for i in range(len(cliques)):
            for j in cliques[i]:
                new_vertices[j] = cliques[i][0]
        graph.contract_vertices(new_vertices, combine_attrs=combine_edges)
        graph.simplify(multiple=True, loops=True, combine_edges=combine_edges)
        graph.delete_vertices([v.index for v in graph.vs if len(v.neighbors()) == 0])

        #Filter out cliques of rooms
        cliques = graph.cliques(min=2)
        to_remove = []
        for i in range(len(cliques)):
            if any(graph.vs[cliques[i]]['isCorridor']):
                to_remove.append(i)
        cliques = [cliques[i] for i in range(len(cliques)) if not i in to_remove]
        #Contract the cliques
        new_vertices = [i for i in range(len(graph.vs))]
        for i in range(len(cliques)):
            for j in cliques[i]:
                new_vertices[j] = cliques[i][0]
        graph.contract_vertices(new_vertices, combine_attrs=combine_edges)
        graph.simplify(multiple=True, loops=True, combine_edges=combine_edges)
        graph.delete_vertices([v.index for v in graph.vs if len(v.neighbors()) == 0])

        # Define the graph layout using the area corners
        layout = ig.Layout(coords=graph.vs['coords'], dim=2)
        return graph, layout


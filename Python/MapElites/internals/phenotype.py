import jsonpickle
import numpy
from internals import area
from internals.area import contains_area
import matplotlib.pyplot as plt
from internals.graph import to_topology_graph_naive, to_topology_graph_vornoi
from internals.visibility import create_visibility_graph_DDA, create_visibility_matrix_grid, create_visibility_matrix_DDA


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

    def map_matrix(self, inverted=False):
        value = 1 if not inverted else 0
        map_matrix = numpy.full([self.mapHeight, self.mapWidth], dtype=numpy.int8, fill_value=abs(value-1))
        for area in self.areas:
            for row in range(area.bottomRow, area.topRow):
                for col in range(area.leftColumn, area.rightColumn):
                    map_matrix[row][col] = value
        return map_matrix

    def simplify(self):
        to_be_removed = []
        for i in range(len(self.areas)):
            for j in range(len(self.areas)):
                if i != j and contains_area(self.areas[i], self.areas[j]):
                    to_be_removed.append(j)
        # Remove the areas that are contained in another area
        self.areas = tuple([self.areas[i] for i in range(len(self.areas)) if not i in to_be_removed])

    def to_topology_graph_naive(self):
        return to_topology_graph_naive(self)
    
    def to_topology_graph_vornoi(self):
        return to_topology_graph_vornoi(self)
    
    # Returns both a graph and a convenient matrix form
    def to_visibility_matrix_DDA(self, return_graph=False):
        map_matrix = self.map_matrix()
        graph = create_visibility_graph_DDA(map_matrix)
        matrix = create_visibility_matrix_DDA(graph, map_matrix)
        if return_graph:
            return matrix, graph
        else:
            return matrix
            
    def to_visibility_matrix_grid(self):
        return create_visibility_matrix_grid(self.map_matrix())
        




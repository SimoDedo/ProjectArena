import jsonpickle
import numpy
from internals import area
from internals.area import contains_area
import matplotlib.pyplot as plt
from internals.graph import to_graph_naive, to_graph_vornoi


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
            for j in range(len(self.areas)):
                if i != j and contains_area(self.areas[i], self.areas[j]):
                    to_be_removed.append(j)
        # Remove the areas that are contained in another area
        self.areas = tuple([self.areas[i] for i in range(len(self.areas)) if not i in to_be_removed])

    def to_graph_naive(self):
        return to_graph_naive(self)
    
    def to_graph_vornoi(self):
        return to_graph_vornoi(self)
            




import jsonpickle
import numpy
from internals import area
from internals.area import overlap_area, contains_area
import igraph as ig
import numpy as np
import matplotlib.pyplot as plt
from enum import Enum

class VertexType(Enum):
    ROOM = 'red'
    CORRIDOR = 'blue'

class EdgeType(Enum):
    ROOM_TO_ROOM = 'red'
    CORRIDOR_TO_CORRIDOR = 'blue'
    ROOM_TO_CORRIDOR = 'purple'


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
                        e_color.append(EdgeType.CORRIDOR_TO_CORRIDOR)
                    elif a.isCorridor or v.isCorridor:
                        e_color.append(EdgeType.ROOM_TO_CORRIDOR)
                    else:
                        e_color.append(EdgeType.ROOM_TO_ROOM)
            vertices.append((a, len(vertices)))
            isCorridor.append(a.isCorridor)
            v_color.append(VertexType.ROOM if not a.isCorridor else VertexType.CORRIDOR)
            coords.append(((a.rightColumn + a.leftColumn)/2, (a.topRow + a.bottomRow)/2))

        
        graph = ig.Graph(n=len(vertices), edges=edges)
        graph.vs['isCorridor'] = isCorridor
        graph.vs['coords'] = coords
        graph.vs['type'] = v_color
        graph.es['type'] = e_color
        #graph.vs['label'] = [str(i) for i in range(len(graph.vs))]
        
        #Filter out cliques of corridors
        cliques = [1]
        while len(cliques) > 0:
            cliques = graph.cliques(min=3)
            to_skip = [i for i in range(len(cliques)) if not all(graph.vs[cliques[i]]['isCorridor'])]
            cliques = [cliques[i] for i in range(len(cliques)) if not i in to_skip]
            
            new_vertices = [i for i in range(len(graph.vs))]
            for i in range(len(cliques)):
                for j in cliques[i]:
                    new_vertices[j] = cliques[i][0]
            
            graph.contract_vertices(new_vertices, combine_attrs=self.__combine__)
            graph.delete_vertices([v.index for v in graph.vs if len(v.neighbors()) == 0])
            if len(graph.vs) >1:
                graph.simplify(multiple=True, loops=True, combine_edges=self.__combine__)

        #Filter out cliques of rooms
        cliques = [1]
        while len(cliques) > 0:
            cliques = graph.cliques(min=2)
            to_skip = [i for i in range(len(cliques)) if any(graph.vs[cliques[i]]['isCorridor'])]
            cliques = [cliques[i] for i in range(len(cliques)) if not i in to_skip]
            
            new_vertices = [i for i in range(len(graph.vs))]
            for i in range(len(cliques)):
                for j in cliques[i]:
                    new_vertices[j] = cliques[i][0]
            
            graph.contract_vertices(new_vertices, combine_attrs=self.__combine__)
            graph.delete_vertices([v.index for v in graph.vs if len(v.neighbors()) == 0])
            if len(graph.vs) >1:
                graph.simplify(multiple=True, loops=True, combine_edges=self.__combine__)

        weights = [round(np.linalg.norm(np.array(graph.vs[graph.es[i].source]['coords']) - np.array(graph.vs[graph.es[i].target]['coords'])),3) for i in (range(len(graph.es)))]
        graph.es['weight'] = [w if w > 0 else 0.01 for w in weights] 
    
        graph.vs['color'] = [v.value for v in graph.vs['type']]
        graph.es['color'] = [e.value for e in graph.es['type']]
        # Define the graph layout using the area corners
        layout = ig.Layout(coords=graph.vs['coords'], dim=2)
        return graph, layout
    

    #TODO: not precise in collapsing corridors.
    def remove_corridors_from_graph(self, graph):
        vertices = [1]
        new_vertices = []
        while vertices != new_vertices:
            vertices = [i for i in range(len(graph.vs))]
            new_vertices = []
            for i in range(len(graph.vs)):
                if not graph.vs[i]['isCorridor']:
                    new_vertices.append(i)
                else:
                    rooms = [j for j in graph.neighbors(i) if not graph.vs[j]['isCorridor']]
                    if len(rooms) > 0:
                        new_vertices.append(rooms[0])
                    else:
                        new_vertices.append(i)
            
            graph.contract_vertices(new_vertices, combine_attrs=self.__combine__)
            graph.delete_vertices([v.index for v in graph.vs if len(v.neighbors()) == 0])

            if len(graph.vs) >1:
                graph.simplify(multiple=False, loops=True, combine_edges=self.__combine__)
            graph.vs['color'] = [v.value for v in graph.vs['type']]
            graph.es['color'] = [e.value for e in graph.es['type']]

        return graph
    
    def __combine__(self, args):
        if len(args) == 0:
            return None
        if len(args) > 0:
            if type (args[0]) is tuple:
                unzipped = [[i for i, j in args],
                        [j for i, j in args]]
                return (np.mean(unzipped[0]), np.mean(unzipped[1]))
            elif type(args[0]) is EdgeType:
                if any([a == EdgeType.ROOM_TO_CORRIDOR for a in args]):
                    return EdgeType.ROOM_TO_CORRIDOR
                else:
                    return args[0]
            elif type(args[0]) is VertexType:
                if any([a == VertexType.ROOM for a in args]):
                    return VertexType.ROOM
                else:
                    return args[0]
            elif type(args[0]) is bool:
                if not all(args):
                    return False
                else:
                    return True
            elif type(args[0]) is str:
                return args[0]
            else:
                return np.sum(args)




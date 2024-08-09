import numpy as np
import igraph as ig

WALL_TILE = 0
SPACE_TILE = 1

def DDA(x0, y0, x1, y1): 
  
    # find absolute differences 
    dx = x1 - x0 
    dy = y1 - y0 
  
    # find maximum difference 
    steps = max(abs(dx), abs(dy)) 
  
    # calculate the increment in x and y 
    xinc = dx/steps 
    yinc = dy/steps 
  
    # start with 1st point 
    x = float(x0) 
    y = float(y0) 
  
    # make a list for coordinates 
    coorinates = [] 
  
    for i in range(steps): 
        # append the x,y coordinates in respective list 
        coorinates.append((int(x), int(y))) 
  
        # increment the values 
        x = x + xinc 
        y = y + yinc 
    return coorinates

def is_path_clear(matrix, start, end):
    x1, y1 = start
    x2, y2 = end
    
    line_points = DDA(x1, y1, x2, y2)
    
    for (x, y) in line_points:
        if matrix[x][y] == WALL_TILE:
            return False, None
    return True, line_points

def create_visibility_graph(map_matrix):
    coords = []
    for i in range(len(map_matrix)):
        for j in range(len(map_matrix[0])):
            if map_matrix[i][j] == SPACE_TILE:
                coords.append((i, j))
    coords = np.array(coords)
    graph = ig.Graph()
    graph.add_vertices(len(coords))
    graph.vs["coords"] = coords


    edges_to_add = []
    checked = np.zeros((len(coords), len(coords)))
    for i in range(len(coords)):
        for j in range(len(coords)-1, i, -1):
            if i == j:
                continue
            if checked[i][j] == 1:
                continue

            start = coords[i]
            end = coords[j]

            clear, line = is_path_clear(map_matrix, start, end)
            if clear:
                edges_to_add.append((i, j))
                checked[i][j] = 1
                checked[j][i] = 1

    graph.add_edges(edges_to_add)
    return graph



def create_visibility_matrix(graph, matrix, init=0, offset=0):
    visibility_matrix = np.full((len(matrix), len(matrix[0])), init)
    for v in graph.vs:
        x, y = v["coords"]
        visibility_matrix[x][y] = graph.degree(v) + offset
    return visibility_matrix
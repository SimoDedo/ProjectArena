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

def efficient_DDA(x0, y0, x1, y1, matrix, checked, edges_to_add, coords_dict):
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
    start_idx = coords_dict[(x0, y0)]

    in_wall = False
    for i in range(steps): 
        # append the x,y coordinates in respective list 
        int_x, int_y = int(x), int(y)
        if matrix[int_x][int_y] == WALL_TILE:
            return coorinates, checked, edges_to_add
            in_wall = True
        else:
            if in_wall:
                start_idx = coords_dict[(int_x, int_y)]
            else:
                new_point_idx = coords_dict[(int_x, int_y)]
                if not checked[start_idx][new_point_idx] and not checked[new_point_idx][start_idx]:
                    checked[start_idx][new_point_idx] = 1
                    checked[new_point_idx][start_idx] = 1
                    edges_to_add.append((start_idx, new_point_idx))
                coorinates.append((int_x, int_y)) 
  
        # increment the values 
        x = x + xinc 
        y = y + yinc 

    end_idx = coords_dict[(x1, y1)]
    checked[start_idx][end_idx] = 1
    checked[end_idx][start_idx] = 1
    edges_to_add.append((start_idx, end_idx))
    return coorinates, checked, edges_to_add

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
    coords_dict = {}
    num_spaces = 0
    for i in range(len(map_matrix)):
        for j in range(len(map_matrix[0])):
            if map_matrix[i][j] == SPACE_TILE:
                coords.append((i, j))
                coords_dict[(i, j)] = num_spaces
                num_spaces += 1
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

            line, checked, edges_to_add = efficient_DDA(start[0], start[1], end[0], end[1], map_matrix, checked, edges_to_add, coords_dict)
            
            #clear, line = is_path_clear(map_matrix, start, end, strat)
            #if clear:
            #    edges_to_add.append((i, j))
            #    checked[i][j] = 1
            #    checked[j][i] = 1

    graph.add_edges(edges_to_add)
    return graph



def create_visibility_matrix(graph, matrix, init=0, offset=0):
    visibility_matrix = np.full((len(matrix), len(matrix[0])), init)
    for v in graph.vs:
        x, y = v["coords"]
        visibility_matrix[x][y] = graph.degree(v) + offset
    return visibility_matrix
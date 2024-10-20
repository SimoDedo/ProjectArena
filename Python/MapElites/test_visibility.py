from z3 import *
from internals.ab_genome.ab_genome import ABGenome
from internals.graph_genome.gg_genome import GraphGenome
from internals.smt_genome.smt_genome import SMTGenome
import matplotlib.pyplot as plt
import numpy as np
import igraph as ig
from matplotlib import cm
from scipy.ndimage import gaussian_filter
import internals.result_extractor as re
import time
from numba import jit
from numba import types
from numba.typed import Dict


WALL_TILE = 0
SPACE_TILE = 1

@jit(nopython=True)
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

def bresenham_line(x1, y1, x2, y2):
    dx = abs(x2 - x1)
    sx = 1 if x1 < x2 else -1
    dy = -abs(y2 - y1)
    sy = 1 if y1 < y2 else -1
    error = dx + dy

    coordinates = []
    while True:
        coordinates.append((x1, y1))
        if (x1 == x2) and (y1 == y2):
            break
        e2 = 2 * error
        if e2 >= dy:
            if x1 == x2:
                break
            error = error + dy
            x1 = x1 + sx
        if e2 <= dx:
            if y1 == y2:
                break
            error = error + dx
            y1 = y1 + sy
    return coordinates

@jit(nopython=True)
def bresenham_line_jit(x1, y1, x2, y2):
    dx = abs(x2 - x1)
    sx = 1 if x1 < x2 else -1
    dy = -abs(y2 - y1)
    sy = 1 if y1 < y2 else -1
    error = dx + dy

    coordinates = []
    while True:
        coordinates.append((x1, y1))
        if (x1 == x2) and (y1 == y2):
            break
        e2 = 2 * error
        if e2 >= dy:
            if x1 == x2:
                break
            error = error + dy
            x1 = x1 + sx
        if e2 <= dx:
            if y1 == y2:
                break
            error = error + dx
            y1 = y1 + sy
    return coordinates

def is_line_without_walls(matrix, line):
    for (x, y) in line:
        if matrix[x][y] == 0:
            return False
    return True


def is_path_clear(matrix, start, end, strat):
    x1, y1 = start
    x2, y2 = end
    
    line_points = strat(x1, y1, x2, y2)
    
    for (x, y) in line_points:
        if matrix[x][y] == WALL_TILE:
            return False, None
    return True, line_points

def create_visibility_graph(map_matrix, strat, efficient=False):
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

            if efficient:
                line, checked, edges_to_add = efficient_DDA(start[0], start[1], end[0], end[1], map_matrix, checked, edges_to_add, coords_dict)
            else:
                clear, line = is_path_clear(map_matrix, start, end, strat)
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

# Note that this only works if WALL_TILE = 0 and SPACE_TILE = 1
@jit(nopython=True)
def visibility_from_corner(matrix):
    for x in range(matrix.shape[0]):
        for y in range(int(x==0), matrix.shape[1]):
            matrix[x,y] *= (x*matrix[x-1,y] + y*matrix[x,y-1]) / (x + y)

def grid_based_visibility(matrix, x0, y0):
    # Copy the grid
    matrix_result = matrix.copy()


    # Compute visibility
    visibility_from_corner(matrix_result[x0:,y0:])
    visibility_from_corner(matrix_result[x0::-1,y0:])
    visibility_from_corner(matrix_result[x0::-1,y0::-1])
    visibility_from_corner(matrix_result[x0:,y0::-1])
    matrix_result[:] = (matrix_result >= 0.5)
    return matrix_result

def visibility_within_cone(matrix, u_direction, v_direction):
    u = np.asarray(u_direction, dtype=int)
    v = np.asarray(v_direction, dtype=int)
    origin = np.array([0,0], dtype=int)
    dims = np.asarray(matrix.shape, dtype=int)
    m = 0
    k = 0
    position = np.array([0,0], dtype=int)
    while np.all(position < dims):
        while np.all(position < dims):
            if not np.all(position == 0):
                pos = tuple(position)
                pos_minus_u = tuple(np.maximum(origin, position - u))
                pos_minus_v = tuple(np.maximum(origin, position - v))
                matrix[pos] *= (m*matrix[pos_minus_u] + 
                                k*matrix[pos_minus_v]) / (m + k)
            k += 1
            position += v
        m += 1
        k = 0
        position = m*u

def grid_based_visibility_four_cones(matrix, x0, y0):
    matrix_result = matrix.copy()

    visibility_within_cone(matrix_result[x0:,y0:], [2,1], [1,0])
    visibility_within_cone(matrix_result[x0::-1,y0:], [2,1], [1,0])
    visibility_within_cone(matrix_result[x0::-1,y0::-1], [2,1], [1,0])
    visibility_within_cone(matrix_result[x0:,y0::-1], [2,1], [1,0])

    visibility_within_cone(matrix_result[x0:,y0:], [2,1], [1,1])
    visibility_within_cone(matrix_result[x0::-1,y0:], [2,1], [1,1])
    visibility_within_cone(matrix_result[x0::-1,y0::-1], [2,1], [1,1])
    visibility_within_cone(matrix_result[x0:,y0::-1], [2,1], [1,1])

    visibility_within_cone(matrix_result[x0:,y0:], [1,2], [1,1])
    visibility_within_cone(matrix_result[x0::-1,y0:], [1,2], [1,1])
    visibility_within_cone(matrix_result[x0::-1,y0::-1], [1,2], [1,1])
    visibility_within_cone(matrix_result[x0:,y0::-1], [1,2], [1,1])

    visibility_within_cone(matrix_result[x0:,y0:], [1,2], [0,1])
    visibility_within_cone(matrix_result[x0::-1,y0:], [1,2], [0,1])
    visibility_within_cone(matrix_result[x0::-1,y0::-1], [1,2], [0,1])
    visibility_within_cone(matrix_result[x0:,y0::-1], [1,2], [0,1])

    return matrix_result

def grid_based_visibility_two_cones(matrix, x0, y0):
    matrix_result = matrix.copy()

    visibility_within_cone(matrix_result[x0:,y0:], [1,1], [1,0])
    visibility_within_cone(matrix_result[x0::-1,y0:], [1,1], [1,0])
    visibility_within_cone(matrix_result[x0::-1,y0::-1], [1,1], [1,0])
    visibility_within_cone(matrix_result[x0:,y0::-1], [1,1], [1,0])

    visibility_within_cone(matrix_result[x0:,y0:], [1,1], [0,1])
    visibility_within_cone(matrix_result[x0::-1,y0:], [1,1], [0,1])
    visibility_within_cone(matrix_result[x0::-1,y0::-1], [1,1], [0,1])
    visibility_within_cone(matrix_result[x0:,y0::-1], [1,1], [0,1])
    
    return matrix_result


def create_visibility_matrix_grid(map_matrix):
    # Create matrix of same shape of map matrix initialized to 0
    visibility_matrix = np.zeros((len(map_matrix), len(map_matrix[0])))
    for i in range(len(map_matrix)):
        for j in range(len(map_matrix[0])):
            if map_matrix[i][j] == SPACE_TILE:
                visibility_matrix = np.add(visibility_matrix, grid_based_visibility(map_matrix, i, j))
    return visibility_matrix

def show_visibility_matrix(visibility_matrix):
    fig, ax = plt.subplots()
    plt.imshow(visibility_matrix, cmap='inferno', interpolation='nearest', zorder=0)
    mask = np.matrix(map_matrix)
    mask = np.ma.masked_where(mask == SPACE_TILE, mask)
    plt.colorbar()
    plt.axis('off')
    plt.gca().invert_yaxis()
    plt.imshow(mask, cmap='binary', zorder=1)
    
if __name__ == "__main__":
    genome = ABGenome.create_random_genome()
    phenotype = genome.phenotype()
    map_matrix = phenotype.map_matrix()

    # Create map
    #nx = 45
    #ny = 45
    #map_matrix = np.ones((nx,ny))
    #wx = nx//10 + 1
    #wy = ny//10 + 1
    #map_matrix[int(.3*nx):int(.3*nx)+wx,int(.1*ny):int(.1*ny)+wy] = 0
    #map_matrix[int(.1*nx):int(.1*nx)+wx,int(.5*ny):int(.5*ny)+wy] = 0
    #map_matrix[int(.6*nx):int(.6*nx)+wx,int(.6*ny):int(.6*ny)+wy] = 0

    #start_time = time.time()
    #graph = create_visibility_graph(map_matrix, bresenham_line_jit)
    #visibility_map = create_visibility_matrix(graph, map_matrix, 0, 0)
    #print(f"Bresenham Jit in: {time.time() - start_time}")
#
    #show_visibility_matrix(visibility_map)

    #start_time = time.time()
    #graph = create_visibility_graph(map_matrix, DDA)
    #visibility_map = create_visibility_matrix(graph, map_matrix, 0, 0)
    #print(f"DDA jit in: {time.time() - start_time}")
#
    #show_visibility_matrix(visibility_map)

    start_time = time.time()
    graph = create_visibility_graph(map_matrix, DDA, True)
    visibility_map = create_visibility_matrix(graph, map_matrix, 0, 0)
    print(f"DDA new in: {time.time() - start_time}")

    show_visibility_matrix(visibility_map)

    start_time = time.time()
    map_matrix = map_matrix.astype(float)
    #map_matrix = upscale_matrix(map_matrix, 2)
    visibility_map = create_visibility_matrix_grid(map_matrix)
    print(f"Grid in: {time.time() - start_time}")

    show_visibility_matrix(visibility_map)


    # Test local maxima
    #masked_heatmap = re.__mask_heatmap(visibility_map, map_matrix, invert = False)
    #lm = re.__get_heatmap_local_maxima(masked_heatmap)
    #for i in range(len(lm)):
    #    x, y = lm[i]
    #    visibility_map[x][y] = 50000
    #show_visibility_matrix(visibility_map)


    # Draw the heatmap
    #fig, ax = plt.subplots()
    #masked_vis = np.ma.masked_where(visibility_map == 0, visibility_map)
    #heatmap = gaussian_filter(masked_vis.T, sigma=3)
    #plt.axis('off')
#
    #mask = np.matrix(map_matrix)
    #mask = np.ma.masked_where(mask == 0, mask)
#
    #lm = re.__get_heatmap_local_maxima(heatmap)
    #mask_lm = np.zeros((len(map_matrix), len(map_matrix[0])))
    #for i in range(len(lm)):
    #    x, y = lm[i]
    #    mask_lm[x][y] = 1
    #mask_lm = np.ma.masked_where(mask_lm == 0, mask_lm)
#
    #plt.contourf(heatmap, cmap="inferno", levels=50, zorder=0)
    #plt.imshow(mask, cmap='binary', zorder=1)
    #plt.imshow(mask_lm, cmap='binary', zorder=2)
    #plt.gca().invert_yaxis()
    #plt.axis('on')  #DEBUG: Add this line to show the axes

    # Draw the map matrix
    #fig, ax = plt.subplots()
    #ax.imshow(map_matrix, cmap='gray', interpolation='nearest')
    #plt.axis('off')
    #plt.gca().invert_yaxis()

    plt.show()

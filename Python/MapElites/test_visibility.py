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

def bresenham_line(x1,y1,x2,y2):
    #print(f"Bresenham of points ({x1}, {y1}) and ({x2}, {y2})")
    x,y = x1,y1
    dx = abs(x2 - x1)
    if dx == 0:
        dx = 0.0001
    dy = abs(y2 -y1)
    gradient = dy/float(dx)

    if gradient > 1:
        dx, dy = dy, dx
        x, y = y, x
        x1, y1 = y1, x1
        x2, y2 = y2, x2

    p = 2*dy - dx
    #print(f"x = {x}, y = {y}")
    # Initialize the plotting points
    coordinates = [(x, y)]

    for k in range(2, dx + 2):
        if p > 0:
            y = y + 1 if y < y2 else y - 1
            p = p + 2 * (dy - dx)
        else:
            p = p + 2 * dy

        x = x + 1 if x < x2 else x - 1

        #print(f"x = {x}, y = {y}")
        coordinates.append((x, y))
    
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

def create_visibility_graph(map_matrix, strat):
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

def show_visibility_matrix(visibility_matrix):
    fig, ax = plt.subplots()
    plt.imshow(visibility_matrix, cmap='inferno', interpolation='nearest', zorder=0)
    mask = np.matrix(map_matrix)
    mask = np.ma.masked_where(mask == SPACE_TILE, mask)
    plt.imshow(mask, cmap='binary', zorder=1)
    plt.gca().invert_yaxis()
    
if __name__ == "__main__":
    genome = ABGenome.create_random_genome()
    phenotype = genome.phenotype()
    map_matrix = phenotype.map_matrix()

    #start_time = time.time()
    #graph = create_visibility_graph(map_matrix, bresenham_line)
    #visibility_map = create_visibility_matrix(graph, map_matrix, 0, 0)
#
#
    #show_visibility_matrix(visibility_map)
    #print(f"Time elapsed breshnam: {time.time() - start_time}")

    start_time = time.time()
    graph = create_visibility_graph(map_matrix, DDA)
    visibility_map = create_visibility_matrix(graph, map_matrix, 0, 0)

    show_visibility_matrix(visibility_map)
    print(f"Showed in: {time.time() - start_time}")

    # Test local maxima
    masked_heatmap = re.__mask_heatmap(visibility_map, map_matrix, invert = False)
    lm = re.__get_heatmap_local_maxima(masked_heatmap)
    for i in range(len(lm)):
        x, y = lm[i]
        visibility_map[x][y] = 50000
    show_visibility_matrix(visibility_map)


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

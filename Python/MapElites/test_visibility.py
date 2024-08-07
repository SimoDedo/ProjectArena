from z3 import *
from internals.ab_genome.ab_genome import ABGenome
from internals.graph_genome.gg_genome import GraphGenome
from internals.smt_genome.smt_genome import SMTGenome
import matplotlib.pyplot as plt
import numpy as np
import igraph as ig
from matplotlib import cm

def bresenham_line(x1, y1, x2, y2):
    points = []
    dx = abs(x2 - x1)
    dy = abs(y2 - y1)
    sx = 1 if x1 < x2 else -1
    sy = 1 if y1 < y2 else -1
    err = dx - dy
    
    while True:
        points.append((x1, y1))
        if x1 == x2 and y1 == y2:
            break
        e2 = err * 2
        if e2 > -dy:
            err -= dy
            x1 += sx
        if e2 < dx:
            err += dx
            y1 += sy
            
    return points

def is_path_clear(matrix, start, end):
    x1, y1 = start
    x2, y2 = end
    
    line_points = bresenham_line(x1, y1, x2, y2)
    print(line_points)
    
    for (x, y) in line_points:
        if matrix[x][y] == 0:
            return False
    return True


if __name__ == "__main__":
    genome = ABGenome.create_random_genome()
    phenotype = genome.phenotype()
    map_matrix = phenotype.map_matrix()

    # Example usage:

    start = (0, 0)
    end = (3, 2)

    result = is_path_clear(map_matrix, start, end)
    print("Path clear:", result)
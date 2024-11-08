from internals.ab_genome.ab_genome import ABGenome
from internals.graph_genome.gg_genome import GraphGenome
from internals.smt_genome.smt_genome import SMTGenome
import matplotlib.pyplot as plt
import numpy as np
import igraph as ig
import pyvoronoi
from matplotlib import cm
from matplotlib import collections  as mc
from shapely import Polygon, Point, distance, transform


# Note that vertices of polygons have their x and y coordinates inverted with respect to the map matrix
# This is to keep consistency with how the maps are displayed in the visualization, which is with the x and y axes inverted.
def get_obstacles_polygons(map, invert_xy=True):
    m = len(map)
    n = len(map[0])

    # Flood fill the outer wall
    tile_type = 2
    flood_fill(map, 0, 0, 0, tile_type)

    # Get the vertices of the outer polygon
    outer_vertices = get_polygon_vertices(map, 1)
    # Invert x and y of the vertices
    for i in range(len(outer_vertices)):
        outer_vertices[i] = [outer_vertices[i][1], outer_vertices[i][0]]
    outer = Polygon(outer_vertices)
    
    obstacles = []
    for row in range(m):
        for col in range(n):
            if map[row][col] == 0:
                # Flood fill the inner polygon
                tile_type += 1
                flood_fill(map, row, col, 0, tile_type)
                # Get the vertices of the inner polygon
                vertices_inner = get_polygon_vertices(map, tile_type)
                # Invert x and y of the vertices
                for i in range(len(vertices_inner)):
                    vertices_inner[i] = [vertices_inner[i][1], vertices_inner[i][0]]
                obstacles.append(Polygon(vertices_inner))

    return outer, obstacles

def get_polygon_vertices(map, tile_type):
    RIGHT = 0
    DOWN = 1
    LEFT = 2
    UP = 3
    directions = [(0, 1), (1, 0), (0, -1), (-1, 0)]
    dir_to_check = [UP, RIGHT, DOWN, LEFT]
    direction = None

    vertices = []
    first_vertex = None
    curr_point = None

    # Find the first vertex, top left corner
    for row in range(len(map)):
        for col in range(len(map[0])):
            if map[row][col] == tile_type:
                first_vertex = [row, col]
                vertices.append([row, col])
                break
        else:
            continue
        break

    # Start moving around the border. We must have either move right or down, otherwise it wouldn't be the leftmost top vertex
    # Note that we are never at the border, so we can always move right or down.
    if map[first_vertex[0]][first_vertex[1]+1] == tile_type:
        curr_point = [first_vertex[0], first_vertex[1]+1]
        direction = RIGHT
    elif map[first_vertex[0]+1][first_vertex[1]] == tile_type:
        curr_point = [first_vertex[0]+1, first_vertex[1]]
        direction = DOWN
    else:
        # We have a single tile.
        return vertices

    # Walk around the border
    while curr_point != first_vertex:
        dir_vec = directions[direction]
        next = map[curr_point[0] + dir_vec[0]][curr_point[1] + dir_vec[1]]

        check_vec = directions[dir_to_check[direction]]         
        check = map[curr_point[0] + check_vec[0]][curr_point[1] + check_vec[1]]
        
        if check == tile_type:
            vertices.append(curr_point)
            direction = (direction - 1) % 4
            dir_vec = directions[direction]
            curr_point = [curr_point[0] + dir_vec[0], curr_point[1] + dir_vec[1]]
        elif next != tile_type: 
            vertices.append(curr_point)
            direction = (direction + 1) % 4
            dir_vec = directions[direction]
            curr_point = [curr_point[0] + dir_vec[0], curr_point[1] + dir_vec[1]]
        else:
            curr_point = [curr_point[0] + dir_vec[0], curr_point[1] + dir_vec[1]]
    return vertices

def is_valid(map, m, n, x, y, prev_tile, new_tile):
    if x<0 or x>= m\
       or y<0 or y>= n or\
         map[x][y]!= prev_tile or\
            map[x][y]== new_tile:
        return False
    return True



# FloodFill function
def flood_fill(screen,  
            x,  
            y, 
            prev_tile, new_tile):
    m = len(screen)
    n = len(screen[0])
    queue = []
     
    # Append the position of starting 
    # pixel of the component
    queue.append([x, y])
 
    # Color the pixel with the new color
    screen[x][y] = new_tile
 
    # While the queue is not empty i.e. the 
    # whole component having prevC color 
    # is not colored with newC color
    while queue:
         
        # Dequeue the front node
        currPixel = queue.pop()
         
        posX = currPixel[0]
        posY = currPixel[1]
        
        # Check if the adjacent
        # pixels are valid
        if is_valid(screen, m, n,  
                posX + 1, posY, prev_tile, new_tile):
             
            # Color with newC
            # if valid and enqueue
            screen[posX + 1][posY] = new_tile
            queue.append([posX + 1, posY])
         
        if is_valid(screen, m, n,  
                    posX-1, posY, prev_tile, new_tile):
            screen[posX-1][posY]= new_tile
            queue.append([posX-1, posY])
         
        if is_valid(screen, m, n,  
                posX, posY + 1, prev_tile, new_tile):
            screen[posX][posY + 1]= new_tile
            queue.append([posX, posY + 1])
         
        if is_valid(screen, m, n,  
                    posX, posY-1, prev_tile, new_tile):
            screen[posX][posY-1]= new_tile
            queue.append([posX, posY-1])

def upscale_matrix(matrix, scale_factor):
    m = matrix.shape[0]
    n = matrix.shape[1]
    new_size_m = m * scale_factor
    new_size_n = n * scale_factor
    new_matrix = np.zeros((new_size_m, new_size_n), dtype=int)
    
    for i in range(m):
        for j in range(n):
            new_matrix[i*scale_factor:(i+1)*scale_factor, j*scale_factor:(j+1)*scale_factor] = matrix[i, j]
    
    return new_matrix


def create_2d_segment_vornoi_diagram(outer_shell, obstacles, map_matrix):
    pv = pyvoronoi.Pyvoronoi(100)
    
    outer_segments = []
    for i in range(len(outer_shell.exterior.coords)-1):
        outer_segments.append([list(outer_shell.exterior.coords[i]), list(outer_shell.exterior.coords[i+1])])
    obstacle_segments = []
    for obstacle in obstacles:
        for i in range(len(obstacle.exterior.coords)-1):
            obstacle_segments.append([list(obstacle.exterior.coords[i]), list(obstacle.exterior.coords[i+1])])        

    for segment in outer_segments:
        pv.AddSegment(segment)
    for segment in obstacle_segments:
        pv.AddSegment(segment)

    pv.Construct()
    return pv

def filter_vornoi_edges(pv, outer_shell, obstacles):
    edges = pv.GetEdges()
    vertices = pv.GetVertices()

    # Filter out edges that are not primary, or that are not inside the outer wall, or that are inside an obstacle (within a small epsilon of it)
    actual_edges = []
    epsilon = 1e-15
    for edge in edges:
        if edge.start != -1 and\
            edge.end != -1 and\
            edge.is_primary and\
            outer_shell.contains(Point(vertices[edge.start].X, vertices[edge.start].Y)) and\
            outer_shell.contains(Point(vertices[edge.end].X, vertices[edge.end].Y)) and\
            not any(Point(vertices[edge.start].X, vertices[edge.start].Y).distance(obstacle) < epsilon or Point(vertices[edge.end].X, vertices[edge.end].Y).distance(obstacle) < epsilon for obstacle in obstacles):
                actual_edges.append(edge)

    
    # Create the graph from the edges
    
    # Get the vertices indexes actually to be used in the graph. These are all the vertices that are part of the actual edges.
    actual_vertices_idxs = sorted(list(set([edge.start for edge in actual_edges] + [edge.end for edge in actual_edges])))

    # Create edges that use the vertices position in the actual vertices, instead of their index
    actual_edges = [(actual_vertices_idxs.index(edge.start), actual_vertices_idxs.index(edge.end)) for edge in actual_edges]

    return actual_vertices_idxs, actual_edges

def create_graph_from_voronoi_diagram(vertices, actual_vertices_idxs, actual_edges):

    # Create the graph
    graph = ig.Graph(n=len(actual_vertices_idxs), edges=actual_edges, directed=False)
    graph.simplify()
    graph.vs['coords'] = [(vertices[actual_vertices_idxs[i]].X, vertices[actual_vertices_idxs[i]].Y) for i in range(len(actual_vertices_idxs))]

    return graph

def prune_graph(graph):
    while True:
        leafs = [v.index for v in graph.vs if len(v.neighbors()) == 1]
        leafs = [v for v in leafs if graph.vs[v]['radius'] < graph.vs[graph.vs[v].neighbors()[0].index]['radius']]
        if len(leafs) == 0:
            break
        graph.delete_vertices(leafs)

def identify_regions(graph, radius_region_threshold):
    region_nodes = [False for i in range(len(graph.vs))]
    for i in range(len(graph.vs)):
        degree = len(graph.vs[i].neighbors())
        if degree > 2:
            region_nodes[i] = True
        elif degree == 1:
            region_nodes[i] = True
        else:
            nodes_in_radius = [v.index for v in graph.vs if v.index != i and distance(Point(graph.vs[i]['coords'][0], graph.vs[i]['coords'][1]), Point(v['coords'][0], v['coords'][1])) < graph.vs[i]['radius']]
            if graph.vs[i]['radius'] > radius_region_threshold and all([graph.vs[i]['radius'] > graph.vs[n]['radius'] for n in nodes_in_radius]):
                region_nodes[i] = True
    graph.vs['region'] = region_nodes


def identify_chokepoints(graph, remove_non_chokepoints=False):
    added_edges = []
    removed_nodes = []

    visited = [False for i in range(len(graph.vs))]
    is_chockepoint = [False for i in range(len(graph.vs))]
    
    region_nodes = [v.index for v in graph.vs if v['region']]
    for r in region_nodes:
        neighbors = [v.index for v in graph.vs[r].neighbors()]
        path = [r]
        for n in neighbors:
            if visited[n]:
                continue
            # Walk to the next region node
            next = n
            while graph.vs[next]['region'] == False:
                visited[next] = True
                path.append(next)
                next = [v.index for v in graph.vs[next].neighbors() if v.index != path[-2]][0]

            if len(path) > 1:
                path.pop(0) # Remove region node, chokepoint will be one non region node
                min_radius = min([graph.vs[i]['radius'] for i in path])
                chokepoints = [i for i in path if graph.vs[i]['radius'] == min_radius]
                chokepoint = chokepoints[int(len(chokepoints)/2)]

                added_edges.append((r, chokepoint))
                added_edges.append((chokepoint, next))
                removed_nodes += [i for i in path if i != chokepoint]
                is_chockepoint[chokepoint] = True

                path = [r]
        visited[r] = True
    
    if remove_non_chokepoints:
        graph.add_edges(added_edges)
        graph.delete_vertices(removed_nodes)
        graph.simplify()
    graph.vs['chokepoint'] = is_chockepoint

def merge_adjacent_regions_no_chokepoint(graph):
    # First, we merge regions that are connected by a single edge, with no chokepoint in between
    merge_groups = []
    region_nodes = [v for v in graph.vs if v['region']]
    region_nodes = [v.index for v in region_nodes]
    visited = [False for i in range(len(region_nodes))]

    # Find groups of directly connected regions to be merged
    for r in region_nodes:
        if visited[region_nodes.index(r)]:
            continue
        path = [r]
        visited[region_nodes.index(r)] = True
        neighbors = [v.index for v in graph.vs[r].neighbors() if v['region'] and not visited[region_nodes.index(v.index)]]
        while len(neighbors) > 0:
            new_neighbors = []
            for n in neighbors:
                path.append(n)
                visited[region_nodes.index(n)] = True
                new_neighbors += [v.index for v in graph.vs[n].neighbors() if v['region'] and not visited[region_nodes.index(v.index)]]                
            neighbors = new_neighbors
        if len(path) > 1:
            merge_groups.append(path)
    # Sort each merge group by decreasing radius
    merge_groups = [[i for i in sorted(g, key=lambda x: graph.vs[x]['radius'], reverse=True)] for g in merge_groups]
    nodes_to_delete = []
    edges_to_add = []
    for group in merge_groups:
        keep = group[0]
        delete = group[1:]
        nodes_to_delete += delete
        for d in delete:
            neighbors = [v.index for v in graph.vs[d].neighbors() if d != keep]
            for n in neighbors:
                edges_to_add.append((keep, n))
    graph.add_edges(edges_to_add)
    graph.delete_vertices(nodes_to_delete)
    graph.simplify()

def merge_adjacent_regions_first_criterion(graph, r_smaller_coeff, r_bigger_coeff):
    #First, two regions are merged if the radius of the choke point connecting them is larger 
    # than 90% of the radius of the smaller region node, or larger than 85% of the radius of the larger region node
    nodes_to_delete = None
    edges_to_add = []
    while nodes_to_delete is None or len(nodes_to_delete) > 0:
        nodes_to_delete = []
        edges_to_add = []

        chokepoint_nodes = [v for v in graph.vs if v['chokepoint']]
        # Sort the chokepoint nodes by decreasing radius
        chokepoint_nodes = sorted(chokepoint_nodes, key=lambda x: x['radius'], reverse=True)
        chokepoint_nodes = [v.index for v in chokepoint_nodes]
        for c in chokepoint_nodes:
            path = [c]
            neighbors = [v.index for v in graph.vs[c].neighbors()]
            n1 = neighbors[0]
            while not graph.vs[n1]['region']:
                path.append(n1)
                n1 = [v.index for v in graph.vs[n1].neighbors() if v.index not in path][0]
            r1 = n1
            n2 = neighbors[1]
            while not graph.vs[n2]['region']:
                path.append(n2)
                n2 = [v.index for v in graph.vs[n2].neighbors() if v.index not in path][0]
            r2 = n2

            r_greater = r1 if graph.vs[r1]['radius'] > graph.vs[r2]['radius'] else r2
            r_smaller = r1 if graph.vs[r1]['radius'] <= graph.vs[r2]['radius'] else r2
            path.append(r_smaller)

            if graph.vs[c]['radius'] > r_smaller_coeff * graph.vs[r_smaller]['radius'] or graph.vs[c]['radius'] > r_bigger_coeff * graph.vs[r_greater]['radius']:
                nodes_to_delete += path
                neighbors_r_smaller = [v.index for v in graph.vs[r_smaller].neighbors() if v.index not in path]
                for n in neighbors_r_smaller:
                    edges_to_add.append((r_greater, n))
                nodes_to_delete = path
            
            if len(nodes_to_delete) > 0:
                break
            
        graph.add_edges(edges_to_add)
        graph.delete_vertices(nodes_to_delete)
        graph.simplify()

def merge_adjacent_regions_second_criterion(graph, coeff):
    # The second criterion applies specifically in the case where one of the regions has exactly two choke points. 
    # For a region with two choke points, the region is merged with the adjacent region that is connected to the larger 
    # of the two choke points if the radius of the larger choke point is greater than 70% of radius of the original region node.
    nodes_to_delete = None
    edges_to_add = []
    while nodes_to_delete is None or len(nodes_to_delete) > 0:
        nodes_to_delete = []
        edges_to_add = []

        region_nodes = [v.index for v in graph.vs if v['region']] 
        if len(region_nodes) < 2:
            break
        region_nodes_two_chokepoints = [v.index for v in graph.vs if v['region'] if len(v.neighbors()) == 2]
        for r in region_nodes_two_chokepoints:
            neighbors = [v.index for v in graph.vs[r].neighbors()]
            
            n1 = neighbors[0]
            path1 = []
            while not graph.vs[n1]['region']:
                path1.append(n1)
                if graph.vs[n1]['chokepoint']:
                    c1 = n1
                n1 = [v.index for v in graph.vs[n1].neighbors() if v.index not in path1 and v.index != r][0]
            r1 = n1

            n2 = neighbors[1]
            path2 = []
            while not graph.vs[n2]['region']:
                path2.append(n2)
                if graph.vs[n2]['chokepoint']:
                    c2 = n2
                n2 = [v.index for v in graph.vs[n2].neighbors() if v.index not in path2 and v.index != r][0]
            r2 = n2

            c_greater = c1 if graph.vs[c1]['radius'] > graph.vs[c2]['radius'] else c2
            path_greater = path1 if c1 == c_greater else path2
            r_merge = r1 if c1 == c_greater else r2

            if graph.vs[c_greater]['radius'] > coeff * graph.vs[r]['radius']:
                r_greater = r if graph.vs[r]['radius'] > graph.vs[r_merge]['radius'] else r_merge
                r_smaller = r if graph.vs[r]['radius'] <= graph.vs[r_merge]['radius'] else r_merge
                path_greater.append(r_smaller)
                nodes_to_delete = path_greater
                neighbors_r_smaller = [v.index for v in graph.vs[r_smaller].neighbors() if v.index not in path_greater]
                for n in neighbors_r_smaller:
                    edges_to_add.append((r_greater, n))
            
            if len(nodes_to_delete) > 0:
                break
        graph.add_edges(edges_to_add)
        graph.delete_vertices(nodes_to_delete)
        graph.simplify()

def identify_dead_ends(graph, radius_dead_end_threshold):
    dead_end_nodes = [False for i in range(len(graph.vs))]
    for i in range(len(graph.vs)):
        if len(graph.vs[i].neighbors()) == 1:
            if graph.vs[i]['radius'] < radius_dead_end_threshold:
                dead_end_nodes[i] = True
    graph.vs['dead_end'] = dead_end_nodes
    
def restore_scale_polygons(outer_shell, obstacles):
    new_outer_shell = Polygon([[(outer_shell.exterior.coords[i][0]/2) -1, (outer_shell.exterior.coords[i][1]/2) -1] for i in range(len(outer_shell.exterior.coords)-1)])
    new_obstacles = [Polygon([[(obstacle.exterior.coords[i][0]/2) - 1, (obstacle.exterior.coords[i][1]/2) -1] for i in range(len(obstacle.exterior.coords)-1)]) for obstacle in obstacles]
    return new_outer_shell, new_obstacles

def restore_scale_graph(graph):
    new_coords = [[(graph.vs[i]['coords'][0]/2)-1, (graph.vs[i]['coords'][1]/2)-1] for i in range(len(graph.vs))]
    new_radius = [graph.vs[i]['radius']/2 for i in range(len(graph.vs))]       
    return new_coords, new_radius

def plot_map(map_matrix):
    fig, ax = plt.subplots()
    plt.imshow(map_matrix, cmap=cm.Greys, alpha=1.0)
    plt.axis('off')

def plot_vornoi_diagram(outer_shell, obstacles, pv, actual_vertices_idx, actual_edges):
    fig, ax = plt.subplots()

    # Plot the outer wall
    x,y = outer_shell.exterior.xy
    plt.plot(x,y)
    # Fill the ouside of the outer wall
    x, y = outer_shell.exterior.xy
    plt.fill([0, 0, map_matrix_upscaled.shape[1], map_matrix_upscaled.shape[1]], 
             [0, map_matrix_upscaled.shape[0], map_matrix_upscaled.shape[0], 0], 
             color='lightgray')
    plt.fill(x, y, color='white')

    # Plot the obstacles
    for obstacle in obstacles:
        x,y = obstacle.exterior.xy
        plt.plot(x, y, color='darkred')
        plt.fill(x, y, color='darkred', alpha=0.5)

    # Plot the voronoi diagram
    lines = [[(pv.GetVertices()[actual_vertices_idx[edge[0]]].X, pv.GetVertices()[actual_vertices_idx[edge[0]]].Y), (pv.GetVertices()[actual_vertices_idx[edge[1]]].X, pv.GetVertices()[actual_vertices_idx[edge[1]]].Y)] for edge in actual_edges]
    lc = mc.LineCollection(lines, colors='blue', linewidths=1)
    ax.add_collection(lc)

    ax.set_xlim(0, map_matrix_upscaled.shape[1])
    ax.set_ylim(0, map_matrix_upscaled.shape[0])
    plt.axis('off')
    plt.gca().invert_yaxis()

def plot_vornoi_graph(outer_shell, obstacles, graph, x_limit, y_limit):
    fig, ax = plt.subplots()

    # Plot the outer wall
    x,y = outer_shell.exterior.xy
    plt.plot(x,y)
    # Fill the ouside of the outer wall
    x, y = outer_shell.exterior.xy
    plt.fill([0, 0, x_limit, x_limit],
                [0, y_limit, y_limit, 0],
                color='lightgray', zorder=-10)
    plt.fill(x, y, color='white', zorder=-10)



    # Plot the obstacles
    for obstacle in obstacles:
        x,y = obstacle.exterior.xy
        plt.plot(x,y, color='darkred')
        plt.fill(x, y, color='darkred', alpha=0.5)

    # Plot the graph
    #graph.vs['label'] = [str(i) for i in range(len(graph.vs))]
    vertex_attr_names = graph.vertex_attributes()
    color = []
    for i in range(len(graph.vs)):
        if 'chokepoint' in vertex_attr_names and graph.vs[i]['chokepoint']:
            color.append('yellow')
        #elif 'dead_end' in vertex_attr_names and graph.vs[i]['dead_end']:
        #    color.append('green')
        elif 'region' in vertex_attr_names and graph.vs[i]['region']:
            color.append('red')
        else:
            color.append('blue')
    graph.vs['color'] = color

    layout = ig.Layout(coords=graph.vs['coords'], dim=2)
    ig.plot(graph, vertex_size=5, layout=layout, vertex_label_color="black",  target=ax, zorder=10)
    ax.set_xlim(0, x_limit)
    ax.set_ylim(0, y_limit)
    plt.axis('off')
    plt.gca().invert_yaxis()

if __name__ == "__main__":
    # Create random genome 
    genome = ABGenome.create_random_genome()
    phenotype = genome.phenotype()
    phenotype.simplify()
    map_matrix = phenotype.map_matrix()

    #map_matrix = [
    #    [0, 0, 1, 1, 1, 1, 1, 1, 1, 1],
    #    [0, 1, 1, 0, 0, 0, 0, 0, 0, 1],
    #    [1, 1, 0, 1, 1, 1, 0, 0, 0, 1],
    #    [1, 1, 0, 1, 0, 0, 1, 0, 0, 1],
    #    [1, 1, 1, 1, 1, 1, 1, 1, 1, 1],
    #]
    map_matrix = np.array(map_matrix)

    plot_map(phenotype.map_matrix(inverted=True))

    # Add a wall around the map of size 1 to ensure walkable areas are not at the border
    map_matrix_upscaled = np.pad(map_matrix, 1, mode='constant', constant_values=0)
    # Upscale the matrix by a factor of 2 to avoid having corridors of width 1, which would cause polygons with less than 4 vertices.
    # Also avoids corridors of width 1 which would result in a polygon that is actually not connected, since it has a lines connecting multiple parts.
    map_matrix_upscaled = upscale_matrix(map_matrix_upscaled, 2)

    outer_shell, obstacles = get_obstacles_polygons(map_matrix_upscaled)
    # Outer shell is a polygon in the shape of the outer border of the walkable map.
    # In order to be able to compute the radius (since all points are inside outer shell, so their distance to the outer wall is 0), 
    # we need to create a polygon with a hole in the shape of the outer wall.
    outer_hole = Polygon(
        shell=[[0,0], [0, map_matrix_upscaled.shape[0]], [map_matrix_upscaled.shape[1], map_matrix_upscaled.shape[0]], [map_matrix_upscaled.shape[1], 0], [0,0]], 
        holes=[[list(outer_shell.exterior.coords[i]) for i in range(len(outer_shell.exterior.coords)-1)]]
    )
    all_polygons = [outer_hole] + obstacles

    # Enlarge the obstacles so that we avoid "gaps" bewteen the obstacles and the outer wall connected by only the edges of the obstacles. 
    # This is needed because we are tracing our polygons "at the center" of tiles in the matrix. Then we also move the polygons to the center of the tiles.
    outer_shell = transform(outer_shell.buffer(0.5,join_style=2), lambda x: x+0.5)
    outer_hole = transform(outer_hole.buffer(0.5,join_style=2), lambda x: x+0.5)
    obstacles = [transform(obstacle.buffer(0.5,join_style=2), lambda x: x+0.5) for obstacle in obstacles]       

    # Create the segment voronoi diagram
    pv = create_2d_segment_vornoi_diagram(outer_shell, obstacles, map_matrix_upscaled)

    # Create the graph from the voronoi diagram
    actual_vertices_idxs, actual_edges = filter_vornoi_edges(pv, outer_shell, obstacles)
    graph = create_graph_from_voronoi_diagram(pv.GetVertices(), actual_vertices_idxs, actual_edges)

    plot_vornoi_diagram(outer_shell, obstacles, pv, [], [])
    plot_vornoi_diagram(outer_shell, obstacles, pv, actual_vertices_idxs, actual_edges)

    # Calculate the radius of each vertex
    graph.vs['radius'] = [min([distance(Point(v['coords'][0], v['coords'][1]), p) for p in all_polygons]) for v in graph.vs]

    # Prune the graph by removing leafs with a radius smaller than the parent's
    prune_graph(graph)
    plot_vornoi_graph(outer_shell, obstacles, graph, len(map_matrix_upscaled[0]), len(map_matrix_upscaled))

    # Identify region nodes
    RADIUS_REGION_THRESHOLD = 4.0
    identify_regions(graph, RADIUS_REGION_THRESHOLD)
    

    # Identify chokepoints nodes (and optionally simplify the graph)
    identify_chokepoints(graph)
    plot_vornoi_graph(outer_shell, obstacles, graph, len(map_matrix_upscaled[0]), len(map_matrix_upscaled))

    # Merge adjacent regions
    merge_adjacent_regions_no_chokepoint(graph)
    merge_adjacent_regions_first_criterion(graph,0.9,0.85)
    merge_adjacent_regions_second_criterion(graph,0.7)


    # Recognize dead ends and add them to the vertices
    RADIUS_DEAD_END_THRESHOLD = 3.0
    identify_dead_ends(graph, RADIUS_DEAD_END_THRESHOLD)

    #PLOT

    # Restore coordinates and radiuses to the original scale
    outer_shell, obstacles = restore_scale_polygons(outer_shell, obstacles)
    graph.vs['coords'], graph.vs['radius'] = restore_scale_graph(graph)

    plot_vornoi_graph(outer_shell, obstacles, graph, len(map_matrix[0]), len(map_matrix))

    density = graph.density(loops=True)
    print("Density of the graph: " + str(density))

    plt.show()




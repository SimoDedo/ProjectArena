from z3 import *
from internals.smt_genome.constants import SMT_ROOMS_NUMBER, SMT_MAX_MAP_WIDTH, SMT_MAX_MAP_HEIGHT, SMT_MAX_ROOM_HEIGHT, SMT_MIN_ROOM_HEIGHT, SMT_LINES_NUMBER \
    , SMT_MAX_ROOM_WIDTH, SMT_MIN_ROOM_WIDTH, GENOME_SCALE_FOR_SMT_SOLVER
import time
from scipy.spatial import Delaunay
from scipy.sparse.csgraph import minimum_spanning_tree
import numpy as np


rooms = []
actual_number_of_rooms = 0

timing_info = {}
accumulated_timing_info = {}
timing_per_run = {}

SCALE_FACTOR = 1

ROOM_WIDTH_MIN = SMT_MIN_ROOM_WIDTH * GENOME_SCALE_FOR_SMT_SOLVER
ROOM_WIDTH_MAX = SMT_MAX_ROOM_WIDTH * GENOME_SCALE_FOR_SMT_SOLVER
ROOM_HEIGHT_MIN = SMT_MIN_ROOM_HEIGHT * GENOME_SCALE_FOR_SMT_SOLVER * SCALE_FACTOR
ROOM_HEIGHT_MAX = SMT_MAX_ROOM_HEIGHT * GENOME_SCALE_FOR_SMT_SOLVER* SCALE_FACTOR

CANVAS_WIDTH = SMT_MAX_MAP_WIDTH * GENOME_SCALE_FOR_SMT_SOLVER
CANVAS_HEIGHT = SMT_MAX_MAP_HEIGHT * GENOME_SCALE_FOR_SMT_SOLVER

BORDER = 0
LINEWIDTH = 10  
LINEWIDTH_Y = LINEWIDTH * SCALE_FACTOR

SEPARATION = 1
SEPARATION_Y = SEPARATION * SCALE_FACTOR


def update_timing():
    global timing_info
    global accumulated_timing_info

    if not accumulated_timing_info:
        #print("Initializing accumulated_timing_info")
        for timing in timing_info:
            accumulated_timing_info[timing] = []

    for timing in timing_info:
        accumulated_timing_info[timing].append(timing_info[timing])

def init_rooms(genome_rooms):
    global rooms, actual_number_of_rooms
    rooms = []
    for i in range(SMT_ROOMS_NUMBER):
        if genome_rooms[i] is not None:
            r = {'x': Int('room_{}_x'.format(i)), 'y': Int('room_{}_y'.format(i)),
                 'width': None, 'height': None, 'quad': None}
            r['width'] = genome_rooms[i].width
            r['height'] = genome_rooms[i].height
            r['quad'] = 1
            rooms.append(r)
    actual_number_of_rooms = len(rooms)

def create_canvas_constraints(slv):
    global and_clause_count, or_clause_count
    for i in range(actual_number_of_rooms):
        slv.add(rooms[i]['x'] >= 0, rooms[i]['x'] + rooms[i]['width'] <= CANVAS_WIDTH)
        slv.add(rooms[i]['y'] >= 0, rooms[i]['y'] + rooms[i]['height'] <= CANVAS_HEIGHT * SCALE_FACTOR)
        and_clause_count = and_clause_count + 4
        or_clause_count = or_clause_count + 0

def create_separation_constraints(slv):
    for i in range(actual_number_of_rooms):
        for j in range(i+1, actual_number_of_rooms):
            add_separation_constraint(slv, i, j)

def add_separation_constraint(slv, i, j):
    global and_clause_count, or_clause_count
    vert_cond = {"above": "rooms[j]['y'] <= (rooms[i]['y'] - rooms[j]['height'] - SEPARATION_Y)",
#                 "same": "rooms[j]['y'] == rooms[i]['y']",
                 "same": None,
                 "below": "rooms[i]['y'] <= (rooms[j]['y'] - rooms[i]['height'] - SEPARATION_Y)" }

    horiz_cond = {"left": "rooms[j]['x'] <= (rooms[i]['x'] - rooms[j]['width'] - SEPARATION)",
#                  "same": "rooms[j]['x'] == rooms[i]['x']",
                  "same": None,
                  "right": "rooms[i]['x'] <= (rooms[j]['x'] - rooms[i]['width'] - SEPARATION)" }

    constraint = "Or("

    # for dir in directions:
    #     if vert_cond[dir['vert']] is not None and horiz_cond[dir['horiz']] is not None:
    #         constraint += "And(" + vert_cond[dir['vert']] + ", " + horiz_cond[dir['horiz']] + "),\n"
    #     if vert_cond[dir['vert']] is None and horiz_cond[dir['horiz']] is not None:
    #         constraint += horiz_cond[dir['horiz']] + ",\n"
    #     if vert_cond[dir['vert']] is not None and horiz_cond[dir['horiz']] is None:
    #         constraint += vert_cond[dir['vert']] + ",\n"
    constraint += vert_cond['above'] + ",\n" + vert_cond['below'] + ",\n" + horiz_cond['left'] + ",\n" + horiz_cond['right'] + ",\n"

    constraint = constraint[:-2]
    constraint += "\n)"
    slv.add(eval(constraint))
    and_clause_count = and_clause_count + 1
    or_clause_count = or_clause_count + 4

def create_lines_constraints(slv, genome_lines):
    """ Add a series of linear constraints following lines created by mousepoints """
    lines = []
    for i in range(SMT_LINES_NUMBER):
        if genome_lines[i] is None:
            continue
        x1 = (genome_lines[i].start[0] - BORDER)
        y1 = (genome_lines[i].start[1] - BORDER) * SCALE_FACTOR
        x2 = (genome_lines[i].end[0] - BORDER)
        y2 = (genome_lines[i].end[1] - BORDER) * SCALE_FACTOR

        m_num = (y2 - y1)
        if (x2-x1) == 0:
            m_den = 1
        else:
            m_den = (x2 - x1)

        l_info = {'m_num': m_num, 'm_den': m_den, 'm': m_num/m_den, 'y1': y1, 'y2': y2, 'x1': x1, 'x2': x2}
        lines.append(l_info)
    if len(lines) > 0:
        create_point_line_constraints(slv, lines)


def create_point_line_constraints(slv, lines):
    global  and_clause_count, or_clause_count

    for i in range(actual_number_of_rooms):
        constraint = "Or("
        for line in lines:
            # Separating numerator and denominator of slope causes significant slowdown, due to division
            #constraint += "((rooms[i]['y'] - " + str(line['y2']) +") == (" + str(line['m_num']) + " * (rooms[i]['x'] - " \
            #               + str(line['x2']) + ")) / " + str(line['m_den']) + "),\n"

            if line['m'] > 0:
                constraint += "And((rooms[i]['y'] <= " + str(line['m']) + "* (rooms[i]['x'] - " \
                               + str(line['x2']) + "+" + str(LINEWIDTH) + ") +" + str(line['y2']) + "),\n"
                constraint += "(rooms[i]['y'] >= " + str(line['m']) + "* (rooms[i]['x'] - " \
                               + str(line['x2']) + "-" + str(LINEWIDTH) + "+" + str(rooms[i]['width']) + ")+" + str(line['y2']) + "),\n"
            else:
                constraint += "And((rooms[i]['y'] >= " + str(line['m']) + "* (rooms[i]['x'] - " \
                               + str(line['x2']) + "+" + str(LINEWIDTH) + ") +" + str(line['y2']) + "),\n"
                constraint += "(rooms[i]['y'] <= " + str(line['m']) + "* (rooms[i]['x'] - " \
                               + str(line['x2']) + "-" + str(LINEWIDTH) + "+" + str(rooms[i]['width']) + ")+" + str(line['y2']) + "),\n"


            if line['y2'] > line['y1']:
                high_y = line['y2']
                low_y = line['y1']
            else:
                high_y = line['y1']
                low_y = line['y2']

            # check to see if y-height range is too small. If so, use x range instead
            if high_y - rooms[i]['height'] > low_y:
                # y range is fine
                constraint += "(rooms[i]['y'] >= " + str(low_y) + "),\n"
                constraint += "(rooms[i]['y'] <= " + str(high_y) + "-" + str(rooms[i]['height']) + ")),\n"
            else:
                # use x range
                if line['x2'] > line['x1']:
                    high_x = line['x2']
                    low_x = line['x1']
                else:
                    high_x = line['x1']
                    low_x = line['x2']
                constraint += "(rooms[i]['x'] >= " + str(low_x) + "),\n"
                constraint += "(rooms[i]['x'] <= " + str(high_x) + "-" + str(rooms[i]['width']) + ")),\n"

            and_clause_count = and_clause_count + 4
            or_clause_count = or_clause_count + 1
        
        constraint = constraint[:-2]
        constraint += "\n)"
        #print("Room: {}  Constraint: \n{}\n\n".format(i,constraint))
        slv.add(eval(constraint))

def init_all_constraints(slv, lines=None):
    global timing_info
    global and_clause_count, or_clause_count

    and_clause_count = 0
    or_clause_count = 0
    begin = time.perf_counter()
    all_begin = begin
    create_canvas_constraints(slv)
    end = time.perf_counter()
    timing_info['create_canvas_constraints'] = end-begin


    begin = time.perf_counter()
    create_separation_constraints(slv)
    end = time.perf_counter()
    timing_info['create_separation_constraints'] = end - begin


    if len(lines) >= 1:
        begin = time.perf_counter()
        create_lines_constraints(slv, lines)
        end = time.perf_counter()
        timing_info['create_control_line_constraints'] = end - begin

    all_end = time.perf_counter()
    timing_info['create_all_constraints'] = all_end - all_begin
    #print("======")
    #print("And clause count: {}".format(and_clause_count))
    #print("Or clause count: {}".format(or_clause_count))

def compute_room_centerpoints(m):
    cp = []

    for i in range(actual_number_of_rooms):
        rooms[i]['center_x'] = m[rooms[i]['x']].as_long() + (rooms[i]['width']/2)
        rooms[i]['center_y'] = m[rooms[i]['y']].as_long() + (rooms[i]['height']/2)
        cp.append((rooms[i]['center_x'], rooms[i]['center_y']))
        ##print("Centerpoint is: {}".format(cp))

    return cp


def distance(p1, p2):
    return math.sqrt(pow(p2[0]-p1[0], 2) + pow((p2[1]-p1[1])/SCALE_FACTOR, 2))

def create_graph_array(tri, cp):
    """ Given a Delaunay triangulation, creates a matrix form of this, with edge weights as lengths """
    graph = np.zeros((actual_number_of_rooms, actual_number_of_rooms))
    ##print("graph is: {}".format(graph))

    if tri is not None:
        for t in tri.simplices:
            graph[t[0]][t[1]] = distance(cp[t[0]], cp[t[1]])
            graph[t[1]][t[2]] = distance(cp[t[1]], cp[t[2]])
            graph[t[2]][t[0]] = distance(cp[t[2]], cp[t[0]])

    ##print("graph is: {}".format(graph))
    return graph

def solve(genome):
    global SEPARATION, SEPARATION_Y 
    SEPARATION = genome.separation
    SEPARATION_Y = SEPARATION * SCALE_FACTOR
    
    init_rooms(genome.rooms)
    
    solver = Solver()
    solver.set('random_seed', 0)
    solver.set(timeout=600)
    init_all_constraints(solver, genome.lines)
    
    begin = time.perf_counter()
    s = solver.check()
    end = time.perf_counter()
    #print(s)
    #print("Solve time: {} ms".format((end-begin)*1000.))
    
    if s.r == Z3_L_TRUE:
        begin = time.perf_counter()
        model = solver.model()
        center_points = compute_room_centerpoints(model)
        tri = Delaunay(center_points)
        end = time.perf_counter()
        timing_info['delaunay_time'] = end - begin
        
        begin = time.perf_counter()
        ar = create_graph_array(tri, center_points)
        mst = minimum_spanning_tree(ar)
        end = time.perf_counter()
        timing_info['mst_time'] = end - begin
        
        update_timing()
        
        rooms_positions = []
        for i in range(actual_number_of_rooms):
            rooms_positions.append((model[rooms[i]['x']].as_long(), model[rooms[i]['y']].as_long()/SCALE_FACTOR))
        
        return rooms_positions, mst
    else:
        #print(f"No solution found with {len([room for room in genome.rooms if room is not None])} rooms and {len([line for line in genome.lines if line is not None])} lines with separation {SEPARATION}")
        raise Exception("No solution found")
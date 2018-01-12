import os
import math
import threading
import networkx as nx
import matplotlib.pyplot as plt

# STRUCTS #
class Room:
    originX = None
    originY = None
    endX = None
    endY = None
    isCorridor = None

# FUNCTIONS #

# Gets the name of the map and get the files path.
def getFiles():
    inputAcquired = False
    text = input("\nInsert the MAPNAME value: ")
    while inputAcquired == False:
        mapFileName = text + "_map.txt"
        ABFileName = text + "_AB.txt"
        mapFilePath = "./" + inputDir + "/" + mapFileName
        ABFilePath = "./" + inputDir + "/" + ABFileName
        if os.path.isfile(mapFilePath) and os.path.isfile(ABFilePath):
            print("Files found.\n")
            inputAcquired = True
        else:
            text = input("Files not found. Insert the MAPNAME value: ")
    return mapFileName, ABFileName, mapFilePath, ABFilePath

# Reads the map.
def readMap(filePath):
    print("Reading the map file... ", end='')
    with open(filePath) as f:
        lines = [x.strip() for x in f.readlines()]
        map = [[lines[i][j] for i in range(len(lines[0]))] for j in range(len(lines))]
    print("Done.")
    return map

# Reads the AB file.
def readAB(filePath):
    print("Reading the AB file... ", end='')
    with open(filePath) as f:
        genome = f.readline()

        rooms = []
        currentValue = ""
        currentChar = 0

        while currentChar < len(genome) and genome[currentChar] == "<":
            room = Room()
            room.isCorridor = False
            currentChar = currentChar + 1

            # Get the x coordinate of the origin.
            while genome[currentChar].isdigit():
                currentValue = currentValue + genome[currentChar]
                currentChar = currentChar + 1
            room.originX = int(currentValue)

            currentValue = ""
            currentChar = currentChar + 1

            # Get the y coordinate of the origin.
            while genome[currentChar].isdigit():
                currentValue = currentValue + genome[currentChar]
                currentChar = currentChar + 1
            room.originY = int(currentValue)

            currentValue = ""
            currentChar = currentChar + 1

            # Get the size of the arena.
            while genome[currentChar].isdigit():
                currentValue = currentValue + genome[currentChar]
                currentChar = currentChar + 1
            room.endX = int(room.originX) + int(currentValue)
            room.endY = int(room.originY) + int(currentValue)
            rooms.append(room)

            currentValue = ""
            currentChar = currentChar + 1

        if currentChar < len(genome) and genome[currentChar] == "|":
            currentChar = currentChar + 1

            while (currentChar < len(genome) and genome[currentChar] == "<"):
                room = Room()
                room.isCorridor = True
                currentChar = currentChar + 1

                # Get the x coordinate of the origin.
                while genome[currentChar].isdigit():
                    currentValue = currentValue + genome[currentChar]
                    currentChar = currentChar + 1
                room.originX = int(currentValue)

                currentValue = ""
                currentChar = currentChar + 1

                # Get the y coordinate of the origin.
                while genome[currentChar].isdigit():
                    currentValue = currentValue + genome[currentChar]
                    currentChar = currentChar + 1
                room.originY = int(currentValue)

                currentValue = ""
                currentChar = currentChar + 1

                # Get the length of the corridor.
                if genome[currentChar] == "-":
                    currentValue = currentValue + genome[currentChar]
                    currentChar = currentChar + 1        
                while genome[currentChar].isdigit():
                    currentValue = currentValue + genome[currentChar]
                    currentChar = currentChar + 1
                if int(currentValue) > 0:
                    room.endX = int(room.originX) + int(currentValue)
                    room.endY = int(room.originY) + 3
                else:
                    room.endX = int(room.originX) + 3
                    room.endY = int(room.originY) - int(currentValue)
                rooms.append(room)

                currentValue = ""
                currentChar = currentChar + 1

    print("Done.")
    return rooms

# Merges AB rooms.
def mergeRooms(rooms):
    mergedCount = 0

    for room in rooms:
        if not room.isCorridor:
            nextRoom = next((nextRoom for nextRoom in rooms if (nextRoom.originX > room.originX and nextRoom.originX <= room.endX + 1 and \
                                                                nextRoom.originY == room.originY and nextRoom.endY == room.endY and \
                                                                nextRoom.endY - nextRoom.originY == room.endY - room.originY)), None)
            if nextRoom is not None and not nextRoom.isCorridor:
                # print("Merging room <" + str(room.originX) + "," +
                # str(room.originY) + ">" + "<" + str(room.endX) + "," +
                # str(room.endY) + ">" + " and <" + \
                #       str(nextRoom.originX) + "," + str(nextRoom.originY) +
                #       ">" + "<" + str(nextRoom.endX) + "," +
                #       str(nextRoom.endY) + ">.")
                room.endX = nextRoom.endX
                room.endY = nextRoom.endY
                rooms.remove(nextRoom)
                mergedCount = mergedCount + 1
            else:
                nextRoom = next((nextRoom for nextRoom in rooms if (nextRoom.originY > room.originY and nextRoom.originY <= room.endY + 1 and \
                                                                    nextRoom.originX == room.originX and nextRoom.endX == room.endX and \
                                                                    nextRoom.endX - nextRoom.originX == room.endX - room.originX)), None) 
                if nextRoom is not None and not nextRoom.isCorridor:
                    # print("Merging room <" + str(room.originX) + "," +
                    # str(room.originY) + ">" + "<" + str(room.endX) + "," +
                    # str(room.endY) + ">" + " and <" + \
                    #       str(nextRoom.originX) + "," + str(nextRoom.originY)
                    #       + ">" + "<" + str(nextRoom.endX) + "," +
                    #       str(nextRoom.endY) + ">.")
                    room.endX = nextRoom.endX
                    room.endY = nextRoom.endY
                    rooms.remove(nextRoom)
                    mergedCount = mergedCount + 1
    
    if mergedCount == 0:
        return
    else:
        mergeRooms(rooms)

# Computes the tile graph.
def getTileGraph(map):
    print("\nCreating the graph... ", end='')

    G = nx.Graph()
    width = len(map[0])
    height = len(map)

    # Add the nodes.
    for x in range(width): 
        for y in range(height): 
            if not map[x][y] == "w":
                G.add_node(subToInd(width, x, y), x = x, y = y, char = map[x][y])

    # Add the edges.
    for x in range(width): 
        for y in range(height): 
            if subToInd(width, x, y) in G:
                if subToInd(width, x + 1, y) in G:
                    G.add_edge(subToInd(width, x + 1, y), subToInd(width, x, y))
                if subToInd(width, x + 1, y + 1) in G:
                    G.add_edge(subToInd(width, x + 1, y + 1), subToInd(width, x, y))                  
                if subToInd(width, x, y + 1) in G:
                    G.add_edge(subToInd(width, x, y + 1), subToInd(width, x, y))
                if subToInd(width, x - 1, y + 1) in G:
                    G.add_edge(subToInd(width, x - 1, y + 1), subToInd(width, x, y))

    print("Done.\n")
    print("The tiles graph has:")
    print("%i nodes." % (nx.number_of_nodes(G)))
    print("%i edges." % (nx.number_of_edges(G)))
    return G

# Computes the rooms and corridors graph.
def getRoomsCorridorsGraph(rooms):
    print("\nCreating the graph... ", end='')

    G = nx.Graph()

    for i in range(len(rooms)):
        G.add_node(i, originX = rooms[i].originX, originY = rooms[i].originY, endX = rooms[i].endX, endY = rooms[i].endY, isCorridor = rooms[i].isCorridor)
    
    for i in range(len(rooms)):
        for j in range(i, len(rooms)):
            if j != i and not (rooms[i].originX >= rooms[j].endX + 1 or rooms[j].originX >= rooms[i].endX + 1) and \
               not (rooms[i].originY >= rooms[j].endY + 1 or rooms[j].originY >= rooms[i].endY + 1):
                G.add_edge(i, j, weight = eulerianDistance((rooms[i].originX / 2 + rooms[i].endX / 2), (rooms[j].originX / 2 + rooms[j].endX / 2), \
                                                           (rooms[i].originY / 2 + rooms[i].endY / 2), (rooms[j].originY / 2 + rooms[j].endY / 2)))
    print("Done.\n")
    print("The tiles graph has:")
    print("%i nodes." % (nx.number_of_nodes(G)))
    print("%i edges." % (nx.number_of_edges(G)))
    return G

# Computes the graph of the room outlines.
def GetRoomsOutlineGraph(rooms):
    print("\nCreating the graph... ", end='')

    G = nx.Graph()
    
    i = 0

    for room in rooms:
        G.add_node(i, x = room.originX, y = room.originY)
        i = i + 1
        G.add_node(i, x = room.endX, y = room.originY)
        G.add_edge(i, i - 1)
        i = i + 1
        G.add_node(i, x = room.endX, y = room.endY)
        G.add_edge(i, i - 1)
        i = i + 1
        G.add_node(i, x = room.originX, y = room.endY)
        G.add_edge(i, i - 1)
        G.add_edge(i, i - 3)
        i = i + 1
        
    print("Done.\n")
    print("The tiles graph has:")
    print("%i nodes." % (nx.number_of_nodes(G)))
    print("%i edges." % (nx.number_of_edges(G)))
    return G

# Computes the visibility graph.
def getVisibilityGraph(map):
    print("\nCreating the graph... ", end='')

    G = nx.Graph()
    width = len(map[0])
    height = len(map)

    # Add the nodes.
    for x in range(width): 
        for y in range(height): 
            if not map[x][y] == "w":
                G.add_node(subToInd(width, x, y), x = x, y = y, char = map[x][y], visibility = 0)
     
    # Add the edges.
    for node1 in G.nodes(data=True):
        for node2 in G.nodes(data=True):
            if node1 is not node2 and isNodeVisible(node1[1]['x'], node1[1]['y'], node2[1]['x'], node2[1]['y'], map):
                G.add_edge(node1[0], node2[0])

    for node in G.nodes(data = True):
        node[1]['visibility'] = G.degree(node[0])

    print("Done.\n")
    print("The tiles graph has:")
    print("%i nodes." % (nx.number_of_nodes(G)))
    print("%i edges." % (nx.number_of_edges(G)))
    return G

# Tells if a tile is inside the map bounds.
def isInMapRange(x, y, map):
    if (x < len(map[0]) and y < len(map)):
        return True
    else:
        return False

# Converts from subscript to linear index.
def subToInd(width, rows, cols):
    return rows * width + cols

# Converts from linear to subscript index.
def indToSub(width, ind):
    rows = (ind.astype('int') / width)
    cols = (ind.astype('int') % width)
    return (rows, cols)

# Computes the eulerian distance.
def eulerianDistance(x1, x2, y1, y2):
    return math.sqrt(math.pow(x1 - x2, 2) + math.pow(y1 - y2, 2))

# Tells if a node is visible from another node. The method is not perfect, but provides a good approximation.
def isNodeVisible(x1, y1, x2, y2, map):
    dy = (y2 - y1) 
    dx = (x2 - x1)

    if dx == 0:
        for y in range(y1, y2) if y1 < y2 else range(y2, y1):
            if map[x1][y] == 'w':
                return False
    elif dy == 0:
        for x in range(x1, x2) if x1 < x2 else range(x2, x1):
            if map[x][y1] == 'w':
                return False
    else:
        m = dy / dx
        c = y1 - m * x1

        if abs(dx) > abs(dy):
            for x in range (x1, x2) if x1 < x2 else range(x2, x1):
                if map[x][int(c + m * x)] == 'w':
                    return False
        else:
            for y in range(y1, y2) if y1 < y2 else range(y2, y1):
                if map[int(y / m - c / m)][y] == 'w':
                    return False
    
    return True

# Blends from a value to another.
def blend(a, b, alpha):
  return (1 - alpha) * a + alpha * b

# Coverts from hex to RGB.
def RGBToHex(r, g, b):
    return '#%02x%02x%02x' % (int(r), int(g), int(b))

# Converts from RGB to hex.
def hexToRGB(hex):
    h = hex.lstrip('#')
    RGB = tuple(int(h[i : i + 2], 16) for i in (0, 2 ,4))
    return RGB[0], RGB[1], RGB[2]

# Blends a color.
def blendColor(h1, h2, alpha):
    r1, g1, b1 = hexToRGB(h1)
    r2, g2, b2 = hexToRGB(h2)
    return RGBToHex(blend(r1, r2, alpha), blend(g1, g2, alpha), blend(b1, b2, alpha))

# Returns the maximum and the minimum visibility.
def minMaxVisibility(G):
    min = float("inf")
    max = 0

    for node in G.nodes(data = True):
        if node[1]["visibility"] > max:
            max = node[1]["visibility"]
        elif node[1]["visibility"] < min:
            min = node[1]["visibility"] 

    return min, max

# MENU FUNCTIONS #

# Manages the graph menu.
def graphMenu():
    while True:
        print("\n[GRAPH GENERATION] Select an option:")
        print("[1] Generate reachability graph")
        print("[2] Generate visibility graph")
        print("[0] Back\n")

        quit = False
        option = input("Option: ")

        while option != "1" and option != "2" and option != "0":
            option = input("Invalid choice. Option: ")
        if option == "1":
            graphMenuReachability()
        elif option == "2":
            G = getVisibilityGraph(map)
            plotVisibilityGraph(G)
        elif option == "0":
            return

# Manages the reachability graph menu.
def graphMenuReachability():
    while True:
        print("\n[REACHABILITY GRAPH GENERATION] Select an option:")
        print("[1] Generate tiles graph")
        print("[2] Generate rooms and corridors graph")
        print("[3] Generate rooms, corridors and resources graph")
        print("[0] Back\n")

        option = input("Option: ")
    
        while option != "1" and option != "2" and option != "3" and option != "0":
            option = input("Invalid choice. Option: ")
    
        if option == "1":
            G = getTileGraph(map)
            plotTilesGraph(G)
        elif option == "2":
            G = getRoomsCorridorsGraph(rooms)
            plotRoomsCorridorsGraph(G)
        elif option == "3":
            print("\nThis has not been implemented yet.")
        elif option == "0":
            return

# Menages the file menu.
def filesMenu():
    # Get the name of the map and get the files path.
    mapFileName, ABFileName, mapFilePath, ABFilePath = getFiles()

    # Read the map.
    map = readMap(mapFilePath)

    # Read the AB file.
    rooms = readAB(ABFilePath)
    print("Refining the AB rooms... ", end='')
    mergeRooms(rooms)
    print("Done")

    return mapFileName, ABFileName, mapFilePath, ABFilePath, map, rooms

# PLOT FUNCTIONS #

# Plots the graph.
def plotRoomsCorridorsGraph(G):
    print("\n[CLOSE THE GRAPH TO CONTNUE]")
    pos = dict([(node, (data["originX"] / 2 + data["endX"] / 2, data["originY"] / 2 + data["endY"] / 2)) for node, data  in G.nodes(data=True)])
    edge_labels = dict([(key, "{:.2f}".format(value)) for key, value in nx.get_edge_attributes(G,'weight').items()])
    nx.draw(G, pos, node_color = '#f44242', node_size = 75)
    nx.draw_networkx_edge_labels(G, pos, edge_labels = edge_labels)
    plt.axis('equal')
    plt.show()

# Plots the graph.
def plotTilesGraph(G):
    print("\n[CLOSE THE GRAPH TO CONTNUE]")
    nx.draw(G, dict([ (node, (data["x"], data["y"])) for node, data  in G.nodes(data=True)]), node_color = '#f44242', node_size = 75)
    plt.axis('equal')
    plt.show()

# Plots the graph.
def plotVisibilityGraph(G):
    print("\n[CLOSE THE GRAPH TO CONTNUE]")
    minC, maxC = minMaxVisibility(G)
    colors = [(blendColor("#0000ff", "#ff0000", (data["visibility"] - minC) / (maxC - minC))) for node, data in G.nodes(data=True)]
    pos = dict([ (node, (data["x"], data["y"])) for node, data in G.nodes(data=True)])
    nx.draw_networkx_nodes(G, pos, node_color = colors, node_size = 75)
    # node_labels = nx.get_node_attributes(G,'visibility')
    # nx.draw_networkx_labels(G, pos, labels = node_labels)
    plt.axis('equal')
    plt.show()

# MAIN #

# Create the input and the output folder if needed.
inputDir = "/Input"
outputDir = "/Output"
if not os.path.exists(inputDir):
    os.makedirs(inputDir)
if not os.path.exists(outputDir):
    os.makedirs(outputDir)

print("MAP ANALYZER\n")
print("This script expects a MAPNAME_map.txt file and MAPNAME_AB.txt file in the input folder.")

# Get the files and process them.
mapFileName, ABFileName, mapFilePath, ABFilePath, map, rooms = filesMenu()

while True:
    print("\n[MENU] Select an option:")
    print("[1] Decorate map")
    print("[2] Generate graphs")
    print("[3] Change files")
    print("[0] Quit\n")

    option = input("Option: ")

    while option != "1" and option != "2" and option != "3" and option != "0":
        option = input("Invalid choice. Option: ")

    if option == "1":
        print("\nThis has not been implemented yet.")
    elif option == "2":
        graphMenu()
    elif option == "3":
        mapFileName, ABFileName, mapFilePath, ABFilePath, map, rooms = filesMenu()
    elif option == "0":
        print()
        break
import os
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
    text = input("Insert the MAPNAME value: ")
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
                G.add_edge(i, j)
    
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

# MAIN #

# Create the input and the output folder if needed.
inputDir = "/Input"
outputDir = "/Output"
if not os.path.exists(inputDir):
    os.makedirs(inputDir)
if not os.path.exists(outputDir):
    os.makedirs(outputDir)

print("MAP ANALYZER\n")
print("This script expects a MAPNAME_map.txt file and MAPNAME_AB.txt file in the input folder.\n")

# Get the name of the map and get the files path.
mapFileName, ABFileName, mapFilePath, ABFilePath = getFiles()

# Read the map.
map = readMap(mapFilePath)

# Read the AB file.
rooms = readAB(ABFilePath)
print("Refining the AB rooms... ", end='')
mergeRooms(rooms)
print("Done")

print("\nSelect the kind of graph to generate:")
print("[1] Reachability graph")
print("[2] Visibility graph\n")

option = input("Graph: ")

while option != "1" and option != "2":
    option = input("Invalid choice. Graph: ")

if option == "1":
    print("\nSelect the kind of rechability graph to generate:")
    print("[1] Tiles graph")
    print("[2] Outlines graph")
    print("[3] Room and corridors graph")
    print("[4] Room, corridors and resources graph\n")

    option = input("Graph: ")

    while option != "1" and option != "2" and option != "3":
        option = input("Invalid choice. Graph: ")
    
    if option == "1":
        G = getTileGraph(map)
        nx.draw(G, dict([ (node, (data["x"], data["y"])) for node, data  in G.nodes(data=True)]), node_color = '#f44242', node_size = 75)
        plt.axis('equal')
        plt.show(block = False)
    elif option == "2":
        G = GetRoomsOutlineGraph(rooms)
        nx.draw(G, dict([ (node, (data["x"], data["y"])) for node, data  in G.nodes(data=True)]), node_color = '#f44242', node_size = 75)
        plt.axis('equal')
        plt.show(block = False)
    elif option == "3":
        G = getRoomsCorridorsGraph(rooms)
        nx.draw(G, dict([ (node, (data["originX"] / 2 + data["endX"] / 2, data["originY"] / 2 + data["endY"] / 2)) for node, data  in G.nodes(data=True)]), node_color = '#f44242', node_size = 75)
        plt.axis('equal')
        plt.show(block = False)
    elif option == "4":
        print("\nThis has not been implemented yet.")
else:
    print("\nThis has not been implemented yet.")

# Use this to show multiple graphs.
# plt.figure()

plt.show()

print();
import os

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

        while (currentChar < len(genome)) and (genome[currentChar] == "<"):
            room = Room()
            room.isCorridor = False;
            currentChar = currentChar + 1

            # Get the x coordinate of the origin.
            while genome[currentChar].isdigit():
                currentValue = currentValue + genome[currentChar]
                currentChar = currentChar + 1
            room.originX = currentValue

            currentValue = ""
            currentChar = currentChar + 1

            # Get the y coordinate of the origin.
            while genome[currentChar].isdigit():
                currentValue = currentValue + genome[currentChar]
                currentChar = currentChar + 1
            room.originY = currentValue

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

        if (currentChar < len(genome)) and (genome[currentChar] == "|"):
            currentChar = currentChar + 1

            while (currentChar < len(genome)) and (genome[currentChar] == "<"):
                room = Room()
                room.isCorridor = True;
                currentChar = currentChar + 1

                # Get the x coordinate of the origin.
                while genome[currentChar].isdigit():
                    currentValue = currentValue + genome[currentChar]
                    currentChar = currentChar + 1
                room.originX = currentValue

                currentValue = ""
                currentChar = currentChar + 1

                # Get the y coordinate of the origin.
                while genome[currentChar].isdigit():
                    currentValue = currentValue + genome[currentChar]
                    currentChar = currentChar + 1
                room.originY = currentValue

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

                currentValue = ""
                currentChar = currentChar + 1

    print("Done.")
    return rooms

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
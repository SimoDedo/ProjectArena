import os
import csv
import numpy as np
import matplotlib.pyplot as plt
import matplotlib.lines as lines
from scipy.stats import wilcoxon, binom_test

### FUNCTIONS ###############################################################

# Gets the file name and parses its data.
def getData(inputDir):
    inputAcquired = False

    # fileName = input("\nInsert the file name: ")
    fileName = "data.csv"
    print("\nInsert the file name: " + fileName)

    while inputAcquired == False:
        if os.path.isfile(inputDir + "/" + fileName):
            print("File found.\n")
            inputAcquired = True
            # Parse the data.
            with open(inputDir + "/" + fileName) as csvfile:
                reader = csv.reader(csvfile, delimiter = ';', quotechar = '|')
                data = list(reader)
        else:
            fileName = input("File not found. Insert the file name: ")
    return data

# Extracts a column from the data.
def getArrayFromData(data, column, conversion=0):
    array = list()

    for i in range(len(data)):
        if (conversion == 1):
            try:
                array.append(int(float(data[i][column])))
            except:
                pass
        elif (conversion == 2):
            try:
                array.append(float(data[i][column]))
            except:
                pass
        else:
            array.append(data[i][column])

    return array

# Counts occurencies in array.
def getCountInData(data, column, map, min, max):
    totalCount = list()

    for i in range(min, max):
        count = 0
        for j in range(len(data)):
            if data[j][1] == map and int(float(data[j][column])) == i:
                count = count + 1
        totalCount.append(count)            

    return totalCount

# Compares the outcomes.
def compareOutcomes(real, perceived, outcome):
    comparison = [0, 0, 0]

    for i in range(len(real)):
        if (real[i] == outcome):
            if (perceived[i] == "safe"):
                comparison[0] = comparison[0] + 1
            elif (perceived[i] == "equal"):
                comparison[1] = comparison[1] + 1
            else:
                comparison[2] = comparison[2] + 1

    return comparison

# Generate the bar diagram of the kills.
def generateBarDiagramKills(data, safe):
    # Extract the data.
    killsSafeArena = getCountInData(data, 6 if safe else 13, "arena", 3, 17)
    killsSafeCorridors = getCountInData(data, 6 if safe else 13, "corridors", 3, 17)
    killsSafeIntense = getCountInData(data, 6 if safe else 13, "intense", 3, 17)
    N = len(killsSafeArena)

    # Setup the graph.
    ind = np.arange(N)
    width = 0.5

    pArena = plt.bar(ind, killsSafeArena, width)
    pCorridors = plt.bar(ind, killsSafeCorridors, width, bottom = killsSafeArena)
    pIntense = plt.bar(ind, killsSafeIntense, width, bottom =  [sum(x) for x in zip(killsSafeArena, killsSafeCorridors)])

    # Plot.
    plt.ylabel('Number of matches')
    plt.xlabel('Kills')
    plt.title('Kills in maps with ' + ('low risk' if safe else 'uniform') + ' heuristic')
    plt.xticks(ind, range(3, 17))
    plt.yticks(np.arange(0, 11, 1))
    plt.legend((pArena[0], pCorridors[0], pIntense[0]), ('Arena', 'Corridors', 'Intense'))
    plt.show()

# Generate the bar diagram of the difficulty.
def generateBarDiagramDifficulty(data):
    # Extract the data.
    realDifficulty = getArrayFromData(data, 16)
    perceivedDifficulty = getArrayFromData(data, 17)
    
    safe = compareOutcomes(realDifficulty, perceivedDifficulty, "safe")
    equal = compareOutcomes(realDifficulty, perceivedDifficulty, "equal")
    uniform = compareOutcomes(realDifficulty, perceivedDifficulty, "uniform")

    s = [safe[0], equal[0], uniform[0]]
    e = [safe[1], equal[1], uniform[1]]
    u = [safe[2], equal[2], uniform[2]]

    # Setup the graph.
    N = len(safe)
    ind = np.arange(N)
    width = 0.5

    # Hide the useless axis.
    # ax = plt.subplot(111)
    # ax.plot()
    # ax.spines['right'].set_visible(False)
    # ax.spines['top'].set_visible(False)

    safeBar = plt.bar(ind, s, width)
    equalBar = plt.bar(ind, e, width, bottom = s)
    uniformBar = plt.bar(ind, u, width, bottom = [sum(x) for x in zip(s, e)])

    # Plot.
    plt.xlabel('Heuristic of the map with least kills')
    plt.ylabel('Number of test sessions')
    plt.title('Comparison between the effective and the percived difficulty')
    plt.yticks(np.arange(0, 18, 1))
    plt.xticks(ind, ("Low risk","No difference","Uniform"))
    plt.legend((uniformBar[0], equalBar[0], safeBar[0]), ('Uniform heuristic map percived as harder', 
                                                          'No difference percived', 
                                                          'Low risk heuristic map percived as harder'))
    plt.show()

# Counts the occurencies.
def countOccurencies(array1, array2, i):
    count = 0

    for j in range(len(array1)):
        if array1[i] == array1[j] and array2[i] == array2[j]:
            count = count + 1 

    return count

# Generate scatter diagram.
def generateScatterDiagram(data, column1, column2, showTicks, xlabel, ylabel, title):
    # Extract the data.
    data1 = getArrayFromData(data, column1, 1)
    data2 = getArrayFromData(data, column2, 1)
    maxData = max([max(data1), max(data2)]) + 1
    maxData = maxData + maxData * 0.025

    area = [(np.pi * (30 * countOccurencies(data1, data2, i))) for i in range(len(data1))]

    # Plot.
    fig, ax = plt.subplots()
    plt.ylabel(ylabel)
    plt.xlabel(xlabel)
    plt.title(title)
    if (showTicks):
        plt.xticks(np.arange(0, maxData, 1))
        plt.yticks(np.arange(0, maxData, 1))
    plt.xlim(0, maxData)
    plt.ylim(0, maxData)
    plt.gca().set_aspect('equal', adjustable='box')
    ax.scatter(data1, data2, area)
    line = lines.Line2D([0, 1], [0, 1], color='red')
    transform = ax.transAxes
    line.set_transform(transform)
    ax.add_line(line)
    plt.show()

### MENU FUNCTIONS ############################################################

# Mengaes the graph menu.
def graphMenu(data):
    index = 0

    while True:
        print("\n[GRAPHS] Select a graph to generate:")
        print("[1] Low risk bar diagram")
        print("[2] Uniform bar diagram")
        print("[3] Kills")
        print("[4] Distance")
        print("[5] Perception")
        print("[0] Back\n")

        option = input("Option: ")
    
        while option != "1" and option != "2" and option != "3" and option != "4" and option != "5"  and option != "0":
            option = input("Invalid choice. Option: ")
    
        if option == "1":
            print("\nGenerating graph...") 
            generateBarDiagramKills(data, True)
        elif option == "2":
            print("\nGenerating graph...")  
            generateBarDiagramKills(data, False)
        elif option == "3":
            print("\nGenerating graph...")  
            generateScatterDiagram(data, 6, 13, True, 'Kills (low risk heuristic)', 
                                   'Kills (uniform heuristic)', 'Experiment outcome')
        elif option == "4":
            print("\nGenerating graph...")
            generateScatterDiagram(data, 8, 15, False, 'AvgKillDistance (low risk heuristic)', 
                                   'AvgKillDistance (uniform heuristic)', 'Experiment outcome')
        elif option == "5":
            print("\nGenerating graph...")
            generateBarDiagramDifficulty(data)
        elif option == "0":
            return

### MAIN ######################################################################

# Create the input folder if needed.
inputDir = "./Input"
if not os.path.exists(inputDir):
    os.makedirs(inputDir)

print("RESULT ANALYSIS")

# Get the files and process them.
data = getData(inputDir)

while True:
    print("[MENU] Select an option:")
    print("[1] Wilcoxon signed-rank test")
    print("[2] Bernulli validation")
    print("[3] Generate graphs")
    print("[4] Change file")
    print("[0] Quit\n")

    option = input("Option: ")

    while option != "1" and option != "2" and option != "3" and option != "4" and option != "0":
        option = input("Invalid choice. Option: ")

    if option == "1":
        killsSafe = getArrayFromData(data, 6, 1)
        killsUniform = getArrayFromData(data, 13, 1)
        statistic, pvalue = wilcoxon(killsSafe, killsUniform, 'pratt')
        print("\n[WILCOXON SIGNED-RANK TEST] Results:")
        print("statistics = " + str(statistic))
        print("p-value (two-tiled) = " + str(pvalue))
        print("p-value (one-tiled) = " + str(pvalue / 2) + "\n")
    elif option == "2":
        harder = getArrayFromData(data, 16)
        safeCount = len([1 for x in harder if x == "safe"])
        uniformCount = len([1 for x in harder if x == "uniform"])
        equalCount = len([1 for x in harder if x == "equal"])
        totalCount = safeCount + uniformCount + equalCount
        pvalue = binom_test(safeCount, totalCount)
        print("\n[BERNULLI TEST] Results:")
        print("#safe = " + str(safeCount))
        print("#uniform = " + str(uniformCount))
        print("#equal = " + str(equalCount))
        print("p-value = " + str(pvalue) + "\n")
    elif option == "3":
        graphMenu(data)
    elif option == "4":
        data = getData(inputDir)
    elif option == "0":
        break
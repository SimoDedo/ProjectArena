import os
import csv
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
            array.append(int(float(data[i][column])))
        elif (conversion == 2):
            array.append(float(data[i][column]))
        else:
            array.append(data[i][column])

    return array

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
    print("[3] Change file")
    print("[0] Quit\n")

    option = input("Option: ")

    while option != "1" and option != "2" and option != "3" and option != "0":
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
        data = getData(inputDir)
    elif option == "0":
        break
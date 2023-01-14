import numpy


def get_statistics(array, decimals=8):
    min_var = round(numpy.min(array),decimals)
    max_var = round(numpy.max(array),decimals)
    mean = round(numpy.mean(array),decimals)
    std_dev = round(numpy.std(array),decimals)
    return min_var, max_var, mean, std_dev


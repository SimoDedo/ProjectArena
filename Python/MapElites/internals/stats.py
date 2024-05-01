import numpy


def get_statistics(values, decimals=8):
    min_value = round(numpy.min(values), decimals)
    max_value = round(numpy.max(values), decimals)
    mean_value = round(numpy.mean(values), decimals)
    std_dev = round(numpy.std(values), decimals)
    return min_value, max_value, mean_value, std_dev


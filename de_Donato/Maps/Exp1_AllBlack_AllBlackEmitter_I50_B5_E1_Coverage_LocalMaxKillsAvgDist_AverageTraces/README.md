Here we optimized the coverage of the map while exploring the average distance between local maxima of Kills perfomed by the bots, and the average length of the kills traces.

From the heatmap we see that kills traces never go beyond 12, and the average distance never goes beyond 50, but as of now it is unclear if its the exploration that fails or if it's the maximum feasible length that we can hope for. As discussed, I plan on looking into that and adding sliding boundaries to avoid this problem. I do suspect that the exploration is somewhat not as efficient as it could be, since maps with higher distance are possible, but only few were found.
Black squares are map too little to be of any interest, so despite having 100% surface coverage their fitness is set to zero.

Regarding the maps itself, at lower x and y maps are very bland, small and mostly "boxes"

As the distance increses, we have either larger maps or more connected maps. Longer traces lead instead to bigger rooms and longer corridors.

The interesting thing is that maps generated using these spatial features tend to be more connected with less "dead-ends" or irrelevant features, and also explore different sizes that lead to interesting designs. In the standouts folder, I have highlighted the following maps:
- map_0_3: Example of map too small, meaning no other map could achieve traces and distance this little
- map_0_15: Close local maxima, meaning a there is a main "chokepoint", but high traces, leading to small corridors and many close rooms. high interconnection.
- map_2_8: Single chokepoint (close local maxima) and short traces. The checkpoint allows for short line of sights.
- map_7_11: Short traces means its small, but far local maxima mean that fights happen all over. 
- map_7_9: High distance means having sometimes labirynthic maps that still have a single chokepoint, but the map is so big that local maxima are also far apart. This may be undesired, and happens a lot. Maybe also weighting each local maxima differently avoids having large distance but still having one clear chokepoint.
- map_6_10: Reminds a lot of a three lane layout, which means interesting and well known design patterns are being explored


The main takeaway is that, compared to the maps generated using entropy, pace, pursue time ecc.. these maps sport more interesting layouts to the human eye. Mainly, due to the fact that well connected maps are more easily explorable by the bots and lead to higher fitness. But some inconsistency between expected look given certain features and the actual map layout still exists, such as map_7_9.
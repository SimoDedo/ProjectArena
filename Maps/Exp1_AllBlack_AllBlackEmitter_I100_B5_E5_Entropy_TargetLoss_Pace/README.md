Here we optimize the entropy of the map, taking inspiration from Bari's thesis. We look for maps that have varying grades of pace and target loss.

The main takeaway is that, without employing sliding boundaries, it is hard to understand what is the actual maximum value that a variable might realistically have. Here, target loss never takes a value higher than 0.4, so the map ends plenty empty, and we may have discared many interesting solution in the "zipped" space in 0-0.5. 

We also notice how the features chosen display a certain grade of correlation. This hinders the exploratin, as it seems hard to achieve high pace and high target loss. 

Regardin the maps themself, we can see that the produces designs are able to elicit certain behaviors, but that often transalates to maps that are unpleasant to the human player.

map 1_10: Fights happen mostly in two spots, that can be viewed as a single chokepoint. the maps seems unfun, since despite featuring many rooms and corridors, only a small subset seem worth exploring. However, we may deduce that achieving low pace and with low target loss may give rise to these kinds of messy maps, where only a few spots are relevant in a large map (low pace), but since these are chokepoints with no escape paths, they lead to low target loss.

map 2_16: High pace maps tend to all look fairly similar: small with a single chokepoint. "Pace" does not account for how big or small a map is, or how long paths are. This, in my opinion, leads to not explore interesting design spaces that could have big maps where player are still able to meet very often, just not as often as in smaller maps 

map 5_3: Many low pace map show very big loopy maps, that while not entirely bad, lack "sub-loops" that keep a map layout fresh and interesting while being played.

map 5_4: This seems like an interesting map visually, however it seems to be under explored by the bots. By not exploring "spatial measures" we may get a map that fosters balanced matches, but with uninteresting gameplay. It can be argued that low pace maps will tend to look big and messy, so it is not clear if this kind of results are truly unwanted, since we are still able to deduce design patterns from them
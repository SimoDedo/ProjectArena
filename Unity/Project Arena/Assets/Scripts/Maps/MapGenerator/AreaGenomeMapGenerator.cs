using System;
using System.Linq;
using Logging;

namespace Maps.MapGenerator
{
    // TODO Since width and height are not used for this map, maybe clean the hierarchy to have them not required in
    // this class.
    public class AreaGenomeMapGenerator : MapGenerator
    {
        private AreasGenome initialAreas;
        private Area[] displacedAreas;

        public void SetGenome(AreasGenome areas)
        {
            initialAreas = areas;
            InitializePseudoRandomGenerator();
            SetReady(true);
        }
        
        public override char[,] GenerateMap()
        {
            displacedAreas = initialAreas.areas.Select(it => DisplaceArea(it, borderSize)).ToArray();
            
            width = initialAreas.width + borderSize * 2;
            height = initialAreas.height + borderSize * 2;
            
            map = new char[height, width];
            MapEdit.FillMap(map, wallChar);
            
            FillMap(displacedAreas, map);
            
            // Compute map excluding corridors, since we do not want to place anything there
            var noCorridorsMap = new char[height, width];
            MapEdit.FillMap(noCorridorsMap, wallChar);
            FillMap(displacedAreas, noCorridorsMap, true);
            
            PopulateMap(noCorridorsMap);

            var textMap = GetMapAsText();
            SaveMapTextGameEvent.Instance.Raise(textMap);
            if (createTextFile) SaveMapAsText(textMap);
            return map;
        }

        private static Area DisplaceArea(Area area, int cellBorder)
        {
            return new Area(
                cellBorder + area.leftColumn,
                cellBorder + area.bottomRow,
                cellBorder + area.rightColumn,
                cellBorder + area.topRow,
                area.isCorridor
                );
        }

        private void FillMap(Area[] areas, char[,] map, bool ignoreCorridors = false)
        {
            // Note: Areas are flipped vertically for some reason.
            foreach (var area in areas)
            {
                if (ignoreCorridors && area.isCorridor) continue;
                for (var row = area.bottomRow; row < area.topRow; row++)
                {
                    for (var col = area.leftColumn; col < area.rightColumn; col++)
                        map[height - row - 1, col] = 'r';
                }
            }
        }

        public override string ConvertMapToAB(bool exportObjects = true)
        {
            throw new NotImplementedException();
        }

        public override Area[] ConvertMapToAreas()
        {
            return displacedAreas;
        }
    }
}
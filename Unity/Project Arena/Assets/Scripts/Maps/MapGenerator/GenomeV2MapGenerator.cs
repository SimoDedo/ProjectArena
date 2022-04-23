using System.Linq;
using Graph;
using Logging;
using Maps.Genomes;

namespace Maps.MapGenerator
{
    // TODO Since width and height are not used for this map, maybe clean the hierarchy to have them not required in
    // this class.
    public class GenomeV2MapGenerator : MapGenerator
    {
        private GraphGenomeV2 genome;
        private Area[] areas = new Area[0];

        public void SetGenome(GraphGenomeV2 genome)
        {
            this.genome = genome;
            InitializePseudoRandomGenerator();
            SetReady(true);
        }
        
        public override char[,] GenerateMap()
        {
            // TODO create area map, apply scaling based on genome, apply offsets, create map.
            areas = genome.ConvertToAreas();
            areas = areas.Select(it => DisplaceArea(it, borderSize)).ToArray();
            
            width = genome.GetWidth() + borderSize * 2;
            height = genome.GetHeight() + borderSize * 2;
            
            map = new char[height, width];
            MapEdit.FillMap(map, wallChar);
            
            FillMap(areas, map);
            
            // Compute map excluding corridors, since we do not want to place anything there
            var noCorridorsMap = new char[height, width];
            MapEdit.FillMap(noCorridorsMap, wallChar);
            FillMap(areas, noCorridorsMap, true);
            
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
                area.isCorridor,
                area.isDummyRoom);
        }

        private void FillMap(Area[] areas, char[,] map, bool ignoreCorridors = false)
        {
            // TODO Areas are flipped vertically. Understand why
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
            throw new System.NotImplementedException();
        }

        public override Area[] ConvertMapToAreas()
        {
            return areas;
        }
    }
}
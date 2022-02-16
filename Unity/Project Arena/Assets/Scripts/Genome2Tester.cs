using System;
using Graph;
using Maps.Genomes;
using UnityEngine;

namespace DefaultNamespace
{
    public class Genome2Tester : MonoBehaviour
    {
        public GraphGenomeV2 genomeV2;
        private void Awake()
        {
            // Build rooms
            genomeV2 = new GraphGenomeV2();
            genomeV2.cellsWidth = 10;
            genomeV2.cellsHeight = 10;
            genomeV2.squareSize = 1;
            
            // genomeV2.rooms = new Room[2,1];
            // genomeV2.rooms[0,0] = new Room(2, 4, 2, 6, false, true);
            // genomeV2.rooms[1,0] = new Room(0, 10, 0, 10, false, false);
            // var stringMap1 = MapUtils.GetStringFromCharMap(MapUtils.TranslateAreaMap(genomeV2.ConvertToAreas()));
            //
            // genomeV2.rooms = new Room[2,1];
            // genomeV2.rooms[0,0] = new Room(2, 4, 2, 10, false, true);
            // genomeV2.rooms[1,0] = new Room(0, 10, 0, 10, false, false);
            //
            // var stringMap2 = MapUtils.GetStringFromCharMap(MapUtils.TranslateAreaMap(genomeV2.ConvertToAreas()));
            //
            // genomeV2.rooms = new Room[2,1];
            // genomeV2.rooms[0,0] = new Room(2, 4, 2, 10, false, true);
            // genomeV2.rooms[1,0] = new Room(4, 6, 0, 10, false, false);
            //
            // var stringMap3 = MapUtils.GetStringFromCharMap(MapUtils.TranslateAreaMap(genomeV2.ConvertToAreas()));
            //
            // genomeV2.rooms = new Room[1,2];
            // genomeV2.rooms[0,0] = new Room(2, 6, 2, 4, true, false);
            // genomeV2.rooms[0,1] = new Room(0, 10, 0, 10, false, false);
            // var stringMap4 = MapUtils.GetStringFromCharMap(MapUtils.TranslateAreaMap(genomeV2.ConvertToAreas()));
            //
            // genomeV2.rooms = new Room[1,2];
            // genomeV2.rooms[0,0] = new Room(2, 10, 2, 4, true, false);
            // genomeV2.rooms[0,1] = new Room(0, 10, 0, 10, false, false);
            //
            // var stringMap5 = MapUtils.GetStringFromCharMap(MapUtils.TranslateAreaMap(genomeV2.ConvertToAreas()));
            //
            // genomeV2.rooms = new Room[1,2];
            // genomeV2.rooms[0,0] = new Room(2, 10, 2, 4, true, false);
            // genomeV2.rooms[0,1] = new Room(0, 10, 4, 6, false, false);
            //
            // var stringMap6 = MapUtils.GetStringFromCharMap(MapUtils.TranslateAreaMap(genomeV2.ConvertToAreas()));

            genomeV2.rooms = new Room[2,2];
            genomeV2.rooms[0,0] = new Room(0, 10, 0, 10, true, true);
            genomeV2.rooms[0,1] = new Room(0, 10, 0, 10, false, false);
            genomeV2.rooms[1,0] = new Room(0, 10, 0, 10, false, false);
            genomeV2.rooms[1,1] = new Room(0, 10, 0, 10, false, false);
            
            var stringMap7 = MapUtils.GetStringFromCharMap(MapUtils.TranslateAreaMap(genomeV2.ConvertToAreas()));
            return;
        }

    }
}
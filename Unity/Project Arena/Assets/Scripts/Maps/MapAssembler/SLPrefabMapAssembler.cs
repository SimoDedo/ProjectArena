using System.Collections.Generic;
using UnityEngine;

namespace Maps.MapAssembler
{
    /// <summary>
    ///     SLPrefabMapAssembler is an implementation of PrefabMapAssembler for single-level maps.
    ///     WARNING: This implementation is currently broken!
    /// </summary>
    public class SLPrefabMapAssembler : PrefabMapAssembler
    {
        private MeshCollider ceilCollider;

        private MeshCollider floorCollider;

        // Map.
        private char[,] map;

        private void Start()
        {
            GameObject childObject;

            childObject = new GameObject("Floor - Collider");
            childObject.transform.parent = transform;
            childObject.transform.localPosition = Vector3.zero;
            floorCollider = childObject.AddComponent<MeshCollider>();

            childObject = new GameObject("Ceil - Collider");
            childObject.transform.parent = transform;
            childObject.transform.localPosition = Vector3.zero;
            ceilCollider = childObject.AddComponent<MeshCollider>();

            SetReady(true);
        }

        public override void AssembleMap(char[,] m, char wChar, char rChar)
        {
            wallChar = wChar;
            roomChar = rChar;
            width = m.GetLength(0);
            height = m.GetLength(1);
            map = m;

            // Process all the tiles.
            ProcessTiles();

            for (var x = 0; x < width; x++)
            for (var y = 0; y < height; y++)
                if (map[x, y] != wallChar)
                {
                    var currentMask = GetNeighbourhoodMask(x, y);
                    foreach (var p in processedTilePrefabs)
                        if (p.mask == currentMask)
                        {
                            AddPrefab(p.prefab, x, y, mapScale, p.rotation, wallHeight);
                            break;
                        }
                }

            // Generate floor and ceil colliders.
            floorCollider.sharedMesh = CreateFlatMesh(width, height, mapScale, wallHeight +
                floorHeight, false);
            ceilCollider.sharedMesh = CreateFlatMesh(width, height, mapScale, wallHeight +
                ceilHeight, true);
        }

        public override void AssembleMap(List<char[,]> maps, char wallChar, char roomChar,
            char voidChar)
        {
        }

        // Gets the neighbours of a cell as a mask.
        protected string GetNeighbourhoodMask(int gridX, int gridY)
        {
            var mask = new char[4];
            mask[0] = GetTileChar(gridX, gridY + 1);
            mask[1] = GetTileChar(gridX + 1, gridY);
            mask[2] = GetTileChar(gridX, gridY - 1);
            mask[3] = GetTileChar(gridX - 1, gridY);
            return new string(mask);
        }

        // Returns the char of a tile.
        protected char GetTileChar(int x, int y)
        {
            if (MapInfo.IsInMapRange(x, y, width, height))
                return map[x, y] == wallChar ? wallChar : roomChar;
            return wallChar;
        }

        // Clears the map.
        public override void ClearMap()
        {
            DestroyAllPrefabs(new List<string> {"Floor - Collider", "Ceil - Collider"});
        }
    }
}
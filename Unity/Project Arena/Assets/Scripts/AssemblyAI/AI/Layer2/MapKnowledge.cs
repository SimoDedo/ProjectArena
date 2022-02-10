using System;
using System.Runtime.CompilerServices;
using UnityEngine;
using Random = UnityEngine.Random;

namespace AssemblyAI.AI.Layer3
{
    public class MapKnowledge
    {
        // private readonly AIEntity me;
        private readonly Transform transform;
        private readonly Area[] areas;

        private readonly int[] areaScores;
        private readonly float gridScale;


        public MapKnowledge(AIEntity entity, GameManager gms)
        {
            // me = entity;
            transform = entity.transform;
            areas = gms.GetAreas();
            if (areas.Length == 0)
            {
                Debug.Log("Cannot use map knowledge, no areas were defined...");
                return;
            }

            gms.GetMap();
            areaScores = new int[areas.Length];
            gridScale = gms.GetMapScale();
        }

        public void Update()
        {
            if (!CanBeUsed)
            {
                return;
            }

            PrintAreas();
            
            // Check in which areas I am in. Mark them as visited (immediately? no timeout?)
            var currentPosition = ConvertToAreasCoordinate(transform.position);
            for (var i = 0; i < areas.Length; i++)
            {
                if (PositionInArea(areas[i], currentPosition))
                {
                    // TODO Maybe use our own counter?
                    areaScores[i] = Time.frameCount;
                }
            }
        }

        private void PrintAreas()
        {
            foreach (var area in areas)
            {
                PrintArea(area);
            }
        }

        private void PrintArea(Area area)
        {
            var topLeft = ConvertToActualMapCoordinate(new Vector3(area.leftColumn, 0, area.topRow));
            var bottomLeft = ConvertToActualMapCoordinate(new Vector3(area.leftColumn, 0, area.bottomRow));
            var topRight = ConvertToActualMapCoordinate(new Vector3(area.rightColumn, 0, area.topRow));
            var bottomRight = ConvertToActualMapCoordinate(new Vector3(area.rightColumn, 0, area.bottomRow));
                
            Debug.DrawLine(topLeft, bottomLeft, Color.green, 2, false);
            Debug.DrawLine( bottomLeft, bottomRight, Color.green, 2, false);
            Debug.DrawLine(bottomRight, topRight, Color.green, 2, false);
            Debug.DrawLine(topRight, topLeft, Color.green, 2, false);
            
        }

        public bool CanBeUsed => areas.Length != 0;

        /**
         * Returns a possible destination to wander to based on:
         * - Knowledge: prefer going to areas which haven been visited in a while
         * - ? Size: prefer visiting big (small?) areas
         * - ? Closeness
         */
        public Vector3 GetRecommendedDestination()
        {
            if (!CanBeUsed)
            {
                throw new InvalidOperationException(
                    "Cannot use map knowledge destination recommender, no area info available"
                );
            }

            var leastKnownArea = 0;
            var leastKnownAreaScore = int.MaxValue;
            for (var i = 0; i < areas.Length; i++)
            {
                var areaScore = areaScores[i];
                if (areaScore < leastKnownAreaScore)
                {
                    leastKnownAreaScore = areaScore;
                    leastKnownArea = i;
                }
                else if (areaScore == leastKnownAreaScore && Random.value < 0.3f)
                {
                    leastKnownAreaScore = areaScore;
                    leastKnownArea = i;
                }
            }

            var selectedArea = areas[leastKnownArea];
            
            PrintArea(selectedArea);
            

            var columnDelta = selectedArea.rightColumn - selectedArea.leftColumn;
            var rowDelta = selectedArea.topRow - selectedArea.bottomRow;
            var areaCenter = new Vector3(
                selectedArea.leftColumn + Random.value * columnDelta,
                0,
                selectedArea.bottomRow + Random.value * rowDelta
            );
            
            return ConvertToActualMapCoordinate(areaCenter);
        }
        
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static bool PositionInArea(Area area, Vector3 position)
        {
            return !(position.z < area.bottomRow) && !(position.z > area.topRow) && !(position.x < area.leftColumn) &&
                !(position.x > area.rightColumn);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private Vector3 ConvertToAreasCoordinate(Vector3 position)
        {
            return (position) / gridScale;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private Vector3 ConvertToActualMapCoordinate(Vector3 position)
        {
            return position * gridScale;
        }
    }
}
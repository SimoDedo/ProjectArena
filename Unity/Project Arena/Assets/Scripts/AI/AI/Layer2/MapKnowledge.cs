using System;
using System.Runtime.CompilerServices;
using Graph;
using Managers.Mode;
using UnityEngine;
using Random = UnityEngine.Random;

namespace AI.AI.Layer2
{
    /// <summary>
    /// This component represents the entity knowledge of the map. It requires support from the map itself, which
    /// must expose its <see cref="Area"/> representation.
    /// </summary>
    public class MapKnowledge
    {
        private readonly Area[] areas;

        private readonly int[] areaScores;

        private readonly float gridScale;

        // private readonly AIEntity me;
        private readonly Transform transform;


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

        /// <summary>
        /// Can this component be used? If not, any operation will return InvalidOperationException.
        /// </summary>
        public bool CanBeUsed => areas.Length != 0;

        public void Update()
        {
            if (!CanBeUsed)
                throw new InvalidOperationException(
                    "Cannot update the map knowledge, no knowledge is available!"
                );

            // PrintAreas();

            // Check in which areas I am in. Mark them as visited (immediately? no timeout?)
            var currentPosition = ConvertToAreasCoordinate(transform.position);
            for (var i = 0; i < areas.Length; i++)
                if (PositionInArea(areas[i], currentPosition))
                {
                    // TODO Maybe use our own counter?
                    areaScores[i] = Time.frameCount;
                    PrintArea(areas[i], Color.magenta);
                }
        }

        private void PrintAreas()
        {
            foreach (var area in areas) PrintArea(area, Color.green);
        }

        private void PrintArea(Area area, Color color)
        {
            var topLeft = ConvertToActualMapCoordinate(new Vector3(area.leftColumn, 0, area.topRow));
            var bottomLeft = ConvertToActualMapCoordinate(new Vector3(area.leftColumn, 0, area.bottomRow));
            var topRight = ConvertToActualMapCoordinate(new Vector3(area.rightColumn, 0, area.topRow));
            var bottomRight = ConvertToActualMapCoordinate(new Vector3(area.rightColumn, 0, area.bottomRow));

            Debug.DrawLine(topLeft, bottomLeft, color, 2, false);
            Debug.DrawLine(bottomLeft, bottomRight, color, 2, false);
            Debug.DrawLine(bottomRight, topRight, color, 2, false);
            Debug.DrawLine(topRight, topLeft, color, 2, false);
        }

        /// <summary>
        /// Returns a possible wander destination by selecting an area which hasn't been visited recently.
        /// Can be used only is CanBeUsed returns true.
        /// </summary>
        public Vector3 GetRecommendedDestination()
        {
            if (!CanBeUsed)
                throw new InvalidOperationException(
                    "Cannot use map knowledge destination recommender, no area info available"
                );

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

            PrintArea(selectedArea, Color.blue);

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
            return !(position.z > area.topRow) && !(position.z < area.bottomRow) && !(position.x < area.leftColumn) &&
                   !(position.x > area.rightColumn);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private Vector3 ConvertToAreasCoordinate(Vector3 position)
        {
            return position / gridScale;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private Vector3 ConvertToActualMapCoordinate(Vector3 position)
        {
            return position * gridScale;
        }
    }
}
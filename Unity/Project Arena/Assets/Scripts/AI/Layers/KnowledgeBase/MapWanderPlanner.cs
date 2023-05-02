using System;
using System.Runtime.CompilerServices;
using AI.Layers.Memory;
using Maps.MapGenerator;
using UnityEngine;
using Random = UnityEngine.Random;

namespace AI.Layers.Planners
{
    /// <summary>
    /// This component represents the entity knowledge of the map. It requires support from the map itself, which
    /// must expose its <see cref="Area"/> representation.
    /// </summary>
    public class MapWanderPlanner
    {
        private readonly AIEntity me;
        private MapMemory _mapMemory;

        public MapWanderPlanner(AIEntity entity)
        {
            me = entity;
        }

        public void Prepare()
        {
            _mapMemory = me.MapMemory;
        }
        
        /// <summary>
        /// Can this component be used? If not, <see cref="GetRecommendedDestination"/> will return
        /// InvalidOperationException.
        /// </summary>
        public bool CanBeUsed => _mapMemory.areas.Length != 0;
        
        private void PrintArea(Area area, Color color)
        {
            // var topLeft = ConvertToActualMapCoordinate(new Vector3(area.leftColumn, 0, area.topRow));
            // var bottomLeft = ConvertToActualMapCoordinate(new Vector3(area.leftColumn, 0, area.bottomRow));
            // var topRight = ConvertToActualMapCoordinate(new Vector3(area.rightColumn, 0, area.topRow));
            // var bottomRight = ConvertToActualMapCoordinate(new Vector3(area.rightColumn, 0, area.bottomRow));
            //
            // Debug.DrawLine(topLeft, bottomLeft, color, 2, false);
            // Debug.DrawLine(bottomLeft, bottomRight, color, 2, false);
            // Debug.DrawLine(bottomRight, topRight, color, 2, false);
            // Debug.DrawLine(topRight, topLeft, color, 2, false);
        }

        /// <summary>
        /// Returns a possible wander destination by selecting an area which hasn't been visited recently.
        /// </summary>
        //  TODO add a bit of randomness?
        public Area GetRecommendedArea()
        {
            if (!CanBeUsed)
                throw new InvalidOperationException(
                    "Cannot use map knowledge destination recommender, no area info available"
                );

            var leastKnownArea = 0;
            var earliestVisitedAreaTime = float.MaxValue;

            if (Random.value < 0.1)
            {
                // Select random destination to avoid getting stuck following the same path
                var areaIndex = Random.Range(0, _mapMemory.areas.Length);
                return _mapMemory.areas[areaIndex];
            }

            for (var i = 0; i < _mapMemory.areas.Length; i++)
            {
                if (_mapMemory.areas[i].isCorridor) continue;
                var areaScore = _mapMemory.areasLastVisitTime[i];
                if (areaScore < earliestVisitedAreaTime)
                {
                    earliestVisitedAreaTime = areaScore;
                    leastKnownArea = i;
                }
                // ReSharper disable once CompareOfFloatsByEqualityOperator
                else if (areaScore == earliestVisitedAreaTime && Random.value < 0.3f)
                {
                    earliestVisitedAreaTime = areaScore;
                    leastKnownArea = i;
                }
            }

            return _mapMemory.areas[leastKnownArea];
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
            var earliestVisitedAreaTime = float.MaxValue;
            for (var i = 0; i < _mapMemory.areas.Length; i++)
            {
                var areaScore = _mapMemory.areasLastVisitTime[i];
                if (areaScore < earliestVisitedAreaTime)
                {
                    earliestVisitedAreaTime = areaScore;
                    leastKnownArea = i;
                }
                // ReSharper disable once CompareOfFloatsByEqualityOperator
                else if (areaScore == earliestVisitedAreaTime && Random.value < 0.3f)
                {
                    earliestVisitedAreaTime = areaScore;
                    leastKnownArea = i;
                }
            }

            var selectedArea = _mapMemory.areas[leastKnownArea];

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
        private Vector3 ConvertToActualMapCoordinate(Vector3 position)
        {
            return position * _mapMemory.gridScale;
        }
    }
}
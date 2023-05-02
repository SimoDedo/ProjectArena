using System.Runtime.CompilerServices;
using Managers.Mode;
using Maps.MapGenerator;
using UnityEngine;

namespace AI.Layers.KnowledgeBase
{
    /// <summary>
    /// This component represents the entity knowledge of the map. It requires support from the map itself, which
    /// must expose its <see cref="Area"/> representation.
    /// </summary>
    public class MapMemory
    {
        public readonly Area[] areas;

        public readonly float[] areasLastVisitTime;

        public readonly float gridScale;

        // private readonly AIEntity me;
        private readonly Transform transform;


        public MapMemory(AIEntity entity, GameManager gms)
        {
            // me = entity;
            transform = entity.transform;
            areas = gms.GetAreas();
            if (areas.Length == 0)
            {
                Debug.Log("Cannot use map knowledge, no areas were defined...");
                return;
            }

            areasLastVisitTime = new float[areas.Length];
            gridScale = gms.GetMapScale();
        }
        
        public void Update()
        {
            // Check in which areas I am in. Mark them as visited (immediately? no timeout?)
            var currentPosition = ConvertToAreasCoordinate(transform.position);
            for (var i = 0; i < areas.Length; i++)
                if (PositionInArea(areas[i], currentPosition))
                {
                    // TODO Maybe use our own counter?
                    areasLastVisitTime[i] = Time.time;
                    PrintArea(areas[i], Color.magenta);
                }
        }

        private void PrintAreas()
        {
            foreach (var area in areas) PrintArea(area, Color.green);
        }

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
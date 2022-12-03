using System;
using AI.Behaviours.Variables;
using AI.Layers.KnowledgeBase;
using AI.Layers.Memory;
using BehaviorDesigner.Runtime.Tasks;
using Others;
using UnityEngine;
using UnityEngine.AI;
using Action = BehaviorDesigner.Runtime.Tasks.Action;
using Random = UnityEngine.Random;

namespace AI.Behaviours.Actions
{
    /// <summary>
    /// Moves to the selected area until either a random chosen point in the area is reached or the we stay in the area
    /// for too long.
    /// </summary>
    [Serializable]
    public class MoveToSelectedArea : Action
    {
        [SerializeField] private SharedSelectedArea selectedArea;
        private NavigationSystem navSystem;
        private Transform t;
        private float mapScale;
        private float timeInArea;
        private float overstayTime; 
        
        public override void OnAwake()
        {
            var entity = GetComponent<AIEntity>();
            t = entity.transform;
            mapScale = entity.MapMemory.gridScale;
            navSystem = entity.NavigationSystem;
        }

        public override void OnStart()
        {
            var area = selectedArea.Value;
            var hasPath = false;
            while (!hasPath)
            {
                var columnDelta = area.rightColumn - area.leftColumn;
                var rowDelta = area.topRow - area.bottomRow;
                var areaCenter = new Vector3(
                    area.leftColumn + Random.value * columnDelta,
                    0,
                    area.bottomRow + Random.value * rowDelta
                );

                var wanderPosition = areaCenter * mapScale;
                var path = navSystem.CalculatePath(wanderPosition);
                if (path.IsComplete())
                {
                    hasPath = true;
                    navSystem.SetPath(path);
                }

                overstayTime = SelectedAreaOverstayTime();
                timeInArea = 0;
            }
        }

        private float SelectedAreaOverstayTime()
        {
            var area = selectedArea.Value;
            var width = (area.rightColumn - area.leftColumn) * mapScale;
            var height = (area.topRow - area.bottomRow) * mapScale;
            var diagonal = Mathf.Sqrt(width * width + height * height);
            var walkTime = diagonal / navSystem.Speed;
            return walkTime * 0.1f;
        }

        public override TaskStatus OnUpdate()
        {
            navSystem.MoveAlongPath();
            if (navSystem.HasArrivedToDestination())
            {
                return TaskStatus.Success;
            }

            if (IsInArea())
            {
                timeInArea += Time.deltaTime;
                if (timeInArea >= overstayTime)
                {
                    return TaskStatus.Success;
                }

                return TaskStatus.Running;
            }

            return TaskStatus.Running;
        }

        private bool IsInArea()
        {
            var position = t.position / mapScale;
            var area = selectedArea.Value;
            if (position.x < area.leftColumn) return false;
            if (position.x > area.rightColumn) return false;
            if (position.z < area.bottomRow) return false;
            if (position.z > area.topRow) return false;
            return true;
        }
    }
}
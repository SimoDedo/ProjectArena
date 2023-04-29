using System;
using AI.Layers.KnowledgeBase;
using Bonsai;
using Maps.MapGenerator;
using Others;
using UnityEngine;
using Random = UnityEngine.Random;
using Task = Bonsai.Core.Task;

namespace AI.BonsaiBehaviours.Actions
{
    /// <summary>
    /// Moves to the selected area until either a random chosen point in the area is reached or the we stay in the area
    /// for too long.
    /// </summary>
    [BonsaiNode("Tasks/")]
    public class MoveToSelectedArea : Task
    {
        public string chosenAreaKey;
        private Area SelectedArea => Blackboard.Get<Area>(chosenAreaKey);
        private NavigationSystem navSystem;
        private Transform t;
        private float mapScale;
        private float timeInArea;
        private float overstayTime; 
        
        public override void OnStart()
        {
            var entity = Actor.GetComponent<AIEntity>();
            t = entity.transform;
            mapScale = entity.MapMemory.gridScale;
            navSystem = entity.NavigationSystem;
        }

        public override void OnEnter()
        {
            var selectedArea = SelectedArea;
            var hasPath = false;
            while (!hasPath)
            {
                var columnDelta = selectedArea.rightColumn - selectedArea.leftColumn;
                var rowDelta = selectedArea.topRow - selectedArea.bottomRow;
                var selectedAreaCenter = new Vector3(
                    selectedArea.leftColumn + Random.value * columnDelta,
                    0,
                    selectedArea.bottomRow + Random.value * rowDelta
                );

                var wanderPosition = selectedAreaCenter * mapScale;
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
            var area = SelectedArea;
            var width = (area.rightColumn - area.leftColumn) * mapScale;
            var height = (area.topRow - area.bottomRow) * mapScale;
            var diagonal = Mathf.Sqrt(width * width + height * height);
            var walkTime = diagonal / navSystem.Speed;
            return walkTime * Random.value / 1.5f;
        }

        public override Status Run()
        {
            navSystem.MoveAlongPath();
            if (navSystem.HasArrivedToDestination())
            {
                return Status.Success;
            }

            if (!IsInArea()) return Status.Running;
            timeInArea += Time.deltaTime;
            return timeInArea >= overstayTime ? Status.Success : Status.Running;
        }

        private bool IsInArea()
        {
            var position = t.position / mapScale;
            var area = SelectedArea;
            if (position.x < area.leftColumn) return false;
            if (position.x > area.rightColumn) return false;
            if (position.z < area.bottomRow) return false;
            if (position.z > area.topRow) return false;
            return true;
        }
    }
}
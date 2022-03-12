using System;
using AI.Behaviours.Variables;
using AI.Layers.KnowledgeBase;
using AI.Layers.Memory;
using AI.Layers.Planners;
using BehaviorDesigner.Runtime.Tasks;
using Others;
using UnityEngine;
using Action = BehaviorDesigner.Runtime.Tasks.Action;

namespace AI.Behaviours.Actions
{
    /// <summary>
    /// Selects the area to visit next while wandering, based on the selection given by the <see cref="MapMemory"/>
    /// </summary>
    [Serializable]
    public class SelectAreaToVisit : Action
    {
        [SerializeField] private SharedSelectedPathInfo pathChosen;
        private MapWanderPlanner wanderPlanner;
        private NavigationSystem navSystem;

        public override void OnAwake()
        {
            var entity = GetComponent<AIEntity>();
            wanderPlanner = entity.MapWanderPlanner;
            navSystem = entity.NavigationSystem;
        }

        public override TaskStatus OnUpdate()
        {
            var destination = wanderPlanner.GetRecommendedDestination();
            var path = navSystem.CalculatePath(destination);
            if (!path.IsComplete()) return TaskStatus.Running;
            pathChosen.Value = path;
            return TaskStatus.Success;
        }
    }
}
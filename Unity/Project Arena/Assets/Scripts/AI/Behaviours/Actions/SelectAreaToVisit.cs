using System;
using AI.AI.Layer2;
using AI.Behaviours.Variables;
using BehaviorDesigner.Runtime.Tasks;
using Others;
using UnityEngine;
using Action = BehaviorDesigner.Runtime.Tasks.Action;

namespace AI.Behaviours.Actions
{
    /// <summary>
    /// Selects the area to visit next while wandering, based on the selection given by the <see cref="MapKnowledge"/>
    /// </summary>
    [Serializable]
    public class SelectAreaToVisit : Action
    {
        [SerializeField] private SharedSelectedPathInfo pathChosen;
        private MapKnowledge mapKnowledge;
        private NavigationSystem navSystem;

        public override void OnAwake()
        {
            var entity = GetComponent<AIEntity>();
            mapKnowledge = entity.MapKnowledge;
            navSystem = entity.NavigationSystem;
        }

        public override TaskStatus OnUpdate()
        {
            var destination = mapKnowledge.GetRecommendedDestination();
            var path = navSystem.CalculatePath(destination);
            if (!path.IsComplete()) return TaskStatus.Running;
            pathChosen.Value = path;
            return TaskStatus.Success;
        }
    }
}
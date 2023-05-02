using System;
using AI.Behaviours.Variables;
using AI.Layers.KnowledgeBase;
using BehaviorDesigner.Runtime.Tasks;
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
        [SerializeField] private SharedSelectedArea areaChosen;
        private MapWanderPlanner wanderPlanner;

        public override void OnAwake()
        {
            var entity = GetComponent<AIEntity>();
            wanderPlanner = entity.MapWanderPlanner;
        }

        public override TaskStatus OnUpdate()
        {
            areaChosen.Value = wanderPlanner.GetRecommendedArea();
            return TaskStatus.Success;
        }
    }
}
using System;
using AI.Behaviours.Variables;
using AI.Layers.Memory;
using AI.Layers.Planners;
using BehaviorDesigner.Runtime.Tasks;
using Bonsai;
using UnityEngine;
using Action = BehaviorDesigner.Runtime.Tasks.Action;
using Task = Bonsai.Core.Task;

namespace AI.BonsaiBehaviours.Actions
{
    /// <summary>
    /// Selects the area to visit next while wandering, based on the selection given by the <see cref="MapMemory"/>
    /// </summary>
    [BonsaiNode("Tasks/")]
    public class SelectAreaToVisit : Task
    {
        private MapWanderPlanner wanderPlanner;

        public override void OnStart()
        {
            var entity = Actor.GetComponent<AIEntity>();
            wanderPlanner = entity.MapWanderPlanner;
        }

        public override Status Run()
        {
            Blackboard.Set("areaChosen", wanderPlanner.GetRecommendedArea());
            return Status.Success;
        }
    }
}
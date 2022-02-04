using System;
using AssemblyAI.AI.Layer2;
using AssemblyAI.AI.Layer3;
using AssemblyAI.Behaviours.Variables;
using BehaviorDesigner.Runtime.Tasks;
using Others;
using UnityEngine;
using Action = BehaviorDesigner.Runtime.Tasks.Action;

namespace AssemblyAI.Behaviours.Actions
{
    [Serializable]
    public class SelectAreaToVisit : Action
    {
        private MapKnowledge mapKnowledge;
        private NavigationSystem navSystem;
        [SerializeField] private SharedSelectedPathInfo pathChosen;

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
            if (!path.IsComplete())
            {
                return TaskStatus.Running;
            }
            pathChosen.Value = path;
            return TaskStatus.Success;
        }
    }
}
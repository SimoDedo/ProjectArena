using System;
using AI.Layers.KnowledgeBase;
using BehaviorDesigner.Runtime.Tasks;

namespace AI.Behaviours.Conditions
{
    /// <summary>
    /// Returns Success if the entity possesses Area knowledge, Failure otherwise.
    /// </summary>
    [Serializable]
    public class HasAreaKnowledge : Conditional
    {
        private MapWanderPlanner wanderPlanner;

        public override void OnAwake()
        {
            wanderPlanner = GetComponent<AIEntity>().MapWanderPlanner;
        }

        public override TaskStatus OnUpdate()
        {
            return wanderPlanner.CanBeUsed ? TaskStatus.Success : TaskStatus.Failure;
        }
    }
}
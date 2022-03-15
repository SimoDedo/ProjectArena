using System;
using AI.Behaviours.Variables;
using AI.Layers.KnowledgeBase;
using BehaviorDesigner.Runtime.Tasks;
using UnityEngine;
using Action = BehaviorDesigner.Runtime.Tasks.Action;

namespace AI.Behaviours.Actions
{
    /// <summary>
    /// Applies the path given in pathToApply to the agent, returning Running while the destination has not been reached
    ///   and Success after.
    /// </summary>
    [Serializable]
    public class ApplyPathToAgent : Action
    {
        [SerializeField] private SharedSelectedPathInfo pathToApply;
        private NavigationSystem navSystem;

        public override void OnAwake()
        {
            navSystem = GetComponent<AIEntity>().NavigationSystem;
        }

        public override void OnStart()
        {
            navSystem.SetPath(pathToApply.Value);
        }

        public override void OnEnd()
        {
            navSystem.CancelPath();
        }

        public override TaskStatus OnUpdate()
        {
            if (navSystem.HasArrivedToDestination())
                return TaskStatus.Success;
            navSystem.MoveAlongPath();
            return TaskStatus.Running;
        }
    }
}
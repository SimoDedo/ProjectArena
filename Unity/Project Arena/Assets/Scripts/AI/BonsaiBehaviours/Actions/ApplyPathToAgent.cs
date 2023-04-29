using System;
using AI.Behaviours.Variables;
using AI.Layers.KnowledgeBase;
using BehaviorDesigner.Runtime.Tasks;
using Bonsai;
using UnityEngine;
using UnityEngine.AI;
using Action = BehaviorDesigner.Runtime.Tasks.Action;
using Task = Bonsai.Core.Task;

namespace AI.BonsaiBehaviours.Actions
{
    /// <summary>
    /// Applies the path given in pathToApply to the agent, returning Running while the destination has not been reached
    ///   and Success after.
    /// </summary>
    [BonsaiNode("Tasks/")]
    public class ApplyPathToAgent : Task
    {
        private NavigationSystem navSystem;

        public override void OnStart()
        {
            navSystem = Actor.GetComponent<AIEntity>().NavigationSystem;
        }

        public override void OnEnter()
        {
            var path = Blackboard.Get("pathChosen") as NavMeshPath;
            navSystem.SetPath(path);
        }

        public override void OnExit()
        {
            navSystem.CancelPath();
        }

        public override Status Run()
        {
            if (navSystem.HasArrivedToDestination())
                return Status.Success;
            navSystem.MoveAlongPath();
            return Status.Running;
        }
    }
}
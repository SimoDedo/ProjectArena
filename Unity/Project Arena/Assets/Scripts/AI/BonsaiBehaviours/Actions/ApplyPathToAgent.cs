using AI.Layers.KnowledgeBase;
using Bonsai;
using UnityEngine.AI;
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
        public string pathChosenKey;
        private NavMeshPath PathChosen => Blackboard.Get<NavMeshPath>(pathChosenKey);

        public override void OnStart()
        {
            navSystem = Actor.GetComponent<AIEntity>().NavigationSystem;
        }

        public override void OnEnter()
        {
            navSystem.SetPath(PathChosen);
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
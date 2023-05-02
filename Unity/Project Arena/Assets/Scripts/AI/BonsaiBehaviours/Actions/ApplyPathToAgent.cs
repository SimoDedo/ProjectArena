using AI.Layers.Actuators;
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
        private MovementController mover;
        public string pathChosenKey;
        private NavMeshPath PathChosen => Blackboard.Get<NavMeshPath>(pathChosenKey);

        public override void OnStart()
        {
            var entity = Actor.GetComponent<AIEntity>();
            navSystem = entity.NavigationSystem;
            mover = entity.MovementController;
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
            mover.MoveToPosition(navSystem.GetNextPosition());
            return Status.Running;
        }
    }
}
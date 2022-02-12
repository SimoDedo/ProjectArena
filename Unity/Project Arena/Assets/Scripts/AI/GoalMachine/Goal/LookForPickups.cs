using AI.AI.Layer3;
using BehaviorDesigner.Runtime;
using BehaviorDesigner.Runtime.Tasks;
using Pickables;
using UnityEngine;

namespace AI.GoalMachine.Goal
{
    public class LookForPickups : IGoal
    {
        private readonly ExternalBehaviorTree externalBt;
        private readonly BehaviorTree behaviorTree;
        private readonly PickupPlanner pickupPlanner;
        private Pickable currentPickable;
        private Pickable nextPickable;

        public LookForPickups(AIEntity entity)
        {
            pickupPlanner = entity.PickupPlanner;
            externalBt = Resources.Load<ExternalBehaviorTree>("Behaviors/NewPickup");
            behaviorTree = entity.gameObject.AddComponent<BehaviorTree>();
            behaviorTree.StartWhenEnabled = false;
            behaviorTree.ResetValuesOnRestart = true;
            behaviorTree.ExternalBehavior = externalBt;
        }

        public float GetScore()
        {
            nextPickable = pickupPlanner.GetChosenPickup();
            return pickupPlanner.GetChosenPickupScore();
        }


        public void Enter()
        {
            // Do not enable behavior, it will be done in Update!
            currentPickable = null;
        }

        public void Update()
        {
            if (currentPickable != nextPickable)
            {
                behaviorTree.DisableBehavior();
                behaviorTree.EnableBehavior();
                BehaviorManager.instance.RestartBehavior(behaviorTree);

                // var pickupInfo = new SelectedPickupInfo
                // {
                //     pickup = nextPickable,
                //     estimatedActivationTime = nextPickableActivationTime
                // };
                // behaviorTree.SetVariableValue("ChosenPickup", pickupInfo);
                // behaviorTree.SetVariableValue("ChosenPickupPosition", nextPickable.transform.position);
                // behaviorTree.SetVariableValue("ChosenPath", newPath);

                currentPickable = nextPickable;
            }

            BehaviorManager.instance.Tick(behaviorTree);

            if (behaviorTree.ExecutionStatus != TaskStatus.Running)
            {
                // The tree finished execution. We must have picked up the pickable or maybe our activation time
                // estimate was wrong. Force update of the planner?
                // Must first force update of the knowledge base? The knowledge base automatically knows the status
                // of the pickup if we are close (even when not looking).
                pickupPlanner.ForceUpdate();
                behaviorTree.DisableBehavior();
                behaviorTree.EnableBehavior();
                BehaviorManager.instance.RestartBehavior(behaviorTree);
            }
        }

        public void Exit()
        {
            behaviorTree.DisableBehavior();
        }
    }
}
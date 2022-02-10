using AssemblyAI.AI.Layer2;
using AssemblyAI.AI.Layer3;
using AssemblyAI.Behaviours.Variables;
using BehaviorDesigner.Runtime;
using BehaviorDesigner.Runtime.Tasks;
using UnityEngine;
using UnityEngine.AI;

namespace AssemblyAI.GoalMachine.Goal
{
    public class LookForPickups : IGoal
    {
        private readonly ExternalBehaviorTree externalBt;
        private readonly BehaviorTree behaviorTree;
        private readonly PickupPlanner pickupPlanner;
        private readonly NavigationSystem navSystem;
        private Pickable currentPickable;
        private Pickable nextPickable;
        private float nextPickableActivationTime;
        private NavMeshPath newPath;

        public LookForPickups(AIEntity entity)
        {
            pickupPlanner = entity.PickupPlanner;
            navSystem = entity.NavigationSystem;
            externalBt = Resources.Load<ExternalBehaviorTree>("Behaviors/NewPickup");
            behaviorTree = entity.gameObject.AddComponent<BehaviorTree>();
            behaviorTree.StartWhenEnabled = false;
            behaviorTree.ResetValuesOnRestart = true;
            behaviorTree.ExternalBehavior = externalBt;
        }

        public float GetScore()
        {
            var (pickup, score, estimatedActivationTime) = pickupPlanner.GetBestPickupInfo();
            nextPickable = pickup;
            nextPickableActivationTime = estimatedActivationTime;
            newPath = navSystem.CalculatePath(nextPickable.transform.position);
            return score;
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

                var pickupInfo = new SelectedPickupInfo
                {
                    pickup = nextPickable,
                    estimatedActivationTime = nextPickableActivationTime
                };
                behaviorTree.SetVariableValue("ChosenPickup", pickupInfo);
                behaviorTree.SetVariableValue("ChosenPickupPosition", nextPickable.transform.position);
                behaviorTree.SetVariableValue("ChosenPath", newPath);

                currentPickable = nextPickable;
            }
            
            BehaviorManager.instance.Tick(behaviorTree);
            
            if (behaviorTree.ExecutionStatus == TaskStatus.Failure)
            {
                Debug.LogError("What happened?");
            }
        }

        public void Exit()
        {
            behaviorTree.DisableBehavior();
        }
    }
}
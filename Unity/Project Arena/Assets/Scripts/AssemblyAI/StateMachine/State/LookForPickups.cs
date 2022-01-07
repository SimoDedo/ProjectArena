using AssemblyAI.AI.Layer3;
using AssemblyAI.Behaviours.Variables;
using AssemblyAI.StateMachine.Transition;
using BehaviorDesigner.Runtime;
using BehaviorDesigner.Runtime.Tasks;
using UnityEngine;
using UnityEngine.AI;

namespace AssemblyAI.StateMachine.State
{
    public class LookForPickups : IState
    {
        private readonly AIEntity entity;
        private ExternalBehaviorTree externalBt;
        private BehaviorTree behaviorTree;
        private readonly PickupPlanner pickupPlanner;
        private Pickable currentPickable;
        private Pickable nextPickable;
        private float nextPickableActivationTime;
        private NavMeshPath newPath;
        public ITransition[] OutgoingTransitions { get; private set; }

        public LookForPickups(AIEntity entity)
        {
            this.entity = entity;
            pickupPlanner = entity.PickupPlanner;
        }

        public float CalculateTransitionScore()
        {
            nextPickable = pickupPlanner.ScorePickups(out var path, out var score, out var activationTime);
            newPath = path;
            nextPickableActivationTime = activationTime;
            return score;
        }


        public void Enter()
        {
            externalBt = Resources.Load<ExternalBehaviorTree>("Behaviors/NewPickup");
            behaviorTree = entity.gameObject.AddComponent<BehaviorTree>();
            behaviorTree.StartWhenEnabled = false;
            behaviorTree.ResetValuesOnRestart = true;
            behaviorTree.ExternalBehavior = externalBt;
            behaviorTree.EnableBehavior();
            BehaviorManager.instance.UpdateInterval = UpdateIntervalType.Manual;
            
            OutgoingTransitions = new ITransition[]
            {
                new ToPickupTransition(this), // Self-loop
                new OnEnemyInSightTransition(entity),
                new ToWanderTransition(entity),
                new OnDamagedTransition(entity),
            };
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
            Resources.UnloadAsset(externalBt);
            Object.Destroy(behaviorTree);
        }
    }
}
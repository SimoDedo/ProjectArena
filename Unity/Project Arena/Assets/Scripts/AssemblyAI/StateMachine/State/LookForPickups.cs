using System.Collections.Generic;
using AI.AI.Layer3;
using AssemblyAI.Behaviours.Variables;
using AssemblyAI.StateMachine;
using AssemblyAI.StateMachine.Transition;
using AssemblyLogging;
using BehaviorDesigner.Runtime;
using BehaviorDesigner.Runtime.Tasks;
using UnityEngine;
using UnityEngine.AI;

namespace AssemblyAI.State
{
    public class LookForPickups : IState
    {
        private AIEntity entity;
        private ExternalBehaviorTree externalBT;
        private BehaviorTree behaviorTree;
        private PickupPlanner pickupPlanner;
        private Pickable currentPickable;
        private Pickable nextPickable;
        private float nextPickableActivationTime;
        private NavMeshPath newPath;
        public ITransition[] OutgoingTransitions { get; private set; }

        public LookForPickups(AIEntity entity)
        {
            this.entity = entity;
            pickupPlanner = entity.GetComponent<PickupPlanner>();
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
            externalBT = Resources.Load<ExternalBehaviorTree>("Behaviors/PlanPickups");
            behaviorTree = entity.gameObject.AddComponent<BehaviorTree>();
            behaviorTree.StartWhenEnabled = false;
            behaviorTree.ResetValuesOnRestart = true;
            behaviorTree.ExternalBehavior = externalBT;
            behaviorTree.EnableBehavior();
            BehaviorManager.instance.UpdateInterval = UpdateIntervalType.Manual;

            OutgoingTransitions = new ITransition[]
            {
                new PickupSelfLoop(this), // Self-loop
                new OnEnemyInSightTransition(entity, EnterFightAction),
                new ToWanderTransition(entity, EnterFightAction),
                new OnDamagedTransition(entity),
            };
        }

        private void EnterFightAction()
        {
            var position = entity.transform.position;
            FightEnterGameEvent.Instance.Raise(
                new EnterFightInfo {x = position.x, z = position.z, entityId = entity.GetID()}
            );
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
                    pickup = nextPickable, estimatedActivationTime = nextPickableActivationTime
                };
                behaviorTree.SetVariableValue("ChosenPickup", pickupInfo);
                behaviorTree.SetVariableValue("ChosenPickupPosition", nextPickable.transform.position);
                behaviorTree.SetVariableValue("ChosenPath", newPath);

                currentPickable = nextPickable;
            }

            BehaviorManager.instance.Tick(behaviorTree);
            if (behaviorTree.ExecutionStatus == TaskStatus.Failure)
            {
                Debug.Log("What happened?");
            }
        }

        public void Exit()
        {
            Resources.UnloadAsset(externalBT);
            Object.Destroy(behaviorTree);
        }
    }
}
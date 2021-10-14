using System.Collections.Generic;
using AI.AI.Layer3;
using AssemblyAI.Behaviours.Variables;
using BehaviorDesigner.Runtime;
using UnityEngine;
using UnityEngine.AI;
using Object = UnityEngine.Object;

namespace AssemblyAI.State
{
    public class LookForPickups : IState
    {
        public LookForPickups(AIEntity entity)
        {
            this.entity = entity;
            pickupPlanner = entity.GetComponent<PickupPlanner>();
        }

        private AIEntity entity;
        private ExternalBehaviorTree externalBT;
        private BehaviorTree behaviorTree;
        private List<IState> outgoingStates = new List<IState>();


        private PickupPlanner pickupPlanner;
        private Pickable currentPickable = null;
        private Pickable nextPickable;
        private float nextPickableActivationTime;
        private NavMeshPath newPath;

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

            outgoingStates.Add(new Fight(entity));
            outgoingStates.Add(new Wander(entity));
            outgoingStates.Add(new SearchForDamageSource(entity));
        }

        public void Update()
        {
            var bestScore = CalculateTransitionScore();
            IState bestState = null;
            foreach (var state in outgoingStates)
            {
                var score = state.CalculateTransitionScore();
                if (score > bestScore)
                {
                    bestScore = score;
                    bestState = state;
                }
            }

            if (bestState != null)
                entity.SetNewState(bestState);
            else
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
            }
        }

        public void Exit()
        {
            Resources.UnloadAsset(externalBT);
            Object.Destroy(behaviorTree);
        }
    }
}
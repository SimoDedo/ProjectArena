using System.Collections.Generic;
using BehaviorDesigner.Runtime;
using UnityEngine;

namespace AssemblyAI.State
{
    public class Wander : IState
    {
        private AIEntity entity;
        public Wander(AIEntity entity)
        {
            this.entity = entity;
        }

        public float CalculateTransitionScore()
        {
            return 0.2f;
        }

        private ExternalBehaviorTree externalBT;
        private BehaviorTree behaviorTree;
        private List<IState> outgoingStates = new List<IState>();
        public void Enter()
        {
            externalBT = Resources.Load<ExternalBehaviorTree>("Behaviors/Wander");
            behaviorTree = entity.gameObject.AddComponent<BehaviorTree>();
            behaviorTree.StartWhenEnabled = false;
            behaviorTree.RestartWhenComplete = true;
            behaviorTree.ExternalBehavior = externalBT;
            behaviorTree.EnableBehavior();
            BehaviorManager.instance.UpdateInterval = UpdateIntervalType.Manual;

            // outgoingStates.Add(new Fight(entity)); 
            // outgoingStates.Add(new LookForPickups(entity)); 
            // outgoingStates.Add(new SearchForDamageSource(entity)); 
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
                BehaviorManager.instance.Tick(behaviorTree);
        }

        public void Exit()
        {
            Resources.UnloadAsset(externalBT);
            Object.Destroy(behaviorTree);
        }
    }
}
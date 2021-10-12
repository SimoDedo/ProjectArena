using System.Collections.Generic;
using BehaviorDesigner.Runtime;
using UnityEngine;

namespace AssemblyAI.State
{
    public class SearchForLostEnemy : IState
    {
        public SearchForLostEnemy(AIEntity entity)
        {
            this.entity = entity;
        }
    
        private AIEntity entity;
        private ExternalBehaviorTree externalBT;
        private BehaviorTree behaviorTree;
        private List<IState> outgoingStates = new List<IState>();

        public float CalculateTransitionScore()
        {
            if (entity.GetEnemy().isAlive && !entity.CanSeeEnemy())
            {
                // TODO calculate based on health and ammo available? No, simply do not give max score!
                //  Also add a penalty term the longer the search is going on
                return 0.7f;
            }
            return 0f;
        }
        public void Enter()
        {
            externalBT = Resources.Load<ExternalBehaviorTree>("Behaviors/SearchForLostEnemy");
            behaviorTree = entity.gameObject.AddComponent<BehaviorTree>();
            behaviorTree.StartWhenEnabled = false;
            behaviorTree.RestartWhenComplete = true;
            behaviorTree.ExternalBehavior = externalBT;
            behaviorTree.EnableBehavior();
            BehaviorManager.instance.UpdateInterval = UpdateIntervalType.Manual;
                
            outgoingStates.Add(new Wander(entity));
            outgoingStates.Add(new LookForPickups(entity));
            outgoingStates.Add(new Fight(entity));
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
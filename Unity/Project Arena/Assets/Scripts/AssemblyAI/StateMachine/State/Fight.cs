using System.Collections.Generic;
using AI.KnowledgeBase;
using AssemblyAI.StateMachine;
using AssemblyAI.StateMachine.Transition;
using BehaviorDesigner.Runtime;
using UnityEngine;

namespace AssemblyAI.State
{
    public class Fight : IState
    {
        private AIEntity entity;
        private TargetKnowledgeBase targetKB;
        private ExternalBehaviorTree externalBT;
        private BehaviorTree behaviorTree;
        public ITransition[] OutgoingTransitions { get; private set; }

        public Fight(AIEntity entity)
        {
            this.entity = entity;
            targetKB = entity.GetComponent<TargetKnowledgeBase>();
        }
        
        public float FightTransitionScore(bool isResumingFight = false)
        {
            // TODO maybe we see enemy, but we want to run away?
            var canSee = targetKB.HasSeenTarget(isResumingFight);
            return canSee ? 0.95f : 0.0f;
        }


        public void Enter()
        {
            externalBT = Resources.Load<ExternalBehaviorTree>("Behaviors/Fight");
            behaviorTree = entity.gameObject.AddComponent<BehaviorTree>();
            behaviorTree.StartWhenEnabled = false;
            behaviorTree.RestartWhenComplete = true;
            behaviorTree.ExternalBehavior = externalBT;
            behaviorTree.EnableBehavior();
            BehaviorManager.instance.UpdateInterval = UpdateIntervalType.Manual;
            
            OutgoingTransitions = new ITransition[]
            {
                new OnEnemyInSightTransition(this), // Self-loop 
                new ToWanderTransition(entity),
                new ToSearchTransition(entity),
                new ToPickupTransition(entity), 
            };
        }
    
        public void Update()
        {
            BehaviorManager.instance.Tick(behaviorTree);
        }
    
        public void Exit()
        {
            Resources.UnloadAsset(externalBT);
            Object.Destroy(behaviorTree);
        }
    }
}
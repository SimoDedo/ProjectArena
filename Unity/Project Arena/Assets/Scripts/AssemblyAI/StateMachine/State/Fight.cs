using AI.KnowledgeBase;
using AssemblyAI.StateMachine.Transition;
using AssemblyEntity.Component;
using BehaviorDesigner.Runtime;
using UnityEngine;

namespace AssemblyAI.StateMachine.State
{
    public class Fight : IState
    {
        private readonly AIEntity entity;
        private readonly TargetKnowledgeBase targetKb;
        private readonly GunManager gunManager;
        private ExternalBehaviorTree externalBt;
        private BehaviorTree behaviorTree;
        public ITransition[] OutgoingTransitions { get; private set; }

        public Fight(AIEntity entity)
        {
            this.entity = entity;
            targetKb = entity.TargetKb;
            gunManager = entity.GunManager;
        }
        
        public float FightTransitionScore(bool isResumingFight = false)
        {
            if (!gunManager.HasAmmo())
                return 0;
            // TODO maybe we see enemy, but we want to run away?
            var canSee = targetKb.HasSeenTarget(isResumingFight);
            return canSee ? 0.95f : 0.0f;
        }


        public void Enter()
        {
            externalBt = Resources.Load<ExternalBehaviorTree>("Behaviors/Fight");
            behaviorTree = entity.gameObject.AddComponent<BehaviorTree>();
            behaviorTree.StartWhenEnabled = false;
            behaviorTree.RestartWhenComplete = true;
            behaviorTree.ExternalBehavior = externalBt;
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
            Resources.UnloadAsset(externalBt);
            Object.Destroy(behaviorTree);
        }
    }
}
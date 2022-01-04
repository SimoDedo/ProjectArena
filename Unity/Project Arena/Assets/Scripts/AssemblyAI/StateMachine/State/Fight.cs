using AI.KnowledgeBase;
using AssemblyAI.StateMachine.Transition;
using AssemblyEntity.Component;
using BehaviorDesigner.Runtime;
using UnityEngine;

namespace AssemblyAI.StateMachine.State
{
    public class Fight : IState
    {
        private const float LOW_HEALTH_PENALTY = 0.6f;
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

        public float FightTransitionScore()
        {
            if (!gunManager.HasAmmo()) return 0;
            // TODO maybe we see enemy, but we want to run away?
            var canSee = targetKb.HasSeenTarget();
            var inverseHealthPercentage = 1f - (float) entity.Health / entity.MaxHealth;
            return canSee ? 0.8f - inverseHealthPercentage * LOW_HEALTH_PENALTY : 0.0f;
        }


        public void Enter()
        {
            // TODO remove behaviour tree here, since it's rather simple
            
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
            if (entity.GetEnemy().isAlive)
            {
                // If I'm exiting this state but the enemy is still alive, I want to react faster than usual if I spot
                // him again. 
                targetKb.ApplyFocus();
            }
            Resources.UnloadAsset(externalBt);
            Object.Destroy(behaviorTree);
        }
    }
}
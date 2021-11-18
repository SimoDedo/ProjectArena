using AssemblyAI.StateMachine;
using AssemblyAI.StateMachine.Transition;
using BehaviorDesigner.Runtime;
using UnityEngine;

namespace AssemblyAI.State
{
    public class Wander : IState
    {
        private AIEntity entity;
        private ExternalBehaviorTree externalBT;
        private BehaviorTree behaviorTree;
        public ITransition[] OutgoingTransitions { get; private set; }

        public Wander(AIEntity entity)
        {
            this.entity = entity;
        }

        public float CalculateTransitionScore()
        {
            return 0.1f;
        }

        public void Enter()
        {
            externalBT = Resources.Load<ExternalBehaviorTree>("Behaviors/Wander");
            behaviorTree = entity.gameObject.AddComponent<BehaviorTree>();
            behaviorTree.StartWhenEnabled = false;
            behaviorTree.RestartWhenComplete = true;
            behaviorTree.ExternalBehavior = externalBT;
            behaviorTree.EnableBehavior();
            BehaviorManager.instance.UpdateInterval = UpdateIntervalType.Manual;
            
            OutgoingTransitions = new ITransition[]
            {
                new ToWanderTransition(this), // Self-loop
                new OnEnemyInSightTransition(entity),
                new ToPickupTransition(entity),
                new OnDamagedTransition(entity)
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
using AssemblyAI.StateMachine;
using AssemblyAI.StateMachine.Transition;
using AssemblyLogging;
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
                new WanderSelfLoop(this), new OnEnemyInSightTransition(entity, EnterFightAction),
                new ToPickupTransition(entity), new OnDamagedTransition(entity)
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
            BehaviorManager.instance.Tick(behaviorTree);
        }

        public void Exit()
        {
            Resources.UnloadAsset(externalBT);
            Object.Destroy(behaviorTree);
        }
    }
}
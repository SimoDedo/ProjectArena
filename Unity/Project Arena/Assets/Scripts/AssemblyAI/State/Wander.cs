using BehaviorDesigner.Runtime;
using UnityEngine;
using UnityEngine.AI;

namespace AI.State
{
    public class Wander : IState
    {
        public Wander(AIEntity entity)
        {
            this.entity = entity;
        }

        private AIEntity entity;
        private ExternalBehaviorTree externalBT;
        private BehaviorTree behaviorTree;

        public void Enter()
        {
            entity.GetComponent<NavMeshAgent>();
            externalBT = Resources.Load<ExternalBehaviorTree>("Behaviors/Wander");
            behaviorTree = entity.gameObject.AddComponent<BehaviorTree>();
            behaviorTree.StartWhenEnabled = false;
            behaviorTree.RestartWhenComplete = true;
            behaviorTree.ExternalBehavior = externalBT;
            behaviorTree.EnableBehavior();
            BehaviorManager.instance.UpdateInterval = UpdateIntervalType.Manual;
        }

        public void Update()
        {
            if (entity.CanSeeEnemy())
                entity.SetState(new Fight(entity));
            else if (entity.ShouldLookForHealth())
                entity.SetState(new LookForHealth(entity));
            else if (entity.HasTakenDamage())
                entity.SetState(new SearchForLostEnemy(entity));
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
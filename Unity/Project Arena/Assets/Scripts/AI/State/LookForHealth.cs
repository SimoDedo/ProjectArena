using BehaviorDesigner.Runtime;
using UnityEngine;

namespace AI.State
{
    public class LookForHealth : IState
    {
        public LookForHealth(AIEntity entity)
        {
            this.entity = entity;
        }

        private AIEntity entity;
        private ExternalBehaviorTree externalBT;
        private BehaviorTree behaviorTree;

        public void Enter()
        {
            externalBT = Resources.Load<ExternalBehaviorTree>("Behaviors/LookForHealth");
            behaviorTree = entity.gameObject.AddComponent<BehaviorTree>();
            behaviorTree.StartWhenEnabled = false;
            behaviorTree.RestartWhenComplete = true;
            behaviorTree.ExternalBehavior = externalBT;
            behaviorTree.EnableBehavior();
            BehaviorManager.instance.UpdateInterval = UpdateIntervalType.Manual;
        }

        public void Update()
        {
            // TODO Ignore can see enemy if I'm close to destination
            // if (entity.CanSeeEnemy())
            // {
            //     entity.SetState(new Fight(entity));
            // }
            // else
            // {
            BehaviorManager.instance.Tick(behaviorTree);
            // }
        }

        public void Exit()
        {
            Resources.UnloadAsset(externalBT);
            Object.Destroy(behaviorTree);
        }
    }
}
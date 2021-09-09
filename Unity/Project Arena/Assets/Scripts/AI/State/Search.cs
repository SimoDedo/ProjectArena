using BehaviorDesigner.Runtime;
using UnityEngine;

namespace AI.State
{
    public class Search : IState
    {
        public Search(AIEntity entity)
        {
            this.entity = entity;
        }

        private AIEntity entity;
        private ExternalBehaviorTree externalBT;
        private BehaviorTree behaviorTree;
        private float startTimeSearch;

        public void Enter()
        {
            externalBT = Resources.Load<ExternalBehaviorTree>("Behaviors/Search");
            behaviorTree = entity.gameObject.AddComponent<BehaviorTree>();
            behaviorTree.StartWhenEnabled = false;
            behaviorTree.RestartWhenComplete = true;
            behaviorTree.ExternalBehavior = externalBT;
            behaviorTree.EnableBehavior();
            BehaviorManager.instance.UpdateInterval = UpdateIntervalType.Manual;
            startTimeSearch = Time.time;
        }

        public void Update()
        {
            if (entity.CanSeeEnemy())
                entity.SetState(new Fight(entity));
            else if (entity.ShouldLookForHealth())
                entity.SetState(new LookForHealth(entity));
            else if (entity.ReachedSearchTimeout(startTimeSearch))
                entity.SetState(new Wander(entity));
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
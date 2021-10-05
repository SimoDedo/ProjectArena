using AI.KnowledgeBase;
using BehaviorDesigner.Runtime;
using UnityEngine;
using UnityEngine.AI;

namespace AI.State
{
    public class Fight : IState
    {
        public Fight(AIEntity entity)
        {
            this.entity = entity;
        }

        private AIEntity entity;
        private ExternalBehaviorTree externalBT;
        private BehaviorTree behaviorTree;

        public void Enter()
        {
            externalBT = Resources.Load<ExternalBehaviorTree>("Behaviors/Fight");
            behaviorTree = entity.gameObject.AddComponent<BehaviorTree>();
            behaviorTree.StartWhenEnabled = false;
            behaviorTree.RestartWhenComplete = true;
            behaviorTree.ExternalBehavior = externalBT;
            behaviorTree.EnableBehavior();
            BehaviorManager.instance.UpdateInterval = UpdateIntervalType.Manual;
        }

        public void Update()
        {
            if (entity.HasLostTarget())
            {
                // TODO personality of AIEntity
                if (entity.ShouldLookForHealth())
                {
                    entity.SetState(new LookForPickups(entity));
                    return;
                }

                if (entity.GetEnemy().isAlive)
                    entity.SetState(new SearchForLostEnemy(entity));
                else
                    entity.SetState(new Wander(entity));
                return;
            }

            BehaviorManager.instance.Tick(behaviorTree);
        }

        public void Exit()
        {
            Resources.UnloadAsset(externalBT);
            Object.Destroy(behaviorTree);
        }
    }
}
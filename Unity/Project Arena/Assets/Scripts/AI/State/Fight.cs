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
        private NavMeshAgent agent;
        private ExternalBehaviorTree externalBT;
        private BehaviorTree behaviorTree;
        private TargetKnowledgeBase targetKnowledgeBase;

        public void Enter()
        {
            agent = entity.GetComponent<NavMeshAgent>();
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
                    entity.SetState(new LookForHealth(entity));
                    return;
                }

                // TODO how to know if target is dead?
                // if (targetKnowledgeBase)
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
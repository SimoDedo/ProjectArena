using AI.KnowledgeBase;
using BehaviorDesigner.Runtime;
using BehaviorDesigner.Runtime.Tasks;
using UnityEngine;

namespace AI.State
{
    public class LookForPickups : IState
    {
        public LookForPickups(AIEntity entity)
        {
            this.entity = entity;
        }

        private AIEntity entity;
        private ExternalBehaviorTree externalBT;
        private BehaviorTree behaviorTree;
        

        public void Enter()
        {
            externalBT = Resources.Load<ExternalBehaviorTree>("Behaviors/PlanPickups");
            behaviorTree = entity.gameObject.AddComponent<BehaviorTree>();
            behaviorTree.StartWhenEnabled = false;
            behaviorTree.ResetValuesOnRestart = true;
            behaviorTree.ExternalBehavior = externalBT;
            behaviorTree.EnableBehavior();
            BehaviorManager.instance.UpdateInterval = UpdateIntervalType.Manual;
            
        }

        public void Update()
        {
            if (!entity.ShouldLookForHealth() && !entity.ShouldLookForAmmo())
            {
                entity.SetState(new Wander(entity));
                return;
            }
            
            // TODO Ignore can see enemy if I'm close to destination
            if (entity.CanSeeEnemy())
            {
                entity.SetState(new Fight(entity));
                return;
            }

            // TODO MOVE PICKUP SELECTION HERE?
            // var currentLastUpdate = knowledgeBase.GetLastUpdateTime();
            // if (currentLastUpdate > lastKnowledgeBaseUpdate)
            // {
            //     lastKnowledgeBaseUpdate = currentLastUpdate;
            //     BehaviorManager.instance.RestartBehavior(behaviorTree);
            // }
            
            BehaviorManager.instance.Tick(behaviorTree);
            if (behaviorTree.ExecutionStatus == TaskStatus.Failure)
            {
                Debug.Log("Tree failed!");
                behaviorTree.DisableBehavior();
                behaviorTree.EnableBehavior();

                BehaviorManager.instance.RestartBehavior(behaviorTree);
            }

            if (behaviorTree.ExecutionStatus == TaskStatus.Success)
            {
                Debug.Log("Tree success!");
                behaviorTree.DisableBehavior();
                behaviorTree.EnableBehavior();
                BehaviorManager.instance.RestartBehavior(behaviorTree);
            }
        }

        public void Exit()
        {
            Resources.UnloadAsset(externalBT);
            Object.Destroy(behaviorTree);
        }
    }
}
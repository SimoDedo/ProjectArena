using AI.KnowledgeBase;
using BehaviorDesigner.Runtime;
using BehaviorDesigner.Runtime.Tasks;
using BehaviorDesigner.Runtime.Tasks.Unity.UnityGameObject;
using UnityEditor.VersionControl;
using UnityEngine;
using UnityEngine.AI;

namespace AI.State
{
    public interface IState
    {
        public void Enter();
        public void Update();
        public void Exit();
    }


    public class Wander : IState
    {
        public Wander(AIEntity entity)
        {
            this.entity = entity;
        }

        private AIEntity entity;
        private NavMeshAgent agent;
        private ExternalBehaviorTree externalBT;
        private BehaviorTree behaviorTree;

        public void Enter()
        {
            agent = entity.GetComponent<NavMeshAgent>();
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
            else
            {
                if (entity.ShouldLookForHealth())
                    entity.SetState(new LookForHealth(entity));
                if (entity.HasTakenDamage())
                    entity.SetState(new Search(entity));
            }
            BehaviorManager.instance.Tick(behaviorTree);
        }

        public void Exit()
        {
            Resources.UnloadAsset(externalBT);
            Object.Destroy(behaviorTree);
        }
    }
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
            if (entity.CanSeeEnemy())
            {
                entity.SetState(new Fight(entity));
            }
            else
            {
                BehaviorManager.instance.Tick(behaviorTree);
            }
        }

        public void Exit()
        {
            Resources.UnloadAsset(externalBT);
            Object.Destroy(behaviorTree);
        }
    }

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
            if (entity.ShouldLookForHealth())
                entity.SetState(new LookForHealth(entity));
            if (entity.ReachedSearchTimeout(startTimeSearch))
                entity.SetState(new Wander(entity));
            BehaviorManager.instance.Tick(behaviorTree);
        }

        public void Exit()
        {
            Resources.UnloadAsset(externalBT);
            Object.Destroy(behaviorTree);
        }
    }
}
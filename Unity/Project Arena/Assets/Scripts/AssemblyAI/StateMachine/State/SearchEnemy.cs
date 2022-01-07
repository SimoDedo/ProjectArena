using AI.KnowledgeBase;
using AssemblyAI.AI.Layer1.Sensors;
using AssemblyAI.StateMachine.Transition;
using BehaviorDesigner.Runtime;
using UnityEngine;

namespace AssemblyAI.StateMachine.State
{
    public class SearchEnemy : IState
    {
        private const float NO_TIME = -1;
        private readonly AIEntity entity;
        private readonly TargetKnowledgeBase targetKb;
        private readonly DamageSensor damageSensor;
        private readonly bool searchDueToDamage;
        private ExternalBehaviorTree externalBt;
        private BehaviorTree behaviorTree;
        private float startSearchTime = NO_TIME;
        public ITransition[] OutgoingTransitions { get; private set; }

        public SearchEnemy(AIEntity entity, bool searchDueToDamage = false)
        {
            this.entity = entity;
            targetKb = entity.TargetKb;
            damageSensor = entity.DamageSensor;
            this.searchDueToDamage = searchDueToDamage;
        }

        public float LostEnemyTransitionScore()
        {
            if (entity.GetEnemy().isAlive && targetKb.HasLostTarget())
            {
                // ReSharper disable once CompareOfFloatsByEqualityOperator
                if (startSearchTime == NO_TIME)
                    return 0.7f;
                // Slowly decrease want to search. After 5 secs, it's zero
                return 1f - (Time.time - startSearchTime) / 5f;
            }

            return 0f;
        }

        public float DamagedTransitionScore()
        {
            if (entity.GetEnemy().isAlive && damageSensor.WasDamagedRecently)
            {
                return 0.7f;
            }

            return 0f;
        }

        public void Enter()
        {
            externalBt = Resources.Load<ExternalBehaviorTree>("Behaviors/SearchEnemy");
            behaviorTree = entity.gameObject.AddComponent<BehaviorTree>();
            behaviorTree.StartWhenEnabled = false;
            behaviorTree.RestartWhenComplete = true;
            behaviorTree.ExternalBehavior = externalBt;
            behaviorTree.EnableBehavior();
            BehaviorManager.instance.UpdateInterval = UpdateIntervalType.Manual;
            behaviorTree.SetVariableValue("SearchDueToDamage", searchDueToDamage);
            startSearchTime = Time.time;
            
            OutgoingTransitions = new ITransition[]
            {
                new ToSearchTransition(this, true), // Self-loop
                new ToSearchTransition(this, false), // Self-loop
                new ToWanderTransition(entity),
                new ToPickupTransition(entity),
                new OnEnemyInSightTransition(entity)
            };
        }

        public void Update()
        {
            BehaviorManager.instance.Tick(behaviorTree);
        }

        public void Exit()
        {
            Resources.UnloadAsset(externalBt);
            Object.Destroy(behaviorTree);
        }
    }
}
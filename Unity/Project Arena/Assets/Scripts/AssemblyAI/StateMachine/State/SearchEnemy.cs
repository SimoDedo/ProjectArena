using System.Collections.Generic;
using AI.KnowledgeBase;
using AssemblyAI.StateMachine;
using AssemblyAI.StateMachine.Transition;
using AssemblyLogging;
using BehaviorDesigner.Runtime;
using UnityEngine;

namespace AssemblyAI.State
{
    public class SearchEnemy : IState
    {
        private readonly AIEntity entity;
        private readonly TargetKnowledgeBase targetKB;
        private readonly bool searchDueToDamage;
        private ExternalBehaviorTree externalBT;
        private BehaviorTree behaviorTree;
        private float startSearchTime = float.NaN;
        private const float SECONDS_AFTER_STOP_SEARCH = 5f;
        public ITransition[] OutgoingTransitions { get; private set; }


        public SearchEnemy(AIEntity entity, bool searchDueToDamage = false)
        {
            this.entity = entity;
            targetKB = entity.GetComponent<TargetKnowledgeBase>();
            this.searchDueToDamage = searchDueToDamage;
        }

        public float LostEnemyTransitionScore()
        {
            if (entity.GetEnemy().isAlive && !targetKB.HasSeenTarget())
            {
                if (float.IsNaN(startSearchTime)) return 0.7f;
                // Slowly decrease want to search. After 5 secs, it's zero
                return 1f - (Time.time - startSearchTime) / 5f;
            }

            return 0f;
        }

        public float DamagedTransitionScore()
        {
            if (entity.GetEnemy().isAlive && entity.HasTakenDamageRecently())
            {
                return 0.7f;
            }

            return 0f;
        }

        public void Enter()
        {
            externalBT = Resources.Load<ExternalBehaviorTree>("Behaviors/SearchEnemy");
            behaviorTree = entity.gameObject.AddComponent<BehaviorTree>();
            behaviorTree.StartWhenEnabled = false;
            behaviorTree.RestartWhenComplete = true;
            behaviorTree.ExternalBehavior = externalBT;
            behaviorTree.EnableBehavior();
            BehaviorManager.instance.UpdateInterval = UpdateIntervalType.Manual;
            behaviorTree.SetVariableValue("SearchDueToDamage", searchDueToDamage);
            startSearchTime = Time.time;

            OutgoingTransitions = new ITransition[]
            {
                new SearchSelfLoop(this), // Self-loop
                new ToWanderTransition(entity, ExitFightAction),
                new ToPickupTransition(entity, ExitFightAction),
                new ResumeFightTransition(entity, EnterFightAction)
            };
        }

        private void ExitFightAction()
        {
            if (!searchDueToDamage)
            {
                var position = entity.transform.position;
                FightExitGameEvent.Instance.Raise(
                    new ExitFightInfo {x = position.x, z = position.z, entityId = entity.GetID()}
                );
                SearchStopGameEvent.Instance.Raise(entity.GetID());
            }
        }

        private void EnterFightAction()
        {
            if (searchDueToDamage)
            {
                var position = entity.transform.position;
                FightEnterGameEvent.Instance.Raise(
                    new EnterFightInfo {x = position.x, z = position.z, entityId = entity.GetID()}
                );
            }
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
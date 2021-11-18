using System.Collections.Generic;
using AI.KnowledgeBase;
using AssemblyAI.StateMachine;
using AssemblyAI.StateMachine.Transition;
using BehaviorDesigner.Runtime;
using UnityEngine;

namespace AssemblyAI.State
{
    public class SearchEnemy : IState
    {
        private AIEntity entity;
        private TargetKnowledgeBase targetKB;
        private bool searchDueToDamage;
        private ExternalBehaviorTree externalBT;
        private BehaviorTree behaviorTree;
        private float startSearchTime = float.NaN;
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
                if (float.IsNaN(startSearchTime))
                    return 0.7f;
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
                new ToSearchTransition(this), // Self-loop
                new ToWanderTransition(entity),
                new ToPickupTransition(entity),
                new ResumeFightTransition(entity),
            };
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
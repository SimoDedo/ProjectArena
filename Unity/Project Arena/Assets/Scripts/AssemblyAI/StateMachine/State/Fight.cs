using System.Collections.Generic;
using AI.KnowledgeBase;
using AssemblyAI.StateMachine;
using AssemblyAI.StateMachine.Transition;
using AssemblyEntity.Component;
using AssemblyLogging;
using BehaviorDesigner.Runtime;
using UnityEngine;

namespace AssemblyAI.State
{
    public class Fight : IState
    {
        private AIEntity entity;
        private TargetKnowledgeBase targetKB;
        private GunManager gunManager;
        private ExternalBehaviorTree externalBT;
        private BehaviorTree behaviorTree;
        public ITransition[] OutgoingTransitions { get; private set; }

        public Fight(AIEntity entity)
        {
            this.entity = entity;
            targetKB = entity.GetComponent<TargetKnowledgeBase>();
            gunManager = entity.GetComponent<GunManager>();
        }

        public float FightTransitionScore(bool isResumingFight = false)
        {
            if (!gunManager.HasAmmo()) return 0;
            // TODO maybe we see enemy, but we want to run away?
            var canSee = targetKB.HasSeenTarget(isResumingFight);
            return canSee ? 0.95f : 0.0f;
        }


        public void Enter()
        {
            externalBT = Resources.Load<ExternalBehaviorTree>("Behaviors/Fight");
            behaviorTree = entity.gameObject.AddComponent<BehaviorTree>();
            behaviorTree.StartWhenEnabled = false;
            behaviorTree.RestartWhenComplete = true;
            behaviorTree.ExternalBehavior = externalBT;
            behaviorTree.EnableBehavior();
            BehaviorManager.instance.UpdateInterval = UpdateIntervalType.Manual;

            OutgoingTransitions = new ITransition[]
            {
                new FightSelfLoop(this), // Self-loop 
                new ToWanderTransition(entity, ExitFightAction), 
                new ToSearchTransition(entity, StartSearchAction),
                new ToPickupTransition(entity, ExitFightAction)
            };
        }

        private void StartSearchAction()
        {
            SearchStartGameEvent.Instance.Raise(entity.GetID());
        }

        private void ExitFightAction()
        {
            var position = entity.transform.position;
            FightExitGameEvent.Instance.Raise(
                new ExitFightInfo {x = position.x, z = position.z, entityId = entity.GetID()}
            );
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
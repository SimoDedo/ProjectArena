using System;
using AI.Layers.KnowledgeBase;
using BehaviorDesigner.Runtime;
using Entity.Component;
using UnityEngine;

namespace AI.GoalMachine.Goal
{
    /// <summary>
    /// Fighting goal. Deals with moving, shooting, finding cover, reloading and so on.
    /// The goal plan is contained in a behaviour tree.
    /// </summary>
    public class Fight : IGoal
    {
        private const float LOW_HEALTH_PENALTY = 0.6f;
        private readonly BehaviorTree behaviorTree;
        private readonly AIEntity entity;
        private readonly ExternalBehaviorTree externalBt;
        private readonly GunManager gunManager;
        private readonly TargetKnowledgeBase _targetKnowledgeBase;
        private readonly float scoreMultiplier = 1.0f;

        public Fight(AIEntity entity)
        {
            this.entity = entity;
            _targetKnowledgeBase = entity.TargetKnowledgeBase;
            var recklessness = entity.Recklessness;
            switch (recklessness)
            {
                case Recklessness.Low:
                    scoreMultiplier /= 1.3f;
                    break;
                case Recklessness.Neutral:
                    break;
                case Recklessness.High:
                    scoreMultiplier *= 1.3f;
                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }
            gunManager = entity.GunManager;
            externalBt = Resources.Load<ExternalBehaviorTree>("Behaviors/NewFight");
            behaviorTree = entity.gameObject.AddComponent<BehaviorTree>();
            behaviorTree.StartWhenEnabled = false;
            behaviorTree.RestartWhenComplete = true;
            behaviorTree.ExternalBehavior = externalBt;
        }

        public float GetScore()
        {
            if (!gunManager.HasAmmo()) return 0;
            // TODO maybe we see enemy, but we want to run away?
            var canSee = _targetKnowledgeBase.HasSeenTarget() && entity.GetEnemy().IsAlive;
            var inverseHealthPercentage = 1f - (float) entity.Health / entity.MaxHealth;
            return canSee ? scoreMultiplier * (1.0f - inverseHealthPercentage * LOW_HEALTH_PENALTY) : 0.0f;
        }


        public void Enter()
        {
            behaviorTree.EnableBehavior();
            BehaviorManager.instance.RestartBehavior(behaviorTree);
        }

        public void Update()
        {
            entity.IsFocusingOnEnemy = true;
            BehaviorManager.instance.Tick(behaviorTree);
        }

        public void Exit()
        {
            entity.IsFocusingOnEnemy = false;
            behaviorTree.DisableBehavior();
            // Resources.UnloadAsset(externalBt);
            // Object.Destroy(behaviorTree);
        }
    }
}
using System;
using AI.Layers.KnowledgeBase;
using AI.Layers.SensingLayer;
using BehaviorDesigner.Runtime;
using Logging;
using UnityEngine;

namespace AI.GoalMachine.Goal
{
    /// <summary>
    /// Search enemy goal.
    /// Deals with searching/predicting the position of the enemy and reaching it.
    /// The goal plan is contained in a behaviour tree.
    /// </summary>
    public class SearchEnemy : IGoal
    {
        private const float NO_TIME = -1;
        private readonly TargetKnowledgeBase _targetKnowledgeBase;
        private readonly BehaviorTree behaviorTree;
        private readonly DamageSensor damageSensor;
        private readonly AIEntity entity;
        private readonly ExternalBehaviorTree externalBt;
        private readonly SoundSensor soundSensor;
        private Recklessness _recklessness;
        private bool resetInUpdate;

        private float searchTriggeringEventTime = NO_TIME;

        public SearchEnemy(AIEntity entity)
        {
            this.entity = entity;
            _targetKnowledgeBase = entity.TargetKnowledgeBase;
            _recklessness = entity.Recklessness;
            damageSensor = entity.DamageSensor;
            soundSensor = entity.SoundSensor;
            externalBt = Resources.Load<ExternalBehaviorTree>("Behaviors/SearchEnemy");
            behaviorTree = entity.gameObject.AddComponent<BehaviorTree>();
            behaviorTree.StartWhenEnabled = false;
            behaviorTree.RestartWhenComplete = true;
            behaviorTree.ExternalBehavior = externalBt;
        }
        
        public float GetScore()
        {
            if (!entity.GetEnemy().IsAlive || _targetKnowledgeBase.HasSeenTarget())
            {
                return 0f;
            }

            var mostRecentEvent = MostRecentEventTimeOrNoTime();

            if (mostRecentEvent == NO_TIME)
            {
                return 0f;
            }
            
            var maxScore = _recklessness switch
            {
                Recklessness.Low => 0.6f,
                Recklessness.Neutral => 0.85f,
                Recklessness.High => 1.0f,
                _ => throw new ArgumentOutOfRangeException()
            };

            var score = Mathf.Max(maxScore - (Time.time - mostRecentEvent) / 10f, 0f);

            if (searchTriggeringEventTime != NO_TIME && searchTriggeringEventTime != mostRecentEvent)
            {
                // I'm already searching but I received a new event. Forcibly cause update
                searchTriggeringEventTime = mostRecentEvent;
                resetInUpdate = true;
            }
            else
            {
                resetInUpdate = false;
            }

            return score;
        }

        public void Enter()
        {
            FocusingOnEnemyGameEvent.Instance.Raise(new FocusOnEnemyInfo
                {entityID = entity.GetID(), isFocusing = true}
            );
            behaviorTree.EnableBehavior();
            BehaviorManager.instance.RestartBehavior(behaviorTree);

            searchTriggeringEventTime = MostRecentEventTimeOrNoTime();
        }

        public void Update()
        {
            if (resetInUpdate)
            {
                Exit();
                Enter();
            }

            BehaviorManager.instance.Tick(behaviorTree);
        }

        public void Exit()
        {
            searchTriggeringEventTime = NO_TIME;
            FocusingOnEnemyGameEvent.Instance.Raise(
                new FocusOnEnemyInfo {entityID = entity.GetID(), isFocusing = false});
            behaviorTree.DisableBehavior();
        }

        private float RecentLossTimeOrNoTime()
        {
            return _targetKnowledgeBase.HasLostTarget() ? _targetKnowledgeBase.LastTimeDetected : NO_TIME;
        }

        private float LastRecentDamageTimeOrNoTime()
        {
            return damageSensor.WasDamagedRecently ? damageSensor.LastTimeDamaged : NO_TIME;
        }

        private float LastRecentNoiseTimeOrNoTime()
        {
            return soundSensor.HeardShotRecently ? soundSensor.LastTimeHeardShot : NO_TIME;
        }

        private float MostRecentEventTimeOrNoTime()
        {
            var timeNoise = LastRecentNoiseTimeOrNoTime();
            var timeDamage = LastRecentDamageTimeOrNoTime();
            var timeLost = RecentLossTimeOrNoTime();

            return Mathf.Max(timeNoise, Mathf.Max(timeDamage, timeLost));
        }
    }
}
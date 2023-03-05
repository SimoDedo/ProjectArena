using System;
using AI.Layers.Actuators;
using AI.Layers.SensingLayer;
using BehaviorDesigner.Runtime.Tasks;
using Entity;
using UnityEngine;
using Action = BehaviorDesigner.Runtime.Tasks.Action;

namespace AI.Behaviours.Actions
{
    /// <summary>
    /// Makes the entity look at the direction where damage or sound came from.
    /// </summary>
    public class LookAtSuspectedDirection : Action
    {
        private AIEntity entity;
        private DamageSensor damageSensor;
        private SoundSensor soundSensor;
        private Vector3 lookPosition;
        private SightController sightController;

        public override void OnAwake()
        {
            entity = GetComponent<AIEntity>();
            damageSensor = entity.DamageSensor;
            soundSensor = entity.SoundSensor;
            sightController = entity.SightController;
        }

        public override void OnStart()
        {
            var enemy = entity.GetEnemy();
            var enemyTracker = enemy.GetComponent<PositionTracker>();
            var eventTime = Mathf.Max(damageSensor.LastTimeDamaged, soundSensor.LastTimeHeardShot);
            lookPosition = enemyTracker.GetPositionAtTime(eventTime);
        }

        public override TaskStatus OnUpdate()
        {
            sightController.LookAtPoint(lookPosition);
            return TaskStatus.Running;
        }
    }
}
using System;
using AI.Layers.Actuators;
using AI.Layers.SensingLayer;
using Bonsai;
using Bonsai.Core;
using Entity;
using UnityEngine;

namespace AI.BonsaiBehaviours.Actions
{
    /// <summary>
    /// Makes the entity look at the direction where damage or sound came from.
    /// </summary>
    [BonsaiNode("Tasks/")]
    public class LookAtSuspectedDirection : Task
    {
        private AIEntity entity;
        private DamageSensor damageSensor;
        private SoundSensor soundSensor;
        private Vector3 lookPosition;
        private SightController sightController;

        public override void OnStart()
        {
            entity = Actor.GetComponent<AIEntity>();
            damageSensor = entity.DamageSensor;
            soundSensor = entity.SoundSensor;
            sightController = entity.SightController;
        }

        public override void OnEnter()
        {
            var enemy = entity.GetEnemy();
            var enemyTracker = enemy.GetComponent<PositionTracker>();
            var eventTime = Mathf.Max(damageSensor.LastTimeDamaged, soundSensor.LastTimeHeardShot);
            lookPosition = enemyTracker.GetPositionAtTime(eventTime);
        }

        public override Status Run()
        {
            sightController.LookAtPoint(lookPosition);
            return Status.Running;
        }
    }
}
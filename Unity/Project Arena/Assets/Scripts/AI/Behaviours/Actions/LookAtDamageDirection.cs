using System;
using AI.AI.Layer1;
using BehaviorDesigner.Runtime.Tasks;
using Entity;
using UnityEngine;
using Action = BehaviorDesigner.Runtime.Tasks.Action;

namespace AI.Behaviours.Actions
{
    public class LookAtDamageDirection : Action
    {
        private DamageSensor damageSensor;
        private Vector3 lookPosition;
        private SightController sightController;

        public override void OnAwake()
        {
            var entity = GetComponent<AIEntity>();
            var enemy = entity.GetEnemy();
            var enemyTracker = enemy.GetComponent<PositionTracker>();
            damageSensor = entity.DamageSensor;
            sightController = entity.SightController;
            var delay = damageSensor.LastTimeDamaged - Time.time;
            (lookPosition, _) = enemyTracker.GetPositionAndVelocityFromDelay(delay);
        }

        public override TaskStatus OnUpdate()
        {
            sightController.LookAtPoint(lookPosition);
            return TaskStatus.Running;
        }
    }
}
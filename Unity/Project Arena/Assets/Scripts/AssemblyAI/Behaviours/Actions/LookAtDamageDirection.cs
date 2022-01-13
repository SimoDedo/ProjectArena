using System;
using AssemblyAI.AI.Layer1.Actuator;
using AssemblyAI.AI.Layer1.Sensors;
using BehaviorDesigner.Runtime.Tasks;
using UnityEngine;
using Action = BehaviorDesigner.Runtime.Tasks.Action;

namespace AssemblyAI.Behaviours.Actions
{
    public class LookAtDamageDirection : Action
    {
        private DamageSensor damageSensor;
        private AISightController sightController;
        private Vector3 lookPosition;
        
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
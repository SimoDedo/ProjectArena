using System;
using BehaviorDesigner.Runtime;
using BehaviorDesigner.Runtime.Tasks;
using UnityEngine;
using Action = BehaviorDesigner.Runtime.Tasks.Action;

namespace AI
{
    public class CanSeeOpponent : Conditional
    {
        public SharedFloat maxDistance;
        public SharedFloat fov;
        public SharedGameObject enemy;
        private Transform t;
        
        public override void OnStart()
        {
            t = transform;
        }

        public override TaskStatus OnUpdate()
        {
            return SpotEnemy()? TaskStatus.Success : TaskStatus.Failure;
        }

        private bool SpotEnemy()
        {
            var position = t.position;
            var direction = enemy.Value.transform.position - position;
            var angle = Vector3.Angle(t.forward, direction);
            if (angle <= fov.Value)
            {
                if (Physics.Raycast(t.position, direction, out var hit, maxDistance.Value) &&
                    hit.collider.gameObject == enemy.Value)
                    return true;
            }

            return false;
        }
    }
}
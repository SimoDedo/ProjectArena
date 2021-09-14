using BehaviorDesigner.Runtime;
using BehaviorDesigner.Runtime.Tasks;
using UnityEngine;

namespace AI
{
    public class TimedCanSeeOpponent : Conditional
    {
        public SharedFloat timeBeforeDetection;
        private float lastTimeNotSeen;
        public SharedFloat maxDistance;
        public SharedFloat fov;
        public SharedGameObject enemy;
        private Transform t;
        private bool spotEnemyRes = false;

        public override void OnAwake()
        {
            t = transform;
            lastTimeNotSeen = Time.time;
        }
        
        public override TaskStatus OnUpdate()
        {
            // Debug.Log(gameObject.name + " TimedCanSee:   " + Time.time + " > " + lastTimeNotSeen + " + " + timeBeforeDetection.Value);
            if (SpotEnemy())
            {
                if (Time.time > lastTimeNotSeen + timeBeforeDetection.Value)
                {
                    return TaskStatus.Success;
                }
            }
            else
                lastTimeNotSeen = Time.time;

            return TaskStatus.Failure;
        }
        
        private bool SpotEnemy()
        {
            var position = t.position;
            var direction = enemy.Value.transform.position - position;
            var angle = Vector3.Angle(t.forward, direction);
            if (angle <= fov.Value)
            {
                Debug.DrawRay(position, direction, Color.cyan);
                if (Physics.Raycast(position, direction, out var hit, maxDistance.Value) &&
                    hit.collider.gameObject == enemy.Value)
                    return true;
            }

            return false;
        }
    }
}
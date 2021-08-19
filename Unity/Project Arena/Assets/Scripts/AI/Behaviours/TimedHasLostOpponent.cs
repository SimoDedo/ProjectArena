using BehaviorDesigner.Runtime;
using BehaviorDesigner.Runtime.Tasks;
using UnityEngine;

namespace AI
{
    public class TimedHasLostOpponent : Conditional
    {
        public SharedFloat timeBeforeLoss;
        private float timeLastSeen;
        public SharedFloat maxDistance;
        public SharedFloat fov;
        public SharedGameObject enemy;
        private Transform t;

        public override void OnAwake()
        {
            t = transform;
            timeLastSeen = Time.time;
        }
        
        public override TaskStatus OnUpdate()
        {
            // Debug.Log("TimedHasLost:   " + Time.time + " > " + timeLastSeen + " + " + timeBeforeLoss.Value);
            if (!SpotEnemy())
            {
                if (Time.time > timeLastSeen + timeBeforeLoss.Value)
                    return TaskStatus.Success;
            }
            else
                timeLastSeen = Time.time;

            return TaskStatus.Failure;
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
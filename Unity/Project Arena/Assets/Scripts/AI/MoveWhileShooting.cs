using Accord.Math;
using BehaviorDesigner.Runtime;
using BehaviorDesigner.Runtime.Tasks;
using UnityEngine;
using UnityEngine.AI;
using Vector3 = UnityEngine.Vector3;

namespace AI
{
    public class MoveWhileShooting: Action
    {
        private NavMeshAgent agent;
        public SharedGameObject target;
        public SharedFloat strifeAmount;
        private bool strifeRight = true;
        private int remainingStrifes = Random.Range(minStrifeLength, maxStrifeLength);

        private const int minStrifeLength = 5;
        private const int maxStrifeLength = 20;
        
        public override void OnStart()
        {
            agent = GetComponent<NavMeshAgent>();
        }
        
        public override void OnEnd()
        {
            agent.updateRotation = true;
            agent.ResetPath();
        }

        public override TaskStatus OnUpdate()
        {
            agent.updateRotation = false;
            if (agent.hasPath && agent.remainingDistance > 0.5)
                return TaskStatus.Running;
            TrySelectDestination();
            return TaskStatus.Running;
        }

        private void TrySelectDestination()
        {
            var currentPos = transform.position;
            var targetPos = target.Value.transform.position;
            targetPos.y = currentPos.y;
            var direction = (targetPos - currentPos).normalized;

            var strifeDir = Vector3.Cross(direction, transform.up);

            remainingStrifes--;
            if (remainingStrifes < 0)
            {
                remainingStrifes = Random.Range(minStrifeLength, maxStrifeLength);
                strifeRight = !strifeRight;
            }

            var offset = strifeDir * (strifeRight ? strifeAmount.Value : -strifeAmount.Value);
            Debug.DrawLine(currentPos, currentPos + offset, Color.magenta);

            agent.SetDestination(currentPos + offset);

            // var radius = (currentPos - targetPos).magnitude;
            // // TODO better angle random. Should not include values close to 0.
            //
            // var minAngle = Mathf.Max(0, minStrife.Value / radius);
            // var maxAngle = Mathf.Min(10f, maxStrife.Value / radius);
            //
            // var numAttempts = 10;
            // while (numAttempts > 0)
            // {
            //     var angle = Random.Range(minAngle, maxAngle) * (Random.value > 0.5 ? +1f : -1f);
            //     var rotation = Quaternion.AngleAxis(angle, transform.up);
            //     var relativePosition = currentPos - targetPos;
            //     var rotatedPos = rotation * relativePosition;
            //     var strifePos = rotatedPos + targetPos;
            //
            //     var path = new NavMeshPath();
            //     agent.CalculatePath(strifePos, path);
            //     if (path.status == NavMeshPathStatus.PathComplete)
            //     {
            //         agent.SetPath(path);
            //         return;
            //     }
            //
            //     numAttempts--;
            // }
        }
    }
}
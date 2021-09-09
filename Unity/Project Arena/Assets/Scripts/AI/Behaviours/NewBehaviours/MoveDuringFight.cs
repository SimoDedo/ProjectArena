using System;
using BehaviorDesigner.Runtime;
using BehaviorDesigner.Runtime.Tasks;
using UnityEngine;
using UnityEngine.AI;
using Action = BehaviorDesigner.Runtime.Tasks.Action;
using Random = UnityEngine.Random;

namespace AI.NewBehaviours
{
    [Serializable]
    public class MoveDuringFight : Action
    {
        private NavMeshAgent agent;
        [SerializeField] private SharedGameObject target;
        [SerializeField] private SharedFloat movementAmount;

        private bool strifeRight = Random.value < 0.5;
        private int remainingStrifes = Random.Range(minStrifeLength, maxStrifeLength);

        private const int minStrifeLength = 5;
        private const int maxStrifeLength = 20;
        private float skill;

        public override void OnStart()
        {
            agent = GetComponent<NavMeshAgent>();
            skill = GetComponent<AIEntity>().GetMovementSkill();
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
            // Don't move at all during shooting
            if (skill < 0.2f) return;

            var currentPos = transform.position;
            var targetPos = target.Value.transform.position;
            targetPos.y = currentPos.y;
            var direction = (targetPos - currentPos).normalized;

            if (skill < 0.6)
            {
                // TODO how to decide if moving forward or backward?
                agent.SetDestination(currentPos + direction * movementAmount.Value);
                return;
            }

            var strifeDir = Vector3.Cross(direction, transform.up);
            if (skill >= 0.9)
            {
                remainingStrifes--;
                if (remainingStrifes < 0)
                {
                    remainingStrifes = Random.Range(minStrifeLength, maxStrifeLength);
                    strifeRight = !strifeRight;
                }
            }

            var offset = strifeDir * (strifeRight ? movementAmount.Value : -movementAmount.Value);
            Debug.DrawLine(currentPos, currentPos + offset, Color.magenta);
            agent.SetDestination(currentPos + offset);
        }
    }
}
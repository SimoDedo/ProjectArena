using System;
using BehaviorDesigner.Runtime;
using BehaviorDesigner.Runtime.Tasks;
using Entities.AI.Layer2;
using UnityEngine;
using UnityEngine.AI;
using Action = BehaviorDesigner.Runtime.Tasks.Action;
using Random = UnityEngine.Random;

namespace AI.NewBehaviours
{
    [Serializable]
    public class MoveDuringFight : Action
    {
        private NavigationSystem agent;
        private Entity target;
        [SerializeField] private SharedFloat movementAmount;

        private bool strifeRight = Random.value < 0.5;
        private int remainingStrifes = Random.Range(minStrifeLength, maxStrifeLength);

        private const int minStrifeLength = 10;
        private const int maxStrifeLength = 30;
        private float skill;

        public override void OnStart()
        {
            agent = GetComponent<NavigationSystem>();
            var aiEntity = GetComponent<AIEntity>();
            target = aiEntity.GetEnemy();
            skill = aiEntity.GetMovementSkill();
            agent.CancelPath();
        }

        public override void OnEnd()
        {
            agent.CancelPath();
        }

        public override TaskStatus OnUpdate()
        {
            if (agent.HasPath() && !agent.HasArrivedToDestination())
                return TaskStatus.Running;
            TrySelectDestination();
            return TaskStatus.Running;
        }

        private void TrySelectDestination()
        {
            // Don't move at all during shooting
            if (skill < 0.2f) return;

            var currentPos = transform.position;
            var targetPos = target.transform.position;
            targetPos.y = currentPos.y;
            var direction = (targetPos - currentPos).normalized;

            if (skill < 0.6)
            {
                // TODO how to decide if moving forward or backward?
                agent.SetDestination(currentPos + direction  * agent.GetSpeed());
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

            var offset = strifeDir * (strifeRight ? agent.GetSpeed() : -agent.GetSpeed());
            Debug.DrawLine(currentPos, currentPos + offset, Color.magenta);
            agent.SetDestination(currentPos + offset);
        }
    }
}
using System;
using AI.AI.Layer2;
using AI.Behaviours.Variables;
using BehaviorDesigner.Runtime.Tasks;
using Others;
using UnityEngine;
using UnityEngine.AI;
using Action = BehaviorDesigner.Runtime.Tasks.Action;
using Random = UnityEngine.Random;

namespace AI.Behaviours.Actions
{
    // For the purpose of this class, a cover is any position from where the enemy cannot be seen.
    [Serializable]
    public class SelectCover : Action
    {
        [SerializeField] private float minCoverDistance;
        [SerializeField] private float maxCoverDistance = 10f;
        [SerializeField] private int maxCoverSearchAttempts = 10;
        [SerializeField] private SharedSelectedPathInfo pathInfo;
        private Entity.Entity enemy;

        private NavigationSystem navSystem;

        public override void OnAwake()
        {
            var entity = gameObject.GetComponent<AIEntity>();
            navSystem = entity.NavigationSystem;
            enemy = entity.GetEnemy();
        }

        public override TaskStatus OnUpdate()
        {
            if (Physics.Linecast(transform.position, enemy.transform.position, out var hit1) &&
                hit1.collider.gameObject != enemy.gameObject)
            {
                // Current position is already covered!
                pathInfo.Value = navSystem.CalculatePath(transform.position);
                return TaskStatus.Success;
            }

            var currentPos = transform.position;
            var smallestPathFound = float.MaxValue;
            NavMeshPath selectedPath = null;
            for (var i = 0; i < maxCoverSearchAttempts; i++)
            {
                var circle = Random.insideUnitCircle * (maxCoverDistance - minCoverDistance);
                var finalPos = new Vector3(
                    currentPos.x + circle.x + minCoverDistance,
                    currentPos.y,
                    currentPos.z + circle.y + minCoverDistance
                );
                var path = navSystem.CalculatePath(finalPos);
                var pathLength = path.Length();
                if (!path.IsComplete() || pathLength > maxCoverDistance)
                    // Path invalid or too long!
                    continue;

                if (!Physics.Linecast(finalPos, enemy.transform.position, out var hit) ||
                    hit.collider.gameObject == enemy.gameObject)
                    // We can still see the enemy from that position, no good! 
                    continue;

                if (pathLength < smallestPathFound)
                {
                    smallestPathFound = pathLength;
                    selectedPath = path;
                }
            }

            if (selectedPath != null)
            {
                // Found a "good" cover position!
                pathInfo.Value = selectedPath;
                return TaskStatus.Success;
            }

            // Found no cover position around me...
            return TaskStatus.Failure;
        }
    }
}
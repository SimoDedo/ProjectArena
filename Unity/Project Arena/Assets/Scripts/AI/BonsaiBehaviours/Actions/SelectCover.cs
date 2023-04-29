using System;
using AI.Behaviours.Variables;
using AI.Layers.KnowledgeBase;
using Bonsai;
using Bonsai.Core;
using Others;
using UnityEngine;
using UnityEngine.AI;
using Random = UnityEngine.Random;

namespace AI.BonsaiBehaviours.Actions
{
    /// <summary>
    /// Selects a cover position. Any position from which the enemy cannot be seen is considered a cover.
    /// </summary>
    [BonsaiNode("Tasks/")]
    public class SelectCover : Task
    {
        public float minCoverDistance;
        public float maxCoverDistance = 10f;
        public int maxCoverSearchAttempts = 10;
        public string pathInfoKey;
        private NavMeshPath PathInfo
        {
            set => Blackboard.Set(pathInfoKey, value);
        }
        private Entity.Entity enemy;

        private NavigationSystem navSystem;
        private int layerMask;
        private Transform transform;

        public override void OnStart()
        {
            var entity = Actor.gameObject.GetComponent<AIEntity>();
            transform = Actor.transform;
            navSystem = entity.NavigationSystem;
            enemy = entity.GetEnemy();
            layerMask = LayerMask.GetMask("Default", "Wall");
        }
        
        public override Status Run()
        {
            if (Physics.Linecast(transform.position, enemy.transform.position, layerMask))
            {
                // Current position is already covered!
                PathInfo = navSystem.CalculatePath(transform.position);
                return Status.Success;
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

                if (!Physics.Linecast(finalPos, enemy.transform.position, layerMask))
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
                PathInfo = selectedPath;
                return Status.Success;
            }

            // Found no cover position around me...
            return Status.Failure;
        }
    }
}
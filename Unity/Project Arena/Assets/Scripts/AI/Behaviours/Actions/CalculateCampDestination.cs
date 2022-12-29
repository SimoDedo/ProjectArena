using System;
using AI.Behaviours.Variables;
using AI.Layers.KnowledgeBase;
using BehaviorDesigner.Runtime;
using BehaviorDesigner.Runtime.Tasks;
using UnityEngine;
using UnityEngine.AI;
using Action = BehaviorDesigner.Runtime.Tasks.Action;
using Random = UnityEngine.Random;

namespace AI.Behaviours.Actions
{
    /// <summary>
    /// Calculates a possible location where to camp given the position around which to camp.
    /// Camping locations are chosen in order to circle around the spot.
    /// </summary>
    [Serializable]
    public class CalculateCampDestination : Action
    {
        private int layerMask = 0;

        [SerializeField] private float minRadius = 1;
        [SerializeField] private float maxRadius = 20;
        [SerializeField] private int maxNumVertices = 20;
        [SerializeField] private float maxRadiusAttempts = 3;
        [SerializeField] private SharedVector3 campLocation;
        [SerializeField] private SharedSelectedPathInfo pathChosen;

        private NavigationSystem navSystem;
        private int startVertex;

        public override void OnAwake()
        {
            layerMask = LayerMask.GetMask("Wall", "Floor", "Default");
            navSystem = GetComponent<AIEntity>().NavigationSystem;
            startVertex = Random.Range(0, maxNumVertices);
        }

        public override TaskStatus OnUpdate()
        {
            return TrySetTarget() ? TaskStatus.Success : TaskStatus.Running;
        }

        private bool TrySetTarget()
        {
            var currentPos = transform.position;
            for (var attemptNum = 1; attemptNum <= maxNumVertices; attemptNum++)
            {
                var direction = currentPos - campLocation.Value;
                direction.y = 0;
                var angle = (startVertex + attemptNum) % maxNumVertices * 360 / maxNumVertices;
                var newDirection = Quaternion.AngleAxis(angle, Vector3.up) * direction;
                newDirection.Normalize();
                for (var j = 0; j < maxRadiusAttempts; j++)
                {
                    var randomRadius = Random.value * (maxRadius - minRadius) + minRadius;
                    var newPoint = newDirection * randomRadius + campLocation.Value;
                    if (Physics.Linecast(campLocation.Value, newPoint, layerMask))
                        // Chosen point is behind something, ignore!
                        continue;
                    // if (!NavigationSystem.IsPointOnNavMesh(newPoint, out var finalPoint)) continue;

                    var path = navSystem.CalculatePath(newPoint);
                    if (path.status != NavMeshPathStatus.PathComplete) continue;
                    pathChosen.Value = path;
                    return true;
                }
            }

            return false;
        }
    }
}
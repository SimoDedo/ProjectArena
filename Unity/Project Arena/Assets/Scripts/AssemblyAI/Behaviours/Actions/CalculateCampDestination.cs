using System;
using AssemblyAI.AI.Layer2;
using AssemblyAI.Behaviours.Variables;
using BehaviorDesigner.Runtime;
using BehaviorDesigner.Runtime.Tasks;
using Others;
using UnityEngine;
using UnityEngine.AI;
using Action = BehaviorDesigner.Runtime.Tasks.Action;
using Random = UnityEngine.Random;

namespace AssemblyAI.Behaviours.Actions
{
    [Serializable]
    public class CalculateCampDestination : Action
    {
        [SerializeField] private float minRadius = 10;
        [SerializeField] private float maxRadius = 20;
        [SerializeField] private float maxNumVertices = 20;
        [SerializeField] private float maxRadiusAttempts = 3;
        [SerializeField] private SharedVector3 campLocation;
        [SerializeField] private SharedSelectedPathInfo pathChosen;

        private static readonly int LayerMask =
            Physics.DefaultRaycastLayers ^ (1 << UnityEngine.LayerMask.NameToLayer("Entity"));

        private NavigationSystem navSystem;

        public override void OnAwake()
        {
            navSystem = GetComponent<AIEntity>().NavigationSystem;
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
                var angle = attemptNum * 360 / maxNumVertices;
                var newDirection = Quaternion.AngleAxis(angle, Vector3.up) * direction;
                newDirection.Normalize();
                for (var j = 0; j < maxRadiusAttempts; j++)
                {
                    var randomRadius = Random.value * (maxRadius - minRadius) + minRadius;
                    var newPoint = newDirection * randomRadius + campLocation.Value;
                    if (Physics.Linecast(campLocation.Value, newPoint, LayerMask))
                    {
                        // Chosen point is behind something, ignore!
                        continue;
                    }
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
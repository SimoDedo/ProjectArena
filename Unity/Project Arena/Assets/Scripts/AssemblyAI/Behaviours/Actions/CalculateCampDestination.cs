using System;
using AssemblyAI.Behaviours.Variables;
using BehaviorDesigner.Runtime;
using BehaviorDesigner.Runtime.Tasks;
using Entities.AI.Layer2;
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
                var angle = attemptNum * 360 / maxNumVertices;
                var newDirection = Quaternion.AngleAxis(angle, Vector3.up) * direction;
                newDirection.Normalize();
                for (var j = 0; j < maxRadiusAttempts; j++)
                {
                    var randomRadius = Random.value * (maxRadius - minRadius) + minRadius;
                    var newPoint = newDirection * randomRadius + campLocation.Value;
                    if (Physics.Linecast(campLocation.Value, newPoint)) continue;
                    if (!navSystem.IsPointOnNavMesh(newPoint, NavMeshUtils.GetAgentIDFromName("Wanderer"),
                        out var finalPoint)) continue;

                    var path = navSystem.CalculatePath(finalPoint);
                    if (path.status != NavMeshPathStatus.PathComplete) continue;
                    pathChosen.Value = path;
                    return true;
                }
            }

            return false;
        }
    }
}
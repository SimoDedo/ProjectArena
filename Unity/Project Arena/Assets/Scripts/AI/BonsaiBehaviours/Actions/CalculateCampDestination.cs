using AI.Layers.KnowledgeBase;
using Bonsai;
using UnityEngine;
using UnityEngine.AI;
using Random = UnityEngine.Random;
using Task = Bonsai.Core.Task;

namespace AI.BonsaiBehaviours.Actions
{
    /// <summary>
    /// Calculates a possible location where to camp given the position around which to camp.
    /// Camping locations are chosen in order to circle around the spot.
    /// </summary>
    [BonsaiNode("Tasks/")]
    public class CalculateCampDestination : Task
    {
        private int layerMask = 0;

       public float minRadius = 1;
       public float maxRadius = 20;
       public int maxNumVertices = 20;
       public float maxRadiusAttempts = 3;

       public string campLocationKey;
       public string pathChosenKey;

       private Vector3 CampLocation => Blackboard.Get<Vector3>(campLocationKey);

       public NavMeshPath PathChosen
       {
           set => Blackboard.Set(pathChosenKey, value);
       }

        private NavigationSystem navSystem;
        private int startVertex;

        public override void OnStart()
        {
            layerMask = LayerMask.GetMask("Wall", "Floor", "Default");
            navSystem = Actor.GetComponent<AIEntity>().NavigationSystem;
            startVertex = Random.Range(0, maxNumVertices);
        }

        public override Status Run()
        {
            return TrySetTarget() ? Status.Success : Status.Running;
        }

        private bool TrySetTarget()
        {
            var campLocation = CampLocation;
            var currentPos = Actor.transform.position;
            for (var attemptNum = 1; attemptNum <= maxNumVertices; attemptNum++)
            {
                var direction = currentPos - campLocation;
                direction.y = 0;
                var angle = (startVertex + attemptNum) % maxNumVertices * 360 / maxNumVertices;
                var newDirection = Quaternion.AngleAxis(angle, Vector3.up) * direction;
                newDirection.Normalize();
                for (var j = 0; j < maxRadiusAttempts; j++)
                {
                    var randomRadius = Random.value * (maxRadius - minRadius) + minRadius;
                    var newPoint = newDirection * randomRadius + campLocation;
                    if (Physics.Linecast(campLocation, newPoint, layerMask))
                        // Chosen point is behind something, ignore!
                        continue;
                    // if (!NavigationSystem.IsPointOnNavMesh(newPoint, out var finalPoint)) continue;

                    var path = navSystem.CalculatePath(newPoint);
                    if (path.status != NavMeshPathStatus.PathComplete) continue;
                    PathChosen = path;
                    return true;
                }
            }

            return false;
        }
    }
}
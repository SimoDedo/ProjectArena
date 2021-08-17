using Accord.Statistics.Distributions.Univariate;
using BehaviorDesigner.Runtime;
using BehaviorDesigner.Runtime.Tasks;
using Others;
using UnityEngine;
using UnityEngine.AI;

namespace AI
{
    /// <summary>
    /// This class handles the logic with which the agent wander around.
    /// It bases the agent movement on a <see href="https://en.wikipedia.org/wiki/L%C3%A9vy_flight">Lévy Flight</see>
    /// 
    /// </summary>
    public class Wander : Action
    {
        private NavMeshAgent agent;

        public SharedInt maxRetries;
        public SharedVector2 levyParams;
        public SharedFloat stoppingDistance;

        public override void OnStart()
        {
            agent = GetComponent<NavMeshAgent>();
        }

        public override void OnEnd()
        {
            agent.ResetPath();
        }

        public override TaskStatus OnUpdate()
        {
            if (agent.hasPath)
            {
                // Debug.Log("Agent has path!");
                var corners = agent.path.corners;
                for (var i = 0; i < corners.Length-1; i++)
                {
                    Debug.DrawLine(corners[i], corners[i+1], Color.blue, 0, false);
                }
                if (agent.remainingDistance < stoppingDistance.Value)
                {
                    // Debug.Log("Destination reached");
                    agent.ResetPath();
                    // TrySetTarget();
                    return TaskStatus.Success;
                }
            }
            else
            {
                // Debug.Log("Agent has path NOPE");
                TrySetTarget();
            }

            return TaskStatus.Running;
        }

        private void TrySetTarget()
        {
            var attempts = maxRetries.Value;
            while (attempts > 0)
            {
                var distribution = new LevyDistribution(levyParams.Value.x, levyParams.Value.y);
                var scale = (float) distribution.Generate();
                // There is no onUnitCircle for some reason...
                var circle = scale * Random.insideUnitCircle.normalized;
                var destination = transform.position + new Vector3(circle.x, 0, circle.y);
                var filter = new NavMeshQueryFilter
                {
                    agentTypeID = NavMeshUtils.GetAgentIDFromName("Wanderer"),
                    areaMask = NavMesh.AllAreas
                };
                if (NavMesh.SamplePosition(destination, out var hit, float.MaxValue, filter))
                {
                    // Debug.Log("Destination chosen: " + destination);

                    var path = new NavMeshPath();
                    if (agent.CalculatePath(hit.position, path) && path.status == NavMeshPathStatus.PathComplete)
                    {
                        agent.SetPath(path);
                        return;
                    }
                }

                attempts--;
            }
        }
    }
}
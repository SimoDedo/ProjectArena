using System;
using Accord.Statistics.Distributions.Univariate;
using AI.Behaviours.NewBehaviours.Variables;
using BehaviorDesigner.Runtime;
using BehaviorDesigner.Runtime.Tasks;
using Entities.AI.Layer2;
using Others;
using UnityEngine;
using UnityEngine.AI;
using Utils;
using Action = BehaviorDesigner.Runtime.Tasks.Action;
using Random = UnityEngine.Random;

namespace AI
{
    /// <summary>
    /// This class handles the logic with which the agent wander around.
    /// It bases the agent movement on a <see href="https://en.wikipedia.org/wiki/L%C3%A9vy_flight">Lévy Flight</see>
    /// 
    /// </summary>
    [Serializable]
    public class SelectWanderDestination : Action
    {
        [SerializeField] private SharedInt maxRetries;
        [SerializeField] private SharedVector2 levyParams;
        [SerializeField] private SharedSelectedPathInfo pathChosen;

        private NavigationSystem navSystem;

        public override void OnAwake()
        {
            navSystem = GetComponent<NavigationSystem>();
        }

        public override TaskStatus OnUpdate()
        {
            return TrySetTarget() ? TaskStatus.Success : TaskStatus.Running;
        }

        private bool TrySetTarget()
        {
            var attempts = maxRetries.Value;
            while (attempts > 0)
            {
                var distribution = new LevyDistribution(levyParams.Value.x, levyParams.Value.y);
                var scale = (float) distribution.Generate();
                // There is no onUnitCircle for some reason...
                var circle = scale * Random.insideUnitCircle.normalized;
                var destination = transform.position + new Vector3(circle.x, 0, circle.y);
                if (navSystem.IsPointOnNavMesh(destination, NavMeshUtils.GetAgentIDFromName("Wanderer"),
                    out var chosenPoint))
                {
                    var path = navSystem.CalculatePath(chosenPoint);
                    if (path.status == NavMeshPathStatus.PathComplete)
                    {
                        pathChosen.Value = path;
                        return true;
                    }
                }

                attempts--;
            }

            // TODO How to deal with incomplete paths?
            Debug.LogWarning("Impossible to find wander destination!");
            return false;
        }
    }
}
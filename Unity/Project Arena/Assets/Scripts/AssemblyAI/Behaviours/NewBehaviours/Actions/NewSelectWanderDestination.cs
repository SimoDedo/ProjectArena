using System;
using AI.Behaviours.NewBehaviours.Variables;
using BehaviorDesigner.Runtime;
using BehaviorDesigner.Runtime.Tasks;
using Entities.AI.Layer2;
using Others;
using UnityEngine;
using UnityEngine.AI;
using Action = BehaviorDesigner.Runtime.Tasks.Action;
using Random = UnityEngine.Random;

namespace AssemblyAI.Behaviours.NewBehaviours
{
    [Serializable]
    public class NewSelectWanderDestination : Action
    {
        [SerializeField] private SharedInt maxRetries = 2;
        [SerializeField] private SharedFloat wanderRate = 1;
        [SerializeField] private SharedFloat minWanderDistance = 20;
        [SerializeField] private SharedFloat maxWanderDistance = 20;
        [SerializeField] private SharedSelectedPathInfo pathChosen;

        private NavigationSystem navSystem;
        private AIMovementController movementController;

        public override void OnAwake()
        {
            navSystem = GetComponent<NavigationSystem>();
            movementController = GetComponent<AIMovementController>();
        }

        public override TaskStatus OnUpdate()
        {
            return TrySetTarget() ? TaskStatus.Success : TaskStatus.Running;
        }

        private bool TrySetTarget()
        {
            var direction = transform.forward; //Or use velocity
            var attempts = maxRetries.Value;
            while (attempts > 0)
            {
                var validDestination = false;
                direction += Random.insideUnitSphere * wanderRate.Value;
                var destination = transform.position + direction.normalized *
                    Random.Range(minWanderDistance.Value, maxWanderDistance.Value);
                validDestination = navSystem.IsPointOnNavMesh(destination, NavMeshUtils.GetAgentIDFromName("Wanderer"),
                    out destination);
                if (validDestination)
                {
                    var path = navSystem.CalculatePath(destination);
                    if (path.status == NavMeshPathStatus.PathComplete)
                    {
                        pathChosen.Value = path;
                        return true;
                    }
                }

                attempts--;
            }

            return false;
        }
    }
}
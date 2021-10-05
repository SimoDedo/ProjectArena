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
    public class NewCalculateCampDestination : Action
    {
        [SerializeField] private int maxRetries = 2;
        [SerializeField] private float radius = 20;
        [SerializeField] private float wanderRate = 20;
        [SerializeField] private SharedVector3 campLocation;
        [SerializeField] private SharedSelectedPathInfo pathChosen;      
        
        [SerializeField] private float minWanderDistance = 20;
        [SerializeField] private float maxWanderDistance = 20;

        private AIMovementController movementController;

        private NavigationSystem navSystem;

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
            var direction = movementController.GetVelocity();
            if (direction == Vector3.zero)
            {
                direction = transform.forward;
            }
            var attempts = maxRetries;
            while (attempts > 0)
            {
                bool validDestination;
                var randomCircle = Random.insideUnitSphere * wanderRate;
                var wanderDirection =  new Vector3(randomCircle.x, 0.0f, randomCircle.y);;
                direction += wanderDirection;
                var destination = transform.position + wanderDirection.normalized *
                    Random.Range(minWanderDistance, maxWanderDistance);
                validDestination = navSystem.IsPointOnNavMesh(destination, NavMeshUtils.GetAgentIDFromName("Wanderer"),
                    out destination);
                validDestination = validDestination && (destination - campLocation.Value).magnitude < radius;
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
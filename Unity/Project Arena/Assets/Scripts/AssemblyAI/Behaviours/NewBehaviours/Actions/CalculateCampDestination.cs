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
    public class CalculateCampDestination : Action
    {
        [SerializeField] private int maxRetries = 2;
        [SerializeField] private float radius = 20;
        [SerializeField] private SharedVector3 campLocation;
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
            var attempts = maxRetries;
            while (attempts > 0)
            {
                var validDestination = false;
                var randomCircle = Random.insideUnitSphere * radius;
                var wanderPosition =  new Vector3(randomCircle.x, 0.0f, randomCircle.y);;
                var destination = campLocation.Value + wanderPosition;
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
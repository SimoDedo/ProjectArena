using System;
using AssemblyAI.AI.Layer2;
using AssemblyAI.Behaviours.Variables;
using BehaviorDesigner.Runtime;
using BehaviorDesigner.Runtime.Tasks;
using Others;
using UnityEngine;
using Action = BehaviorDesigner.Runtime.Tasks.Action;
using Random = UnityEngine.Random;

namespace AssemblyAI.Behaviours.Actions
{
    [Serializable]
    public class SelectWanderDestination : Action
    {
        [SerializeField] private SharedInt maxRetries = 10;
        [SerializeField] private SharedFloat wanderRate = 1;
        [SerializeField] private SharedFloat minWanderDistance = 20;
        [SerializeField] private SharedFloat maxWanderDistance = 20;
        [SerializeField] private SharedSelectedPathInfo pathChosen;
        
        [SerializeField] private float nextUnrestrictedTravelTime;

        // TODO Cooldown should be only after 
        private const float UNRESTRICTED_TRAVEL_COOLDOWN = 20;

        private NavigationSystem navSystem;

        public override void OnAwake()
        {
            navSystem = GetComponent<AIEntity>().NavigationSystem;
            // Do not immediately start with a long travel
            nextUnrestrictedTravelTime = Time.time + UNRESTRICTED_TRAVEL_COOLDOWN;
        }
        
        public override TaskStatus OnUpdate()
        {
            var result = false;
            if (nextUnrestrictedTravelTime < Time.time)
            {
                Debug.Log("Will now select long path!");
                result = SelectLongWanderDestination();
            }
            if (result)
            {
                Debug.Log("Long path valid destination found!");
                // We got a valid destination! Apply cooldown
                nextUnrestrictedTravelTime = Time.time + navSystem.EstimatePathDuration(pathChosen.Value) + 
                    UNRESTRICTED_TRAVEL_COOLDOWN;
            }
            else
            {
                // We didn't got a result, apply short travel for now...
                result = SelectShortWanderDestination();
            }

            return result ? TaskStatus.Success : TaskStatus.Running;
        }

        private bool SelectLongWanderDestination()
        {
            var currentPos = transform.position;
            for (var i = 0; i < maxRetries.Value; i++)
            {
                var displacement = Random.insideUnitCircle * (0.8f + Random.value * 0.2f) * 100;
                var chosenDestination = currentPos;
                chosenDestination.x += displacement.x;
                chosenDestination.z += displacement.y;

                Debug.DrawLine(transform.position, chosenDestination, Color.blue, 3f, false);

                var path = navSystem.CalculatePath(chosenDestination);
                if (path.IsComplete())
                {
                    pathChosen.Value = path;
                    return true;
                }
            }

            return false;
        }

        private bool SelectShortWanderDestination()
        {
            var direction = transform.forward; //Or use velocity
            for (var attempts = 0; attempts < maxRetries.Value; attempts++)
            {
                bool validDestination;
                direction += Random.insideUnitSphere * wanderRate.Value;
                var destination = transform.position + direction.normalized *
                    Random.Range(minWanderDistance.Value, maxWanderDistance.Value);
                validDestination = NavigationSystem.IsPointOnNavMesh(
                    destination,
                    NavMeshUtils.GetAgentIDFromName("Wanderer"),
                    out destination
                );
                if (validDestination)
                {
                    var path = navSystem.CalculatePath(destination);
                    if (path.IsComplete())
                    {
                        pathChosen.Value = path;
                        return true;
                    }
                }
            }

            return false;
        }
    }
}
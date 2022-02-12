using System;
using System.Linq;
using AI.AI.Layer1;
using AI.AI.Layer2;
using AI.Behaviours.Variables;
using BehaviorDesigner.Runtime;
using BehaviorDesigner.Runtime.Tasks;
using Others;
using UnityEngine;
using Action = BehaviorDesigner.Runtime.Tasks.Action;
using Random = UnityEngine.Random;

namespace AI.Behaviours.Actions
{
    [Serializable]
    public class SelectWanderDestination : Action
    {
        // TODO Cooldown should be only after 
        private const float UNRESTRICTED_TRAVEL_COOLDOWN = 20;

        private const float LOOK_AHEAD = 10f;
        [SerializeField] private SharedInt maxRetries = 10;
        [SerializeField] private SharedFloat wanderRate = 1;
        [SerializeField] private SharedFloat minWanderDistance = 20;
        [SerializeField] private SharedFloat maxWanderDistance = 20;
        [SerializeField] private SharedSelectedPathInfo pathChosen;

        [SerializeField] private float nextUnrestrictedTravelTime;

        private MovementController movementController;
        private NavigationSystem navSystem;

        public override void OnAwake()
        {
            var entity = GetComponent<AIEntity>();
            navSystem = entity.NavigationSystem;
            movementController = entity.MovementController;
            // Do not immediately start with a long travel
            nextUnrestrictedTravelTime = Time.time + UNRESTRICTED_TRAVEL_COOLDOWN;
        }

        public override TaskStatus OnUpdate()
        {
            var result = false;
            if (nextUnrestrictedTravelTime < Time.time) result = SelectLongWanderDestination();

            if (result)
                // We got a valid destination! Apply cooldown
                nextUnrestrictedTravelTime = Time.time + navSystem.EstimatePathDuration(pathChosen.Value) +
                                             UNRESTRICTED_TRAVEL_COOLDOWN;
            else
                // We didn't got a result, apply short travel for now...
                result = SelectShortWanderDestination();

            return result ? TaskStatus.Success : TaskStatus.Failure;
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
            var velocity = movementController.GetVelocity();
            var direction = velocity == Vector3.zero ? transform.forward : velocity.normalized;
            for (var attempts = 0; attempts < maxRetries.Value; attempts++)
            {
                var newDisplacement = Random.insideUnitCircle * wanderRate.Value;
                direction.x += newDisplacement.x;
                direction.z += newDisplacement.y;
                var destination = transform.position + direction.normalized *
                    Random.Range(minWanderDistance.Value, maxWanderDistance.Value);

                var path = navSystem.CalculatePath(destination);
                if (path.IsComplete())
                {
                    // Check that the path doesn't lead in front of a wall...
                    var corners = path.corners;
                    var onGroundDestination = corners.Last();
                    var validPointsFound = 0;

                    var penultimatePoint = corners.Length < 2 ? transform.position : corners[corners.Length - 2];

                    var forwardDirectionAtArrival = (onGroundDestination - penultimatePoint).normalized;

                    for (var i = -4; i <= 4; i++)
                    {
                        var angle = i * 90f / 4;

                        var currentLookDirection = Quaternion.AngleAxis(angle, Vector3.up) * forwardDirectionAtArrival;

                        if (!Physics.Raycast(onGroundDestination, currentLookDirection, LOOK_AHEAD)) validPointsFound++;
                    }

                    if (validPointsFound > 3)
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
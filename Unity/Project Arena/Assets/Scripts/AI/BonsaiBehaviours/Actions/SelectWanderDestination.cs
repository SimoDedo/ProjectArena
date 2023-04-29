using System;
using AI.Layers.Actuators;
using AI.Layers.KnowledgeBase;
using BehaviorDesigner.Runtime;
using Bonsai;
using Others;
using UnityEngine;
using UnityEngine.AI;
using Random = UnityEngine.Random;
using Task = Bonsai.Core.Task;

namespace AI.BonsaiBehaviours.Actions
{
    /// <summary>
    /// Selects a wander destination. Can be used when the better <see cref="SelectAreaToVisit"/> cannot be used.
    /// </summary>
    [BonsaiNode("Tasks/")]
    public class SelectWanderDestination : Task
    {
        private const float UNRESTRICTED_TRAVEL_COOLDOWN = 20;

        private const float LOOK_AHEAD = 10f;
        public int maxRetries = 4;
        public float wanderRate = 1;
        public float minWanderDistance = 20;
        public float maxWanderDistance = 20;
        public float nextUnrestrictedTravelTime;

        
        private MovementController movementController;
        private NavigationSystem navSystem;

        public string pathChosenKey;

        private NavMeshPath PathChosen
        {
            get => Blackboard.Get<NavMeshPath>(pathChosenKey);
            set => Blackboard.Set(pathChosenKey, value);
        }
        
        public override void OnStart()
        {
            var entity = Actor.GetComponent<AIEntity>();
            navSystem = entity.NavigationSystem;
            movementController = entity.MovementController;
            // Do not immediately start with a long travel
            nextUnrestrictedTravelTime = Time.time + UNRESTRICTED_TRAVEL_COOLDOWN;
        }

        public override Status Run()
        {
            var result = false;
            if (nextUnrestrictedTravelTime < Time.time) result = SelectLongWanderDestination();

            if (result)
                // We got a valid destination! Apply cooldown
                nextUnrestrictedTravelTime = Time.time + navSystem.EstimatePathDuration(PathChosen) +
                                             UNRESTRICTED_TRAVEL_COOLDOWN;
            else
                // We didn't got a result, apply short travel for now...
                result = SelectShortWanderDestination();

            return result ? Status.Success : Status.Running;
        }

        private bool SelectLongWanderDestination()
        {
            var currentPos = Actor.transform.position;
            for (var i = 0; i < maxRetries; i++)
            {
                var displacement = Random.insideUnitCircle * ((0.8f + Random.value * 0.2f) * 100);
                var chosenDestination = currentPos;
                chosenDestination.x += displacement.x;
                chosenDestination.z += displacement.y;

                var path = navSystem.CalculatePath(chosenDestination);
                if (!path.IsComplete()) continue;
                PathChosen = path;
                return true;
            }

            return false;
        }

        private bool SelectShortWanderDestination()
        {
            var velocity = movementController.GetVelocity();
            var direction = velocity == Vector3.zero ? Actor.transform.forward : velocity.normalized;
            for (var attempts = 0; attempts < maxRetries; attempts++)
            {
                var newDisplacement = Random.insideUnitCircle * wanderRate;
                direction.x += newDisplacement.x;
                direction.z += newDisplacement.y;
                var destination = Actor.transform.position + direction.normalized *
                    Random.Range(minWanderDistance, maxWanderDistance);

                var path = navSystem.CalculatePath(destination);
                if (path.IsComplete())
                {
                    // // Check that the path doesn't lead in front of a wall...
                    // var corners = path.corners;
                    // var onGroundDestination = corners.Last();
                    // var validPointsFound = 0;
                    //
                    // var penultimatePoint = corners.Length < 2 ? transform.position : corners[^2];
                    //
                    // var forwardDirectionAtArrival = (onGroundDestination - penultimatePoint).normalized;
                    //
                    // for (var i = -4; i <= 4; i++)
                    // {
                    //     var angle = i * 90f / 4;
                    //
                    //     var currentLookDirection = Quaternion.AngleAxis(angle, Vector3.up) * forwardDirectionAtArrival;
                    //
                    //     if (!Physics.Raycast(onGroundDestination, currentLookDirection, LOOK_AHEAD)) validPointsFound++;
                    // }
                    //
                    // if (validPointsFound > 3)
                    // {
                        PathChosen = path;
                        return true;
                    // }
                }
            }

            return false;
        }
    }
}
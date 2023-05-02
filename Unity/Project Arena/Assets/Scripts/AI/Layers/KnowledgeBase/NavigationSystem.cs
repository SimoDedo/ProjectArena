using System;
using Others;
using UnityEngine;
using UnityEngine.AI;

namespace AI.Layers.KnowledgeBase
{
    /// <summary>
    /// This component deals with planning and execution of paths.
    /// </summary>
    public class NavigationSystem
    {
        private readonly float acceleration;
        private readonly float angularSpeed;
        private readonly AIEntity me;
        private readonly Transform transform;

        private NavMeshAgent agent;

        public NavigationSystem(AIEntity me, float speed)
        {
            this.me = me;
            transform = me.transform;
            baseSpeed = speed;
            acceleration = 10000;
            angularSpeed = 1000000;
            speedMultiplier = 1f;
        }

        private readonly float baseSpeed;
        private float speedMultiplier;
        public float Speed => baseSpeed * speedMultiplier;

        public void Prepare()
        {
            agent = me.gameObject.AddComponent<NavMeshAgent>();
            var agentSettings = NavMesh.GetSettingsByIndex(0);
            agent.radius = agentSettings.agentRadius;
            agent.agentTypeID = agentSettings.agentTypeID;
            agent.height = agentSettings.agentHeight;
            agent.baseOffset = 1f;
            agent.autoBraking = false;
            agent.updatePosition = false;
            agent.updateRotation = false;
            agent.speed = Speed;
            agent.acceleration = acceleration;
            agent.angularSpeed = angularSpeed;
        }

        /// <summary>
        /// Returns a path from this entity current position to the destination specified, if possible.
        /// </summary>
        /// <param name="destination">The destination to reach.</param>
        /// <param name="throwIfNotComplete">If true, this method will throw an InvalidOperationException if the
        /// path calculated is not complete.</param>
        public NavMeshPath CalculatePath(Vector3 destination, bool throwIfNotComplete = false)
        {
            // agent.speed = Speed;
            var rtn = new NavMeshPath();
            agent.CalculatePath(destination, rtn);
            if (throwIfNotComplete && !rtn.IsComplete())
                throw new InvalidOperationException("Attempted to calculate incomplete path");
            return rtn;
        }

        /// <summary>
        /// Returns a path from origin to the destination specified, if possible.
        /// </summary>
        /// <param name="origin">The starting position.</param>
        /// <param name="destination">The destination to reach.</param>
        /// <param name="throwIfNotComplete">If true, this method will throw an InvalidOperationException if the
        /// path calculated is not complete.</param>
        public NavMeshPath CalculatePath(Vector3 origin, Vector3 destination, bool throwIfNotComplete = false)
        {
            // agent.speed = Speed;
            var rtn = new NavMeshPath();
            NavMesh.CalculatePath(origin, destination, agent.areaMask, rtn);
            if (throwIfNotComplete && !rtn.IsComplete())
                throw new InvalidOperationException("Attempted to calculate incomplete path");
            return rtn;
        }

        public Vector3 GetNextPosition()
        {
            return agent.nextPosition;
        }

        /// <summary>
        /// Cancels any path the entity is currently following.
        /// </summary>
        public void CancelPath()
        {
            if (agent.isOnNavMesh) agent.ResetPath();
        }

        /// <summary>
        /// Sets a path for this component to follow.
        /// </summary>
        public void SetPath(NavMeshPath path)
        {
            // agent.speed = Speed;
            // Check that I didn't have a path already
            if (!agent.SetPath(path))
            {
                // Sometimes the agent gets stuck in a position from where it is unable to set a path.
                // When that happens, warping helps.
                agent.Warp(agent.transform.position);
                if (!agent.SetPath(path))
                {
                    Debug.LogError("Cannot set path to entity: unknown problem!");
                }
            }
        }

        /// <summary>
        /// Sets whether this component is enabled or not.
        /// </summary>
        public void SetEnabled(bool b)
        {
            agent.enabled = b;
        }

        /// <summary>
        /// Returns whether the entity is close enough to the destination.
        /// </summary>
        public bool HasArrivedToDestination()
        {
            return agent.remainingDistance < 0.5f;
        }

        /// <summary>
        /// Returns an estimate on how long the path takes to complete in seconds.
        /// </summary>
        public float EstimatePathDuration(NavMeshPath path)
        {
            // Be a little pessimistic on arrival time.
            return path.Length() / Speed * 1.1f;
        }

        /// <summary>
        /// Returns whether this component was given a path to follow.
        /// </summary>
        public bool HasPath()
        {
            return agent.hasPath;
        }

        public void SetMovementMultiplier(float multiplier)
        {
            speedMultiplier = multiplier;
            agent.speed = Speed;
        }
    }
}
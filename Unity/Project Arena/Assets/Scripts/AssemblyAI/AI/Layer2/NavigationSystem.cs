using System;
using AssemblyAI.AI.Layer1.Actuator;
using Others;
using UnityEngine;
using UnityEngine.AI;

namespace AssemblyAI.AI.Layer2
{
    public class NavigationSystem
    {
        private readonly AIEntity me;
        private readonly Transform transform;
        private float Acceleration { get; }
        private float AngularSpeed { get; }
        public float Speed { get; }
        
        private NavMeshAgent agent;
        private AIMovementController mover;

        private static readonly float AgentHeight = NavMesh.GetSettingsByIndex(0).agentHeight;

        public NavigationSystem(AIEntity me, float speed)
        {
            Speed = speed;
            this.me = me;
            transform = me.transform;
            Acceleration = 100;
            AngularSpeed = 1000000;
        }

        public void Prepare()
        {
            mover = me.MovementController;
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
            agent.acceleration = Acceleration;
            agent.angularSpeed = AngularSpeed;
        }

        public NavMeshPath CalculatePath(Vector3 destination, bool throwIfNotComplete = false)
        {
            var rtn = new NavMeshPath();
            agent.CalculatePath(destination, rtn);
            if (throwIfNotComplete && !rtn.IsComplete())
            {
                throw new InvalidOperationException("Attempted to calculate incomplete path");
            }
            return rtn;
        }

        public static bool IsPointOnNavMesh(Vector3 point, out Vector3 validPoint)
        {
            var filter = new NavMeshQueryFilter {areaMask = 1};
            // TODO Should use agent height, but that's not statically accessible
            if (NavMesh.SamplePosition(point, out var hit, AgentHeight * 2, filter))
            {
                point.y = hit.position.y;
                if (point == hit.position)
                {
                    validPoint = hit.position;
                    return true;
                }
            }
            validPoint = point;
            return false;
        } 

        public void MoveAlongPath()
        {
            var position = transform.position;
            Debug.DrawLine(position, agent.destination, Color.green, 0, false);
            Debug.DrawLine(position, agent.nextPosition, Color.red, 0.4f);
            mover.MoveToPosition(agent.nextPosition);
        }

        public void CancelPath()
        {
            if (agent.isOnNavMesh)
            {
                agent.ResetPath();
            }
        }

        /// <summary>
        /// Use this to set a path to the navigation system.
        /// If this method is called multiple times during a frame, only the last call counts to
        /// determine the destination of the agent path.
        /// </summary>
        public void SetPath(NavMeshPath path)
        {
            agent.SetPath(path);
        }

        /// <summary>
        /// Use this to set a destination to the navigation system.
        /// If this method is called multiple times during a frame, only the last call counts to
        /// determine the destination of the agent path.
        /// If the destination is not reachable, this method throws an exception!
        /// </summary>
        public void SetDestination(Vector3 destination)
        {
            var path = CalculatePath(destination);
            if (!path.IsComplete())
            {
                throw new ArgumentException("Destination is not reachable!");
            }

            agent.SetPath(path);
        }

        public void SetEnabled(bool b)
        {
            agent.enabled = b;
        }

        public bool HasArrivedToDestination()
        {
            return agent.remainingDistance < 0.5f;
        }

        public float EstimatePathDuration(NavMeshPath path)
        {
            // Be a little pessimistic on arrival time.
            return path.Length() / Speed * 1.1f;
        }

        public bool HasPath()
        {
            return agent.hasPath;
        }
    }
}
using System;
using System.Linq;
using AssemblyAI.AI.Layer1.Actuator;
using BehaviorDesigner.Runtime.Tasks;
using JetBrains.Annotations;
using Others;
using UnityEngine;
using UnityEngine.AI;

namespace AssemblyAI.AI.Layer2
{
    public class NavigationSystem
    {
        private static readonly Vector3 NoDestination = new Vector3(1000, 1000, 1000);
        private readonly AIEntity me;
        private readonly Transform transform;
        private float Acceleration { get; }
        private float AngularSpeed { get; }
        public float Speed { get; }

        /// <summary>
        /// The destination that the agent was trying to reach at the beginning of the frame. 
        /// </summary>
        private Vector3 currentDestination = NoDestination;

        /// <summary>
        /// The latest destination set for the agent to follow. 
        /// </summary>
        private Vector3 latestDestination = NoDestination;

        private NavMeshPath latestDestinationPath;

        private NavMeshAgent agent;
        private AIMovementController mover;

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
            agent.radius = 0.2f;
            agent.baseOffset = 1f;
            agent.autoBraking = false;
            agent.updatePosition = false;
            agent.updateRotation = false;
            agent.speed = Speed;
            agent.acceleration = Acceleration;
            agent.angularSpeed = AngularSpeed;
        }

        public NavMeshPath CalculatePath(Vector3 destination)
        {
            var rtn = new NavMeshPath();
            agent.CalculatePath(destination, rtn);
            return rtn;
        }

        // public NavMeshPath CalculatePath(Vector3 destination, int agentId)
        // {
        //     var path = new NavMeshPath();
        //     var filter = new NavMeshQueryFilter {agentTypeID = agentId, areaMask = NavMesh.AllAreas};
        //     if (NavMesh.SamplePosition(destination, out var hit, float.MaxValue, filter))
        //     {
        //         agent.CalculatePath(hit.position, path);
        //         return path;
        //     }
        //
        //     return path;
        // }

        public static bool IsPointOnNavMesh(Vector3 point, int agentId, out Vector3 validPoint)
        {
            var filter = new NavMeshQueryFilter {agentTypeID = agentId, areaMask = NavMesh.AllAreas};

            if (NavMesh.SamplePosition(point, out var hit, float.MaxValue, filter))
            {
                validPoint = hit.position;
                return true;
            }

            validPoint = point;
            return false;
        }

        public bool IsPointOnNavMesh(Vector3 point, out Vector3 validPoint)
        {
            return IsPointOnNavMesh(point, agent.agentTypeID, out validPoint);
        }

        public void MoveAlongPath()
        {
            if (latestDestination != currentDestination)
            {
                currentDestination = latestDestination;
                var path = latestDestinationPath ?? CalculatePath(currentDestination);
                agent.SetPath(path);
            }

            Debug.DrawLine(transform.position, agent.destination, Color.green, 0, false);
            Debug.DrawLine(transform.position, agent.nextPosition, Color.red, 0.4f);
            mover.MoveToPosition(agent.nextPosition);
        }

        public void CancelPath()
        {
            agent.ResetPath();
            currentDestination = NoDestination;
        }

        /// <summary>
        /// Use this to set a path to the navigation system.
        /// If this method is called multiple times during a frame, only the last call counts to
        /// determine the destination of the agent path.
        /// </summary>
        public void SetPathToDestination(NavMeshPath path)
        {
            latestDestinationPath = path;
            latestDestination = path.corners.Last();
        }

        /// <summary>
        /// Use this to set a destination to the navigation system.
        /// If this method is called multiple times during a frame, only the last call counts to
        /// determine the destination of the agent path.
        /// If the destination is not reachable, this method throws an exception!
        /// </summary>
        public void SetDestination(Vector3 destination)
        {
            if (destination == latestDestination) return;
            
            var path = CalculatePath(destination);
            if (!path.IsComplete())
            {
                throw new ArgumentException("Destination is not reachable!");
            }
            latestDestination = destination;
            latestDestinationPath = path;
        }

        public void SetEnabled(bool b)
        {
            agent.enabled = b;
        }

        public bool HasArrivedToDestination(Vector3 pathDestination)
        {
            IsPointOnNavMesh(transform.position, out var floor1);
            IsPointOnNavMesh(pathDestination, out var floor2);
            return (floor1 - floor2).magnitude < 0.5f;
        }
    }
}
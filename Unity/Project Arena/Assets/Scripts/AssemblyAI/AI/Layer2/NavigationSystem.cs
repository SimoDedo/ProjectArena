using System;
using UnityEngine;
using UnityEngine.AI;
using Utils;

namespace Entities.AI.Layer2
{
    public class NavigationSystem : MonoBehaviour
    {
        private NavMeshAgent agent;
        private AIMovementController mover;
        private float speed;
        private float acceleration;
        private float angularSpeed;

        public void SetParameters(AIMovementController mover, float speed, float acceleration, float angularSpeed)
        {
            this.mover = mover;
            this.speed = speed;
            this.acceleration = acceleration;
            this.angularSpeed = angularSpeed;
        }

        private void Start()
        {
            agent = gameObject.AddComponent<NavMeshAgent>();
            agent.radius = 0.2f;
            agent.baseOffset = 1f;
            agent.autoBraking = false;
            agent.updatePosition = false;
            agent.updateRotation = false;
            agent.speed = speed;
            agent.acceleration = acceleration;
            agent.angularSpeed = angularSpeed;
        }

        public NavMeshPath CalculatePath(Vector3 destination)
        {
            var path = new NavMeshPath();
            agent.CalculatePath(destination, path);
            return path;
        }

        public NavMeshPath CalculatePath(Vector3 destination, int agentId)
        {
            var path = new NavMeshPath();
            var filter = new NavMeshQueryFilter
            {
                agentTypeID = agentId,
                areaMask = NavMesh.AllAreas
            };
            if (NavMesh.SamplePosition(destination, out var hit, float.MaxValue, filter))
            {
                agent.CalculatePath(hit.position, path);
                return path;
            }

            return path;
        }

        public bool IsPointOnNavMesh(Vector3 point, int agentId, out Vector3 validPoint)
        {
            var filter = new NavMeshQueryFilter
            {
                agentTypeID = agentId,
                areaMask = NavMesh.AllAreas
            };
            if (NavMesh.SamplePosition(point, out var hit, float.MaxValue, filter))
            {
                validPoint = hit.position;
                return true;
            }
            validPoint = Vector3.zero;
            return false;
        }

        public void SetPath(NavMeshPath path)
        {
            agent.SetPath(path);
        }

        public bool HasPath()
        {
            return agent.hasPath;
        }

        private void Update()
        {
            mover.MoveToPosition(agent.nextPosition);
        }

        public bool HasArrivedToDestination()
        {
            return agent.remainingDistance < 0.5f;
        }

        public float GetEstimatedPathDuration(NavMeshPath path)
        {
            var lenght = NavigationUtils.GetPathLenght(path);
            return lenght / agent.speed;
        }

        public void CancelPath()
        {
            agent.ResetPath();
        }

        public void SetDestination(Vector3 movementAmountValue)
        {
            var path = new NavMeshPath();
            agent.CalculatePath(movementAmountValue, path);
            agent.SetPath(path);
        }
    }
}
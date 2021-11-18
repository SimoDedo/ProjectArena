using UnityEngine;
using UnityEngine.AI;

namespace Entities.AI.Layer2
{
    public class NavigationSystem : MonoBehaviour
    {
        private NavMeshAgent agent;
        private AIMovementController mover;
        private float speed;
        private float acceleration;
        private float angularSpeed;

        public void Prepare(AIMovementController mover, float speed)
        {
            this.mover = mover;
            this.speed = speed;
            acceleration = 1000000;
            angularSpeed = 1000000;
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

        public float GetSpeed()
        {
            return speed;
        }
        public NavMeshPath CalculatePath(Vector3 destination)
        {
            var path = new NavMeshPath();
            agent.CalculatePath(destination, path);
            return path;
        }

        public NavMeshPath CalculatePath(Vector3 startPoint, Vector3 destination)
        {
            var path = new NavMeshPath();
            NavMesh.CalculatePath(startPoint, destination, agent.areaMask, path);
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
            validPoint = point;
            return false;
        }
        
        public bool IsPointOnNavMesh(Vector3 point, out Vector3 validPoint)
        {
            return IsPointOnNavMesh(point, agent.agentTypeID, out validPoint);
        }

        public void SetPath(NavMeshPath path)
        {
            agent.SetPath(path);
        }

        public bool HasPath()
        {
            return agent.hasPath;
        }

        public void MoveAlongPath()
        {
            mover.MoveToPosition(agent.nextPosition);
        }

        public bool HasArrivedToDestination()
        {
            return agent.remainingDistance < 0.5f;
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

        public void SetEnabled(bool b)
        {
            agent.enabled = b;
        }
    }
}
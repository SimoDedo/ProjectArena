using AI.KnowledgeBase;
using BehaviorDesigner.Runtime.Tasks;
using Others;
using UnityEngine;
using UnityEngine.AI;

namespace AI
{
    public class LookForHealth : Action
    {
        private GameObject target;
        private NavMeshAgent agent;
        private PickupKnowledgeBase kb;

        public override void OnStart()
        {
            kb = GetComponent<PickupKnowledgeBase>();
            agent = GetComponent<NavMeshAgent>();
        }

        public override void OnReset()
        {
            target = null;
            agent.ResetPath();
        }

        public override TaskStatus OnUpdate()
        {
            if (target == null || !agent.hasPath)
            {
                var possibleTargets = kb.GetProbablyActive(Pickable.PickupType.MEDKIT);
                if (possibleTargets.Count == 0)
                {
                    Debug.LogError("For some reason no health pickups are available");
                    // Decide a better way to handle this? Maybe go towards the one that is supposed to respawn sooner?
                    return TaskStatus.Failure;
                }

                var bestPathLength = float.PositiveInfinity;
                var bestPath = new NavMeshPath();
                foreach (var currentTarget in possibleTargets)
                {
                    var path = new NavMeshPath();
                    if (agent.CalculatePath(currentTarget.transform.position, path))
                    {
                        if (path.status == NavMeshPathStatus.PathComplete)
                        {
                            var currentPathLength = NavMeshUtils.GetPathLength(path);
                            if (currentPathLength < bestPathLength)
                            {
                                bestPathLength = currentPathLength;
                                bestPath = path;
                                target = currentTarget;
                            }
                        }
                        else
                        {
                            Debug.LogWarning("For some reason path returned was incomplete");
                        }
                    }
                }

                if (target == null)
                {
                    Debug.LogError("For some reason no medkit is reachable");
                    return TaskStatus.Failure;
                }

                agent.SetPath(bestPath);
                Debug.Log("Set path called");
            }
            else
            {
                NavMeshUtils.DrawPath(agent.path, Color.green);
                if (!kb.IsProbablyActive(target))
                {
                    agent.ResetPath();
                    target = null;
                }
            }
            return TaskStatus.Running;
        }
    }
}
using BehaviorDesigner.Runtime;
using BehaviorDesigner.Runtime.Tasks;
using Others;
using UnityEngine;
using UnityEngine.AI;

namespace AI
{
    // TODO should stop navigation if destination is not currently available (e.g. ammo pickup not respawned yet)
    public class LookForTaggedItem : Action
    {
        public SharedString tagToSearch;
        private GameObject[] taggedObjects;
        private NavMeshAgent agent;

        public override void OnAwake()
        {
            taggedObjects = GameObject.FindGameObjectsWithTag(tagToSearch.Value);
            agent = GetComponent<NavMeshAgent>();
        }

        public override TaskStatus OnUpdate()
        {
            if (!agent.hasPath)
                TryFindPath();
            return TaskStatus.Running;
        }

        private void TryFindPath()
        {
            var minLength = float.MaxValue;
            NavMeshPath bestPath = null;
            foreach (var obj in taggedObjects)
            {
                var path = new NavMeshPath();
                if (agent.CalculatePath(obj.transform.position, path))
                {
                    if (path.status == NavMeshPathStatus.PathComplete)
                    {
                        var currentPathLenght = NavMeshUtils.GetPathLength(path);
                        if (currentPathLenght < minLength)
                        {
                            bestPath = path;
                            minLength = currentPathLenght;
                        }
                    }
                }
            }

            agent.SetPath(bestPath);
        }
    }
}
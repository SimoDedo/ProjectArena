using BehaviorDesigner.Runtime;
using BehaviorDesigner.Runtime.Tasks;
using UnityEngine.AI;

namespace AI
{
    // TODO for now when searching we assume the user knows the position of the target
    public class MoveToTarget: Action
    {
        public SharedGameObject enemy;
        private NavMeshAgent agent;

        public override void OnStart()
        {
            agent = GetComponent<NavMeshAgent>();
        }

        public override void OnEnd()
        {
            agent.ResetPath();
        }

        public override TaskStatus OnUpdate()
        {
            agent.SetDestination(enemy.Value.transform.position);
            return TaskStatus.Running;
        }
    }
}
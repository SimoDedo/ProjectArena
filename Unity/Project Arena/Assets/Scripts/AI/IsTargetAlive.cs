using BehaviorDesigner.Runtime;
using BehaviorDesigner.Runtime.Tasks;

namespace AI
{
    public class IsTargetAlive : Conditional
    {
        public SharedGameObject enemy;


        public override TaskStatus OnUpdate()
        {
            if (enemy.Value.GetComponent<Entity>().isAlive)
                return TaskStatus.Success;
            return TaskStatus.Failure;
        }
    }
}
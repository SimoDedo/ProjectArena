using BehaviorDesigner.Runtime;
using BehaviorDesigner.Runtime.Tasks;

namespace AI
{
    public class AttackOpponent : Action
    {
        public SharedGameObject enemy;
        private AIEntity ai;

        public override void OnStart()
        {
            ai = GetComponent<AIEntity>();
        }

        public override TaskStatus OnUpdate()
        {
            var entity = enemy.Value.GetComponent<Entity>();
            // ai.TryAttack(entity);
            return TaskStatus.Running;
        }
    }
}
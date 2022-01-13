using AssemblyAI.AI.Layer1.Sensors;
using BehaviorDesigner.Runtime.Tasks;

namespace AssemblyAI.Behaviours.Conditions
{
    public class ShouldWatchOutForEnemy : Conditional
    {
        private FightingMovementSkill skill;
        private DamageSensor damageSensor;

        public override void OnAwake()
        {
            var entity = GetComponent<AIEntity>();
            skill = entity.MovementSkill;
            damageSensor = entity.DamageSensor;
        }

        public override TaskStatus OnUpdate()
        {
            if (skill == FightingMovementSkill.StandStill)
            {
                // Never fight back if I don't have the required movement skill!
                return TaskStatus.Failure;
            }

            return damageSensor.WasDamagedRecently ? TaskStatus.Success : TaskStatus.Failure;
        }
    }
}
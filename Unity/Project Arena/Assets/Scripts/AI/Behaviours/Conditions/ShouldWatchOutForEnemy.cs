using System;
using AI.AI.Layer1;
using BehaviorDesigner.Runtime.Tasks;

namespace AI.Behaviours.Conditions
{
    /// <summary>
    /// Returns Success if we have taken damage recently and should react about it, Failure otherwise.
    /// </summary>
    [Serializable]
    public class ShouldWatchOutForEnemy : Conditional
    {
        private DamageSensor damageSensor;
        private FightingMovementSkill skill;

        public override void OnAwake()
        {
            var entity = GetComponent<AIEntity>();
            skill = entity.MovementSkill;
            damageSensor = entity.DamageSensor;
        }

        public override TaskStatus OnUpdate()
        {
            if (skill == FightingMovementSkill.StandStill)
                // Never fight back if I don't have the required movement skill!
                return TaskStatus.Failure;

            return damageSensor.WasDamagedRecently ? TaskStatus.Success : TaskStatus.Failure;
        }
    }
}
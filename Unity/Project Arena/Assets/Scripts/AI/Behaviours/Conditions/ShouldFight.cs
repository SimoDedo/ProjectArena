using System;
using AI.AI.Layer2;
using AI.AI.Layer3;
using BehaviorDesigner.Runtime.Tasks;

namespace AI.Behaviours.Conditions
{
    /// <summary>
    /// Returns Success if we should fight the enemy, Failure otherwise.
    /// </summary>
    [Serializable]
    public class ShouldFight : Conditional
    {
        private AIEntity entity;
        private FightingMovementSkill skill;
        private TargetPlanner targetPlanner;

        public override void OnAwake()
        {
            entity = GetComponent<AIEntity>();
            targetPlanner = entity.TargetPlanner;
            skill = entity.MovementSkill;
        }

        public override TaskStatus OnUpdate()
        {
            if (skill == FightingMovementSkill.StandStill)
                // Never fight back if I don't have the required movement skill!
                return TaskStatus.Failure;

            if (targetPlanner.HasSeenTarget())
            {
                entity.IsFocusingOnEnemy = true;
                return TaskStatus.Success;
            }

            return TaskStatus.Failure;
        }
    }
}
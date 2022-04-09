using System;
using AI.Layers.KnowledgeBase;
using BehaviorDesigner.Runtime.Tasks;
using Logging;

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
        private TargetKnowledgeBase _targetKnowledgeBase;

        public override void OnAwake()
        {
            entity = GetComponent<AIEntity>();
            _targetKnowledgeBase = entity.TargetKnowledgeBase;
            skill = entity.MovementSkill;
        }

        public override TaskStatus OnUpdate()
        {
            if (skill == FightingMovementSkill.StandStill)
                // Never fight back if I don't have the required movement skill!
                return TaskStatus.Failure;

            if (_targetKnowledgeBase.HasSeenTarget())
            {
                FocusingOnEnemyGameEvent.Instance.Raise(new FocusOnEnemyInfo {entityID = entity.GetID(), isFocusing = true});
                return TaskStatus.Success;
            }
            FocusingOnEnemyGameEvent.Instance.Raise(new FocusOnEnemyInfo {entityID = entity.GetID(), isFocusing = false});
            return TaskStatus.Failure;
        }
    }
}
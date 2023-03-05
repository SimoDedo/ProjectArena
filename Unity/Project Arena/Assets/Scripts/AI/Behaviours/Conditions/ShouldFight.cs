using System;
using AI.Layers.KnowledgeBase;
using BehaviorDesigner.Runtime.Tasks;
using Logging;
using UnityEngine;
using Random = UnityEngine.Random;

namespace AI.Behaviours.Conditions
{
    /// <summary>
    /// Returns Success if we should fight the enemy, Failure otherwise.
    /// </summary>
    [Serializable]
    public class ShouldFight : Conditional
    {
        private AIEntity entity;
        private TargetKnowledgeBase _targetKnowledgeBase;
        private const float TIMEOUT_CHANGE_IDEA = 1.5f; 
        private float timestampChangeIdeaAllowed; 
        private float probabilityFightBack;
        private bool hasDecidedToFight;

        public override void OnAwake()
        {
            entity = GetComponent<AIEntity>();
            _targetKnowledgeBase = entity.TargetKnowledgeBase;
            probabilityFightBack = entity.Characteristics.FightBackWhenCollectingPickup;
        }

        public override void OnStart()
        {
            hasDecidedToFight = false;
        }

        public override TaskStatus OnUpdate()
        {
            if (Time.time < timestampChangeIdeaAllowed)
            {
                // I won't change my decision for now.
                return hasDecidedToFight ? TaskStatus.Success : TaskStatus.Failure;
            }

            hasDecidedToFight = false;
            if (_targetKnowledgeBase.HasDetectedTarget())
            {
                timestampChangeIdeaAllowed = Time.time + TIMEOUT_CHANGE_IDEA;
                if (Random.value > probabilityFightBack)
                {
                    // Not feeling enough confident to fight back.
                    FocusingOnEnemyGameEvent.Instance.Raise(new FocusOnEnemyInfo {entityID = entity.GetID(), isFocusing = false});
                    return TaskStatus.Failure;
                }
                
                FocusingOnEnemyGameEvent.Instance.Raise(new FocusOnEnemyInfo {entityID = entity.GetID(), isFocusing = true});
                hasDecidedToFight = true;
                return TaskStatus.Success;
            }
            FocusingOnEnemyGameEvent.Instance.Raise(new FocusOnEnemyInfo {entityID = entity.GetID(), isFocusing = false});
            return TaskStatus.Failure;
        }
    }
}
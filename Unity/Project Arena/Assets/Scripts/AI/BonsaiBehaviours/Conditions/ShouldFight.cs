using System;
using AI.Layers.KnowledgeBase;
using Bonsai;
using Bonsai.CustomNodes;
using Logging;
using UnityEngine;
using Random = UnityEngine.Random;

namespace AI.BonsaiBehaviours.Conditions
{
    /// <summary>
    /// Returns Success if we should fight the enemy, Failure otherwise.
    /// </summary>
    [BonsaiNode("Conditional/")]
    public class ShouldFight : AutoConditionalAbort
    {
        private AIEntity entity;
        private TargetKnowledgeBase _targetKnowledgeBase;
        private const float TIMEOUT_CHANGE_IDEA = 1.5f; 
        private float timestampChangeIdeaAllowed; 
        private float probabilityFightBack;

        public override void OnStart()
        {
            entity = Actor.GetComponent<AIEntity>();
            _targetKnowledgeBase = entity.TargetKnowledgeBase;
            probabilityFightBack = entity.Characteristics.FightBackWhenCollectingPickup;
        }

        public override void OnEnter()
        {
        }

        public override bool Condition()
        {
            if (Time.time < timestampChangeIdeaAllowed)
            {
                // I won't change my decision for now.
                return false;
            }

            if (_targetKnowledgeBase.HasDetectedTarget())
            {
                timestampChangeIdeaAllowed = Time.time + TIMEOUT_CHANGE_IDEA;
                if (Random.value > probabilityFightBack)
                {
                    // Not feeling enough confident to fight back.
                    FocusingOnEnemyGameEvent.Instance.Raise(new FocusOnEnemyInfo {entityID = entity.GetID(), isFocusing = false});
                    return false;
                }
                
                FocusingOnEnemyGameEvent.Instance.Raise(new FocusOnEnemyInfo {entityID = entity.GetID(), isFocusing = true});
                return true;
            }
            FocusingOnEnemyGameEvent.Instance.Raise(new FocusOnEnemyInfo {entityID = entity.GetID(), isFocusing = false});
            return false;
        }

        public override Status Run()
        {
            return Condition() ? Status.Success : Status.Failure;
        }
    }
}
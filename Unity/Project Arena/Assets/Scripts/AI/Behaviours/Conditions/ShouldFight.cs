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
        private const float TIMEOUT_CHANGE_IDEA = 0.5f; 
        private float timestampChangeIdeaAllowed; 
        private float probabilityFightBack; 

        public override void OnAwake()
        {
            entity = GetComponent<AIEntity>();
            _targetKnowledgeBase = entity.TargetKnowledgeBase;
            probabilityFightBack = entity.FightBackWhenCollectingPickup;
        }

        public override TaskStatus OnUpdate()
        {
            if (_targetKnowledgeBase.HasSeenTarget())
            {
                if (Time.time < timestampChangeIdeaAllowed)
                {
                    // I won't change my decision for now, do not fight
                    FocusingOnEnemyGameEvent.Instance.Raise(new FocusOnEnemyInfo {entityID = entity.GetID(), isFocusing = false});
                    return TaskStatus.Failure;
                }

                if (Random.value > probabilityFightBack)
                {
                    // Not feeling enough confident to fight back.
                    // Debug.Log(entity.name + ": I see the enemy, but I'm not feeling it...");
                    timestampChangeIdeaAllowed = Time.time + TIMEOUT_CHANGE_IDEA;
                    FocusingOnEnemyGameEvent.Instance.Raise(new FocusOnEnemyInfo {entityID = entity.GetID(), isFocusing = false});
                    return TaskStatus.Failure;
                }
                
                // Debug.Log(entity.name + ": I see the enemy, engaging");
                FocusingOnEnemyGameEvent.Instance.Raise(new FocusOnEnemyInfo {entityID = entity.GetID(), isFocusing = true});
                return TaskStatus.Success;
            }
            FocusingOnEnemyGameEvent.Instance.Raise(new FocusOnEnemyInfo {entityID = entity.GetID(), isFocusing = false});
            return TaskStatus.Failure;
        }
    }
}